using UnityEditor;
using UnityEngine;

public class BallBalance : MonoBehaviour
{
    [SerializeField] private MeshRenderer platform;
    [SerializeField] private float maxTurningAngle = 45f;
    [SerializeField] private float positionGain = 5f;    // Kp
    [SerializeField] private float velocityGain = 2f;    // Kd

    private Rigidbody ballRb;
    private BoxCollider boxCollider;

    void Start()
    {
        // build a collider matching the platform bounds for collision callbacks
        boxCollider = GetComponent<BoxCollider>()
                      ?? gameObject.AddComponent<BoxCollider>();
        boxCollider.size = platform.localBounds.size;
        boxCollider.center = platform.localBounds.center;
    }

    void Update()
    {
        if (ballRb == null) return;

        var platformPos = platform.transform.position;
        var ballPos = ballRb.position;
        var error = ballPos - platformPos;        // position error
        var vel = ballRb.velocity;              // current velocity :contentReference[oaicite:0]{index=0}

        // PD control: angleX tilts around X to correct Z error & velocity
        float angleX = Mathf.Clamp(
            -(positionGain * error.z + velocityGain * vel.z),
            -maxTurningAngle, maxTurningAngle);

        // angleZ tilts around Z to correct X error & velocity
        float angleZ = Mathf.Clamp(
              (positionGain * error.x + velocityGain * vel.x),
            -maxTurningAngle, maxTurningAngle);

        var targetRot = Quaternion.Euler(angleX, 0f, angleZ);
        // smooth toward target rotation :contentReference[oaicite:1]{index=1}
        platform.transform.rotation = Quaternion.Lerp(
            platform.transform.rotation,
            targetRot,
            Time.deltaTime * 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // grab the ball's Rigidbody once it lands
        if (collision.rigidbody != null)
            ballRb = collision.rigidbody;
    }

    private void OnCollisionExit(Collision collision)
    {
        // clear when it rolls off
        if (collision.rigidbody == ballRb)
            ballRb = null;
    }
}
