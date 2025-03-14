using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TopDownMousePlayer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float sensitivity = 5.0f;
    [SerializeField] private float decelerationFactor = 10.0f;

    [Header("Camera Settings")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cameraHeight = 10.0f;
    [SerializeField] private Vector3 cameraOffset = new Vector3(0, 10, -10);
    [SerializeField] private float zoomSpeed = 2.0f;
    [SerializeField] private float minZoom = 5.0f;
    [SerializeField] private float maxZoom = 20.0f;
    [SerializeField] private bool curvedZoom = true;
    [SerializeField] private bool canRotate = false;

    [Header("Cursor Settings")]
    [SerializeField] private Texture2D defaultCursor;
    [SerializeField] private Texture2D grabbingCursor;
    [SerializeField] private Texture2D rotatingCursor;

    private Vector3 moveDirection = Vector3.zero;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private bool isRotating = false;
    private float currentZoom;

#if ENABLE_INPUT_SYSTEM
    private InputAction mouseInputAction;
#endif

    private void Start()
    {
        // Setup camera
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        currentZoom = cameraOffset.magnitude;
        UpdateCameraPosition();

        // Set default cursor
        if (defaultCursor != null)
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }

#if ENABLE_INPUT_SYSTEM
        // Assuming `TopDownControls` is your InputAction asset with a "MouseDrag" action
        var controls = new InputSystem_Actions();
        controls.Enable();
        mouseInputAction = controls.TopDown_Player.MouseDrag;
#endif
    }

    private void Update()
    {
        HandleInput();
        HandleZoom();

        // Smoothly decelerate if no input is detected
        if (!isDragging && moveDirection.magnitude > 0)
        {
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, decelerationFactor * Time.deltaTime);
        }

        // Move the character
        transform.Translate(moveDirection * Time.deltaTime * sensitivity, Space.World);
        //transform.RotateAround(Vector3.zero , Vector3.up, moveDirection.x);

        // Update camera position
        if (mainCamera != null)
        {
            UpdateCameraPosition();
        }
    }

    private void HandleInput()
    {
        Vector3 inputDelta = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
        // New Input System: Read mouse input delta
        if (Mouse.current.leftButton.isPressed)
        {
            isDragging = true;
            Vector2 mouseDelta = mouseInputAction.ReadValue<Vector2>();
            inputDelta = new Vector3(-mouseDelta.x, 0, -mouseDelta.y); // Inverted input
        }
        else
        {
            isDragging = false;
        }
        if (Mouse.current.rightButton.isPressed && canRotate)
        {
            isRotating = true;
            Vector2 mouseDelta = mouseInputAction.ReadValue<Vector2>();
            inputDelta = new Vector3(-mouseDelta.x, 0, -mouseDelta.y); // Inverted input
        }
        else
        {
            isRotating = false;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        // Legacy Input Manager: Read mouse input delta
        if (Input.GetMouseButton(0))
        {
            isDragging = true;
            Vector3 currentMousePosition = Input.mousePosition;

            if (lastMousePosition != Vector3.zero)
            {
                Vector3 mouseDelta = currentMousePosition - lastMousePosition;
                inputDelta = new Vector3(-mouseDelta.x, 0, -mouseDelta.y); // Inverted input
            }

            lastMousePosition = currentMousePosition;
        }
        else
        {
            isDragging = false;
            lastMousePosition = Vector3.zero;
        }
#endif

            // Update cursor based on dragging state
            UpdateCursor();

        // Convert screen-space input delta to world-space movement
        if (mainCamera != null)
        {
            Vector3 cameraForward = mainCamera.transform.forward + mainCamera.transform.up;
            cameraForward.y = 0; // Ignore vertical component
            Vector3 cameraRight = mainCamera.transform.right;

            moveDirection = (cameraForward * inputDelta.z + cameraRight * inputDelta.x);
        }
    }

    private void HandleZoom()
    {
        float scrollInput = 0.0f;

#if ENABLE_INPUT_SYSTEM
        // New Input System: Read mouse scroll input
        if (Mouse.current != null)
        {
            scrollInput = Mouse.current.scroll.ReadValue().y;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
    // Legacy Input Manager: Read mouse scroll input
    scrollInput = Input.mouseScrollDelta.y;
#endif

        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            currentZoom -= scrollInput * zoomSpeed * Time.deltaTime;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            UpdateCameraPosition();
        }
    }


    private void UpdateCameraPosition()
    {
        if (curvedZoom)
        {
            // Calculate a tilt angle based on the zoom level
            float tiltAngle = Mathf.Lerp(0f, 45f, (currentZoom - minZoom) / (maxZoom - minZoom)); // Adjust angles as needed
            Quaternion tiltRotation = Quaternion.Euler(tiltAngle, 0, 0);

            // Update camera offset with the curved zoom effect
            cameraOffset = Vector3.Lerp(new Vector3(0, minZoom, -minZoom), new Vector3(0, maxZoom, -maxZoom), (currentZoom - minZoom) / (maxZoom - minZoom));
            Vector3 adjustedOffset = tiltRotation * cameraOffset;

            // Set the camera position and rotation
            mainCamera.transform.position = transform.position + adjustedOffset;
            mainCamera.transform.LookAt(transform);
        }
        else
        {
            // Standard zoom logic
            cameraOffset = cameraOffset.normalized * currentZoom;
            mainCamera.transform.position = transform.position + cameraOffset;
            mainCamera.transform.LookAt(transform);
        }
    }


    private void UpdateCursor()
    {
        if (isDragging)
        {
            if (grabbingCursor != null)
            {
                Cursor.SetCursor(grabbingCursor, Vector2.zero, CursorMode.Auto);
            }
        }
        else
        {
            if (defaultCursor != null)
            {
                Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
            }
        }
    }
}
