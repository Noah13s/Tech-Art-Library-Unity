using System.Collections.Generic;
using TechArtUtility;
using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class FluidDynamicsSimulation : MonoBehaviour
{
    [SerializeField] public Vector3 simulationScale = new Vector3(1, 1, 1);
    [SerializeField] public float simulatedVelocity = 1.0f;
    [SerializeField] private int resolution = 4;
    [SerializeField] public float particleSize = 0.1f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private List<GameObject> _particles = new List<GameObject>();


    private bool isSimulationRunning = false;

    private void OnDrawGizmos()
    {
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.localPosition, transform.localRotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(transform.position, simulationScale);  
        DebugUtility.DrawFilledCone(
            transform.position + transform.forward * simulationScale.z * 0.5f,
            transform.forward,
            35f,
            .5f,
            10,
            Color.green
        );

        startPosition = Vector3.forward * simulationScale.z * 0.5f;
        endPosition = -Vector3.forward * simulationScale.z * 0.5f;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(startPosition, new Vector3(simulationScale.x, simulationScale.y, 0.1f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(endPosition, new Vector3(simulationScale.x, simulationScale.y, 0.1f));
    }

    public void StartSimulation()
    {
        if (!isSimulationRunning)
        {
            isSimulationRunning = true;
            Debug.Log("Fluid Dynamics Simulation Started");

            // local start center (on the Z‑face)
            Vector3 localStart = Vector3.forward * simulationScale.z * 0.5f;

            float halfWidth = simulationScale.x * 0.5f;
            float halfHeight = simulationScale.y * 0.5f;

            // spawn a resolution×resolution grid in local X & Y
            for (int ix = 0; ix < resolution; ix++)
                for (int iy = 0; iy < resolution; iy++)
                {
                    // normalized [0…1]
                    float tx = resolution == 1 ? 0.5f : (float)ix / (resolution - 1);
                    float ty = resolution == 1 ? 0.5f : (float)iy / (resolution - 1);

                    // offset in X & Y from –half to +half
                    float offsetX = Mathf.Lerp(-halfWidth, halfWidth, tx);
                    float offsetY = Mathf.Lerp(-halfHeight, halfHeight, ty);

                    Vector3 localPos = localStart
                                     + Vector3.right * offsetX
                                     + Vector3.up * offsetY;

                    // world-space spawn point
                    Vector3 worldPos = transform.TransformPoint(localPos);
                    // ↑ converts local→world :contentReference[oaicite:0]{index=0}

                    // make the particle
                    GameObject go = new GameObject($"Particle_{ix}_{iy}");
                    go.transform.parent = transform;
                    go.transform.position = worldPos;
                    go.transform.rotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;

                    var particle = go.AddComponent<FluidSimulationParticle>();
                    particle.simulation = this;

                    _particles.Add(go);
                }
        }
        else
        {
            Debug.LogWarning("Simulation is already running.");
        }
    }

    public void StopSimulation()
    {
        if (isSimulationRunning)
        {
            isSimulationRunning = false;
            Debug.Log("Fluid Dynamics Simulation Stopped");
            // Destroy all spawned particles
            foreach (var p in _particles)
            {
                if (Application.isPlaying)
                    Destroy(p);
                else
                    DestroyImmediate(p);
            }
            _particles.Clear();
        }
        else
        {
            Debug.LogWarning("Simulation is not running.");
        }
    }

#if UNITY_EDITOR
    // Custom Editor for the script
    [CustomEditor(typeof(FluidDynamicsSimulation))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default Inspector
            DrawDefaultInspector();

            // Get reference to the script
            FluidDynamicsSimulation script = (FluidDynamicsSimulation)target;

            if (Application.isPlaying) {
                // Add a custom button
                if (GUILayout.Button("Start Simulation"))
                {
                    script.StartSimulation();
                }
                // Add a custom button
                if (GUILayout.Button("Stop Simulation"))
                {
                    script.StopSimulation();
                }
            }
        }
    }
#endif
}
