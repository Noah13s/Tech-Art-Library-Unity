using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

public class NodeTreeSystem : MonoBehaviour
{
    [System.Serializable]
    public class NodeEntry
    {
        public GameObject nodeObject;
        public Vector2Int position;

        public NodeEntry(GameObject obj, Vector2Int pos)
        {
            nodeObject = obj;
            position = pos;
        }
    }

    public List<NodeEntry> nodeEntries = new List<NodeEntry>();

    public GameObject GetNodeAtPosition(Vector2Int position)
    {
        NodeEntry entry = nodeEntries.FirstOrDefault(e => e.position == position);
        return entry?.nodeObject;
    }

    public Vector2Int GetPositionOfNode(GameObject nodeobject)
    {
        NodeEntry entry = nodeEntries.FirstOrDefault(e => e.nodeObject == nodeobject);
        if (entry != null)
        {
            return entry.position;
        }
        else
        {
            return Vector2Int.zero;
        }
    }

    public bool HasNodeAtPosition(Vector2Int position)
    {
        return nodeEntries.Any(e => e.position == position);
    }

    public void SetNodeAtPosition(Vector2Int position, GameObject nodeObject)
    {
        NodeEntry entry = nodeEntries.FirstOrDefault(e => e.position == position);
        if (entry != null)
        {
            entry.nodeObject = nodeObject;
        }
        else
        {
            nodeEntries.Add(new NodeEntry(nodeObject, position));
        }
    }

    public void RemoveNodeAtPosition(Vector2Int position)
    {
        nodeEntries.RemoveAll(e => e.position == position);
    }

    public List<Vector2Int> GetAllPositions()
    {
        return nodeEntries.Select(e => e.position).ToList();
    }

    private void OnValidate()
    {
        if (nodeEntries == null)
        {
            nodeEntries = new List<NodeEntry>();
        }

        if (nodeEntries.Count == 0)
        {
            nodeEntries.Add(new NodeEntry(null, Vector2Int.zero));
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(NodeTreeSystem))]
public class NodeGridSystemEditor : Editor
{
    private const float nodeSize = 60f;
    private const float spacing = 20f;
    private const float buttonSize = 20f;

    // Directions for connections: Up, Right, Down, Left
    static public Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NodeTreeSystem nodeSystem = (NodeTreeSystem)target;

        EditorGUILayout.LabelField("Node Grid System", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Add button to open in popup window
        if (GUILayout.Button("Open Node Grid in Popup Window", GUILayout.Height(30)))
        {
            NodeGridPopupWindow.ShowWindow(nodeSystem);
        }

        EditorGUILayout.Space();

        // Ensure the default node (0,0) always exists.
        if (!nodeSystem.HasNodeAtPosition(Vector2Int.zero))
        {
            nodeSystem.SetNodeAtPosition(Vector2Int.zero, null);
        }

        DrawNodeGrid(nodeSystem);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

        serializedObject.ApplyModifiedProperties();
    }

    // Checks connectivity using BFS from the default node (0,0) after removing posToRemove.
    static public bool IsConnectedAfterRemoval(NodeTreeSystem nodeSystem, Vector2Int posToRemove)
    {
        List<Vector2Int> allPositions = nodeSystem.GetAllPositions();
        // Remove the node we plan to remove.
        allPositions.Remove(posToRemove);
        if (!allPositions.Contains(Vector2Int.zero))
            return false;  // Default node is missing – should not happen.

        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(Vector2Int.zero);
        visited.Add(Vector2Int.zero);

        while (queue.Count > 0)
        {
            Vector2Int current = queue.Dequeue();
            foreach (Vector2Int dir in directions)
            {
                Vector2Int neighbor = current + dir;
                if (allPositions.Contains(neighbor) && !visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        return visited.Count == allPositions.Count;
    }

    // Determines if a node can be safely removed.
    private bool CanRemoveNodeSafely(NodeTreeSystem nodeSystem, Vector2Int posToRemove)
    {
        // Do not allow removal of the default node.
        if (posToRemove == Vector2Int.zero)
            return false;

        // Ensure connectivity after removal.
        return IsConnectedAfterRemoval(nodeSystem, posToRemove);
    }

    public static void DrawNodeGrid(NodeTreeSystem nodeSystem, bool isWindow = false, Vector2 scrollPosition = default)
    {
        List<Vector2Int> positions = nodeSystem.GetAllPositions();
        if (positions.Count == 0)
            return;

        // Calculate the furthest distance from the default node (0,0)
        int minX = positions.Min(p => p.x);
        int maxX = positions.Max(p => p.x);
        int minY = positions.Min(p => p.y);
        int maxY = positions.Max(p => p.y);

        // Calculate extents relative to (0,0) so that the default node stays in the center.
        float leftExtent = Mathf.Abs(Mathf.Min(minX, 0));
        float rightExtent = Mathf.Abs(Mathf.Max(maxX, 0));
        float bottomExtent = Mathf.Abs(Mathf.Min(minY, 0));
        float topExtent = Mathf.Abs(Mathf.Max(maxY, 0));

        float horizontalExtent = Mathf.Max(leftExtent, rightExtent);
        float verticalExtent = Mathf.Max(topExtent, bottomExtent);

        // Compute total width and height needed with some padding.
        float widthPerCell = nodeSize + spacing;
        float totalWidth = (horizontalExtent * 2) * widthPerCell + nodeSize + spacing * 2;
        float gridHeight = (topExtent + bottomExtent) * widthPerCell + nodeSize;
        float totalHeight = gridHeight + spacing * 2;

        Rect gridArea;
        Vector2 defaultCenter;

        if (isWindow)
        {
            // For the window, use a fixed size that depends on the grid dimensions
            // and make it scrollable if needed
            float windowWidth = Mathf.Min(totalWidth, EditorGUIUtility.currentViewWidth - 20);
            float windowHeight = Mathf.Min(totalHeight, 600); // Limit height to 600 for very large grids

            gridArea = new Rect(0, 0, totalWidth, totalHeight);

            // The center position needs to account for scroll position
            float originCenterY = gridArea.y + spacing + topExtent * widthPerCell + nodeSize * 0.5f;
            defaultCenter = new Vector2(gridArea.center.x, originCenterY);

            // Adjust for scroll position
            defaultCenter.x -= scrollPosition.x;
            defaultCenter.y -= scrollPosition.y;
        }
        else
        {
            // For the inspector, use the standard approach
            gridArea = EditorGUILayout.GetControlRect(false, totalHeight);
            float originCenterY = gridArea.y + spacing + topExtent * widthPerCell + nodeSize * 0.5f;
            defaultCenter = new Vector2(gridArea.center.x, originCenterY);
        }

        Dictionary<Vector2Int, Rect> nodeRects = new Dictionary<Vector2Int, Rect>();

        // Position each node relative to (0,0) at the center.
        foreach (Vector2Int pos in positions)
        {
            // Compute offset based on grid coordinates.
            // Positive pos.x goes to the right and positive pos.y goes upward.
            float x = defaultCenter.x + pos.x * (nodeSize + spacing) - nodeSize / 2;
            float y = defaultCenter.y - pos.y * (nodeSize + spacing) - nodeSize / 2;
            nodeRects[pos] = new Rect(x, y, nodeSize, nodeSize);
        }

        // Draw connections between adjacent nodes.
        Handles.color = Color.gray;
        foreach (Vector2Int pos in positions)
        {
            if (!nodeRects.ContainsKey(pos))
                continue;

            Rect nodeRect = nodeRects[pos];
            Vector2 nodeCenter = new Vector2(nodeRect.x + nodeRect.width / 2, nodeRect.y + nodeRect.height / 2);

            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjacentPos = pos + dir;
                if (!nodeSystem.HasNodeAtPosition(adjacentPos))
                    continue;
                if (!nodeRects.ContainsKey(adjacentPos))
                    continue;

                Rect adjacentRect = nodeRects[adjacentPos];
                Vector2 adjacentCenter = new Vector2(adjacentRect.x + adjacentRect.width / 2, adjacentRect.y + adjacentRect.height / 2);
                Handles.DrawLine(nodeCenter, adjacentCenter);
            }
        }

        // Draw expansion buttons for adding new nodes.
        foreach (Vector2Int pos in positions)
        {
            if (!nodeRects.ContainsKey(pos))
                continue;

            Rect nodeRect = nodeRects[pos];

            foreach (Vector2Int dir in directions)
            {
                Vector2Int adjacentPos = pos + dir;
                if (nodeSystem.HasNodeAtPosition(adjacentPos))
                    continue;

                Rect buttonRect;
                if (dir == new Vector2Int(0, 1))      // Up
                    buttonRect = new Rect(nodeRect.x + (nodeRect.width - buttonSize) / 2, nodeRect.y - buttonSize - 5, buttonSize, buttonSize);
                else if (dir == new Vector2Int(1, 0))  // Right
                    buttonRect = new Rect(nodeRect.x + nodeRect.width + 5, nodeRect.y + (nodeRect.height - buttonSize) / 2, buttonSize, buttonSize);
                else if (dir == new Vector2Int(0, -1)) // Down
                    buttonRect = new Rect(nodeRect.x + (nodeRect.width - buttonSize) / 2, nodeRect.y + nodeRect.height + 5, buttonSize, buttonSize);
                else                                 // Left
                    buttonRect = new Rect(nodeRect.x - buttonSize - 5, nodeRect.y + (nodeRect.height - buttonSize) / 2, buttonSize, buttonSize);

                if (GUI.Button(buttonRect, "+"))
                {
                    Undo.RecordObject(nodeSystem, "Add Node");
                    nodeSystem.SetNodeAtPosition(adjacentPos, null);
                    EditorUtility.SetDirty(nodeSystem);
                    Event.current.Use();
                    return;
                }
            }
        }

        // Draw nodes.
        foreach (Vector2Int pos in positions)
        {
            if (!nodeRects.ContainsKey(pos))
                continue;

            GameObject nodeObj = nodeSystem.GetNodeAtPosition(pos);
            Rect nodeRect = nodeRects[pos];

            // Default node (0,0) is in blue; the others are gray.
            Color nodeColor = (pos == Vector2Int.zero) ? Color.blue : new Color(0.3f, 0.3f, 0.3f);
            EditorGUI.DrawRect(nodeRect, nodeColor);

            Rect objectFieldRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width - buttonSize - 2, nodeRect.height);
            EditorGUI.BeginChangeCheck();
            GameObject newNodeObj = (GameObject)EditorGUI.ObjectField(objectFieldRect, nodeObj, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(nodeSystem, "Change Node Reference");
                nodeSystem.SetNodeAtPosition(pos, newNodeObj);
                EditorUtility.SetDirty(nodeSystem);
            }

            // Only allow deletion if safe (and not the default node)
            if (pos != Vector2Int.zero && IsConnectedAfterRemoval(nodeSystem, pos))
            {
                Rect removeButtonRect = new Rect(nodeRect.x + nodeRect.width - buttonSize, nodeRect.y, buttonSize, buttonSize);
                if (GUI.Button(removeButtonRect, "-"))
                {
                    Undo.RecordObject(nodeSystem, "Remove Node");
                    nodeSystem.RemoveNodeAtPosition(pos);
                    EditorUtility.SetDirty(nodeSystem);
                    Event.current.Use();
                    return;
                }
            }
        }
    }

    private void DrawNodeGrid(NodeTreeSystem nodeSystem)
    {
        DrawNodeGrid(nodeSystem, false);
    }
}

// Create new editor window for the node grid
public class NodeGridPopupWindow : EditorWindow
{
    private NodeTreeSystem nodeSystem;
    private Vector2 scrollPosition;
    private Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(-1, 0)
    };

    public static void ShowWindow(NodeTreeSystem nodeSystem)
    {
        NodeGridPopupWindow window = GetWindow<NodeGridPopupWindow>("Node Grid Viewer");
        window.nodeSystem = nodeSystem;
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    private void OnGUI()
    {
        if (nodeSystem == null)
        {
            EditorGUILayout.HelpBox("No Node Grid System selected.", MessageType.Warning);
            return;
        }

        // Display the node system name
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        EditorGUILayout.LabelField($"Editing: {nodeSystem.name}", EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        // Create scroll view
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        // Calculate suitable box size based on node count
        List<Vector2Int> positions = nodeSystem.GetAllPositions();
        int minX = positions.Min(p => p.x);
        int maxX = positions.Max(p => p.x);
        int minY = positions.Min(p => p.y);
        int maxY = positions.Max(p => p.y);

        float nodeSize = 60f;
        float spacing = 20f;
        float widthPerCell = nodeSize + spacing;

        float width = (maxX - minX + 2) * widthPerCell;
        float height = (maxY - minY + 2) * widthPerCell;

        // Minimum size so we always have space
        width = Mathf.Max(width, 600);
        height = Mathf.Max(height, 600);

        GUILayout.Box("", GUILayout.Width(width), GUILayout.Height(height));

        // Draw the node grid
        NodeGridSystemEditor.DrawNodeGrid(nodeSystem, true, scrollPosition);

        EditorGUILayout.EndScrollView();

        Repaint();
    }
}
#endif