using System;
using UnityEngine;
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
    private PCInputs PCcontrols;
    private TouchInputs Touchcontrols;
#endif

    private void Awake()
    {

        // Initialize the new input system controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols = new PCInputs();
        Touchcontrols = new TouchInputs();
#endif
    }

    private void OnEnable()
    {
        // Enable the input controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols.Enable();
        Touchcontrols.Enable();
#endif
    }

    private void OnDisable()
    {
        // Disable the input controls
#if ENABLE_INPUT_SYSTEM
        PCcontrols.Disable();
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
            if (PCcontrols.Mouse.LeftButton.IsPressed())
            {
                // Rotate the camera based on mouse input            
                currentRotationX += PCcontrols.Mouse.Delta.ReadValue<Vector2>().x * sensitivityX;
                currentRotationY -= PCcontrols.Mouse.Delta.ReadValue<Vector2>().y * sensitivityY;
            }
#else
            if (Input.GetMouseButton(1))
            {
                // Rotate the camera based on mouse input            
                currentRotationX += Input.GetAxis("Mouse X") * sensitivityX;
                currentRotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
            }
#endif
        } else
        {
#if ENABLE_INPUT_SYSTEM
            // Rotate the camera based on mouse input            
            currentRotationX += PCcontrols.Mouse.Delta.ReadValue<Vector2>().x * sensitivityX;
            currentRotationY -= PCcontrols.Mouse.Delta.ReadValue<Vector2>().y * sensitivityY;
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
        if (allowScrolling)
        {
#if ENABLE_INPUT_SYSTEM
            distance += PCcontrols.Mouse.ScrollWheel.ReadValue<Vector2>().y * scrollSpeed;
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
}
