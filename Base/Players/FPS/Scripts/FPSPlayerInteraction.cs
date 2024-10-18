using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events; // Include this for UnityEvent support
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // New Input System namespace
#endif

public class FPSPlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionDistance = 3.0f;  // How far the player can reach to interact
    [SerializeField] private LayerMask interactionLayer = default;  // Layer to detect interactable objects

    [Header("Debug Settings")]
    [SerializeField] private bool debugMode = false;  // Toggle for enabling/disabling debug mode

    private Camera playerCamera;
    private bool isInteracting = false;  // Track interaction to change ray color
    private bool isInteractionAvailable = false;  // Track interaction availability state

    // Unity Events for interaction
    public UnityEvent onInteractionKeyPressed;   // Invoked when the interaction key is pressed
    public UnityEvent onInteractionKeyReleased;   // Invoked when the interaction key is released
    public UnityEvent onInteractionPossible;      // Invoked when an interaction is possible
    public UnityEvent onInteractionUnavailable;    // Invoked when no interaction is available

#if ENABLE_INPUT_SYSTEM
    private InputAction interactAction;
    private FPS_Controls controls;
#endif

    private void Start()
    {
        playerCamera = Camera.main;

#if ENABLE_INPUT_SYSTEM
        // Initialize input actions for new Input System
        controls = new FPS_Controls();
        controls.Enable();
        interactAction = controls.FPS_Action_Map.Interact; // Using the Interact action from the input map
#endif
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        HandleNewInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
        HandleLegacyInput();
#endif

        CheckInteractionAvailability(); // Check for interaction availability continuously

        if (debugMode)
        {
            DebugRay();
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void HandleNewInputSystem()
    {
        if (interactAction.triggered)
        {
            isInteracting = true;
            TryInteract();
            onInteractionKeyPressed?.Invoke();  // Invoke the event when the interaction key is pressed
        }
        else if (interactAction.WasReleasedThisFrame()) // Check if the interaction key was released
        {
            isInteracting = false;
            onInteractionKeyReleased?.Invoke(); // Invoke the event when the interaction key is released
            CheckInteractionAvailability(); // Recheck interaction availability on key release
        }
    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER 
    private void HandleLegacyInput()
    {
#if !UNITY_ANDROID
        if (Input.GetButtonDown("Fire1"))  // Left mouse button as the interaction key
        {
            isInteracting = true;
            TryInteract();
            onInteractionKeyPressed?.Invoke();  // Invoke the event when the interaction key is pressed
        }
        else if (Input.GetButtonUp("Fire1")) // Check if the interaction key was released
        {
            isInteracting = false;
            onInteractionKeyReleased?.Invoke(); // Invoke the event when the interaction key is released
        }
#endif
    }
#endif

    public void ExternalInteractionCall()
    {
        TryInteract();
    }

    // Perform a raycast to check if the player is looking at an interactive object
    private void TryInteract()
    {
        RaycastHit hit;

        // Raycast from the center of the screen (camera forward)
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, interactionLayer))
        {
            // Check if the object hit has an InteractiveObject component
            InteractiveObject interactiveObject = hit.collider.GetComponent<InteractiveObject>();
            if (interactiveObject != null && interactiveObject.IsActive) // Check if the script is active
            {
                interactiveObject.Interact();  // Call the object's Interact method
            }
        }
    }

    // Check for interaction availability continuously
    private void CheckInteractionAvailability()
    {
        RaycastHit hit;
        bool currentAvailability = Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out hit, interactionDistance, interactionLayer);

        // If interaction is available and the hit object contains InteractiveObject
        if (currentAvailability)
        {
            InteractiveObject interactiveObject = hit.collider.GetComponent<InteractiveObject>();
            if (interactiveObject != null && interactiveObject.IsActive) // Check if the script is active
            {
                if (true) // Used to only invoke if the state has changed but removed that 
                {
                    isInteractionAvailable = true;
                    onInteractionPossible?.Invoke(); // Invoke when interaction becomes possible
                }
            }
            else if (isInteractionAvailable) // Check if we were previously available
            {
                isInteractionAvailable = false; // Set to unavailable
                onInteractionUnavailable?.Invoke(); // Invoke when interaction becomes unavailable
            }
        }
        else if (isInteractionAvailable) // If no object is hit and was previously available
        {
            isInteractionAvailable = false;
            onInteractionUnavailable?.Invoke(); // Invoke when interaction becomes unavailable
        }
    }

    // Draw the debug ray
    private void DebugRay()
    {
        // Define the ray color based on whether the player is interacting or not
        Color rayColor = isInteracting ? Color.green : Color.red;

        // Get the start and end positions of the ray
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward * interactionDistance;

        // Draw the ray in the Scene view (visible during runtime if gizmos are enabled)
        Debug.DrawRay(rayOrigin, rayDirection, rayColor);
    }
}
