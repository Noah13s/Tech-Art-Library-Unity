using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class BulletObject : MonoBehaviour
{
    public BulletData bulletData;

    private Rigidbody rb;
    private float lifetimeTimer;
    private bool insideObject = false;
    private float penetrationDragMultiplier = 10f; // drag boost inside objects
    private float currentPenetrationDepth = 0f;   // track how far we've penetrated
    private Transform currentPenetratedObject;    // track which object we're inside
    private Vector3 entryPoint;                   // track where we entered the object

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.drag = bulletData.ballisticCoefficient;
        rb.velocity = transform.forward * bulletData.speed;
        lifetimeTimer = 0f;
    }

    private void Update()
    {
        // Lifetime & stop check
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= bulletData.lifetime || rb.velocity.sqrMagnitude <= 0.01f)
        {
            Destroy(gameObject);
            return;
        }

        // Extra slow-down when inside something
        if (insideObject && currentPenetratedObject != null)
        {
            // Calculate penetration depth by distance traveled since entry
            currentPenetrationDepth = Vector3.Distance(entryPoint, transform.position);

            // Get material properties based on the collider's material or tag
            float materialDensity = GetMaterialDensity(currentPenetratedObject);

            // Calculate maximum penetration depth based on bullet and material properties
            float maxPenetrationDepth = CalculateMaxPenetrationDepth(materialDensity);

            // Slow down based on penetration depth percentage
            float penetrationRatio = currentPenetrationDepth / maxPenetrationDepth;
            float slowdownFactor = Mathf.Lerp(1f, 10f, penetrationRatio * penetrationRatio);

            // Apply increased drag the deeper we go
            var v = rb.velocity;
            v *= Mathf.Clamp01(1f - (bulletData.ballisticCoefficient * penetrationDragMultiplier * slowdownFactor * Time.deltaTime));
            rb.velocity = v;

            // Add progressively increasing random trajectory deviation during penetration
            float deviationStrength = Mathf.Lerp(0.05f, 25f, penetrationRatio);
            Vector3 randomDeviation = Random.insideUnitSphere * deviationStrength;
            rb.velocity += Vector3.Lerp(Vector3.zero, randomDeviation, Time.deltaTime * penetrationRatio);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Don't re-enter tracking if already inside an object
        if (!insideObject)
        {
            insideObject = true;
            currentPenetratedObject = other.transform;
            entryPoint = transform.position;
            currentPenetrationDepth = 0f;

            // Log entry for debugging
            Debug.Log($"Bullet entered: {other.name}");

            if (bulletData.fragmentation)
            {
                // Handle fragmentation logic here
                Debug.Log("Bullet fragmented!");

                // Number of fragments to create
                int fragmentCount = 5;

                for (int i = 0; i < fragmentCount; i++)
                {
                    // Instantiate a new fragment
                    GameObject fragment = Instantiate(bulletData.bulletPrefab, transform.position, Quaternion.identity);

                    // Apply random direction and reduced speed to the fragment
                    Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
                    if (fragmentRb != null)
                    {
                        Vector3 randomDirection = Random.insideUnitSphere.normalized;
                        fragmentRb.velocity = randomDirection * (bulletData.speed * 0.5f); // Half the original speed
                    }
                }

                // Destroy the original bullet after fragmentation
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Only reset if we're exiting the current object we're tracking
        if (currentPenetratedObject == other.transform)
        {
            insideObject = false;
            currentPenetratedObject = null;
            currentPenetrationDepth = 0f;

            // Log exit for debugging
            Debug.Log($"Bullet exited: {other.name}");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Handle immediate stops for very dense materials
        if (ShouldStopImmediately(collision.collider))
        {
            StopBullet();
            return;
        }

        Debug.Log($"Bullet hit: {collision.collider.name}");
    }

    private float GetMaterialDensity(Transform obj)
    {
        // Get material density based on tag or material name
        // You can expand this to use a material database or scriptable objects

        if (obj.CompareTag("Concrete") || obj.name.ToLower().Contains("concrete"))
            return 15f; // High density for concrete
        else if (obj.CompareTag("Metal") || obj.name.ToLower().Contains("metal"))
            return 20f; // Very high for metal
        else if (obj.CompareTag("Wood") || obj.name.ToLower().Contains("wood"))
            return 5f;  // Medium for wood
        else
            return 3f;  // Default for generic materials
    }

    private float CalculateMaxPenetrationDepth(float materialDensity)
    {
        // Calculate max penetration based on bullet data and material density
        // Formula can be tuned based on desired simulation accuracy

        float bulletEnergy = bulletData.speed * (bulletData.mass/1000);
        float penetration = bulletEnergy / (materialDensity * 10f);

        // Respect minimum thickness to allow small objects to actually stop bullets
        return Mathf.Max(penetration, 0.05f);
    }

    private bool ShouldStopImmediately(Collider collider)
    {
        // Check for materials that should stop bullets immediately
        // Like extremely dense metals, etc.

        if (collider.CompareTag("Impenetrable") ||
            collider.name.ToLower().Contains("steel_thick") ||
            collider.name.ToLower().Contains("armored"))
            return true;

        return false;
    }

    private void StopBullet()
    {
        // Stop the bullet completely
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true; // Stop physics simulation
        }

        // Optional: Create impact effect here

        // Either destroy or keep the bullet lodged in place
        // Uncomment this if you want the bullet to disappear immediately
        // Destroy(gameObject);
    }
}