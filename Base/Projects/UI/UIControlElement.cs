using UnityEngine;
using UnityEngine.Events;

public class UIControlElement : MonoBehaviour
{
    public bool selected = false;
    [SerializeField] private UnityEvent onSelectEnter;
    [SerializeField] private UnityEvent onSelectExit;
    [SerializeField] private UnityEvent onInteraction;

    public void SelectEnter()
    {
        selected = true;
        onSelectEnter?.Invoke();
    }
    public void SelectExit()
    {
        selected = false; 
        onSelectExit?.Invoke();
    }

    public void Interact()
    {
        onInteraction?.Invoke();
    }


}
