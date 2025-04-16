using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

        if (Input.GetKeyDown(KeyCode.Return))
        {
            GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
            UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
            currentElement.Interact();
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
                    node.GetComponent<UIControlElement>().SelectExit();
                }
            }
        }
    }
#if ENABLE_INPUT_SYSTEM
    private void NewInputInit()
    {
        // Initialize the input actions for the new Input System
        if (controls == null)
        {
            controls = new();
            controls.Enable();
            move = controls.UIControls.Move;
            enter = controls.UIControls.Enter;
            exit = controls.UIControls.Exit;
            tabChange = controls.UIControls.TabSwitch;
        }

        move.performed += Move;
        enter.performed += Enter;
        exit.performed += Exit;
    }

    private void OnDisable()
    {
        move.performed -= Move;
        enter.performed -= Enter;
        exit.performed -= Exit;
    }
    private void Move(InputAction.CallbackContext obj)
    {
        direction = Vector2Int.zero;
        direction = Vector2Int.RoundToInt(obj.ReadValue<Vector2>());
        Debug.Log(direction);
    }
    private void Enter(InputAction.CallbackContext obj)
    {
        GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
        UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
        currentElement.Interact();
    }
    private void Exit(InputAction.CallbackContext obj)
    {
        exitInteraction?.Invoke();
        if (resetSelection)
        {
            if (resetSelectionPosition) { currentSelectedPosition = Vector2Int.zero; }
            foreach (Vector2Int pos in nodeGridSystem.GetAllPositions())
            {
                GameObject node = nodeGridSystem.GetNodeAtPosition(pos);
                node.GetComponent<UIControlElement>().SelectExit();
            }
        }
    }
#endif

    private void SelectNode(Vector2Int newPos)
    {
        // Call SelectExit on the currently selected node if it has UIControlElement attached
        GameObject currentNode = nodeGridSystem.GetNodeAtPosition(currentSelectedPosition);
        if (currentNode != null)
        {
            UIControlElement currentElement = currentNode.GetComponent<UIControlElement>();
            if (currentElement != null)
            {
                currentElement.SelectExit();
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
                newElement.SelectEnter();
            }
        }
    }
}
