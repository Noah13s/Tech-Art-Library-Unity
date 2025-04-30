using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class UISliderElement : UIControlElement
{
    [Tooltip("If true, the slider can be focused this will lock navigation to the slider when entered. If false, the slider cannot be entered and is navigable via selection only.")]
    [SerializeField] private bool needFocusToNavigate = true;

    private Slider slider;
    private bool focused;

#if ENABLE_INPUT_SYSTEM
    private InputSystem_Actions controls;
    private InputAction move;
    private InputAction exit;
#endif

#if ENABLE_INPUT_SYSTEM
    private void OnEnable()
    {
        NewInputInit();
    }
    private void OnDisable()
    {
        move.performed -= Move;
        exit.performed -= Exit;
        controls.Disable();
    }

    /// <summary>
    /// New input system function for initializing the control reference and control events on enable.<br></br>
    /// The new input system uses events while the legacy system uses <see cref="Update()"/>.
    /// </summary>
    private void NewInputInit()
    {
        // Initialize the input actions for the new Input System
        if (controls == null)
        {
            controls = new();
            move = controls.UIControls.Move;
            exit = controls.UIControls.Exit;
        }
        controls.Enable();

        move.performed += Move;
        exit.performed += Exit;
    }
    /// <summary>
    /// New input system function called by the "move" InputAction.<br></br>
    /// This function moves the selection depending on the input int.
    /// </summary>
    /// <param name="obj"></param>
    private void Move(InputAction.CallbackContext obj)
    {
        if (needFocusToNavigate && !focused) { return; }
        if (selected) { slider.value = Mathf.Clamp(slider.value + obj.ReadValue<Vector2>().x, slider.minValue, slider.maxValue); }        
    }
    private void Exit(InputAction.CallbackContext obj)
    {
        ExitSlider();
    }
#endif
    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public override void InteractEnter()
    {
        base.InteractEnter();
        if (!needFocusToNavigate) { return; }    
        controlsystem.enabled = false;
        focused = true;
    }

    public override void MouseSelectExit()
    {
        base.MouseSelectExit();
        if (!needFocusToNavigate) { return; }
        ExitSlider();
    }

    private void ExitSlider()
    {
        controlsystem.enabled = true;
        focused = false;
    }
}
