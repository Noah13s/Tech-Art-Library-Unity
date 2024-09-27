using UnityEngine;
using UnityEngine.Events; // For UnityEvent

public class GrabableObject : InteractiveObject // Inherit from InteractiveObject
{
    [Header("Grab Settings")] // Changed from Interaction Settings to Grab Settings
    [SerializeField] private float grabDistance = 1.5f; // Distance from the player to hold the object

    private bool isBeingHeld = false;  // Track whether the object is currently being held
    private Transform playerTransform;  // Reference to the player's transform

    // UnityEvent to be invoked when the object is grabbed
    public UnityEvent onGrab;

    private void Start()
    {
        // Find the player transform (assuming the player has a tag "Player")
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform; // Use null-conditional operator
        if (playerTransform == null)
        {
            Debug.LogError("Player transform not found. Make sure there is a GameObject with the 'Player' tag in the scene.");
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

        // Disable physics interaction while being held
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Prevents physics from affecting the object
        }

        // Parent the object to the player
        if (playerTransform != null)
        {
            transform.SetParent(playerTransform); // Parent the object to the player
        }

        // Invoke the UnityEvent when the object is grabbed
        onGrab?.Invoke();
    }

    // Method to release the object
    private void Release()
    {
        isBeingHeld = false;
        Debug.Log("Releasing " + gameObject.name);

        // Restore physics interaction when released
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false; // Allow physics to affect the object again
            rb.velocity = Vector3.zero; // Reset velocity to prevent sudden movement
        }

        // Detach the object from the player
        transform.SetParent(null); // Remove parenting to allow independent movement
    }

    private void Update()
    {
        // Find the player transform again if it's null
        if (playerTransform == null)
        {
            playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (playerTransform == null)
            {
                Debug.LogError("Player transform not found. Make sure there is a GameObject with the 'Player' tag in the scene.");
                return; // Exit if playerTransform is still null
            }
        }

        // Update the position of the object while being held
        if (isBeingHeld)
        {
            // Continuously position the object in front of the player at a specified distance
            transform.position = playerTransform.position + playerTransform.forward * grabDistance;

            // Optionally, adjust the rotation to align with the player's view
            transform.rotation = playerTransform.rotation; // Align object rotation with player's rotation
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
