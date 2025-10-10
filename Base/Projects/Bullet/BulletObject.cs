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
    private bool isFragment=false;

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

    private void FixedUpdate()
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
        // Ne pas ré-entrer si déjà à l'intérieur d'un objet
        if (!insideObject)
        {
            insideObject = true;
            currentPenetratedObject = other.transform;
            entryPoint = transform.position;
            currentPenetrationDepth = 0f;

            Debug.Log($"Bullet entered: {other.name}");

            // Récupère la densité du matériau
            float materialDensity = GetMaterialDensity(currentPenetratedObject);

            // Récupère l'épaisseur du mur (exemple : via la taille du collider)
            float wallThickness = 0f;
            if (other is BoxCollider box)
                wallThickness = box.size.z * box.transform.lossyScale.z;
            else if (other is CapsuleCollider capsule)
                wallThickness = capsule.height * capsule.transform.lossyScale.z;
            else if (other is SphereCollider sphere)
                wallThickness = sphere.radius * 2f * sphere.transform.lossyScale.z;
            else
                wallThickness = 1f; // Valeur par défaut
            // Calcule la pénétration maximale
            float maxPenetrationDepth = CalculateMaxPenetrationDepth(materialDensity);

            // Si le mur est trop épais, la balle s'arrête à la surface 10m max
            if (wallThickness >= 10f)
            {
                StopBullet();
                Debug.Log("Bullet stopped at the surface due to wall thickness.");
                return;
            }

            if (bulletData.fragmentation && isFragment != true)
            {
                Debug.Log("Bullet fragmented!");

                // --- inside your fragmentation block ---
                int fragmentCount = 5;
                float angleVariation = 20f; // degrees cone half-angle

                float foreignObjectMass = other.GetComponent<BulletMaterial>().mass;

                for (int i = 0; i < fragmentCount; i++)
                {
                    GameObject fragment = Instantiate(bulletData.bulletPrefab, transform.position, Quaternion.identity);

                    Rigidbody fragmentRb = fragment.GetComponent<Rigidbody>();
                    if (fragmentRb != null)
                    {
                        BulletObject bulletScript = fragment.GetComponent<BulletObject>();
                        if (bulletScript != null)
                        {
                            bulletScript.bulletData = bulletData;
                            bulletScript.isFragment = true;
                        }

                        Vector3 baseDirection = rb.velocity.normalized;
                        Vector3 fragmentDirection = RandomDirectionInCone(baseDirection, angleVariation);

                        float penetrationRatio = Mathf.Clamp01(wallThickness / foreignObjectMass);
                        float energyLostPercent = Mathf.Clamp01(penetrationRatio) * 100f;
                        Debug.Log($"Energy lost in penetration through {other.name}: {energyLostPercent:F1}%");


                        // Optional: small random speed variance
                        float minSpeedFactor = 0.2f;  // fraction of original speed if fully stopped
                        float maxSpeedFactor = 0.7f;  // fraction of original speed when just exiting

                        float speedFactor = Mathf.Lerp(maxSpeedFactor, minSpeedFactor, penetrationRatio);
                        fragmentRb.velocity = fragmentDirection * (bulletData.speed * speedFactor);

                        // Orient the fragment to face travel direction (if desired)
                        fragment.transform.rotation = Quaternion.LookRotation(fragmentDirection);
                    }
                }
                Destroy(gameObject);
            }
        }
    }

    private Vector3 RandomDirectionInCone(Vector3 baseDir, float maxAngleDeg)
    {
        // Ensure baseDir is normalized
        baseDir = baseDir.normalized;

        float maxAngleRad = maxAngleDeg * Mathf.Deg2Rad;

        // Sample uniformly in the cone: pick cos(theta) uniformly between cos(maxAngle) and 1
        float cosTheta = Mathf.Cos(maxAngleRad);
        float u = Random.value;
        float cos = Mathf.Lerp(cosTheta, 1f, u);
        float sin = Mathf.Sqrt(1f - cos * cos);
        float phi = Random.value * Mathf.PI * 2f;

        // Build orthonormal basis (baseDir, orth1, orth2)
        Vector3 orth1 = Vector3.Cross(baseDir, Vector3.up);
        if (orth1.sqrMagnitude < 1e-6f) // baseDir is parallel to up
            orth1 = Vector3.Cross(baseDir, Vector3.right);
        orth1.Normalize();
        Vector3 orth2 = Vector3.Cross(baseDir, orth1);

        // Direction in that basis
        Vector3 dir = baseDir * cos + orth1 * (Mathf.Cos(phi) * sin) + orth2 * (Mathf.Sin(phi) * sin);
        return dir.normalized;
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

    private float GetMaterialDensity(Transform obj)
    {
        // Get material density based on tag or material name
        // You can expand this to use a material database or scriptable objects
            return 3f;  // Default for generic materials
    }

    private float CalculateMaxPenetrationDepth(float materialDensity)
    {
        // Exemple très simplifié inspiré de la balistique terminale
        float bulletEnergy = 0.5f * (bulletData.mass / 1000f) * bulletData.speed * bulletData.speed; // en Joules
        float penetration = bulletEnergy / (materialDensity * 100f); // Ajustez le facteur selon vos tests

        // Respect minimum thickness to allow small objects to actually stop bullets
        return Mathf.Max(penetration, 0.05f);
    }

    private void StopBullet()
    {
        // Stop the bullet completely
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
        }

        // Optional: Create impact effect here

        // Either destroy or keep the bullet lodged in place
        // Uncomment this if you want the bullet to disappear immediately
        // Destroy(gameObject);
    }
}