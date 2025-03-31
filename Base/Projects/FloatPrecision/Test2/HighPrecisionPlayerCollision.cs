using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HighPrecisionPlayerCollision : MonoBehaviour
{
    [Tooltip("Reference to the high-precision player controller.")]
    public FloatPrecisionPlayer highPrecisionPlayer;

    [Tooltip("Radius of the collision sphere (should match the collider's size).")]
    public float collisionRadius = 0.5f;

    [Tooltip("Layers that the player collides with.")]
    public LayerMask collisionLayers;

    // A small buffer to avoid jitter.
    public float separationBuffer = 0.01f;

    private Collider playerCollider;

    void Start()
    {
        if (highPrecisionPlayer == null)
        {
            Debug.LogError("HighPrecisionPlayerCollision: No highPrecisionPlayer assigned!");
        }

        playerCollider = GetComponent<Collider>();
        if (playerCollider == null)
        {
            Debug.LogError("HighPrecisionPlayerCollision: No Collider found on this GameObject!");
        }
    }

    void FixedUpdate()
    {
        // Use the transform.position (which is kept near the origin for high precision) for collision detection.
        Vector3 currentPos = transform.position;

        // Query all colliders overlapping a sphere at currentPos.
        Collider[] hits = Physics.OverlapSphere(currentPos, collisionRadius, collisionLayers);
        foreach (Collider hit in hits)
        {
            // Skip self-collisions.
            if (hit == playerCollider)
                continue;

            // Compute the penetration vector between the player's collider and the hit collider.
            Vector3 direction;
            float distance;
            bool overlapping = Physics.ComputePenetration(
                playerCollider, currentPos, transform.rotation,
                hit, hit.transform.position, hit.transform.rotation,
                out direction, out distance);

            if (overlapping)
            {
                // Push out the player by the penetration distance plus a small buffer.
                currentPos += direction * (distance + separationBuffer);
            }
        }

        // Update the transform position.
        transform.position = currentPos;
        // Also update the high precision player's double-precision position.
        highPrecisionPlayer.playerPosition = new DoubleVector3(currentPos.x, currentPos.y, currentPos.z);
    }
}
