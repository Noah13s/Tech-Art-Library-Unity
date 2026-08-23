using UnityEngine;

[RequireComponent(typeof(PerspectiveIllusionObject))]
public class PlanetGravityHandler : MonoBehaviour
{
    [Tooltip("Mass of the planet in Kg.")]
    [SerializeField] private double mass = 5.972e24;

    private const double GravitationalConstant = 6.67430e-11;

    private PerspectiveIllusionObject planet;
    private FloatPrecisionPlayer player;

    private void Awake()
    {
        planet = GetComponent<PerspectiveIllusionObject>();
        player = planet.player;
    }

    private void FixedUpdate()
    {
        if (player == null)
            return;

        player.AddVelocity(CalculateGravityForceAtPosition(player.playerPosition) * Time.fixedDeltaTime);
    }

    public double CalculateGravityAtPosition(DoubleVector3 position)
    {
        if (planet == null)
            return 0;

        DoubleVector3 direction = planet.simulationPosition - position;
        double distanceSquared = direction.Dot(direction);

        return distanceSquared < 1e-6
            ? 0
            : GravitationalConstant * mass / distanceSquared;
    }

    public DoubleVector3 CalculateGravityForceAtPosition(DoubleVector3 position)
    {
        if (planet == null)
            return DoubleVector3.Zero;

        DoubleVector3 direction = planet.simulationPosition - position;
        double distanceSquared = direction.Dot(direction);
        if (distanceSquared < 1e-6)
            return DoubleVector3.Zero;

        return direction.Normalized() * (GravitationalConstant * mass / distanceSquared);
    }
}
