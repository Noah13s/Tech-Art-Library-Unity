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

    public virtual void ControllerSelectEnter()
    {
        selected = true;
        onControllerSelectEnter?.Invoke();
    }
    public virtual void ControllerSelectExit()
    {
        selected = false; 
        onControllerSelectExit?.Invoke();
    }

    public virtual void InteractEnter()
    {
        onInteractionEnter?.Invoke();
    }
    public virtual void InteractExit()
    {
        onInteractionExit?.Invoke();
    }

    public virtual void MouseSelectEnter()
    {
        selected = true;
        onMouseSelectEnter?.Invoke();
    }
    public virtual void MouseSelectExit()
    {
        selected = false;
        onMouseSelectExit?.Invoke();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        MouseSelectEnter();
        if (controlsystem != null) { controlsystem.SelectNode(controlsystem.nodeGridSystem.GetPositionOfNode(this.gameObject)); }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        MouseSelectExit();
    }

    public virtual void OnPointerDown(PointerEventData eventData)
    {
        InteractEnter();
    }
    public virtual void OnPointerUp(PointerEventData eventData)
    {
        InteractExit();
    }
}
