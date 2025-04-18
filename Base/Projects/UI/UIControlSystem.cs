using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Class for UI navigation for both unity input systems using <see cref="NodeGridSystem"/> for the navigation setup.
/// </summary>
public class UIControlSystem : MonoBehaviour
{
    [Header("Setup")]
    public NodeGridSystem nodeGridSystem;               // Reference to your NodeGridSystem
    public Vector2Int currentSelectedPosition = Vector2Int.zero; // Default selection at (0,0)
    [Header("Parameters")]
    public bool LbRbNavigation = false;
    public bool resetSelection = false;
    public bool resetSelectionPosition = false;
    [SerializeField] private UnityEvent exitInteraction;
    private Vector2Int direction = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
    private InputSystem_Actions controls;
    private InputAction move;
    private InputAction tabChange;
    private InputAction enter;
    private InputAction exit;
#endif

    void Start()
    {
        // Auto-assign the NodeGridSystem if not set
        if (nodeGridSystem == null)
            nodeGridSystem = FindObjectOfType<NodeGridSystem>();

        // Ensure that the default node exists in NodeGridSystem
        if (!nodeGridSystem.HasNodeAtPosition(Vector2Int.zero))
            nodeGridSystem.SetNodeAtPosition(Vector2Int.zero, gameObject); // or assign a dedicated GameObject

        foreach (var node in nodeGridSystem.nodeEntries)
        {
            UIControlElement currentElement = node.nodeObject.GetComponent<UIControlElement>();
            currentElement.controlsystem = this;
        }
    }

    private void OnEnable()
    {
        SelectNode(currentSelectedPosition);
#if ENABLE_INPUT_SYSTEM
        NewInputInit();
#endif
    }

    void Update()
    {
#if !ENABLE_INPUT_SYSTEM
        LegacyInputHandler();
#endif
    }

    private void LegacyInputHandler()
    {
        direction = Vector2Int.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))
            direction = new Vector2Int(0, 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            direction = new Vector2Int(0, -1);
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            direction = new Vector2Int(-1, 0);
        else if (Input.GetKeyDown(KeyCode.RightArrow))
            direction = new Vector2Int(1, 0);

        // Apply requested direction
        if (direction != Vector2Int.zero)
        {
            Vector2Int newPos = currentSelectedPosition + direction;
            GameObject newNode = nodeGridSystem.GetNodeAtPosition(newPos);
            if (newNode != null)
            {
                SelectNode(newPos);
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
            UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
            currentElement.InteractEnter();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitInteraction?.Invoke();
            if (resetSelection)
            {
                if (resetSelectionPosition) { currentSelectedPosition = Vector2Int.zero; }
                foreach (Vector2Int pos in nodeGridSystem.GetAllPositions())
                {
                    GameObject node = nodeGridSystem.GetNodeAtPosition(pos);
                    node.GetComponent<UIControlElement>().ControllerSelectExit();
                }
            }
        }

    }
#if ENABLE_INPUT_SYSTEM
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
            enter = controls.UIControls.Enter;
            exit = controls.UIControls.Exit;
            tabChange = controls.UIControls.TabSwitch;
        }
        controls.Enable();

        move.performed += Move;
        enter.started += EnterPressed;
        enter.canceled += EnterReleased;
        exit.performed += Exit;
    }

    private void OnDisable()
    {
        move.performed -= Move;
        enter.started -= EnterPressed;
        enter.canceled -= EnterReleased;
        exit.performed -= Exit;
        controls.Disable();
    }
    /// <summary>
    /// New input system function called by the "move" InputAction.<br></br>
    /// This function moves the selection depending on the input int.
    /// </summary>
    /// <param name="obj"></param>
    private void Move(InputAction.CallbackContext obj)
    {
        direction = Vector2Int.zero;
        direction = Vector2Int.RoundToInt(obj.ReadValue<Vector2>());
        // Apply requested direction
        if (direction != Vector2Int.zero)
        {
            Vector2Int newPos = currentSelectedPosition + direction;
            GameObject newNode = nodeGridSystem.GetNodeAtPosition(newPos);
            if (newNode != null)
            {
                SelectNode(newPos);
            }
        }
    }
    /// <summary>
    /// New input system function for when the interact button is pressed.
    /// </summary>
    /// <param name="obj"></param>
    private void EnterPressed(InputAction.CallbackContext obj)
    {
        GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
        UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
        currentElement.InteractEnter();
    }
    /// <summary>
    /// New input system function for when the interact button is released.
    /// </summary>
    /// <param name="obj"></param>
    private void EnterReleased(InputAction.CallbackContext obj)
    {
        GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
        UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
        currentElement.InteractExit();
    }
    /// <summary>
    /// New input system function for when the exit button is pressed.
    /// </summary>
    /// <param name="obj"></param>
    private void Exit(InputAction.CallbackContext obj)
    {
        exitInteraction?.Invoke();
        if (resetSelection)
        {
            if (resetSelectionPosition) { currentSelectedPosition = Vector2Int.zero; }
            foreach (Vector2Int pos in nodeGridSystem.GetAllPositions())
            {
                GameObject node = nodeGridSystem.GetNodeAtPosition(pos);
                node.GetComponent<UIControlElement>().ControllerSelectExit();
            }
        }
    }
#endif

    public void SelectNode(Vector2Int newPos)
    {
        // Call SelectExit on the currently selected node if it has UIControlElement attached
        GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
        if (currentNode != null)
        {
            UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
            if (currentElement != null)
            {
                currentElement.ControllerSelectExit();
            }
        }

        // Update the current selection
        currentSelectedPosition = newPos;
        GameObject newNode = nodeGridSystem.GetNodeAtPosition(newPos);

        // Call SelectEnter on the new node if it has UIControlElement attached
        if (newNode != null)
        {
            UIControlElement newElement = newNode.GetComponent<UIControlElement>();
            if (newElement != null)
            {
                newElement.ControllerSelectEnter();
            }
        }
    }
}
