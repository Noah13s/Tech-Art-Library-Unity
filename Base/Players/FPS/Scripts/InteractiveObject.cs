using UnityEngine;
using UnityEngine.Events; // For UnityEvent

public class InteractiveObject : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private bool isActive = true;  // Control whether the object is interactable

    // UnityEvent to be invoked when the object is interacted with
    public UnityEvent onInteract;

    // Property to get or set the isActive state
    public bool IsActive
    {
        get { return isActive; }
        set
        {
            isActive = value;
            string status = isActive ? "enabled" : "disabled";
            Debug.Log(gameObject.name + " interaction " + status + ".");
        }
    }

    // Base interaction method to be overridden by specific interactive objects
    public virtual void Interact()
    {
        if (!isActive)
        {
            Debug.Log("Interaction is disabled for " + gameObject.name);
            return; // Exit if the object is not active
        }

        Debug.Log("Interacting with " + gameObject.name);

        // Invoke the UnityEvent when the object is interacted with
        onInteract?.Invoke();
    }

    // Method to enable the interaction
    public void EnableInteraction()
    {
        IsActive = true; // Use the property to enable interaction
    }

    // Method to disable the interaction
    public void DisableInteraction()
    {
        IsActive = false; // Use the property to disable interaction
    }
}
