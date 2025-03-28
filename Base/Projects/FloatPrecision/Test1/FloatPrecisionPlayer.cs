using System;
using UnityEngine;
using UnityEngine.Events;
[System.Serializable]
public struct DoubleVector3
{
    public double x, y, z;
    public DoubleVector3(double x, double y, double z)
    {
        this.x = x; this.y = y; this.z = z;
    }
    public static DoubleVector3 operator +(DoubleVector3 a, DoubleVector3 b) =>
        new DoubleVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    public static DoubleVector3 operator -(DoubleVector3 a, DoubleVector3 b) =>
        new DoubleVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    public static DoubleVector3 operator *(DoubleVector3 a, double d) =>
        new DoubleVector3(a.x * d, a.y * d, a.z * d);
    public double Magnitude() => Math.Sqrt(x * x + y * y + z * z);
    public DoubleVector3 Negate() => new DoubleVector3(-x, -y, -z);
    public DoubleVector3 Normalized()
    {
        double mag = Magnitude();
        return mag > 0 ? new DoubleVector3(x / mag, y / mag, z / mag) : new DoubleVector3(0, 0, 0);
    }
    public static explicit operator Vector3(DoubleVector3 d) =>
        new Vector3((float)d.x, (float)d.y, (float)d.z);

    public DoubleVector3 Cross(DoubleVector3 other) =>
    new DoubleVector3(
        y * other.z - z * other.y,
        z * other.x - x * other.z,
        x * other.y - y * other.x
    );
}

public class FloatPrecisionPlayer : MonoBehaviour
{
    [SerializeField] GameObject world;
    public float moveSpeed = 10f;
    [SerializeField] float sensitivity = 1.0f;

    public DoubleVector3 playerPosition = new DoubleVector3(0, 0, 0);

    [SerializeField] private UnityEvent<string> playerPositionEvent;

    void Update()
    {
        if (Input.GetKey(KeyCode.W)) { transform.Rotate(sensitivity, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.S)) { transform.Rotate(-sensitivity, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.D)) { transform.Rotate(0, sensitivity, 0, Space.Self); }
        if (Input.GetKey(KeyCode.A)) { transform.Rotate(0, -sensitivity, 0, Space.Self); }
        if (Input.GetKey(KeyCode.E)) { transform.Rotate(0, 0, -sensitivity, Space.Self); }
        if (Input.GetKey(KeyCode.Q)) { transform.Rotate(0, 0, sensitivity, Space.Self); }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Convert transform.forward to DoubleVector3 and update position in double space.
            DoubleVector3 forward = new DoubleVector3(transform.forward.x, transform.forward.y, transform.forward.z);
            playerPosition = playerPosition + forward * (moveSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            // Convert transform.forward to DoubleVector3 and update position in double space.
            DoubleVector3 forward = new DoubleVector3(transform.forward.x, transform.forward.y, transform.forward.z);
            playerPosition = playerPosition + forward.Negate() * (moveSpeed * Time.deltaTime);
        }

        if (playerPositionEvent != null) { playerPositionEvent.Invoke($"X:{playerPosition.x}\nY:{playerPosition.y}\nZ:{playerPosition.z}"); }
    }
}
