using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Third_Person_Player : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField]
    private bool showCursor = false;

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

    private Vector3 moveDirection = Vector3.zero;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform cameraTransform;

#if ENABLE_INPUT_SYSTEM
    private TPS_Controls controls; // Reference to your Input Action Asset
    private InputAction forwardAction;
    private InputAction backwardAction;
    private InputAction rightAction;
    private InputAction leftAction;
    private InputAction jumpAction;
    private InputAction lookAction;
#endif

    void Initalise()
    {
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = showCursor;

#if ENABLE_INPUT_SYSTEM
        // Initialize the input actions for the new Input System
        controls = new TPS_Controls();
        controls.Enable();
        forwardAction = controls.TPS_Action_Map.Forward;
        backwardAction = controls.TPS_Action_Map.Backwards;
        rightAction = controls.TPS_Action_Map.Right;
        leftAction = controls.TPS_Action_Map.Left;
        jumpAction = controls.TPS_Action_Map.Jump;
        lookAction = controls.TPS_Action_Map.Look;
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
        float forwardMovement = forwardAction.ReadValue<float>() - backwardAction.ReadValue<float>();
        float rightMovement = rightAction.ReadValue<float>() - leftAction.ReadValue<float>();

        // Movement speed based on whether the player is running or walking
        forwardMovement = Keyboard.current.leftShiftKey.isPressed ? forwardMovement * 2 : forwardMovement;
        rightMovement = Keyboard.current.leftShiftKey.isPressed ? rightMovement * 2 : rightMovement;

        // Update animator parameters
        animator.SetFloat("VelocityY", forwardMovement);
        animator.SetFloat("VelocityX", rightMovement);

        // Check if there is any movement input
        Vector3 inputDirection = new Vector3(rightMovement, 0, forwardMovement).normalized;
        if (inputDirection.magnitude > 0)
        {
            // Rotate character to face the camera direction
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0; // Keep rotation on the horizontal plane
            Quaternion targetRotation = Quaternion.LookRotation(cameraForward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * lookSpeed);
        }

        // Move the character
        Vector3 movement = inputDirection * walkSpeed * Time.deltaTime;
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
    // Handle input from the old Input Manager
    private void HandleLegacyInput() { }
#endif
}
