using UnityEngine;
using UnityEngine.Events; // For UnityEvent

public class InteractiveObject : MonoBehaviour
{
    // UnityEvent to be invoked when the object is interacted with
    public UnityEvent onInteract;

    // Base interaction method to be overridden by specific interactive objects
    public virtual void Interact()
    {
        Debug.Log("Interacting with " + gameObject.name);

        // Invoke the UnityEvent when the object is interacted with
        if (onInteract != null)
        {
            onInteract.Invoke();
        }
    }
}
