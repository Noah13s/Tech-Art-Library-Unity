using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class JoystickControl : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform outerJoystick;   // Static outer joystick (RectTransform)
    public RectTransform innerJoystick;   // Movable inner joystick (RectTransform)
    public float maxDistance = 100f;      // Maximum distance the inner joystick can move from the center

    private Vector2 inputVector;          // Normalized input vector from the joystick
    private Vector2 joystickCenter;       // Center position of the outer joystick
    private float outerJoystickRadius;    // Radius of the outer joystick
    private bool isJoystickActive = false; // Flag to track if joystick is being used

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private TouchInputs touchInputs;      // New Input System touch controls
#endif

    void Start()
    {
        // Store the center of the outer joystick
        joystickCenter = outerJoystick.position;

        // Calculate the radius of the outer joystick based on its size
        outerJoystickRadius = outerJoystick.sizeDelta.x / 2f;

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        touchInputs = new TouchInputs();
        touchInputs.Touch.Enable();
#endif
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        HandleNewInputSystem();
#else
        HandleLegacyInputSystem();
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    // Handle new input system (Unity's Input System Package)
    private void HandleNewInputSystem()
    {
        var touchPress = touchInputs.Touch.Press.ReadValue<float>();
        if (touchPress > 0)
        {
            var touchPosition = touchInputs.Touch.Position.ReadValue<Vector2>();
            if (isJoystickActive || IsWithinJoystickBounds(touchPosition)) // Allow tracking once activated
            {
                MoveJoystick(touchPosition);
            }
        }
    }
#endif

    // Handle legacy input system (Unity's old Input Manager)
    private void HandleLegacyInputSystem()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            if (isJoystickActive || IsWithinJoystickBounds(mousePosition)) // Allow tracking once activated
            {
                MoveJoystick(mousePosition);
            }
        }
    }

    // Check if the touch/mouse position is within the outer joystick bounds
    private bool IsWithinJoystickBounds(Vector2 position)
    {
        // Calculate the distance from the touch position to the joystick center
        float distanceFromCenter = Vector2.Distance(position, joystickCenter);

        // Check if the distance is within the outer joystick's radius
        return distanceFromCenter <= outerJoystickRadius;
    }

    // Joystick movement logic
    private void MoveJoystick(Vector2 position)
    {
        // Set joystick active flag
        isJoystickActive = true;

        // Calculate direction from the center of the joystick to the touch position
        Vector2 direction = position - joystickCenter;

        // Clamp the distance to within the max radius of the outer joystick
        float distance = Mathf.Min(direction.magnitude, maxDistance);

        // Normalize the direction and multiply by the clamped distance
        inputVector = direction.normalized * (distance / maxDistance);

        // Move the inner joystick within the bounds of the outer joystick
        innerJoystick.position = joystickCenter + direction.normalized * distance;
    }

    // Called when the user touches or clicks the joystick
    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData); // Start dragging immediately
    }

    // Called when the user drags the joystick
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition = eventData.position;
        if (IsWithinJoystickBounds(touchPosition)) // Start tracking if within bounds
        {
            MoveJoystick(touchPosition);
        }
    }

    // Called when the user releases the joystick
    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset the inner joystick back to the center of the outer joystick
        innerJoystick.position = joystickCenter;

        // Reset input vector to zero
        inputVector = Vector2.zero;

        // Reset joystick active flag
        isJoystickActive = false;
    }

    // Get the horizontal input (-1 to 1)
    public float GetHorizontal()
    {
        return inputVector.x;
    }

    // Get the vertical input (-1 to 1)
    public float GetVertical()
    {
        return inputVector.y;
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        touchInputs.Touch.Disable();
#endif
    }
}
