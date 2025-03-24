using UnityEngine;
using UnityEngine.InputSystem;

public class TopDownPlayerImproved : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] Transform origin;
    [SerializeField] Transform target;

    [Header("Controls")]
    [SerializeField] float sensitivity = 5.0f;
    [SerializeField] bool canMove = true;
    [SerializeField] bool canRotate = false;
    private enum RotatationTarget {
        originBased = 0, clickBased
    }
    [ConditionalVisibility("canRotate")]
    [SerializeField] RotatationTarget rotateMode;

    [Header("Camera Parameters")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 10.0f;
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float minZoom = 5.0f;
    [SerializeField] private float maxZoom = 20.0f;
    [SerializeField] private bool curvedZoom = true;

    [Header("local Properties")]
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private bool isRotating = false;
    private float currentZoom;

#if ENABLE_INPUT_SYSTEM
    private InputAction mouseInputAction;
    private InputAction moveAction;
#endif

    void Start()
    {
        // Setup camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
#if ENABLE_INPUT_SYSTEM
        // Example: initializing your InputAction asset (replace with your actual setup)
        var controls = new InputSystem_Actions();
        controls.Enable();
        mouseInputAction = controls.TopDown_Player.MouseDrag;
        moveAction = controls.TopDown_Player.Move;
#endif
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleZoom();
    }

    void HandleMovement()
    {
        // Left click drag for movement
        if (Input.GetMouseButtonDown(0))
        {
            lastMousePosition = Input.mousePosition;
            isDragging = true;
        }

        if (Input.GetMouseButton(0) && isDragging && canMove)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            Vector3 cameraForward = mainCamera.transform.forward + mainCamera.transform.up;
            cameraForward.y = 0; // Ignore vertical component
            Vector3 cameraRight = mainCamera.transform.right;

            // Convert delta into world space movement on the XZ plane.
            Vector3 move = (cameraForward * delta.y + cameraRight *delta.x) * sensitivity * Time.deltaTime;
            transform.Translate(move, Space.World);
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    void HandleRotation()
    {
        // Right click drag for rotation
        if (Input.GetMouseButtonDown(1))
        {
            lastMousePosition = Input.mousePosition;
            isRotating = true;
            // For clickBased, update target based on the raycast hit
            if (rotateMode == RotatationTarget.clickBased)
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    target.position = hit.point;
                }
            }
        }

        if (Input.GetMouseButton(1) && isRotating && canRotate)
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float angle = delta.x * sensitivity * Time.deltaTime;
            if (rotateMode == RotatationTarget.originBased)
            {
                // Rotate around the origin transform
                mainCamera.transform.RotateAround(origin.position, Vector3.up, angle);
            }
            else // clickBased
            {
                // Rotate around the target transform
                mainCamera.transform.RotateAround(target.position, Vector3.up, angle);
            }
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            if (mainCamera.orthographic)
            {
                float zoomAdjustment = curvedZoom ? scroll * zoomSpeed * zoomSpeed : scroll * zoomSpeed;
                currentZoom -= zoomAdjustment;
                currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
                mainCamera.orthographicSize = currentZoom;
            }
            else
            {
                // For perspective cameras, you could adjust the field of view or move the camera along the Y-axis.
                mainCamera.transform.position += mainCamera.transform.forward * scroll * zoomSpeed;
            }
        }
    }
}