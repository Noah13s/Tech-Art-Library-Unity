using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generates the visible surface of a perspective-illusion planet.
///
/// The mesh is a camera-facing spherical cap rather than a complete sphere. Its
/// angular radius is derived from the real double-precision player/planet
/// distance, so geometry behind the horizon is never generated. Tessellation is
/// selected from surface distance measured in planet radii: close views spend the
/// budget on a small, detailed horizon patch while distant views use a lightweight
/// hemisphere.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(PerspectiveIllusionObject))]
public sealed class PlanetSurfaceGenerator : MonoBehaviour
{
    [Header("Visibility")]
    [SerializeField] private bool renderOnlyVisibleSurface = true;
    [SerializeField, Range(0.25f, 12f)] private float horizonPaddingDegrees = 3f;
    [SerializeField, Range(0.01f, 5f)] private float minimumDirectionUpdateDegrees = 0.08f;
    [SerializeField, Range(0.01f, 0.5f)] private float relativeDirectionUpdate = 0.08f;

    [Header("Distance LOD (surface distance / planet radius)")]
    [SerializeField, Min(0.0001f)] private float closeDistance = 0.02f;
    [SerializeField, Min(0.0001f)] private float nearDistance = 0.25f;
    [SerializeField, Min(0.0001f)] private float mediumDistance = 1f;
    [SerializeField, Min(0.0001f)] private float farDistance = 5f;

    [Header("Close LOD")]
    [SerializeField, Range(8, 192)] private int closeRings = 96;
    [SerializeField, Range(16, 384)] private int closeSegments = 192;

    [Header("Near LOD")]
    [SerializeField, Range(8, 192)] private int nearRings = 80;
    [SerializeField, Range(16, 384)] private int nearSegments = 160;

    [Header("Medium LOD")]
    [SerializeField, Range(8, 192)] private int mediumRings = 56;
    [SerializeField, Range(16, 384)] private int mediumSegments = 112;

    [Header("Far LOD")]
    [SerializeField, Range(8, 192)] private int farRings = 36;
    [SerializeField, Range(16, 384)] private int farSegments = 72;

    [Header("Distant LOD")]
    [SerializeField, Range(8, 192)] private int distantRings = 24;
    [SerializeField, Range(16, 384)] private int distantSegments = 48;

    private PerspectiveIllusionObject planet;
    private MeshFilter meshFilter;
    private Mesh generatedMesh;
    private Mesh originalMesh;
    private Vector3 lastViewAxis = Vector3.zero;
    private int lastLod = -1;
    private double lastPlanetDiameter = -1.0;
    private float lastCapAngle = -1f;

    public int CurrentVertexCount => generatedMesh != null ? generatedMesh.vertexCount : 0;
    public int CurrentTriangleCount => generatedMesh != null && generatedMesh.subMeshCount > 0
        ? (int)(generatedMesh.GetIndexCount(0) / 3)
        : 0;
    public string CurrentLodName => GetLodName(lastLod);
    public float VisibleCapAngleDegrees => lastCapAngle * Mathf.Rad2Deg;

    private void OnEnable()
    {
        EnsureReferences();
        GenerateNow();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        UpdateMeshIfNeeded(false);
    }

    private void OnDestroy()
    {
        if (meshFilter != null && meshFilter.sharedMesh == generatedMesh)
        {
            meshFilter.sharedMesh = originalMesh;
        }

        DestroyGeneratedMesh();
    }

#if UNITY_EDITOR
    private bool validationQueued;

    private void OnValidate()
    {
        ClampSettings();
        if (validationQueued)
        {
            return;
        }

        validationQueued = true;
        EditorApplication.delayCall += GenerateAfterValidation;
    }

    private void GenerateAfterValidation()
    {
        validationQueued = false;
        if (this == null || Application.isPlaying)
        {
            return;
        }

        EnsureReferences();
        GenerateNow();
    }
#endif

    [ContextMenu("Regenerate Planet Surface")]
    public void GenerateNow()
    {
        ClampSettings();
        EnsureReferences();
        UpdateMeshIfNeeded(true);
    }

    private void EnsureReferences()
    {
        planet ??= GetComponent<PerspectiveIllusionObject>();
        meshFilter ??= GetComponent<MeshFilter>();

        if (generatedMesh == null)
        {
            originalMesh = meshFilter != null ? meshFilter.sharedMesh : null;
            generatedMesh = new Mesh
            {
                name = $"{name} Visible Planet Surface",
                hideFlags = HideFlags.DontSave
            };
            generatedMesh.MarkDynamic();
        }
    }

    private void UpdateMeshIfNeeded(bool force)
    {
        if (planet == null || meshFilter == null || generatedMesh == null)
        {
            return;
        }

        GetViewState(
            out Vector3 viewAxis,
            out float capAngle,
            out double relativeSurfaceDistance);

        int lod = SelectLod(relativeSurfaceDistance);
        float directionThreshold = Mathf.Clamp(
            capAngle * Mathf.Rad2Deg * relativeDirectionUpdate,
            minimumDirectionUpdateDegrees,
            Mathf.Max(minimumDirectionUpdateDegrees, horizonPaddingDegrees * 0.5f));
        bool directionChanged = lastViewAxis == Vector3.zero ||
            Vector3.Angle(lastViewAxis, viewAxis) >= directionThreshold;
        float capThreshold = Mathf.Min(
            Mathf.Max(0.002f, lastCapAngle * 0.04f),
            horizonPaddingDegrees * 0.5f * Mathf.Deg2Rad);
        bool capChanged = lastCapAngle < 0f ||
            Mathf.Abs(lastCapAngle - capAngle) >= capThreshold;

        if (!force && lod == lastLod && !directionChanged && !capChanged &&
            Math.Abs(lastPlanetDiameter - planet.simulationScale) < 0.001)
        {
            return;
        }

        GetLodResolution(lod, out int rings, out int segments);
        BuildSphericalCap(viewAxis, capAngle, rings, segments);

        lastViewAxis = viewAxis;
        lastCapAngle = capAngle;
        lastLod = lod;
        lastPlanetDiameter = planet.simulationScale;
    }

    private void GetViewState(
        out Vector3 localViewAxis,
        out float capAngle,
        out double relativeSurfaceDistance)
    {
        double diameter = Math.Max(0.000001, planet.simulationScale);
        double radius = diameter * 0.5;
        DoubleVector3 toPlayer = planet.player != null
            ? planet.player.playerPosition - planet.simulationPosition
            : new DoubleVector3(0.0, 0.0, 1.0);
        double centerDistance = Math.Max(0.000001, toPlayer.Magnitude());
        double surfaceDistance = Math.Max(0.0, centerDistance - radius);
        relativeSurfaceDistance = surfaceDistance / radius;

        Vector3 worldViewAxis = (Vector3)toPlayer.Normalized();
        if (worldViewAxis.sqrMagnitude < 0.5f)
        {
            Camera camera = Camera.main;
            worldViewAxis = camera != null
                ? (camera.transform.position - transform.position).normalized
                : Vector3.forward;
        }

        localViewAxis = transform.InverseTransformDirection(worldViewAxis).normalized;
        if (localViewAxis.sqrMagnitude < 0.5f)
        {
            localViewAxis = Vector3.forward;
        }

        float visibleAngle;
        if (!renderOnlyVisibleSurface || centerDistance <= radius)
        {
            visibleAngle = Mathf.PI;
        }
        else
        {
            visibleAngle = Mathf.Acos(Mathf.Clamp((float)(radius / centerDistance), 0f, 1f));
        }

        capAngle = Mathf.Min(
            Mathf.PI,
            visibleAngle + horizonPaddingDegrees * Mathf.Deg2Rad);
    }

    private int SelectLod(double relativeSurfaceDistance)
    {
        if (relativeSurfaceDistance <= closeDistance) return 0;
        if (relativeSurfaceDistance <= nearDistance) return 1;
        if (relativeSurfaceDistance <= mediumDistance) return 2;
        if (relativeSurfaceDistance <= farDistance) return 3;
        return 4;
    }

    private void GetLodResolution(int lod, out int rings, out int segments)
    {
        switch (lod)
        {
            case 0: rings = closeRings; segments = closeSegments; break;
            case 1: rings = nearRings; segments = nearSegments; break;
            case 2: rings = mediumRings; segments = mediumSegments; break;
            case 3: rings = farRings; segments = farSegments; break;
            default: rings = distantRings; segments = distantSegments; break;
        }

        rings = Mathf.Max(2, rings);
        segments = Mathf.Max(3, segments);
    }

    private void BuildSphericalCap(Vector3 axis, float capAngle, int rings, int segments)
    {
        int estimatedVertices = 1 + rings * (segments + 1) + rings * 2;
        int estimatedIndices = segments * 3 + (rings - 1) * segments * 6;
        var vertices = new List<Vector3>(estimatedVertices);
        var normals = new List<Vector3>(estimatedVertices);
        var uvs = new List<Vector2>(estimatedVertices);
        var indices = new List<int>(estimatedIndices);

        Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) < 0.95f
            ? Vector3.up
            : Vector3.right;
        Vector3 tangent = Vector3.Cross(reference, axis).normalized;
        Vector3 bitangent = Vector3.Cross(axis, tangent).normalized;

        AddVertex(axis, vertices, normals, uvs);

        for (int ring = 1; ring <= rings; ring++)
        {
            float theta = capAngle * ring / rings;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int segment = 0; segment <= segments; segment++)
            {
                float phi = 2f * Mathf.PI * segment / segments;
                Vector3 radial = tangent * Mathf.Cos(phi) + bitangent * Mathf.Sin(phi);
                Vector3 direction = (axis * cosTheta + radial * sinTheta).normalized;
                AddVertex(direction, vertices, normals, uvs);
            }
        }

        for (int segment = 0; segment < segments; segment++)
        {
            AddSeamSafeTriangle(
                0,
                1 + segment,
                1 + segment + 1,
                vertices,
                normals,
                uvs,
                indices);
        }

        for (int ring = 1; ring < rings; ring++)
        {
            int inner = 1 + (ring - 1) * (segments + 1);
            int outer = inner + segments + 1;
            for (int segment = 0; segment < segments; segment++)
            {
                int innerCurrent = inner + segment;
                int innerNext = innerCurrent + 1;
                int outerCurrent = outer + segment;
                int outerNext = outerCurrent + 1;

                AddSeamSafeTriangle(
                    innerCurrent, outerCurrent, outerNext,
                    vertices, normals, uvs, indices);
                AddSeamSafeTriangle(
                    innerCurrent, outerNext, innerNext,
                    vertices, normals, uvs, indices);
            }
        }

        generatedMesh.Clear();
        generatedMesh.indexFormat = vertices.Count > ushort.MaxValue
            ? IndexFormat.UInt32
            : IndexFormat.UInt16;
        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(indices, 0, true);
        generatedMesh.RecalculateTangents();
        generatedMesh.RecalculateBounds();
        meshFilter.sharedMesh = generatedMesh;
    }

    private static void AddVertex(
        Vector3 direction,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        direction.Normalize();
        vertices.Add(direction * 0.5f);
        normals.Add(direction);

        float longitude = Mathf.Atan2(-direction.z, -direction.x) / (2f * Mathf.PI);
        float u = Mathf.Repeat(longitude, 1f);
        float v = 1f - Mathf.Acos(Mathf.Clamp(direction.y, -1f, 1f)) / Mathf.PI;
        uvs.Add(new Vector2(u, v));
    }

    private static void AddSeamSafeTriangle(
        int a,
        int b,
        int c,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs,
        List<int> indices)
    {
        float minU = Mathf.Min(uvs[a].x, Mathf.Min(uvs[b].x, uvs[c].x));
        float maxU = Mathf.Max(uvs[a].x, Mathf.Max(uvs[b].x, uvs[c].x));
        if (maxU - minU > 0.5f)
        {
            if (uvs[a].x < 0.5f) a = DuplicateWrappedVertex(a, vertices, normals, uvs);
            if (uvs[b].x < 0.5f) b = DuplicateWrappedVertex(b, vertices, normals, uvs);
            if (uvs[c].x < 0.5f) c = DuplicateWrappedVertex(c, vertices, normals, uvs);
        }

        indices.Add(a);
        indices.Add(b);
        indices.Add(c);
    }

    private static int DuplicateWrappedVertex(
        int source,
        List<Vector3> vertices,
        List<Vector3> normals,
        List<Vector2> uvs)
    {
        int index = vertices.Count;
        vertices.Add(vertices[source]);
        normals.Add(normals[source]);
        Vector2 uv = uvs[source];
        uv.x += 1f;
        uvs.Add(uv);
        return index;
    }

    private void ClampSettings()
    {
        horizonPaddingDegrees = Mathf.Clamp(horizonPaddingDegrees, 0.25f, 12f);
        minimumDirectionUpdateDegrees = Mathf.Clamp(minimumDirectionUpdateDegrees, 0.01f, 5f);
        relativeDirectionUpdate = Mathf.Clamp(relativeDirectionUpdate, 0.01f, 0.5f);
        closeDistance = Mathf.Max(0.0001f, closeDistance);
        nearDistance = Mathf.Max(closeDistance, nearDistance);
        mediumDistance = Mathf.Max(nearDistance, mediumDistance);
        farDistance = Mathf.Max(mediumDistance, farDistance);

        closeRings = Mathf.Clamp(closeRings, 8, 192);
        closeSegments = Mathf.Clamp(closeSegments, 16, 384);
        nearRings = Mathf.Clamp(nearRings, 8, 192);
        nearSegments = Mathf.Clamp(nearSegments, 16, 384);
        mediumRings = Mathf.Clamp(mediumRings, 8, 192);
        mediumSegments = Mathf.Clamp(mediumSegments, 16, 384);
        farRings = Mathf.Clamp(farRings, 8, 192);
        farSegments = Mathf.Clamp(farSegments, 16, 384);
        distantRings = Mathf.Clamp(distantRings, 8, 192);
        distantSegments = Mathf.Clamp(distantSegments, 16, 384);
    }

    private static string GetLodName(int lod)
    {
        return lod switch
        {
            0 => "Close",
            1 => "Near",
            2 => "Medium",
            3 => "Far",
            4 => "Distant",
            _ => "Uninitialized"
        };
    }

    private void DestroyGeneratedMesh()
    {
        if (generatedMesh == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(generatedMesh);
        }
        else
        {
            DestroyImmediate(generatedMesh);
        }

        generatedMesh = null;
    }
}
