using UnityEngine;

/// <summary>
/// A complementary collision handler for FloatPrecisionPlayer. 
/// Attach this script to the same GameObject as your FloatPrecisionPlayer.
/// When a collision is detected, it applies a small corrective offset to 
/// FloatPrecisionPlayer.playerPosition, effectively pushing the world away.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class FloatPrecisionPlayerCollisionHandler : MonoBehaviour
{
    [Tooltip("Reference to the FloatPrecisionPlayer component.")]
    public FloatPrecisionPlayer floatPrecisionPlayer;

    [Tooltip("Strength of the collision push applied to the player's position.")]
    public float collisionPushStrength = 0.1f;

    private Rigidbody rb;

    void Awake()
    {
        // Ensure we have a kinematic rigidbody for collision detection
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    void OnCollisionStay(Collision collision)
    {
        Debug.Log("test");
        // Compute an average contact normal from all contacts
        Vector3 averageNormal = Vector3.zero;
        foreach (ContactPoint contact in collision.contacts)
        {
            averageNormal += contact.normal;
        }
        if (collision.contacts.Length > 0)
        {
            averageNormal /= collision.contacts.Length;
            // Create a correction vector in double precision
            DoubleVector3 correction = new DoubleVector3(averageNormal.x, averageNormal.y, averageNormal.z) * collisionPushStrength;
            // Adjust the player's logical (double precision) position
            floatPrecisionPlayer.playerPosition += correction;
        }
    }
}
