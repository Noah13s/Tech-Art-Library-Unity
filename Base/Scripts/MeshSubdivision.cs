using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class DynamicGridCubeGenerator : MonoBehaviour
{
    public Vector3 zoneSize = new Vector3(10f, 10f, 10f); // Size of the zone to subdivide
    public float cubeSize = 1.0f; // Size of each cube

    private void OnDrawGizmos()
    {
        Vector3 start = transform.position - zoneSize / 2f;

        int gridSizeX = Mathf.CeilToInt(zoneSize.x / cubeSize);
        int gridSizeY = Mathf.CeilToInt(zoneSize.y / cubeSize);
        int gridSizeZ = Mathf.CeilToInt(zoneSize.z / cubeSize);

        Vector3 actualCubeSize = new Vector3(zoneSize.x / gridSizeX, zoneSize.y / gridSizeY, zoneSize.z / gridSizeZ);

        // Iterate through the grid
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                for (int z = 0; z < gridSizeZ; z++)
                {
                    Vector3 cubePosition = start + new Vector3(actualCubeSize.x * (x + 0.5f), actualCubeSize.y * (y + 0.5f), actualCubeSize.z * (z + 0.5f));
                    Gizmos.DrawWireCube(cubePosition, actualCubeSize);
                }
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DynamicGridCubeGenerator))]
public class DynamicGridCubeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        DynamicGridCubeGenerator gridGenerator = (DynamicGridCubeGenerator)target;

        if (GUILayout.Button("Regenerate Grid"))
        {
            SceneView.RepaintAll(); // Repaint scene view to reflect changes
        }
    }
}
#endif
