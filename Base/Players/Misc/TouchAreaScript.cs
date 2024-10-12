using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

public class UITouchControl : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private TouchInputs touchInputs;          // New Input System touch controls
#endif
    private Vector2 lastTouchPosition;        // Last recorded touch position
    private Vector2 currentTouchPosition;     // Current touch position
    private bool isTouching = false;          // Flag to check if touch is active
    private Vector2 touchDelta;               // Change in touch position

    void Start()
    {
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

            if (!IsPointerOverUI(touchPosition) && (isTouching || IsTouchWithinBounds(touchPosition)))
            {
                MoveTouch(touchPosition);
            }
        } else {
            isTouching = false;
        }
    }
#endif

    // Handle legacy input system (Unity's old Input Manager)
    private void HandleLegacyInputSystem()
    {
        if (Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;

            if (!IsPointerOverUI(mousePosition) && (isTouching || IsTouchWithinBounds(mousePosition)))
            {
                MoveTouch(mousePosition);
            }
        }
    }

    // Check if the touch/mouse is within the bounds (can customize for UI elements)
    private bool IsTouchWithinBounds(Vector2 position)
    {
        // Customize this based on your specific needs for the touch area
        return true; // Always return true if no specific bounds are required
    }

    // Check if the pointer is over any UI element
    private bool IsPointerOverUI(Vector2 screenPosition)
    {
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        return results.Count > 0;
    }

    // Move touch logic (similar to joystick move logic)
    private void MoveTouch(Vector2 position)
    {
        isTouching = true;

        // Calculate touch delta
        currentTouchPosition = position;
        touchDelta = currentTouchPosition - lastTouchPosition;
        lastTouchPosition = currentTouchPosition;
    }

    // Called when the user touches or clicks the UI
    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPointerOverUI(eventData.position)) // Check if pointer is over UI
        {
            OnDrag(eventData); // Start dragging immediately if not over UI
        }
    }

    // Called when the user drags the touch/mouse
    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition = eventData.position;
        if (!IsPointerOverUI(touchPosition) && IsTouchWithinBounds(touchPosition))
        {
            MoveTouch(touchPosition);
        }
    }

    // Called when the user releases the touch/mouse
    public void OnPointerUp(PointerEventData eventData)
    {
        // Reset touch delta and touch state
        touchDelta = Vector2.zero;
        isTouching = false;
    }

    // Get touch delta
    public Vector2 GetTouchDelta()
    {
        return isTouching ? touchDelta : Vector2.zero;
    }

    // Check if currently touching
    public bool IsTouching()
    {
        return isTouching;
    }

    void OnDisable()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        touchInputs.Touch.Disable();
#endif
    }
}
