using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class FluidSimulationParticle : MonoBehaviour
{
    [HideInInspector] public FluidDynamicsSimulation simulation;
    public AnimationCurve deviationPreview = new AnimationCurve();
    public AnimationCurve movementPreview = new AnimationCurve();


    private Bounds bounds;
    private Rigidbody rb;
    private Vector3 startLocalPos;

    private List<Vector2> deviationOverTime = new List<Vector2>();
    private List<Vector2> movementOverTime = new List<Vector2>();
    private bool isFinished = false;

    // Start is called before the first frame update
    void Start()
    {
        startLocalPos = this.transform.localPosition;
        var go = this.gameObject;
        // trail
        var trail = go.AddComponent<TrailRenderer>();
        trail.time = float.PositiveInfinity;
        trail.startWidth = simulation.particleSize;
        trail.endWidth = simulation.particleSize;
        trail.minVertexDistance = 0f;
        trail.numCornerVertices = 4;
        trail.material = new Material(Shader.Find("Unlit/TrailColorRamp"));

        // physics & launch
        rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        // ↑ immediate velocity in world‑space :contentReference[oaicite:1]{index=1}

        // physic material
        var phmat = new PhysicMaterial();
        phmat.dynamicFriction = 0.0f;
        phmat.staticFriction = 0.0f;

        // optional collider
        var col = go.AddComponent<SphereCollider>();
        col.radius = 0.05f;
        col.center = Vector3.zero;
        col.material = phmat;

        // set bounds
        bounds = new Bounds(simulation.transform.position, simulation.simulationScale * 1.05f);
    }

    void CalculateDeviation(Vector3 localPos)
    {
        float dx = localPos.x - startLocalPos.x;
        float dy = localPos.y - startLocalPos.y;

        float delta = Mathf.Sqrt(dy * dy + dx * dx);
        delta = (float)Math.Round((double)delta, 3);

        deviationOverTime.Add(new Vector2(delta, Math.Abs((localPos.z - startLocalPos.z))));
    }

    Vector3 previousPos = Vector3.zero;
    void CalculateMovement(Vector3 localPos)
    {
        if (previousPos == Vector3.zero)
        {
            previousPos = startLocalPos;
            return;
        }

        float dx = localPos.x - previousPos.x;
        float dy = localPos.y - previousPos.y;

        float delta = Mathf.Sqrt(dy * dy + dx * dx);
        delta = (float)Math.Round((double)delta, 3);

        movementOverTime.Add(new Vector2(delta, Math.Abs((localPos.z - startLocalPos.z))));
        previousPos = localPos;
    }

    void ParseDeviation()
    {
        // add keys to the AnimationCurve
        foreach (var deltaData in deviationOverTime)
        {
            var keyframe = new Keyframe(deltaData.y, deltaData.x);
            int keyIndex = deviationPreview.AddKey(keyframe);
        }
        // set the tangent mode for each key to ClampedAuto
        for (int i = 0; i < deviationPreview.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(deviationPreview, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }

    void ParseMovement()
    {
        // add keys to the AnimationCurve
        foreach (var deltaData in movementOverTime)
        {
            var keyframe = new Keyframe(deltaData.y, deltaData.x);
            int keyIndex = movementPreview.AddKey(keyframe);
        }
        // set the tangent mode for each key to ClampedAuto
        for (int i = 0; i < movementPreview.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(movementPreview, i, AnimationUtility.TangentMode.ClampedAuto);
        }
    }

    void GenerateTexture()
    {
        // Create or assign the material with the correct shader
        var trail = GetComponent<TrailRenderer>();
        var mat = new Material(Shader.Find("Unlit/TrailColorRamp")); // ← your custom shader
        mat.SetTexture("_RampTex", AnimationCurveToRampTexture(deviationPreview, MakeFlatGradient(), 256));
        trail.material = mat;
        trail.colorGradient = new Gradient
        {
            colorKeys = new GradientColorKey[] {
                new GradientColorKey(new Color(1, 1, 1), 0.4f),
                new GradientColorKey(new Color(0, 0, 0), 1)
            },
            alphaKeys = new GradientAlphaKey[] {
                new GradientAlphaKey(1, 0),
                new GradientAlphaKey(1, 1)
            }
        };
    }

    private Gradient MakeFlatGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
            new GradientColorKey(new Color(0, 1, 0), 0),
            new GradientColorKey(new Color(1, 0, 0), 1)
            },
            new GradientAlphaKey[] {
            new GradientAlphaKey(1, 0),
            new GradientAlphaKey(1, 1)
            });
        return g;
    }

    public static Keyframe GetAnimationCurveMaxValue(AnimationCurve curve)
    {
        if (curve == null || curve.length == 0)
            return new Keyframe(0, 0);
        Keyframe maxKey = curve.keys[0];
        foreach (var key in curve.keys)
        {
            if (key.value > maxKey.value)
                maxKey = key;
        }
        return maxKey;
    }

    public static Texture2D AnimationCurveToRampTexture(AnimationCurve curve, Gradient colorGradient, int width = 256)
    {
        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float minTime = curve.keys.First().time;
        float maxTime = curve.keys.Last().time;

        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            float curveTime = Mathf.Lerp(minTime, maxTime, t);
            float scalarValue = curve.Evaluate(curveTime);

            // Normalize scalar (optional): You could map value to 0-1 before using it
            float normalized = Mathf.InverseLerp(0f, GetAnimationCurveMaxValue(curve).value, scalarValue);
            Color col = colorGradient.Evaluate(normalized);

            tex.SetPixel(i, 0, col);
        }

        tex.Apply();
        return tex;
    }


    // Update is called once per frame
    void Update()
    {
        // turn the particle’s world pos into sim-local:
        Vector3 localPos = simulation.transform.InverseTransformPoint(transform.position);
        if (bounds.Contains(localPos))
        {
            rb.velocity = -transform.forward * simulation.simulatedVelocity;
            CalculateDeviation(localPos);
            CalculateMovement(localPos);
        }
        else
        {
            if (isFinished) return;
            rb.isKinematic = true;
            ParseDeviation();
            ParseMovement();
            GenerateTexture();
            isFinished = true;
        }
        transform.forward = simulation.transform.forward;
    }

    private void OnDrawGizmos()
    {
        //Handles.Label(transform.position, $"{delta}");
    }

#if UNITY_EDITOR
    // Custom Editor for the script
    [CustomEditor(typeof(FluidSimulationParticle))]
    public class CustomViewportEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default Inspector
            DrawDefaultInspector();

            // Get reference to the script
            FluidSimulationParticle script = (FluidSimulationParticle)target;

            if (Application.isPlaying)
            {
                // Add a custom button
                if (GUILayout.Button("Calculate gradient"))
                {
                    script.ParseDeviation();
                }
                // Add a custom button
                if (GUILayout.Button("Generate texture"))
                {
                    script.GenerateTexture();
                }
            }
        }
    }
#endif
}
