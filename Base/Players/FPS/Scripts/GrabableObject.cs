using UnityEngine;
using UnityEngine.Events;

public class GrabableObject : InteractiveObject // Inherit from InteractiveObject
{
    [Header("Grab Settings")]
    [SerializeField] private float grabDistance = 1.5f; // Distance from the player to hold the object
    [SerializeField] private float moveSpeed = 10f; // Speed at which the object moves toward the target position
    [SerializeField] private bool usePhysicsWhileGrabbed = true; // Toggle between physics-based and direct-transform movement

    [Header("Axis Lock Settings")]
    [SerializeField] private bool lockPositionX = false;
    [SerializeField] private bool lockPositionY = false;
    [SerializeField] private bool lockPositionZ = false;
    [SerializeField] private bool lockRotationX = false;
    [SerializeField] private bool lockRotationY = false;
    [SerializeField] private bool lockRotationZ = false;

    private bool isBeingHeld = false;  // Track whether the object is currently being held
    private Transform playerTransform;  // Reference to the player's transform
    private Transform playerCameraTransform; // Reference to the player's camera transform
    private Rigidbody rb; // Reference to the Rigidbody component
    private Vector3 targetPosition; // Target position to move the object toward
    private bool originalUseGravity; // Store the original gravity setting

    // UnityEvent to be invoked when the object is grabbed
    public UnityEvent onGrab;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("No Rigidbody found on " + gameObject.name + ". Please add a Rigidbody component.");
            return;
        }

        // Store the original gravity setting of the object
        originalUseGravity = rb.useGravity;

        // Enable interpolation for smoother physics movement
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Find the player transform and camera transform (assuming the player has a tag "Player")
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        playerCameraTransform = Camera.main?.transform; // Get the main camera's transform
        if (playerTransform == null)
        {
            Debug.LogError("Player transform not found. Make sure there is a GameObject with the 'Player' tag in the scene.");
        }
        if (playerCameraTransform == null)
        {
            Debug.LogError("Camera transform not found. Make sure there is a Camera tagged as 'MainCamera' in the scene.");
        }
    }

    // Override the Interact method from InteractiveObject
    public override void Interact()
    {
        if (!IsActive)  // Use the inherited property IsActive
        {
            Debug.Log("Interaction is disabled for " + gameObject.name);
            return; // Exit if the object is not active
        }

        if (!isBeingHeld)
        {
            Grab(); // Call the Grab method when interacted with
        }
        else
        {
            Release(); // Call the Release method if already being held
        }
    }

    // Method to grab the object
    private void Grab()
    {
        isBeingHeld = true;
        Debug.Log("Grabbing " + gameObject.name);

        if (usePhysicsWhileGrabbed)
        {
            // Disable gravity and optionally physics while being held
            if (rb != null)
            {
                originalUseGravity = rb.useGravity; // Store the original gravity state
                rb.useGravity = false; // Disable gravity while being held
                rb.isKinematic = false; // We'll control its movement via MovePosition
            }
        }
        else
        {
            // Parent the object to the player (old system)
            if (playerTransform != null)
            {
                transform.SetParent(playerTransform); // Parent the object to the player
            }

            // Disable physics (old system)
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        // Invoke the UnityEvent when the object is grabbed
        onGrab?.Invoke();
    }

    // Method to release the object
    private void Release()
    {
        isBeingHeld = false;
        Debug.Log("Releasing " + gameObject.name);

        if (usePhysicsWhileGrabbed)
        {
            // Restore gravity and physics interaction when released
            if (rb != null)
            {
                rb.useGravity = originalUseGravity; // Restore original gravity state
                rb.isKinematic = false; // Allow physics to affect the object again
                rb.velocity = Vector3.zero; // Reset velocity to prevent sudden movement
            }
        }
        else
        {
            // Detach the object from the player (old system)
            transform.SetParent(null); // Remove parenting to allow independent movement

            // Restore physics (old system)
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }

    private void Update()
    {
        // Update the position of the object while being held
        if (isBeingHeld)
        {
            // Calculate the target position in front of the camera
            if (playerCameraTransform != null)
            {
                targetPosition = playerCameraTransform.position + playerCameraTransform.forward * grabDistance;

                // Adjust target position based on axis lock settings
                if (lockPositionX) targetPosition.x = transform.position.x;
                if (lockPositionY) targetPosition.y = transform.position.y;
                if (lockPositionZ) targetPosition.z = transform.position.z;

                if (usePhysicsWhileGrabbed && rb != null)
                {
                    // Smoothly interpolate the object's position toward the target using Rigidbody.MovePosition
                    Vector3 newPosition = Vector3.Lerp(rb.position, targetPosition, moveSpeed * Time.deltaTime);
                    rb.MovePosition(newPosition);
                }
                else if (!usePhysicsWhileGrabbed)
                {
                    // Old system: Directly set the transform position
                    transform.position = targetPosition;

                    // Adjust rotation based on axis lock settings
                    Vector3 newRotation = playerCameraTransform.rotation.eulerAngles;
                    if (lockRotationX) newRotation.x = transform.rotation.eulerAngles.x;
                    if (lockRotationY) newRotation.y = transform.rotation.eulerAngles.y;
                    if (lockRotationZ) newRotation.z = transform.rotation.eulerAngles.z;
                    transform.rotation = Quaternion.Euler(newRotation);
                }
            }
        }
    }

    // Property to get or set the isActive state from the base class
    public new bool IsActive // Use new to hide the base class property
    {
        get { return base.IsActive; }
        set
        {
            base.IsActive = value;
            string status = IsActive ? "enabled" : "disabled";
            Debug.Log(gameObject.name + " interaction " + status + ".");
        }
    }
}
