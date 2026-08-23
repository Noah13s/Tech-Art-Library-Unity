using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Simple world space static spaceship player that moves via a <seealso cref="DoubleVector3"/> playerPosition parameter.
/// </summary>
public class FloatPrecisionPlayer : MonoBehaviour
{
    [Tooltip("Position movement speed in m/s.")]
    [SerializeField] private double moveSpeed = 10f;
    [Tooltip("Angular movement speed.")]
    [SerializeField] float sensitivity = 1.0f;

    [Tooltip("Actual DoubleVector3 player position in the world.\nNot to confuse with the transforms position.")]
    public DoubleVector3 playerPosition = new(0, 0, 0);

    [SerializeField] bool velocityActive = false;
    [ConditionalVisibility("velocityActive")]
    [SerializeField] private DoubleVector3 velocity;
    
    [SerializeField] private UnityEvent<string> playerPositionEvent;
    [SerializeField] private UnityEvent<double> playerSpeed;

    void Update()
    {
        HandleVelocity();

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) { transform.Rotate(sensitivity, 0, 0, Space.Self); }
            if (keyboard.sKey.isPressed) { transform.Rotate(-sensitivity, 0, 0, Space.Self); }
            if (keyboard.dKey.isPressed) { transform.Rotate(0, sensitivity, 0, Space.Self); }
            if (keyboard.aKey.isPressed) { transform.Rotate(0, -sensitivity, 0, Space.Self); }
            if (keyboard.eKey.isPressed) { transform.Rotate(0, 0, -sensitivity, Space.Self); }
            if (keyboard.qKey.isPressed) { transform.Rotate(0, 0, sensitivity, Space.Self); }

            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                MoveAlongForward(1.0);
            }

            if (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed)
            {
                MoveAlongForward(-1.0);
            }
        }

        playerPositionEvent?.Invoke($"X:{playerPosition.x}\nY:{playerPosition.y}\nZ:{playerPosition.z}");
        playerSpeed?.Invoke(velocityActive ? velocity.Magnitude() : moveSpeed);
    }

    private void MoveAlongForward(double direction)
    {
        // Convert transform.forward to DoubleVector3 and update position in double space.
        DoubleVector3 forward = new(transform.forward.x, transform.forward.y, transform.forward.z);
        DoubleVector3 movement = forward * (direction * moveSpeed * Time.deltaTime);

        if (velocityActive)
        {
            velocity += movement;
        }
        else
        {
            playerPosition += movement;
        }
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

        // Velocity is stored in meters per second. Integrating without deltaTime made
        // motion frame-rate dependent and greatly exaggerated residual ground speed.
        AddPosition(velocity * Time.deltaTime);
    }

    public void AddVelocity(DoubleVector3 _velocity)
    {
        velocity += _velocity;
    }

    public DoubleVector3 GetVelocity()
    {
        return velocity;
    }

    public void SetVelocity(DoubleVector3 newVelocity)
    {
        velocity = newVelocity;
    }

    public DoubleVector3 GetPosition()
    {
        return playerPosition;
    }
}
