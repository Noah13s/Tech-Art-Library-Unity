using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Carrousel))]
public class UILeftRightElement : UIControlElement
{
    [Header("Move Events")]
    [SerializeField] private UnityEvent onMoveRight;
    [SerializeField] private UnityEvent onMoveLeft;


    [Header("Carrousel Settings")]
    [Tooltip("If true, the carrousel can be focused; this will lock navigation to the carrousel when entered. If false, the carrousel cannot be entered and is navigable via selection only.")]
    [SerializeField] private bool needFocusToNavigate = true;
    [SerializeField] private UnityEvent onFocusEnter;
    [SerializeField] private UnityEvent onFocusExit;

    private Carrousel carrousel;
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

    private void NewInputInit()
    {
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

    private void Move(InputAction.CallbackContext obj)
    {
        if (needFocusToNavigate && !focused) { return; }
        if (selected)
        {
            float x = obj.ReadValue<Vector2>().x;
            if (x > 0.1f)
            {
                carrousel.Next();
                onMoveRight.Invoke();
            }
            else if (x < -0.1f)
            {
                carrousel.Previous();
                onMoveLeft.Invoke();
            }
        }
    }
    private void Exit(InputAction.CallbackContext obj)
    {
        ExitCarrousel();
    }
#endif

    private void Awake()
    {
        carrousel = GetComponent<Carrousel>();
    }

    public override void InteractEnter()
    {
        base.InteractEnter();
        if (!needFocusToNavigate) { return; }
        controlsystem.enabled = false;
        focused = true;
        onFocusEnter?.Invoke();
    }

    public override void InteractExit()
    {
        base.InteractExit();
        if (!needFocusToNavigate) { return; }
        ExitCarrousel();
    }

    public override void MouseSelectExit()
    {
        base.MouseSelectExit();
        if (!needFocusToNavigate) { return; }
        //ExitCarrousel();
    }

    private void ExitCarrousel()
    {
        controlsystem.enabled = true;
        focused = false;
        onFocusExit?.Invoke();
    }

    public void ManualMove(int x)
    {
        if (needFocusToNavigate && !focused) { return; }
        if (selected)
        {
            if (x > 0)
            {
                carrousel.Next();
                onMoveRight.Invoke();
            }
            else if (x < 0)
            {
                carrousel.Previous();
                onMoveLeft.Invoke();
            }
        }
    }
}
