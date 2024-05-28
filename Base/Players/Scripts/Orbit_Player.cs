using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // The target object to orbit around
    public Vector3 targetOffset; // Offset for the target position
    public float distance = 10f; // The distance from the target
    public float sensitivityX = 2f; // Mouse X sensitivity
    public float sensitivityY = 2f; // Mouse Y sensitivity
    public bool allowScrolling = true; // Whether scrolling is allowed
    public float scrollSpeed = 2f; // Scrollwheel sensitivity
    public bool showCursor = true; // Whether to show the cursor
    public float cameraLag = 5f; // Camera lag for target position
    public LayerMask collisionLayer; // Layer mask for collision detection
    public float collisionOffset = 0.2f; // Offset for collision detection

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private Vector3 targetPosition;

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
    }

    void Update()
    {
        // Rotate the camera based on mouse input
        currentRotationX += Input.GetAxis("Mouse X") * sensitivityX;
        currentRotationY -= Input.GetAxis("Mouse Y") * sensitivityY;
        currentRotationY = Mathf.Clamp(currentRotationY, -90f, 90f); // Clamp Y rotation

        // Zoom in and out using scrollwheel if scrolling is allowed
        if (allowScrolling)
        {
            distance += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
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
