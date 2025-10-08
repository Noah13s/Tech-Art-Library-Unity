using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for UI control elements that can be selected and interacted with.
/// </summary>
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

    [NonSerialized] public UIControlSystem controlsystem;// Reference to the UIControlSystem that this element belongs to.

    /// <summary>
    /// Called when the element is selected by a controller.
    /// </summary>
    public virtual void ControllerSelectEnter()
    {
        selected = true;
        onControllerSelectEnter?.Invoke();
    }
    /// <summary>
    /// Called when the element is deselected by a controller.
    /// </summary>
    public virtual void ControllerSelectExit()
    {
        selected = false; 
        onControllerSelectExit?.Invoke();
    }

    /// <summary>
    /// Called when the element is interacted with (e.g., clicked or pressed).
    /// </summary>
    public virtual void InteractEnter()
    {
        onInteractionEnter?.Invoke();
    }
    /// <summary>
    /// Called when the interaction with the element ends (e.g., released).
    /// </summary>
    public virtual void InteractExit()
    {
        onInteractionExit?.Invoke();
    }
    /// <summary>
    /// Called when the element is selected by mouse input.
    /// </summary>
    public virtual void MouseSelectEnter()
    {
        selected = true;
        onMouseSelectEnter?.Invoke();
    }
    /// <summary>
    /// Called when the element is deselected by mouse input.
    /// </summary>
    public virtual void MouseSelectExit()
    {
        selected = false;
        onMouseSelectExit?.Invoke();
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if (controlsystem == null) { return; }
        if (controlsystem.enabled == false) { return; } // Prevents mouse selection when the system is disabled.
        MouseSelectEnter();
        if (controlsystem != null) { controlsystem.SelectNode(controlsystem.nodeGridSystem.GetPositionOfNode(this.gameObject)); }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        if (controlsystem == null) { return; } 
        if (controlsystem.enabled == false) { return; } // Prevents mouse selection when the system is disabled.
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
