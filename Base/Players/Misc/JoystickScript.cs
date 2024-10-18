using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
#endif

public class JoystickControl : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    public RectTransform outerJoystick;   // Static outer joystick (RectTransform)
    public RectTransform innerJoystick;   // Movable inner joystick (RectTransform)
    public float maxDistance = 100f;      // Maximum distance the inner joystick can move from the center

    private Vector2 inputVector;          // Normalized input vector from the joystick
    private Vector2 joystickCenter;       // Center position of the outer joystick
    private bool isJoystickActive = false; // Flag to track if joystick is being used
    private int dragTouchId = -1;        // Track which touch ID is dragging the joystick

    public UnityEvent onDragStart;        // Event for when dragging starts
    public UnityEvent onDragging;         // Event for during dragging
    public UnityEvent onDragEnd;          // Event for when dragging ends

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_EDITOR
    [StyledString(12, 1, 1, 0)]
#endif
    [SerializeField]
    private string Warning = "In legacy input the finger ids include 0 (0=1finger, 1=2fingers...)";
#endif

    public int[] authorisedFingerIds;     // Array of authorized touch IDs

    void Start()
    {
        joystickCenter = outerJoystick.position; // Store the center of the outer joystick
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EnhancedTouchSupport.Enable();    // Enable Enhanced Touch Support
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
    private void HandleNewInputSystem()
    {
        ReadOnlyArray<UnityEngine.InputSystem.EnhancedTouch.Touch> activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        // Check if there are any active touches
        if (activeTouches.Count > 0)
        {
            for (int i = 0; i < activeTouches.Count; i++)
            {
                var touch = activeTouches[i];
                // Check if the current touch ID is authorized
                if (IsAuthorizedFinger(activeTouches.Count))
                {
                    if (isJoystickActive && touch.touchId == dragTouchId)
                    {
                        // Continue moving the joystick if it's active
                        MoveJoystick(touch.screenPosition);
                    }
                    else if (!isJoystickActive && IsWithinJoystickBounds(touch.screenPosition))
                    {
                        // Start dragging if within bounds
                        MoveJoystick(touch.screenPosition);
                        dragTouchId = touch.touchId; // Track the dragging touch ID
                        onDragStart?.Invoke(); // Trigger onDragStart event
                        isJoystickActive = true; // Mark joystick as active
                    }
                }
            }
        }
        else
        {
            // Reset joystick position when no active touches
            if (isJoystickActive) // Only reset if joystick was active
            {
                ResetJoystick(); // Call reset when no active touches
            }
        }
    }
#endif

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
    private void HandleLegacyInputSystem()
    {
        if (Input.touchCount > 0) // Check if there are any active touches
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.touches[i];

                // Check if the current touch ID is authorized
                if (IsAuthorizedFinger(touch.fingerId))
                {
                    if (isJoystickActive && touch.fingerId == dragTouchId)
                    {
                        // Continue moving the joystick if it's active
                        MoveJoystick(touch.position);
                    }
                    else if (!isJoystickActive && IsWithinJoystickBounds(Input.mousePosition))
                    {
                        // Start dragging if within bounds
                        MoveJoystick(Input.mousePosition);
                        dragTouchId = touch.fingerId; // Track the dragging touch ID
                        onDragStart?.Invoke(); // Trigger onDragStart event
                        isJoystickActive = true; // Mark joystick as active
                    }
                }
            } 
        } 
        else
        {
            // Reset joystick position when no active touches
            if (isJoystickActive) // Only reset if joystick was active
            {
                ResetJoystick(); // Call reset when no active touches
            }
        }
    }
#endif

    private bool IsAuthorizedFinger(int touchId)
    {
        if (authorisedFingerIds == null || authorisedFingerIds.Length == 0)
        {
            return true; // Authorize any finger if no specific IDs are provided
        }

        // Check if the touch ID exists in the authorized finger IDs array
        foreach (int authorizedId in authorisedFingerIds)
        {
            if (authorizedId == touchId)
            {
                return true; // Return true if the touch ID is authorized
            }
        }
        return false; // Return false if not authorized
    }

    private void MoveJoystick(Vector2 position)
    {
        Vector2 direction = position - joystickCenter; // Calculate direction
        float distance = Mathf.Min(direction.magnitude, maxDistance); // Clamp distance
        inputVector = direction.normalized * (distance / maxDistance); // Normalize and scale input vector
        innerJoystick.position = joystickCenter + direction.normalized * distance; // Move the inner joystick

        onDragging?.Invoke(); // Trigger onDragging event during movement
    }

    private bool IsWithinJoystickBounds(Vector2 position)
    {
        float distanceFromCenter = Vector2.Distance(position, joystickCenter);
        return distanceFromCenter <= (outerJoystick.sizeDelta.x / 2f);
    }

    private void ResetJoystick()
    {
        innerJoystick.position = joystickCenter; // Reset inner joystick position
        inputVector = Vector2.zero;              // Reset input vector
        isJoystickActive = false;                // Mark joystick as inactive
        dragTouchId = -1;                        // Reset drag touch ID
        onDragEnd?.Invoke();                     // Trigger onDragEnd event when released
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Check if the pointer is an authorized finger
        if (IsAuthorizedFinger(eventData.pointerId))
        {
            MoveJoystick(eventData.position); // Start moving the joystick immediately
            dragTouchId = eventData.pointerId; // Track the dragging touch ID
            isJoystickActive = true; // Mark joystick as active
            onDragStart?.Invoke(); // Trigger onDragStart event
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (isJoystickActive)
        {
            MoveJoystick(eventData.position); // Update joystick position while dragging
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (isJoystickActive) // Ensure it's active before resetting
        {
            ResetJoystick(); // Reset the joystick position when the pointer is released
        }
    }

    public float GetHorizontal() => inputVector.x; // Get horizontal input
    public float GetVertical() => inputVector.y;   // Get vertical input
}
