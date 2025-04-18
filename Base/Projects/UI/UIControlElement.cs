using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class UIControlElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool selected = false;
    [Header("Events")]
    [SerializeField] private UnityEvent onControllerSelectEnter;
    [SerializeField] private UnityEvent onControllerSelectExit;
    [SerializeField] private UnityEvent onInteractionEnter;
    [SerializeField] private UnityEvent onInteractionExit;
    [SerializeField] private UnityEvent onMouseSelectEnter;
    [SerializeField] private UnityEvent onMouseSelectExit;

    [NonSerialized] public UIControlSystem controlsystem;

    public void ControllerSelectEnter()
    {
        selected = true;
        onControllerSelectEnter?.Invoke();
    }
    public void ControllerSelectExit()
    {
        selected = false; 
        onControllerSelectExit?.Invoke();
    }

    public void InteractEnter()
    {
        onInteractionEnter?.Invoke();
    }
    public void InteractExit()
    {
        onInteractionExit?.Invoke();
    }

    public void MouseSelectEnter()
    {
        selected = true;
        onMouseSelectEnter?.Invoke();
    }
    public void MouseSelectExit()
    {
        selected = false;
        onMouseSelectExit?.Invoke();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MouseSelectEnter();
        if (controlsystem != null) { controlsystem.SelectNode(controlsystem.nodeGridSystem.GetPositionOfNode(this.gameObject)); }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseSelectExit();
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
