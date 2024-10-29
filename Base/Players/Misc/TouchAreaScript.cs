using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.InputSystem.Utilities;
#endif

public class UITouchControl : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private Vector2 lastTouchPosition;        // Last recorded touch position
    private Vector2 currentTouchPosition;     // Current touch position
    private bool isTouching = false;          // Flag to check if touch is active
    private Vector2 touchDelta;               // Change in touch position

    public UnityEvent onTouchStart;        // Event for when dragging starts
    public UnityEvent onTouching;         // Event for during dragging
    public UnityEvent onTouchEnd;          // Event for when dragging ends

#if !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
#if UNITY_EDITOR
    [StyledString(12, 1, 1, 0)]
#endif
    [SerializeField]
    private string Warning = "In legacy input the finger ids include 0 (0=1finger, 1=2fingers...)";
#endif
    [SerializeField]
    public List<int> authorisedFingerIds;

    private int activeFingerId = -1;      // Track the active touch finger ID


    void Start()
    {
        if (authorisedFingerIds == null)
        {
            authorisedFingerIds = new List<int>();
        }
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        EnhancedTouchSupport.Enable();    // Enable Enhanced Touch Support
#endif
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        HandleNewInputSystem();
#elif !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
        HandleLegacyInputSystem();
#endif
    }

#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
    private void HandleNewInputSystem()
    {
        ReadOnlyArray<UnityEngine.InputSystem.EnhancedTouch.Touch> activeTouches = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches;

        if (activeTouches.Count > 0)
        {

            for (int i = 0; i < activeTouches.Count; i++)
            {
                var touch = activeTouches[i];
                if ((activeFingerId == -1 || activeFingerId == touch.touchId) && IsAuthorizedFinger(activeTouches.Count) && !IsPointerOverUI(touch.screenPosition))
                {
                    if (!isTouching)
                    {
                        activeFingerId = touch.touchId;
                    }
                    MoveTouch(touch.screenPosition);
                }
            }

            // Detect if the active finger was released
            bool activeFingerReleased = true;
            for (int i = 0; i < activeTouches.Count; i++)
            {
                if (activeTouches[i].touchId == activeFingerId && activeTouches[i].phase != UnityEngine.InputSystem.TouchPhase.Ended && activeTouches[i].phase != UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    activeFingerReleased = false;
                    break;
                }
            }

            if (activeFingerReleased && isTouching)
            {
                ResetTouch();
            }
        }
        else
        {
            if (isTouching) // Only reset if a touch was happening
            {
                ResetTouch();
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
                Debug.Log(IsAuthorizedFinger(touch.fingerId));
                // Check if the current touch ID is authorized
                if ((activeFingerId == -1 || activeFingerId == touch.fingerId) && IsAuthorizedFinger(touch.fingerId) && !IsPointerOverUI(touch.position))
                {
                    if (!isTouching)
                    {
                        activeFingerId = touch.fingerId;
                    }
                    MoveTouch(touch.position);
                }
            }
            // Detect if the active finger was released
            bool activeFingerReleased = true;
            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.touches[i].fingerId == activeFingerId && Input.touches[i].phase != UnityEngine.TouchPhase.Ended && Input.touches[i].phase != UnityEngine.TouchPhase.Canceled)
                {
                    activeFingerReleased = false;
                    break;
                }
            }

            if (activeFingerReleased && isTouching)
            {
                ResetTouch();
            }
        }
        else
        {
            // Reset joystick position when no active touches
            if (isTouching) // Only reset if joystick was active
            {
                ResetTouch(); // Call reset when no active touches
            }
        }
    
}
#endif

    private bool IsTouchWithinBounds(Vector2 position)
    {
        return true; // Always return true if no specific bounds are required
    }

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

    private void MoveTouch(Vector2 position)
    {
        if (!isTouching)
        {
            // Initialize the last touch position when dragging starts
            lastTouchPosition = position;
            touchDelta = Vector2.zero; // Ensure first delta is (0, 0)
            isTouching = true;

            // Fire the touch start event
            onTouchStart?.Invoke();
        }
        else
        {
            // Calculate touch delta only if already dragging
            currentTouchPosition = position;
            touchDelta = currentTouchPosition - lastTouchPosition;
            lastTouchPosition = currentTouchPosition;

            // Fire the touch dragging event
            onTouching?.Invoke();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IsPointerOverUI(eventData.position))
        {
            // Initialize the lastTouchPosition when the touch begins
            lastTouchPosition = eventData.position;
            isTouching = true;

            // Start dragging immediately if not over UI
            OnDrag(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 touchPosition = eventData.position;
        if (!IsPointerOverUI(touchPosition) && IsTouchWithinBounds(touchPosition))
        {
            MoveTouch(touchPosition);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData != null && eventData.pointerId == activeFingerId)
        {
            ResetTouch();
        }
    }

    private void ResetTouch()
    {
        touchDelta = Vector2.zero;
        isTouching = false;

        // Clear the last touch position to avoid incorrect delta calculations
        lastTouchPosition = Vector2.zero;
        currentTouchPosition = Vector2.zero;

        activeFingerId = -1; // Reset active finger ID

        // Optionally clear authorizedFingerIds after touch ends if needed
        // authorisedFingerIds.Clear();  // Uncomment this if you want to clear after every touch

        // Fire the touch end event
        onTouchEnd?.Invoke();
    }


    public Vector2 GetTouchDelta()
    {
        return isTouching ? touchDelta : Vector2.zero; // Return touch delta if touching
    }

    public bool IsTouching()
    {
        return isTouching; // Return the touch state
    }

    private bool IsAuthorizedFinger(int touchId)
    {
        if (authorisedFingerIds == null || authorisedFingerIds.Count == 0)
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

    public void AddAuthorizedFingerId(int fingerId)
    {
        // Check if the list is initialized
        if (authorisedFingerIds == null)
        {
            authorisedFingerIds = new List<int>();  // Initialize the list if null
        }

        // Add the finger ID only if it's not already in the list
        if (!authorisedFingerIds.Contains(fingerId))
        {
            authorisedFingerIds.Add(fingerId);
            Debug.Log("Added finger ID: " + fingerId);
        }
        else
        {
            Debug.Log("Finger ID already exists in the list.");
        }

        // Debug log to verify the list is updated
        Debug.Log("Updated authorisedFingerIds: " + string.Join(",", authorisedFingerIds));
    }

    public void RemoveAuthorizedFingerId(int fingerId)
    {
        // Check if the list is initialized and contains the finger ID
        if (authorisedFingerIds != null && authorisedFingerIds.Contains(fingerId))
        {
            authorisedFingerIds.Remove(fingerId);
            Debug.Log("Removed finger ID: " + fingerId);
        }
        else
        {
            Debug.Log("Finger ID not found in the list.");
        }

        // Debug log to verify the list is updated
        Debug.Log("Updated authorisedFingerIds: " + string.Join(",", authorisedFingerIds));
    }


}
