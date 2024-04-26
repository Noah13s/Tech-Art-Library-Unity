using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    public Transform target; // The target object to orbit around
    public float distance = 10f; // The distance from the target
    public float sensitivityX = 2f; // Mouse X sensitivity
    public float sensitivityY = 2f; // Mouse Y sensitivity
    public bool allowScrolling = true; // Whether scrolling is allowed
    public float scrollSpeed = 2f; // Scrollwheel sensitivity
    public bool showCursor = true; // Whether to show the cursor

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    void Initalise()
    {
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;// If showCursor true .none else .Locked
        Cursor.visible = showCursor;
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
        Vector3 desiredPosition = target.position + direction;

        // Set the camera's position and rotation
        transform.position = desiredPosition;
        transform.rotation = rotation;
    }
}
