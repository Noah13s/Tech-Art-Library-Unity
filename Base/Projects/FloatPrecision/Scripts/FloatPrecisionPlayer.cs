using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vector 3 based on doubles instead of floats for handling very large values.
/// </summary>
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

    public static DoubleVector3 Lerp(DoubleVector3 a, DoubleVector3 b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        return new DoubleVector3(
            a.x + (b.x - a.x) * t,
            a.y + (b.y - a.y) * t,
            a.z + (b.z - a.z) * t
        );
    }

    public double Dot(DoubleVector3 other) =>
        x * other.x + y * other.y + z * other.z;
}

/// <summary>
/// Simple world space static spaceship player that moves via a <seealso cref="DoubleVector3"/> playerPosition parameter.
/// </summary>
public class FloatPrecisionPlayer : MonoBehaviour
{
    [Tooltip("Position movement speed in m/s.")]
    [SerializeField] private float moveSpeed = 10f;
    [Tooltip("Angular movement speed.")]
    [SerializeField] float sensitivity = 1.0f;

    [Tooltip("Actual DoubleVector3 player position in the world.\nNot to confuse with the transforms position.")]
    public DoubleVector3 playerPosition = new(0, 0, 0);

    [SerializeField] bool velocityActive = false;
    [ConditionalVisibility("velocityActive")]
    [SerializeField] private DoubleVector3 velocity;
    
    [SerializeField] private UnityEvent<string> playerPositionEvent;
    [SerializeField] private UnityEvent<float> playerSpeed;

    void Update()
    {
        HandleVelocity();
        if (Input.GetKey(KeyCode.W)) { transform.Rotate(sensitivity, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.S)) { transform.Rotate(-sensitivity, 0, 0, Space.Self); }
        if (Input.GetKey(KeyCode.D)) { transform.Rotate(0, sensitivity, 0, Space.Self); }
        if (Input.GetKey(KeyCode.A)) { transform.Rotate(0, -sensitivity, 0, Space.Self); }
        if (Input.GetKey(KeyCode.E)) { transform.Rotate(0, 0, -sensitivity, Space.Self); }
        if (Input.GetKey(KeyCode.Q)) { transform.Rotate(0, 0, sensitivity, Space.Self); }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            // Convert transform.forward to DoubleVector3 and update position in double space.
            DoubleVector3 forward = new (transform.forward.x, transform.forward.y, transform.forward.z);
            if (velocityActive)
            {
                velocity += forward * (moveSpeed * Time.deltaTime);
            }
            else
            {
                playerPosition += forward * (moveSpeed * Time.deltaTime);
            }
        }
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            // Convert transform.forward to DoubleVector3 and update position in double space.
            DoubleVector3 forward = new(transform.forward.x, transform.forward.y, transform.forward.z);
            if (velocityActive)
            {
                velocity += forward.Negate() * (moveSpeed * Time.deltaTime);
            } else
            {
                playerPosition += forward.Negate() * (moveSpeed * Time.deltaTime);
            }
        }

        playerPositionEvent?.Invoke($"X:{playerPosition.x}\nY:{playerPosition.y}\nZ:{playerPosition.z}");
        playerSpeed?.Invoke(moveSpeed);
    }

    /// <summary>
    /// Sets the position movement speed.
    /// </summary>
    /// <param name="_speed">New speed value in m/s</param>
    public void SetSpeed(float _speed)
    {
        moveSpeed = _speed;
    }

    /// <summary>
    /// Adds position to the current player position.
    /// </summary>
    /// <param name="_position">Position vector to increment to the current position.</param>
    public void AddPosition(Vector3 _position)
    {
        playerPosition += new DoubleVector3(_position.x,_position.y, _position.z);
    }

    /// <summary>
    /// Adds position to the current player position.
    /// </summary>
    /// <param name="_position">Position vector to increment to the current position.</param>
    public void AddPosition(DoubleVector3 _position)
    {
        playerPosition += _position;
    }

    private void HandleVelocity()
    {
        if (!velocityActive) { return; }

        AddPosition(velocity);
    }

    public void AddVelocity(DoubleVector3 _velocity)
    {
        velocity += _velocity;
    }

    public DoubleVector3 GetVelocity()
    {
        return velocity;
    }
}
