using UnityEngine;

public class BulletObject : MonoBehaviour
{
    public BulletData bulletData;       // Bullet data with speed, ballistic coefficient, lifetime, etc.
    public float velocity;              // Bullet's current speed
    private Vector3 direction;          // Bullet's direction of travel
    private Vector3 velocityVector;     // Velocity vector for x, y, and z components
    private float gravity = -9.81f;     // Gravity constant (you can adjust this for stronger/weaker gravity)
    private float lifetimeTimer;        // Timer to track the bullet's lifetime

    private void Start()
    {
        velocity = bulletData.speed;               // Initialize bullet velocity
        direction = transform.forward;             // Set bullet's initial direction of travel
        velocityVector = direction * velocity;     // Initialize velocity vector with forward motion
        lifetimeTimer = 0f;                         // Initialize the lifetime timer
    }

    // Update is called once per frame
    void Update()
    {
        // Update the lifetime timer and check if the bullet should be destroyed
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= bulletData.lifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Simulate air drag slowing down the bullet (simplified for now)
        velocity -= bulletData.ballisticCoefficient * velocity * Time.deltaTime;

        // Apply gravity to the bullet's position directly
        Vector3 gravityEffect = Vector3.up * gravity * Time.deltaTime;
        velocityVector += gravityEffect;

        // Move the bullet based on its velocity vector
        transform.position += velocityVector * Time.deltaTime;

        // If the bullet's velocity drops to zero or below, destroy it
        if (velocity <= 0)
        {
            Destroy(gameObject);
        }
    }
}
