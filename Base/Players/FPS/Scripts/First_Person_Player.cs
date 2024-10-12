using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System namespace
#endif

[RequireComponent(typeof(CharacterController))]
public class First_Person_Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showCursor = false;
    [SerializeField]
    private bool airControl = false;
    [SerializeField]
    private bool enableSlideBounce = false; // Combined sliding and bouncing variable
    [SerializeField]
    private float decelerationFactor = 30f; // Speed at which to decelerate
    [SerializeField]
    private float bounceMultiplier = 0.5f; // Multiplier for the bounce effect
    [SerializeField]
    private float bounceThreshold = 10f; // Height from which to start bouncing

    [Header("Navigation")]
    [SerializeField]
    private float walkSpeed = 5.0f;
    [SerializeField]
    private float runSpeed = 10.0f;
    [SerializeField]
    private float jumpForce = 10f;
    [SerializeField]
    private float gravity = 20f;
    [SerializeField]
    private float lookSpeed = 2.0f;
    [SerializeField]
    private float lookXLimit = 80.0f;

    // New fields for optional joystick and touch input
    [Header("Touch & Joystick (Optional)")]
    public JoystickControl joystick;        // Optional joystick control
    public UITouchControl touchControl;     // Optional touch control

    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

#if ENABLE_INPUT_SYSTEM
    private FPS_Controls controls; // Reference to your Input Action Asset
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction jumpAction;
    private InputAction lookAction;
#endif

    void Initalise()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = showCursor;

#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new FPS_Controls();
        controls.Enable();
        forwardAction = controls.FPS_Action_Map.Forward;
        backwardAction = controls.FPS_Action_Map.Backwards;
        rightAction = controls.FPS_Action_Map.Right;
        leftAction = controls.FPS_Action_Map.Left;
        jumpAction = controls.FPS_Action_Map.Jump;
        lookAction = controls.FPS_Action_Map.Look;
#endif
    }

    // Reinitialize on var changed
    private void OnValidate()
    {
        Initalise();
    }

    // Start is called before the first frame update
    void Start()
    {
        Initalise();
    }

    // Update is called once per frame
    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        HandleNewInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
        HandleLegacyInput();
#endif
    }

#if ENABLE_INPUT_SYSTEM
    // Handle input from the new Input System
    private void HandleNewInputSystem()
    {
        // Movement input based on joystick or keyboard actions
        float forwardMovement = 0;
        float rightMovement = 0;

        // Use joystick input if available
        if (joystick != null)
        {
            forwardMovement = joystick.GetVertical();
            rightMovement = joystick.GetHorizontal();
        }
        else
        {
            forwardMovement = forwardAction.ReadValue<float>() - backwardAction.ReadValue<float>();
            rightMovement = rightAction.ReadValue<float>() - leftAction.ReadValue<float>();
        }

        // Movement speed based on whether the player is running or walking
        float moveSpeed = Keyboard.current.leftShiftKey.isPressed ? runSpeed : walkSpeed;

        // Calculate the desired horizontal movement
        Vector3 desiredMovement = transform.TransformDirection(new Vector3(rightMovement * moveSpeed, 0, forwardMovement * moveSpeed));

        // Check if the player is grounded
        if (characterController.isGrounded)
        {
            // Apply sliding and bouncing if enabled
            if (enableSlideBounce)
            {
                // Apply sliding deceleration
                if (desiredMovement.magnitude > 0)
                {
                    moveDirection = Vector3.Lerp(moveDirection, desiredMovement, Time.deltaTime * decelerationFactor);
                }
                else
                {
                    // Gradually decelerate to a stop
                    moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, Time.deltaTime * decelerationFactor);
                }

                // Jumping logic
                if (jumpAction.triggered)
                {
                    moveDirection.y = jumpForce;
                }

                // Handle bouncing if enabled
                float fallSpeed = moveDirection.y; // Capture the current vertical speed
                if (fallSpeed < -bounceThreshold) // Check if falling fast enough
                {
                    // Apply bounce based on the negative fall speed
                    moveDirection.y = Mathf.Abs(fallSpeed) * bounceMultiplier;
                }
            }
            else
            {
                // Instant change in direction if sliding and bouncing are disabled
                moveDirection = desiredMovement;

                // Jumping logic
                if (jumpAction.triggered)
                {
                    moveDirection.y = jumpForce;
                }
            }
        }
        else if (airControl)
        {
            // Maintain horizontal movement input in the air without affecting vertical movement
            moveDirection.x = desiredMovement.x;
            moveDirection.z = desiredMovement.z;
        }

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Camera rotation (looking around) with touch input
        Vector2 lookInput = Vector2.zero;

        // Use touch input if touchControl is active
        if (touchControl != null && touchControl.IsTouching())
        {
            lookInput = touchControl.GetTouchDelta();
        }
        // Fallback to mouse/keyboard if touch input is not available
        else
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }

        // Update rotation based on touch input
        rotationX -= lookInput.y * lookSpeed; // Invert y-axis for a more natural look
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        float rotationY = lookInput.x * lookSpeed;

        // Apply rotation
        transform.Rotate(0, rotationY, 0); // Add the X rotation to the current rotation
        Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0); // Set the local rotation


        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);
    }
#endif


#if ENABLE_LEGACY_INPUT_MANAGER
    // Handle input from the old Input Manager
    private void HandleLegacyInput()
    {
        // Calculate movement
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        // Movement input based on joystick or keyboard actions
        float forwardMovement = 0;
        float rightMovement = 0;

        // Use joystick input if available
        if (joystick != null)
        {
            forwardMovement = joystick.GetVertical();
            rightMovement = joystick.GetHorizontal();
        }
        else
        {
            forwardMovement = Input.GetAxis("Vertical") * moveSpeed;
            rightMovement = Input.GetAxis("Horizontal") * moveSpeed;
        }

        // Desired movement vector
        Vector3 desiredMovement = transform.TransformDirection(new Vector3(rightMovement, 0, forwardMovement));

        // Check if the player is grounded
        if (characterController.isGrounded)
        {
            // Apply sliding and bouncing if enabled
            if (enableSlideBounce)
            {
                // Apply sliding deceleration
                if (desiredMovement.magnitude > 0)
                {
                    moveDirection = Vector3.Lerp(moveDirection, desiredMovement, Time.deltaTime * decelerationFactor);
                }
                else
                {
                    // Gradually decelerate to a stop
                    moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, Time.deltaTime * decelerationFactor);
                }

                // Jumping logic
                if (Input.GetButton("Jump"))
                {
                    moveDirection.y = jumpForce;
                }

                // Handle bouncing if enabled
                float fallSpeed = moveDirection.y; // Capture the current vertical speed
                if (fallSpeed < -bounceThreshold) // Check if falling fast enough
                {
                    // Apply bounce based on the negative fall speed
                    moveDirection.y = Mathf.Abs(fallSpeed) * bounceMultiplier;
                }
            }
            else
            {
                // Instant change in direction if sliding and bouncing are disabled
                moveDirection = desiredMovement;

                // Jumping logic
                if (Input.GetButton("Jump"))
                {
                    moveDirection.y = jumpForce;
                }
            }
        }
        else if (airControl)
        {
            // Maintain horizontal movement input in the air without affecting vertical movement
            moveDirection.x = desiredMovement.x;
            moveDirection.z = desiredMovement.z;
        }

        // Camera rotation (looking around)
        float rotationY = Input.GetAxis("Mouse X") * lookSpeed;
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        transform.Rotate(0, rotationY, 0);
        Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // Apply gravity
        moveDirection.y -= gravity * Time.deltaTime;

        // Move the character
        characterController.Move(moveDirection * Time.deltaTime);
    }
#endif
}
