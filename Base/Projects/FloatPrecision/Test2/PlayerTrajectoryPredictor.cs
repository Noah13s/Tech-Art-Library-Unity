using UnityEngine;

public class PlayerTrajectoryPredictorFixedPoints : MonoBehaviour
{
    [Tooltip("Reference to the FloatPrecisionPlayer component.")]
    public FloatPrecisionPlayer player;

    [Tooltip("Reference to the PlanetGravityHandler for gravity calculations.")]
    public PlanetGravityHandler planetGravity;

    [Tooltip("The fixed prediction distance from the player (in simulation units).")]
    public float predictionDistance = 100f;

    [Tooltip("Number of prediction points to display.")]
    public int predictionPointCount = 20;

    [Tooltip("Distribution factor for the points. 1 = uniform; >1 concentrates points near the start; <1 distributes them farther out.")]
    public float distributionFactor = 1f;

    [Tooltip("Color used to draw the predicted trajectory gizmos.")]
    public Color trajectoryColor = Color.green;

    [Tooltip("Size of the debug spheres drawn at each predicted point.")]
    public float sphereSize = 0.5f;

    void OnDrawGizmos()
    {
        if (player == null || planetGravity == null)
            return;

        // Get the player's current high-precision position and velocity.
        Vector3 initialPosition = transform.position;
        DoubleVector3 initialVelocity = player.GetVelocity();

        // Calculate the player's current speed.
        float playerSpeed = (float)player.GetVelocity().Magnitude();
        // Determine total prediction time so that the prediction horizon is fixed.
        float totalPredictionTime = playerSpeed > 0.001f ? predictionDistance / playerSpeed : 5f;

        Gizmos.color = trajectoryColor;
        Vector3 prevPoint = (Vector3)initialPosition;

        // Loop for a fixed number of prediction points.
        for (int i = 0; i < predictionPointCount; i++)
        {
            // Calculate normalized parameter (0 to 1).
            float u = (predictionPointCount > 1) ? (float)i / (predictionPointCount - 1) : 0f;
            // Apply distribution factor for nonlinear spacing.
            float t = Mathf.Pow(u, distributionFactor) * totalPredictionTime;

            // Simulate trajectory from the initial state over time t using Euler integration.
            DoubleVector3 simPos = new(initialPosition.x, initialPosition.y, initialPosition.z);
            DoubleVector3 velocity = initialVelocity;
            int integrationSteps = 10;
            float dt = t / integrationSteps;
            for (int step = 0; step < integrationSteps; step++)
            {
                DoubleVector3 gravityForce = planetGravity.CalculateGravityForceAtPosition(simPos);
                // Assume unit mass so acceleration equals force.
                velocity += gravityForce * dt;
                simPos += velocity * dt;
            }

            Vector3 currentPoint = (Vector3)simPos;
            Gizmos.DrawSphere(currentPoint, sphereSize);
            if (i > 0)
                Gizmos.DrawLine(prevPoint, currentPoint);
            prevPoint = currentPoint;
        }
    }
}
