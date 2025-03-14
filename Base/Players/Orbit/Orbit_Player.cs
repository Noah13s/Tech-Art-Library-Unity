using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // The target object to orbit around
    public Vector3 targetOffset; // Offset for the target position
    public float distance = 10f; // The distance from the target
    public float sensitivityX = 1f; // Mouse X sensitivity
    public float sensitivityY = 1f; // Mouse Y sensitivity
    public bool allowScrolling = true; // Whether scrolling is allowed
    public float scrollSpeed = 1f; // Scrollwheel sensitivity
    public bool showCursor = true; // Whether to show the cursor
    public float cameraLag = 5f; // Camera lag for target position
    public LayerMask collisionLayer; // Layer mask for collision detection
    public float collisionOffset = 0.2f; // Offset for collision detection
    public bool RightClickMove = true; // Whether moving the camera by right-clicking is allowed

    [NonSerialized]
    public bool lockCamera = false; // Lock the camera movements

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 targetPosition;
#if ENABLE_INPUT_SYSTEM
    private InputSystem_Actions PCcontrols;
    private ControllerInputs ControllerControls;
    private TouchInputs Touchcontrols;
#endif

    private void Awake()
    {

        // Initialize the new input system controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols = new ();
        ControllerControls = new ControllerInputs();
        Touchcontrols = new TouchInputs();
#endif
    }

    private void OnEnable()
    {
        // Enable the input controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols.Enable();
        ControllerControls.Enable();
        Touchcontrols.Enable();
#endif
    }

    private void OnDisable()
    {
        // Disable the input controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols.Disable();
        ControllerControls.Disable();
        Touchcontrols.Disable();
#endif
    }

    void Initialize()
    {
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked; // If showCursor true .none else .Locked
        Cursor.visible = showCursor;
    }

    // Reinitialize on var changed
    private void OnValidate()
    {
        Initialize();
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        // Initialize target position
        targetPosition = target.position + targetOffset;

        // Initialize looking direction 
        currentRotationX = target.rotation.eulerAngles.y + 180;
    }

    void Update()
    {
        if (lockCamera) return;

        if (RightClickMove)
        {
#if ENABLE_INPUT_SYSTEM
            if (PCcontrols.Orbit_Player.LeftButton.IsPressed() && IsCursorOverGameWindow())
            {
                // Rotate the camera based on mouse input            
                currentRotationX += PCcontrols.Orbit_Player.Delta.ReadValue<Vector2>().x * sensitivityX;
                currentRotationY -= PCcontrols.Orbit_Player.Delta.ReadValue<Vector2>().y * sensitivityY;
            }
#else
            if (Input.GetMouseButton(1) && IsCursorOverGameWindow())
            {
                // Rotate the camera based on mouse input            
                currentRotationX += Input.GetAxis("Mouse X") * sensitivityX;
                currentRotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
            }
#endif
        }
        else
        {
#if ENABLE_INPUT_SYSTEM
            // Rotate the camera based on input
            currentRotationX += ControllerControls.ControllerActionMap.RightJoystick.ReadValue<Vector2>().x * sensitivityX;
            currentRotationY -= ControllerControls.ControllerActionMap.RightJoystick.ReadValue<Vector2>().y * sensitivityY;
            currentRotationX += PCcontrols.Orbit_Player.Delta.ReadValue<Vector2>().x * sensitivityX;
            currentRotationY -= PCcontrols.Orbit_Player.Delta.ReadValue<Vector2>().y * sensitivityY;
            currentRotationX += Touchcontrols.Touch.Delta.ReadValue<Vector2>().x * sensitivityX;
            currentRotationY -= Touchcontrols.Touch.Delta.ReadValue<Vector2>().y * sensitivityY;
#else
            // Rotate the camera based on mouse input            
            currentRotationX += Input.GetAxis("Mouse X") * sensitivityX;
            currentRotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
#endif
        }
        currentRotationY = Mathf.Clamp(currentRotationY, -90f, 90f); // Clamp Y rotation

        // Zoom in and out using scrollwheel if scrolling is allowed
        if (allowScrolling && IsCursorOverGameWindow())
        {
#if ENABLE_INPUT_SYSTEM
            distance += PCcontrols.Orbit_Player.ScrollWheel.ReadValue<Vector2>().y * scrollSpeed;
#else
            distance -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
#endif
            distance = Mathf.Clamp(distance, 1f, Mathf.Infinity); // Clamp distance
        }

        // Calculate the rotation and position
        Quaternion rotation = Quaternion.Euler(currentRotationY, currentRotationX, 0);
        Vector3 direction = rotation * Vector3.forward * -distance;
        Vector3 desiredPosition = target.position + targetOffset + direction;

        // Handle camera collision
        RaycastHit hit;
        if (Physics.Linecast(target.position + targetOffset, desiredPosition, out hit, collisionLayer))
        {
            // If collision detected, adjust the desired position
            targetPosition = hit.point + hit.normal * collisionOffset;
        }
        else
        {
            // No collision, use the desired position directly
            targetPosition = desiredPosition;
        }

        // Apply camera lag if cameraLag is greater than zero
        if (cameraLag > 0)
        {
            // Smoothly move towards the target position
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * cameraLag);
        }
        else
        {
            // No camera lag, directly set the position
            transform.position = targetPosition;
        }

        // Set the camera's rotation
        transform.rotation = rotation;
    }

    bool IsCursorOverGameWindow()
    {
        // Check if EventSystem is used (for UI detection)
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // Check cursor position using the Legacy Input System or New Input System
        Vector2 mousePosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // New Input System
        mousePosition = PCcontrols.Orbit_Player.Position.ReadValue<Vector2>();
#else
        // Legacy Input System
        mousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
#endif

        // Check if the cursor is within screen bounds
        if (mousePosition.x >= 0 && mousePosition.x <= Screen.width &&
            mousePosition.y >= 0 && mousePosition.y <= Screen.height)
        {
            return true;
        }

        return false;
    }
}
