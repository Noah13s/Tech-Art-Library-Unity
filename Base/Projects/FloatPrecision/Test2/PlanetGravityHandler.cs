using UnityEngine;

[RequireComponent(typeof(PerspectiveIllusionObject))]
public class PlanetGravityHandler : MonoBehaviour
{
    [Tooltip("Mass of the planet in Kg.")]
    [SerializeField] private double mass = 5.972e24; // Default to Earth's mass

    [Tooltip("Gravitational constant.")]
    private const double G = 6.67430e-11; // m³/kg/s²

    private PerspectiveIllusionObject planet;
    private FloatPrecisionPlayer player;
    private double gravityPull;

    private void Start()
    {
        planet = GetComponent<PerspectiveIllusionObject>();
        player = planet.player;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (player == null) return;

        // Convert player position to a DoubleVector3
        DoubleVector3 planetPosition = planet.simulationPosition;
        DoubleVector3 playerPosition = player.playerPosition;

        // Calculate direction and distance
        DoubleVector3 direction = planetPosition - playerPosition;
        double distanceSquared = direction.Dot(direction); // Equivalent to Vector3.sqrMagnitude
        if (distanceSquared < 1e-6) return; // Prevent division by zero

        // Normalize direction
        DoubleVector3 gravityDirection = direction.Normalized();

        // Apply Newton's law of universal gravitation: F = G * (m1 * m2) / r²
        DoubleVector3 gravityForce = gravityDirection * (G * mass / distanceSquared);
        gravityPull = gravityForce.Magnitude();
        // Apply gravity to player
        player.AddVelocity(gravityForce * Time.deltaTime);
    }
}
