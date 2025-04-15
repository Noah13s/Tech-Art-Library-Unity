using UnityEngine;
using UnityEngine.Events;

public class UIControlSystem : MonoBehaviour
{
    public bool resetSelection = false;
    public bool resetSelectionPosition = false;
    public NodeGridSystem nodeGridSystem;               // Reference to your NodeGridSystem
    public Vector2Int currentSelectedPosition = Vector2Int.zero; // Default selection at (0,0)
    [SerializeField] private UnityEvent exitInteraction;

    void Start()
    {
        // Auto-assign the NodeGridSystem if not set
        if (nodeGridSystem == null)
            nodeGridSystem = FindObjectOfType<NodeGridSystem>();

        // Ensure that the default node exists in NodeGridSystem
        if (!nodeGridSystem.HasNodeAtPosition(Vector2Int.zero))
            nodeGridSystem.SetNodeAtPosition(Vector2Int.zero, gameObject); // or assign a dedicated GameObject

        // Immediately select the default node
        SelectNode(currentSelectedPosition);
    }

    private void OnEnable()
    {
        SelectNode(currentSelectedPosition);
    }

    void Update()
    {
        Vector2Int direction = Vector2Int.zero;
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
