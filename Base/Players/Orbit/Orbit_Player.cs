using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // The target object to orbit around
    [SerializeField] private Vector3 positionOffset; // Offset for the target position
    [SerializeField] private Vector3 rotationOffset; // Offset for the target position
    [SerializeField] private float distance = 10f; // The distance from the target
    [SerializeField] private float sensitivityX = 1f; // Mouse X sensitivity
    [SerializeField] private float sensitivityY = 1f; // Mouse Y sensitivity
    [SerializeField] private bool allowScrolling = true; // Whether scrolling is allowed
    [SerializeField] private float scrollSpeed = 1f; // Scrollwheel sensitivity
    [SerializeField] private bool showCursor = true; // Whether to show the cursor
    [SerializeField] private float cameraLag = 5f; // Camera lag for target position
    [SerializeField] private LayerMask collisionLayer; // Layer mask for collision detection
    [SerializeField] private float collisionOffset = 0.2f; // Offset for collision detection
    [SerializeField] private bool rightClickMove = true; // Whether moving the camera by right-clicking is allowed
    [SerializeField] private bool absoluteUp = true; // If true, the camera rotates around world up. If false, it follows the target's rotation.
    [SerializeField][ConditionalVisibility("rightClickMove")] private UnityEvent onClickMoveEnded;

    [NonSerialized]
    public bool lockCamera = false; // Lock the camera movements


    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 targetPosition;
#if ENABLE_INPUT_SYSTEM
    private InputSystem_Actions controls;
#endif

    private void Awake()
    {

        // Initialize the new input system controls
#if ENABLE_INPUT_SYSTEM
        controls = new ();
#endif
    }

    private void OnEnable()
    {
        // Enable the input controls
#if ENABLE_INPUT_SYSTEM
        controls.Enable();
        controls.Orbit_Player.RightButton.performed += OnButtonPressed;
        controls.Orbit_Player.RightButton.canceled += OnButtonReleased;
#endif
    }

    private void OnDisable()
    {
        // Disable the input controls
#if ENABLE_INPUT_SYSTEM
        controls.Disable();
        controls.Orbit_Player.RightButton.performed -= OnButtonPressed;
        controls.Orbit_Player.RightButton.canceled -= OnButtonReleased;
#endif
    }

    void Initialize()
    {
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked; // If showCursor true .none else .Locked
        Cursor.visible = showCursor;
    }

    // Start is called before the first frame update
    void Start()
    {
        Initialize();
        // Initialize target position
        targetPosition = target.position + positionOffset;

        // Initialize looking direction 
        currentRotationX = target.rotation.eulerAngles.y + rotationOffset.y;
    }

    void Update()
    {
        if (lockCamera) return;

        if (rightClickMove)
        {
#if ENABLE_INPUT_SYSTEM
            if (controls.Orbit_Player.RightButton.IsPressed() && IsCursorOverGameWindow())
            {
                // Rotate the camera based on mouse input            
                currentRotationX += controls.Orbit_Player.Delta.ReadValue<Vector2>().x * sensitivityX;
                currentRotationY -= controls.Orbit_Player.Delta.ReadValue<Vector2>().y * sensitivityY;
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
            currentRotationX += controls.Orbit_Player.Delta.ReadValue<Vector2>().x * sensitivityX;
            currentRotationY -= controls.Orbit_Player.Delta.ReadValue<Vector2>().y * sensitivityY;
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
            distance += controls.Orbit_Player.ScrollWheel.ReadValue<Vector2>().y * scrollSpeed;
#else
            distance -= Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
#endif
            distance = Mathf.Clamp(distance, 1f, Mathf.Infinity); // Clamp distance
        }
    }

    private void LateUpdate()
    {
        if (lockCamera || target == null)
        {
            return;
        }

        // Calculate the rotation and position
        // Compute the base rotation based on absoluteUp
        Quaternion baseRotation = absoluteUp ? Quaternion.Euler(0, currentRotationX, 0) : target.rotation * Quaternion.Euler(0, currentRotationX, 0);
        Quaternion rotation = baseRotation * Quaternion.Euler(currentRotationY, 0, 0);

        // Apply targetOffset relative to the target's local orientation when absoluteUp is false
        Vector3 effectiveOffset = absoluteUp ? positionOffset : target.rotation * positionOffset;

        Vector3 direction = rotation * Vector3.forward * -distance;
        Vector3 desiredPosition = target.position + effectiveOffset + direction;

        // Handle camera collision
        RaycastHit hit;
        if (Physics.Linecast(target.position + positionOffset, desiredPosition, out hit, collisionLayer))
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

    private bool IsCursorOverGameWindow()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        // Check cursor position using the Legacy Input System or New Input System
        Vector2 mousePosition = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        // New Input System
        mousePosition = controls.Orbit_Player.Position.ReadValue<Vector2>();
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

    public void ResetCamera()
    {
        currentRotationX = target.rotation.eulerAngles.y + rotationOffset.y;
    }

    private void OnButtonPressed(InputAction.CallbackContext context)
    {
        // You can start rotating here if desired
    }

    private void OnButtonReleased(InputAction.CallbackContext context)
    {
        onClickMoveEnded.Invoke();
    }
}
