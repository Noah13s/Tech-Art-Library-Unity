using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIControlElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool selected = false;
    [SerializeField] private UnityEvent onSelectEnter;
    [SerializeField] private UnityEvent onSelectExit;
    [SerializeField] private UnityEvent onInteractionEnter;
    [SerializeField] private UnityEvent onInteractionExit;

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

    public void InteractEnter()
    {
        onInteractionEnter?.Invoke();
    }
    public void InteractExit()
    {
        onInteractionExit?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        SelectEnter();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SelectExit();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        InteractEnter();
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        InteractExit();
    }
}
