using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

/// <summary>
/// Runtime-built, double-precision navigation map for the Float Precision demo.
/// The visual map is a scaled view of simulation space; it never moves simulation objects.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class SimulationMapController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FloatPrecisionPlayer player;
    [SerializeField] private Camera flightCamera;

    [Header("Map")]
    [SerializeField, Range(20f, 100f)] private float systemRadius = 48f;
    [SerializeField, Min(64)] private int trajectorySteps = 320;
    [SerializeField, Min(0.05f)] private float trajectoryRefreshInterval = 0.25f;
    [SerializeField] private Color backgroundColor = new(0.008f, 0.012f, 0.025f, 1f);

    private const int MapLayer = 29;
    private static readonly float[] TimeScaleOptions =
    {
        0.1f, 0.25f, 0.5f, 1f, 2f, 4f, 8f, 16f, 32f, 64f
    };

    private sealed class MapMarker
    {
        public PerspectiveIllusionObject body;
        public GameObject visual;
        public Text label;
        public Color color;
        public bool isPlayer;
        public float screenDiameter;
        public Vector3 visualAspect = Vector3.one;

        public string DisplayName => isPlayer ? "Player" : body.name;
        public DoubleVector3 SimulationPosition => isPlayer ? body.player.playerPosition : body.simulationPosition;
    }

    private readonly List<MapMarker> markers = new();
    private readonly List<PlanetGravityHandler> gravitySources = new();
    private readonly List<Material> runtimeMaterials = new();
    private readonly List<LineRenderer> referenceLines = new();
    private readonly Dictionary<Canvas, bool> suspendedCanvasStates = new();

    private GameObject mapWorld;
    private Camera mapCamera;
    private Canvas mapCanvas;
    private Canvas flightTimeCanvas;
    private RectTransform labelsRoot;
    private Text detailsText;
    private Text trajectoryStatusText;
    private Text titleText;
    private Text mapTimeText;
    private Text flightTimeText;
    private Text mapPauseButtonText;
    private Text flightPauseButtonText;
    private LineRenderer trajectoryLine;
    private GameObject trajectoryEndpointMarker;
    private Text trajectoryEndpointLabel;
    private MapMarker playerMarker;
    private MapMarker selectedMarker;
    private PerspectiveIllusionObject simulationOriginBody;
    private OrbitCamera flightOrbitCamera;
    private Font uiFont;

    private DoubleVector3 simulationOrigin;
    private double metersPerMapUnit = 1.0;
    private float mapExtent = 10f;
    private Vector3 orbitPivot;
    private float orbitYaw = -35f;
    private float orbitPitch = 28f;
    private float orbitDistance = 80f;
    private float nextTrajectoryRefresh;
    private string trajectoryStatus = "Velocity mode inactive";
    private string trajectoryEndpointText;
    private bool mapOpen;
    private bool previousFlightCameraEnabled;
    private bool previousOrbitCameraEnabled;
    private CursorLockMode previousCursorLockMode;
    private bool previousCursorVisible;
    private float originalTimeScale;
    private float originalFixedDeltaTime;
    private float lastRunningTimeScale = 1f;
    private int timeScaleIndex = 3;
    private bool timeInitialized;

    public bool IsOpen => mapOpen;

    private void Awake()
    {
        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Time.fixedDeltaTime;
        timeInitialized = true;
        InitializeTimeScale();

        player ??= GetComponent<FloatPrecisionPlayer>();
        flightCamera ??= Camera.main;
        if (flightCamera != null)
        {
            flightOrbitCamera = flightCamera.GetComponent<OrbitCamera>();
        }

        BuildMap();
        SetMapVisible(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        HandleTimeInput(keyboard);
        UpdateTimeReadouts();

        if (keyboard != null && keyboard.tabKey.wasPressedThisFrame)
        {
            SetMapVisible(!mapOpen);
        }

        if (!mapOpen)
        {
            return;
        }

        HandleMapNavigation();
        UpdateCameraTransform();
        UpdateMapObjects();
        UpdateLabels();
        UpdateDetails();

        if (Time.unscaledTime >= nextTrajectoryRefresh)
        {
            RefreshTrajectory();
            nextTrajectoryRefresh = Time.unscaledTime + trajectoryRefreshInterval;
        }
    }

    private void InitializeTimeScale()
    {
        float initialScale = Mathf.Clamp(originalTimeScale, 0f, TimeScaleOptions[^1]);
        if (initialScale > 0f)
        {
            lastRunningTimeScale = initialScale;
            timeScaleIndex = FindClosestTimeScaleIndex(initialScale);
        }
        ApplyTimeScale(initialScale);
    }

    private void HandleTimeInput(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.leftBracketKey.wasPressedThisFrame) DecreaseTimeScale();
        if (keyboard.rightBracketKey.wasPressedThisFrame) IncreaseTimeScale();
        if (keyboard.backslashKey.wasPressedThisFrame) TogglePause();
        if (keyboard.backspaceKey.wasPressedThisFrame) ResetTimeScale();
    }

    private void DecreaseTimeScale()
    {
        float referenceScale = Time.timeScale > 0f ? Time.timeScale : lastRunningTimeScale;
        timeScaleIndex = Mathf.Max(0, FindClosestTimeScaleIndex(referenceScale) - 1);
        ApplyTimeScale(TimeScaleOptions[timeScaleIndex]);
    }

    private void IncreaseTimeScale()
    {
        float referenceScale = Time.timeScale > 0f ? Time.timeScale : lastRunningTimeScale;
        timeScaleIndex = Mathf.Min(TimeScaleOptions.Length - 1, FindClosestTimeScaleIndex(referenceScale) + 1);
        ApplyTimeScale(TimeScaleOptions[timeScaleIndex]);
    }

    private void TogglePause()
    {
        ApplyTimeScale(Time.timeScale > 0f ? 0f : lastRunningTimeScale);
    }

    private void ResetTimeScale()
    {
        timeScaleIndex = FindClosestTimeScaleIndex(1f);
        ApplyTimeScale(1f);
    }

    private void ApplyTimeScale(float scale)
    {
        scale = Mathf.Clamp(scale, 0f, TimeScaleOptions[^1]);
        if (scale > 0f)
        {
            lastRunningTimeScale = scale;
            timeScaleIndex = FindClosestTimeScaleIndex(scale);
        }

        Time.timeScale = scale;
        // Scale the fixed simulation step with time warp. This keeps the real-time
        // physics workload bounded instead of attempting thousands of gravity steps.
        Time.fixedDeltaTime = scale > 0f
            ? originalFixedDeltaTime * scale
            : originalFixedDeltaTime;
        UpdateTimeReadouts();
    }

    private static int FindClosestTimeScaleIndex(float scale)
    {
        int closest = 0;
        float smallestDifference = float.PositiveInfinity;
        for (int i = 0; i < TimeScaleOptions.Length; i++)
        {
            float difference = Mathf.Abs(TimeScaleOptions[i] - scale);
            if (difference < smallestDifference)
            {
                smallestDifference = difference;
                closest = i;
            }
        }
        return closest;
    }

    private void UpdateTimeReadouts()
    {
        string scaleLabel = Time.timeScale <= 0f
            ? "TIME  PAUSED"
            : $"TIME  {Time.timeScale:0.##}x";
        string pauseLabel = Time.timeScale <= 0f ? "Resume" : "Pause";

        if (mapTimeText != null) mapTimeText.text = scaleLabel;
        if (flightTimeText != null) flightTimeText.text = scaleLabel;
        if (mapPauseButtonText != null) mapPauseButtonText.text = pauseLabel;
        if (flightPauseButtonText != null) flightPauseButtonText.text = pauseLabel;
    }

    private void BuildMap()
    {
        if (player == null)
        {
            Debug.LogError("Simulation map requires a FloatPrecisionPlayer.", this);
            enabled = false;
            return;
        }

        DiscoverSimulationObjects();
        CalculateMapScale();
        BuildMapWorld();
        BuildMapCamera();
        BuildInterface();
        BuildFlightTimeInterface();
        BuildMarkers();
        BuildReferenceGrid();
        BuildTrajectoryLine();
        UpdateMapObjects();
        FitAll();
        UpdateCameraTransform();
        UpdateMapObjects();
    }

    private void DiscoverSimulationObjects()
    {
        PerspectiveIllusionObject[] bodies = FindObjectsByType<PerspectiveIllusionObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        Array.Sort(bodies, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

        double largestDiameter = double.MinValue;
        foreach (PerspectiveIllusionObject body in bodies)
        {
            if (body == null || body.player != player)
            {
                continue;
            }

            Color color = GetBodyColor(body.name);
            markers.Add(new MapMarker { body = body, color = color });

            if (body.simulationScale > largestDiameter)
            {
                largestDiameter = body.simulationScale;
                simulationOriginBody = body;
            }
        }

        PlanetGravityHandler[] gravities = FindObjectsByType<PlanetGravityHandler>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        gravitySources.AddRange(gravities);

        simulationOrigin = simulationOriginBody != null
            ? simulationOriginBody.simulationPosition
            : player.playerPosition;
    }

    private void CalculateMapScale()
    {
        double maximumDistance = 1.0;
        foreach (MapMarker marker in markers)
        {
            maximumDistance = Math.Max(
                maximumDistance,
                (marker.body.simulationPosition - simulationOrigin).Magnitude());
        }

        maximumDistance = Math.Max(
            maximumDistance,
            (player.playerPosition - simulationOrigin).Magnitude());

        metersPerMapUnit = maximumDistance / Math.Max(1f, systemRadius);
        mapExtent = Mathf.Max(5f, (float)(maximumDistance / metersPerMapUnit));
    }

    private void BuildMapWorld()
    {
        mapWorld = new GameObject("Simulation Map World");
        mapWorld.layer = MapLayer;
    }

    private void BuildMapCamera()
    {
        GameObject cameraObject = new("Simulation Map Camera");
        cameraObject.layer = MapLayer;
        mapCamera = cameraObject.AddComponent<Camera>();
        mapCamera.enabled = false;
        mapCamera.clearFlags = CameraClearFlags.SolidColor;
        mapCamera.backgroundColor = backgroundColor;
        mapCamera.cullingMask = 1 << MapLayer;
        // Map selection is handled explicitly through the Input System. Prevent
        // Unity's legacy SendMouseEvents pass from trying to invert this camera's
        // extreme near/far projection every frame and emitting frustum warnings.
        mapCamera.eventMask = 0;
        mapCamera.nearClipPlane = 0.000001f;
        mapCamera.farClipPlane = 5000f;
        mapCamera.fieldOfView = 45f;
        mapCamera.allowHDR = false;
        mapCamera.depth = 100f;

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;

        VolumetricCloudsCameraOverride cloudOverride =
            cameraObject.AddComponent<VolumetricCloudsCameraOverride>();
        cloudOverride.renderClouds = false;
    }

    private void BuildInterface()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new("Simulation Map UI");
        mapCanvas = canvasObject.AddComponent<Canvas>();
        mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mapCanvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform header = CreatePanel(
            canvasObject.transform,
            "Header",
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(0f, 58f),
            new Color(0.02f, 0.035f, 0.065f, 0.94f));

        titleText = CreateText(header, "Title", "NAVIGATION MAP", 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(titleText.rectTransform, new Vector2(0f, 0f), new Vector2(0.32f, 1f), new Vector2(24f, 0f), new Vector2(-10f, 0f));

        trajectoryStatusText = CreateText(header, "Trajectory Status", string.Empty, 17, FontStyle.Bold, TextAnchor.MiddleRight);
        trajectoryStatusText.color = new Color(0.35f, 0.9f, 1f);
        SetRect(trajectoryStatusText.rectTransform, new Vector2(0.68f, 0f), Vector2.one, new Vector2(0f, 0f), new Vector2(-24f, 0f));

        RectTransform mapTimePanel = CreatePanel(
            header,
            "Map Time Controls",
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(390f, 42f),
            new Color(0.025f, 0.07f, 0.1f, 0.92f));
        PopulateTimeControls(mapTimePanel, true);

        RectTransform navigationPanel = CreatePanel(
            canvasObject.transform,
            "Navigation",
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            new Vector2(16f, -76f),
            new Vector2(210f, -96f),
            new Color(0.015f, 0.025f, 0.05f, 0.9f));

        Text objectsHeader = CreateText(navigationPanel, "Objects Header", "OBJECTS", 15, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(objectsHeader.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(14f, -42f), new Vector2(-14f, -12f));

        float buttonY = -50f;
        CreateNavigationButton(navigationPanel, "Overview", buttonY, FitAll);
        buttonY -= 42f;
        CreateNavigationButton(navigationPanel, "Player", buttonY, () => SelectMarker(playerMarker, true));
        buttonY -= 42f;

        foreach (MapMarker marker in markers)
        {
            MapMarker captured = marker;
            CreateNavigationButton(navigationPanel, marker.DisplayName, buttonY, () => SelectMarker(captured, true));
            buttonY -= 42f;
        }

        RectTransform detailsPanel = CreatePanel(
            canvasObject.transform,
            "Details",
            new Vector2(1f, 0f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-326f, -76f),
            new Vector2(310f, -96f),
            new Color(0.015f, 0.025f, 0.05f, 0.9f));

        detailsText = CreateText(detailsPanel, "Details Text", string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft);
        detailsText.horizontalOverflow = HorizontalWrapMode.Wrap;
        detailsText.verticalOverflow = VerticalWrapMode.Overflow;
        detailsText.lineSpacing = 1.15f;
        SetRect(detailsText.rectTransform, Vector2.zero, Vector2.one, new Vector2(16f, 16f), new Vector2(-16f, -16f));

        RectTransform helpPanel = CreatePanel(
            canvasObject.transform,
            "Help",
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 16f),
            new Vector2(1120f, 42f),
            new Color(0.015f, 0.025f, 0.05f, 0.86f));

        Text helpText = CreateText(
            helpPanel,
            "Controls",
            "RMB orbit  •  MMB / Shift+RMB pan  •  Wheel zoom  •  F focus  •  Home overview  •  [ / ] time  •  \\ pause  •  Tab close",
            13,
            FontStyle.Normal,
            TextAnchor.MiddleCenter);
        SetRect(helpText.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-10f, 0f));

        GameObject labelsObject = new("Map Labels");
        labelsRoot = labelsObject.AddComponent<RectTransform>();
        labelsRoot.SetParent(canvasObject.transform, false);
        SetRect(labelsRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        if (EventSystem.current == null)
        {
            GameObject eventSystem = new("Map EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void BuildFlightTimeInterface()
    {
        GameObject canvasObject = new("Flight Time UI");
        flightTimeCanvas = canvasObject.AddComponent<Canvas>();
        flightTimeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        flightTimeCanvas.sortingOrder = 900;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform panel = CreatePanel(
            canvasObject.transform,
            "Flight Time Controls",
            Vector2.one,
            Vector2.one,
            Vector2.one,
            new Vector2(-16f, -16f),
            new Vector2(390f, 48f),
            new Color(0.015f, 0.025f, 0.05f, 0.88f));
        PopulateTimeControls(panel, false);
        UpdateTimeReadouts();
    }

    private void PopulateTimeControls(RectTransform panel, bool mapControls)
    {
        CreateCompactButton(panel, "-", -164f, 42f, DecreaseTimeScale, out _);
        CreateCompactButton(panel, "Pause", -108f, 64f, TogglePause, out Text pauseText);

        Text scaleText = CreateText(panel, "Time Scale", string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform scaleRect = scaleText.rectTransform;
        scaleRect.anchorMin = new Vector2(0.5f, 0.5f);
        scaleRect.anchorMax = new Vector2(0.5f, 0.5f);
        scaleRect.pivot = new Vector2(0.5f, 0.5f);
        scaleRect.anchoredPosition = new Vector2(-6f, 0f);
        scaleRect.sizeDelta = new Vector2(126f, 32f);

        CreateCompactButton(panel, "1x", 83f, 50f, ResetTimeScale, out _);
        CreateCompactButton(panel, "+", 145f, 42f, IncreaseTimeScale, out _);

        if (mapControls)
        {
            mapTimeText = scaleText;
            mapPauseButtonText = pauseText;
        }
        else
        {
            flightTimeText = scaleText;
            flightPauseButtonText = pauseText;
        }
    }

    private void BuildMarkers()
    {
        foreach (MapMarker marker in markers)
        {
            Renderer sourceRenderer = marker.body.GetComponent<Renderer>();
            Material sourceMaterial = sourceRenderer != null ? sourceRenderer.sharedMaterial : null;
            marker.visual = CreateMarkerVisual(
                marker.DisplayName,
                PrimitiveType.Sphere,
                marker.color,
                sourceMaterial);
            marker.label = CreateMapLabel(marker.DisplayName, marker.color);
        }

        playerMarker = new MapMarker
        {
            body = markers.Count > 0 ? markers[0].body : null,
            color = new Color(1f, 0.85f, 0.15f),
            isPlayer = true
        };
        playerMarker.visual = CreatePlayerMarkerVisual(out Vector3 playerAspect);
        playerMarker.visualAspect = playerAspect;
        playerMarker.label = CreateMapLabel("PLAYER", playerMarker.color);
        markers.Add(playerMarker);
        selectedMarker = playerMarker;
    }

    private GameObject CreateMarkerVisual(
        string objectName,
        PrimitiveType primitive,
        Color color,
        Material sourceMaterial = null)
    {
        GameObject marker = GameObject.CreatePrimitive(primitive);
        marker.name = $"Map Marker - {objectName}";
        marker.layer = MapLayer;
        marker.transform.SetParent(mapWorld.transform, false);

        Renderer markerRenderer = marker.GetComponent<Renderer>();
        markerRenderer.sharedMaterial = CreateMapMaterial($"Map {objectName}", color, sourceMaterial);
        markerRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        markerRenderer.receiveShadows = false;
        return marker;
    }

    private GameObject CreatePlayerMarkerVisual(out Vector3 visualAspect)
    {
        Renderer sourceRenderer = player.GetComponentInChildren<Renderer>(true);
        MeshFilter sourceMeshFilter = sourceRenderer != null
            ? sourceRenderer.GetComponent<MeshFilter>()
            : null;

        if (sourceRenderer == null || sourceMeshFilter == null || sourceMeshFilter.sharedMesh == null)
        {
            visualAspect = new Vector3(1f, 0.28f, 0.55f);
            return CreateMarkerVisual("Player", PrimitiveType.Cube, new Color(0.9f, 0.08f, 0.04f));
        }

        GameObject marker = new("Map Marker - Player");
        marker.layer = MapLayer;
        marker.transform.SetParent(mapWorld.transform, false);

        MeshFilter meshFilter = marker.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = sourceMeshFilter.sharedMesh;

        MeshRenderer meshRenderer = marker.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateMapMaterial(
            "Map Player",
            new Color(0.9f, 0.08f, 0.04f),
            sourceRenderer.sharedMaterial);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        marker.AddComponent<BoxCollider>();

        Vector3 sourceScale = sourceRenderer.transform.lossyScale;
        sourceScale = new Vector3(Mathf.Abs(sourceScale.x), Mathf.Abs(sourceScale.y), Mathf.Abs(sourceScale.z));
        float largestAxis = Mathf.Max(sourceScale.x, Mathf.Max(sourceScale.y, sourceScale.z));
        visualAspect = largestAxis > Mathf.Epsilon
            ? sourceScale / largestAxis
            : new Vector3(1f, 0.28f, 0.55f);
        return marker;
    }

    private Text CreateMapLabel(string label, Color color)
    {
        Text text = CreateText(labelsRoot, $"{label} Label", label, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.color = color;
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);
        text.rectTransform.sizeDelta = new Vector2(150f, 24f);
        return text;
    }

    private void BuildReferenceGrid()
    {
        Color gridColor = new(0.12f, 0.25f, 0.4f, 0.34f);
        Color axisColor = new(0.2f, 0.55f, 0.8f, 0.55f);
        float ringStep = mapExtent / 5f;

        for (int ring = 1; ring <= 5; ring++)
        {
            int pointCount = 97;
            LineRenderer line = CreateLine($"Reference Ring {ring}", gridColor, 0.035f);
            referenceLines.Add(line);
            line.loop = true;
            line.positionCount = pointCount - 1;
            Vector3[] points = new Vector3[pointCount - 1];
            float radius = ringStep * ring;
            for (int i = 0; i < points.Length; i++)
            {
                float angle = i / (float)points.Length * Mathf.PI * 2f;
                points[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            }
            line.SetPositions(points);
        }

        CreateAxis("X Axis", Vector3.right, axisColor);
        CreateAxis("Z Axis", Vector3.forward, axisColor);
    }

    private void CreateAxis(string axisName, Vector3 direction, Color color)
    {
        LineRenderer line = CreateLine(axisName, color, 0.055f);
        referenceLines.Add(line);
        line.positionCount = 2;
        line.SetPosition(0, -direction * mapExtent * 1.1f);
        line.SetPosition(1, direction * mapExtent * 1.1f);
    }

    private void BuildTrajectoryLine()
    {
        trajectoryLine = CreateLine("Predicted Player Trajectory", new Color(0.2f, 0.95f, 1f, 0.95f), 0.12f);
        trajectoryLine.positionCount = 0;

        trajectoryEndpointMarker = CreateMarkerVisual(
            "Trajectory Endpoint",
            PrimitiveType.Sphere,
            new Color(1f, 0.35f, 0.12f));
        Collider endpointCollider = trajectoryEndpointMarker.GetComponent<Collider>();
        if (endpointCollider != null)
        {
            Destroy(endpointCollider);
        }
        trajectoryEndpointLabel = CreateMapLabel("Trajectory Endpoint", new Color(1f, 0.5f, 0.2f));
        SetTrajectoryEndpointVisible(false);
    }

    private LineRenderer CreateLine(string lineName, Color color, float width)
    {
        GameObject lineObject = new(lineName);
        lineObject.layer = MapLayer;
        lineObject.transform.SetParent(mapWorld.transform, false);
        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.sharedMaterial = CreateUnlitMaterial(lineName, color);
        line.startColor = color;
        line.endColor = color;
        line.startWidth = width;
        line.endWidth = width;
        line.alignment = LineAlignment.View;
        line.generateLightingData = false;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        return line;
    }

    private Material CreateUnlitMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new(shader) { name = materialName, color = color };
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        }
        material.doubleSidedGI = true;
        runtimeMaterials.Add(material);
        return material;
    }

    private Material CreateMapMaterial(string materialName, Color fallbackColor, Material sourceMaterial)
    {
        Texture sourceTexture = GetFirstTexture(
            sourceMaterial,
            "_DiffuseTexture",
            "_BaseMap",
            "_MainTex",
            "_EmissionTexture");
        Color sourceColor = sourceTexture != null
            ? Color.white
            : GetFirstColor(sourceMaterial, fallbackColor, "_BaseColor", "_Color");
        Material material = CreateUnlitMaterial(materialName, sourceColor);

        if (sourceTexture != null)
        {
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", sourceTexture);
            }
            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", sourceTexture);
            }
        }
        return material;
    }

    private static Texture GetFirstTexture(Material material, params string[] propertyNames)
    {
        if (material == null)
        {
            return null;
        }

        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                Texture texture = material.GetTexture(propertyName);
                if (texture != null)
                {
                    return texture;
                }
            }
        }
        return null;
    }

    private static Color GetFirstColor(Material material, Color fallback, params string[] propertyNames)
    {
        if (material == null)
        {
            return fallback;
        }

        foreach (string propertyName in propertyNames)
        {
            if (material.HasProperty(propertyName))
            {
                return material.GetColor(propertyName);
            }
        }
        return fallback;
    }

    private void SetMapVisible(bool visible)
    {
        if (mapWorld == null || mapCamera == null || mapCanvas == null || player == null)
        {
            return;
        }

        if (visible == mapOpen)
        {
            mapWorld.SetActive(visible);
            mapCanvas.gameObject.SetActive(visible);
            mapCamera.gameObject.SetActive(visible);
            mapCamera.enabled = visible;
            return;
        }

        mapOpen = visible;
        if (visible)
        {
            previousCursorLockMode = Cursor.lockState;
            previousCursorVisible = Cursor.visible;
            previousFlightCameraEnabled = flightCamera != null && flightCamera.enabled;
            previousOrbitCameraEnabled = flightOrbitCamera != null && flightOrbitCamera.enabled;

            if (flightCamera != null) flightCamera.enabled = false;
            if (flightOrbitCamera != null) flightOrbitCamera.enabled = false;
            player.FlightInputEnabled = false;
            SuspendOtherCanvases();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            mapWorld.SetActive(true);
            mapCanvas.gameObject.SetActive(true);
            mapCamera.gameObject.SetActive(true);
            mapCamera.enabled = true;
            nextTrajectoryRefresh = 0f;
            UpdateMapObjects();
            UpdateCameraTransform();
            RefreshTrajectory();
        }
        else
        {
            mapWorld.SetActive(false);
            mapCanvas.gameObject.SetActive(false);
            mapCamera.enabled = false;
            mapCamera.gameObject.SetActive(false);

            if (flightCamera != null) flightCamera.enabled = previousFlightCameraEnabled;
            if (flightOrbitCamera != null) flightOrbitCamera.enabled = previousOrbitCameraEnabled;
            player.FlightInputEnabled = true;
            RestoreOtherCanvases();
            Cursor.lockState = previousCursorLockMode;
            Cursor.visible = previousCursorVisible;
        }
    }

    private void HandleMapNavigation()
    {
        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        if (mouse == null)
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        bool panModifier = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

        if (mouse.rightButton.isPressed && !panModifier)
        {
            orbitYaw += delta.x * 0.18f;
            orbitPitch = Mathf.Clamp(orbitPitch - delta.y * 0.18f, -85f, 85f);
        }

        if (mouse.middleButton.isPressed || (mouse.rightButton.isPressed && panModifier))
        {
            float panScale = orbitDistance * 0.0015f;
            orbitPivot -= mapCamera.transform.right * delta.x * panScale;
            orbitPivot -= mapCamera.transform.up * delta.y * panScale;
        }

        float scroll = mouse.scroll.ReadValue().y;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            orbitDistance *= Mathf.Exp(-scroll * 0.0015f);
            orbitDistance = Mathf.Clamp(orbitDistance, 0.00001f, mapExtent * 40f);
        }

        if (keyboard != null)
        {
            Vector3 pan = Vector3.zero;
            if (keyboard.wKey.isPressed) pan += mapCamera.transform.up;
            if (keyboard.sKey.isPressed) pan -= mapCamera.transform.up;
            if (keyboard.dKey.isPressed) pan += mapCamera.transform.right;
            if (keyboard.aKey.isPressed) pan -= mapCamera.transform.right;
            if (pan.sqrMagnitude > 0f)
            {
                orbitPivot += pan.normalized * orbitDistance * Time.unscaledDeltaTime;
            }

            if (keyboard.fKey.wasPressedThisFrame) FocusSelection();
            if (keyboard.homeKey.wasPressedThisFrame) FitAll();
        }

        if (mouse.leftButton.wasPressedThisFrame && !IsPointerOverInterface())
        {
            if (TryGetMapPointerRay(mouse.position.ReadValue(), out Ray ray) &&
                Physics.Raycast(ray, out RaycastHit hit, mapCamera.farClipPlane, 1 << MapLayer))
            {
                MapMarker marker = FindMarker(hit.collider.gameObject);
                if (marker != null)
                {
                    SelectMarker(marker, false);
                }
            }
        }
    }

    private bool TryGetMapPointerRay(Vector2 pointerPosition, out Ray ray)
    {
        ray = default;

        if (mapCamera == null || !mapCamera.isActiveAndEnabled)
        {
            return false;
        }

        Rect pixelRect = mapCamera.pixelRect;
        if (pixelRect.width <= Mathf.Epsilon || pixelRect.height <= Mathf.Epsilon ||
            !pixelRect.Contains(pointerPosition))
        {
            return false;
        }

        float viewportX = Mathf.InverseLerp(pixelRect.xMin, pixelRect.xMax, pointerPosition.x);
        float viewportY = Mathf.InverseLerp(pixelRect.yMin, pixelRect.yMax, pointerPosition.y);

        // Camera.ScreenPointToRay/ViewportPointToRay reject this map camera when its
        // near plane is tiny enough to inspect player-scale objects in a solar-system
        // scene. Build the ray directly so picking does not depend on Unity inverting
        // that extreme projection matrix.
        float normalizedX = viewportX * 2f - 1f;
        float normalizedY = viewportY * 2f - 1f;
        float aspect = pixelRect.width / pixelRect.height;
        Transform cameraTransform = mapCamera.transform;

        if (mapCamera.orthographic)
        {
            float halfHeight = mapCamera.orthographicSize;
            Vector3 origin = cameraTransform.position +
                             cameraTransform.right * (normalizedX * halfHeight * aspect) +
                             cameraTransform.up * (normalizedY * halfHeight);
            ray = new Ray(origin, cameraTransform.forward);
            return true;
        }

        float halfFovTangent = Mathf.Tan(mapCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 localDirection = new(
            normalizedX * halfFovTangent * aspect,
            normalizedY * halfFovTangent,
            1f);
        ray = new Ray(
            cameraTransform.position,
            cameraTransform.TransformDirection(localDirection).normalized);
        return true;
    }

    private static bool IsPointerOverInterface()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private MapMarker FindMarker(GameObject hitObject)
    {
        foreach (MapMarker marker in markers)
        {
            if (marker.visual == hitObject)
            {
                return marker;
            }
        }
        return null;
    }

    private void SelectMarker(MapMarker marker, bool focus)
    {
        if (marker == null)
        {
            return;
        }

        selectedMarker = marker;
        if (focus)
        {
            FocusSelection();
        }
        UpdateDetails();
    }

    private void FocusSelection()
    {
        if (selectedMarker?.visual == null)
        {
            return;
        }

        orbitPivot = selectedMarker.visual.transform.position;
        float nearestDistance = float.PositiveInfinity;
        foreach (MapMarker marker in markers)
        {
            if (marker == selectedMarker || marker.visual == null)
            {
                continue;
            }

            float distance = Vector3.Distance(orbitPivot, marker.visual.transform.position);
            if (distance > 0.00001f)
            {
                nearestDistance = Mathf.Min(nearestDistance, distance);
            }
        }

        if (!float.IsFinite(nearestDistance))
        {
            nearestDistance = mapExtent * 0.1f;
        }

        orbitDistance = Mathf.Clamp(nearestDistance * 2.2f, 0.00001f, mapExtent * 3f);
    }

    private void FitAll()
    {
        if (markers.Count == 0)
        {
            orbitPivot = Vector3.zero;
            orbitDistance = 30f;
            return;
        }

        Bounds bounds = new(markers[0].visual != null ? markers[0].visual.transform.position : Vector3.zero, Vector3.zero);
        foreach (MapMarker marker in markers)
        {
            if (marker.visual != null)
            {
                bounds.Encapsulate(marker.visual.transform.position);
            }
        }

        orbitPivot = bounds.center;
        float halfFovRadians = mapCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        orbitDistance = Mathf.Max(12f, bounds.extents.magnitude / Mathf.Tan(halfFovRadians) * 1.25f);
        selectedMarker = playerMarker;
    }

    private void UpdateMapObjects()
    {
        float viewportHeight = Mathf.Max(1f, Screen.height);
        float halfFovTangent = Mathf.Tan(mapCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);

        foreach (MapMarker marker in markers)
        {
            if (marker.visual == null)
            {
                continue;
            }

            marker.visual.transform.position = ToMapPosition(marker.SimulationPosition);
            float distance = Mathf.Max(0.000001f, Vector3.Distance(mapCamera.transform.position, marker.visual.transform.position));
            float basePixels = marker == selectedMarker
                ? 24f
                : marker.isPlayer
                    ? 17f
                    : Mathf.Lerp(12f, 22f, GetBodySizeFactor(marker.body));

            // Keep the selected/local context readable without allowing distant solar-system
            // markers to stay full-sized while inspecting a player-scale orbit.
            float pivotDistance = Vector3.Distance(orbitPivot, marker.visual.transform.position);
            float contextRatio = pivotDistance / Mathf.Max(orbitDistance, 0.000001f);
            float contextScale = marker == selectedMarker
                ? 1f
                : 1f / Mathf.Sqrt(1f + contextRatio * 0.45f);
            float iconPixels = Mathf.Clamp(basePixels * contextScale, 2f, basePixels);
            float iconDiameter = 2f * distance * halfFovTangent * iconPixels / viewportHeight;
            float physicalDiameter = marker.isPlayer || marker.body == null
                ? 0f
                : Mathf.Max(0.000001f, (float)(marker.body.simulationScale / metersPerMapUnit));
            float diameter = Mathf.Max(iconDiameter, physicalDiameter);
            float physicalPixels = physicalDiameter > 0f
                ? physicalDiameter / (2f * distance * halfFovTangent) * viewportHeight
                : 0f;
            marker.screenDiameter = Mathf.Max(iconPixels, physicalPixels);
            marker.visual.transform.localScale = marker.visualAspect * Mathf.Clamp(
                diameter,
                0.000001f,
                mapExtent * 0.2f);

            if (marker.isPlayer)
            {
                marker.visual.transform.rotation = player.transform.rotation;
            }
            else
            {
                marker.visual.transform.rotation = marker.body.transform.rotation;
            }
        }

        // A screen-space-like width keeps small orbital curves visible instead of
        // turning them into a solid, apparently straight strip at close zoom.
        float trajectoryWidth = 2f * orbitDistance * halfFovTangent * 1.5f / viewportHeight;
        trajectoryWidth = Mathf.Clamp(trajectoryWidth, 0.000001f, mapExtent * 0.003f);
        trajectoryLine.startWidth = trajectoryWidth;
        trajectoryLine.endWidth = trajectoryWidth;

        if (trajectoryEndpointMarker.activeSelf)
        {
            float endpointDistance = Mathf.Max(
                0.01f,
                Vector3.Distance(mapCamera.transform.position, trajectoryEndpointMarker.transform.position));
            float endpointDiameter = 2f * endpointDistance * halfFovTangent * 10f / viewportHeight;
            trajectoryEndpointMarker.transform.localScale = Vector3.one * Mathf.Max(0.000001f, endpointDiameter);
        }

        bool showSystemGuides = orbitDistance >= mapExtent * 0.08f;
        float guideWidth = Mathf.Clamp(
            2f * orbitDistance * halfFovTangent * 0.55f / viewportHeight,
            0.000001f,
            mapExtent * 0.0015f);
        foreach (LineRenderer referenceLine in referenceLines)
        {
            if (referenceLine == null)
            {
                continue;
            }

            referenceLine.enabled = showSystemGuides;
            referenceLine.startWidth = guideWidth;
            referenceLine.endWidth = guideWidth;
        }
    }

    private void UpdateCameraTransform()
    {
        Quaternion rotation = Quaternion.Euler(orbitPitch, orbitYaw, 0f);
        mapCamera.transform.SetPositionAndRotation(
            orbitPivot + rotation * new Vector3(0f, 0f, -orbitDistance),
            rotation);
    }

    private void UpdateLabels()
    {
        foreach (MapMarker marker in markers)
        {
            Vector3 screen = mapCamera.WorldToScreenPoint(marker.visual.transform.position);
            bool onScreen = screen.x >= 0f && screen.x <= Screen.width &&
                            screen.y >= 0f && screen.y <= Screen.height;
            bool visible = screen.z > 0f && onScreen &&
                           (marker == selectedMarker || marker.screenDiameter >= 7f);
            marker.label.gameObject.SetActive(visible);
            if (visible)
            {
                float labelOffset = marker.isPlayer
                    ? 18f
                    : Mathf.Clamp(marker.screenDiameter * 0.5f + 10f, 18f, 100f);
                marker.label.rectTransform.position = new Vector3(screen.x, screen.y + labelOffset, 0f);
                marker.label.fontStyle = marker == selectedMarker ? FontStyle.Bold : FontStyle.Normal;
            }
        }


        if (trajectoryEndpointMarker.activeSelf)
        {
            Vector3 screen = mapCamera.WorldToScreenPoint(trajectoryEndpointMarker.transform.position);
            bool onScreen = screen.z > 0f &&
                            screen.x >= 0f && screen.x <= Screen.width &&
                            screen.y >= 0f && screen.y <= Screen.height;
            trajectoryEndpointLabel.gameObject.SetActive(onScreen);
            if (onScreen)
            {
                trajectoryEndpointLabel.rectTransform.position = new Vector3(screen.x, screen.y + 16f, 0f);
            }
        }
    }

    private void UpdateDetails()
    {
        if (detailsText == null || selectedMarker == null)
        {
            return;
        }

        if (selectedMarker.isPlayer)
        {
            PlanetGravityHandler dominant = FindDominantGravity(player.playerPosition);
            string nearest = dominant?.Planet != null ? dominant.Planet.name : "None";
            double altitude = dominant?.Planet != null
                ? (player.playerPosition - dominant.Planet.simulationPosition).Magnitude() - dominant.Planet.simulationScale * 0.5
                : 0.0;
            DoubleVector3 velocity = player.GetVelocity();
            double displayedSpeed = player.VelocityActive
                ? velocity.Magnitude()
                : player.MovementSpeed;
            double centerDistance = dominant?.Planet != null
                ? (player.playerPosition - dominant.Planet.simulationPosition).Magnitude()
                : 0.0;
            double localGravity = dominant != null
                ? dominant.CalculateGravityAtPosition(player.playerPosition)
                : 0.0;
            double circularOrbitSpeed = dominant != null && centerDistance > double.Epsilon
                ? Math.Sqrt(dominant.GravitationalParameter / centerDistance)
                : 0.0;

            detailsText.text =
                "PLAYER\n\n" +
                $"Mode\n{(player.VelocityActive ? "Velocity simulation" : "Direct movement")}\n\n" +
                $"Time scale\n{(Time.timeScale <= 0f ? "Paused" : Time.timeScale.ToString("0.##") + "x")}\n\n" +
                $"Speed\n{FormatDistance(displayedSpeed)}/s\n\n" +
                $"Dominant body\n{nearest}\n\n" +
                $"Altitude\n{FormatDistance(altitude)}\n\n" +
                $"Local gravity\n{localGravity:0.###} m/s2\n\n" +
                $"Circular orbit speed\n{FormatDistance(circularOrbitSpeed)}/s\n\n" +
                $"Trajectory\n{trajectoryStatus}\n\n" +
                $"Simulation position\nX {FormatCoordinate(player.playerPosition.x)}\nY {FormatCoordinate(player.playerPosition.y)}\nZ {FormatCoordinate(player.playerPosition.z)}";
        }
        else
        {
            PerspectiveIllusionObject body = selectedMarker.body;
            PlanetGravityHandler gravity = body.GetComponent<PlanetGravityHandler>();
            double centerDistance = (player.playerPosition - body.simulationPosition).Magnitude();
            double altitude = centerDistance - body.simulationScale * 0.5;

            detailsText.text =
                $"{body.name.ToUpperInvariant()}\n\n" +
                $"Diameter\n{FormatDistance(body.simulationScale)}\n\n" +
                $"Player distance\n{FormatDistance(centerDistance)}\n\n" +
                $"Player altitude\n{FormatDistance(altitude)}\n\n" +
                $"Mass\n{(gravity != null ? gravity.Mass.ToString("0.###e+0") + " kg" : "Not simulated")}\n\n" +
                $"Simulation position\nX {FormatCoordinate(body.simulationPosition.x)}\nY {FormatCoordinate(body.simulationPosition.y)}\nZ {FormatCoordinate(body.simulationPosition.z)}";
        }

        trajectoryStatusText.text = player.VelocityActive
            ? trajectoryStatus.ToUpperInvariant()
            : "TRAJECTORY HIDDEN — VELOCITY MODE OFF";
    }

    private void RefreshTrajectory()
    {
        if (!player.VelocityActive)
        {
            trajectoryLine.positionCount = 0;
            trajectoryStatus = "Velocity mode inactive";
            SetTrajectoryEndpointVisible(false);
            return;
        }

        DoubleVector3 position = player.playerPosition;
        DoubleVector3 velocity = player.GetVelocity();
        PlanetGravityHandler dominant = FindDominantGravity(position);
        double horizon = CalculatePredictionHorizon(position, velocity, dominant);
        double sampleTimeStep = horizon / Math.Max(1, trajectorySteps - 1);
        List<Vector3> points = new(trajectorySteps) { ToMapPosition(position) };
        string collisionName = null;

        for (int i = 1; i < trajectorySteps; i++)
        {
            PlanetGravityHandler integrationDominant = FindDominantGravity(position);
            int substeps = CalculateIntegrationSubsteps(position, sampleTimeStep, integrationDominant);
            double timeStep = sampleTimeStep / substeps;

            for (int substep = 0; substep < substeps; substep++)
            {
                // Velocity Verlet is much more stable for curved and closed orbits
                // than the previous single-step Euler prediction.
                DoubleVector3 acceleration = CalculateAccelerationAtPosition(position);
                position += velocity * timeStep + acceleration * (0.5 * timeStep * timeStep);
                DoubleVector3 nextAcceleration = CalculateAccelerationAtPosition(position);
                velocity += (acceleration + nextAcceleration) * (0.5 * timeStep);

                collisionName = FindCollisionName(position);
                if (collisionName != null)
                {
                    break;
                }
            }

            points.Add(ToMapPosition(position));

            if (collisionName != null)
            {
                break;
            }
        }

        trajectoryLine.positionCount = points.Count;
        trajectoryLine.SetPositions(points.ToArray());
        trajectoryStatus = ClassifyTrajectory(player.playerPosition, player.GetVelocity(), dominant, collisionName);

        trajectoryEndpointMarker.transform.position = points[^1];
        trajectoryEndpointText = collisionName != null
            ? $"IMPACT: {collisionName.ToUpperInvariant()}"
            : "PREDICTION LIMIT";
        trajectoryEndpointLabel.text = trajectoryEndpointText;
        SetTrajectoryEndpointVisible(true);
    }

    private void SetTrajectoryEndpointVisible(bool visible)
    {
        if (trajectoryEndpointMarker != null)
        {
            trajectoryEndpointMarker.SetActive(visible);
        }
        if (trajectoryEndpointLabel != null)
        {
            trajectoryEndpointLabel.gameObject.SetActive(visible);
        }
    }

    private string FindCollisionName(DoubleVector3 position)
    {
        foreach (MapMarker marker in markers)
        {
            if (marker.isPlayer || marker.body == null)
            {
                continue;
            }

            double distanceToCenter = (position - marker.body.simulationPosition).Magnitude();
            if (distanceToCenter <= marker.body.simulationScale * 0.5)
            {
                return marker.body.name;
            }
        }
        return null;
    }

    private DoubleVector3 CalculateAccelerationAtPosition(DoubleVector3 position)
    {
        DoubleVector3 acceleration = DoubleVector3.Zero;
        foreach (PlanetGravityHandler gravity in gravitySources)
        {
            if (gravity != null && gravity.isActiveAndEnabled)
            {
                acceleration += gravity.CalculateGravityForceAtPosition(position);
            }
        }
        return acceleration;
    }

    private static int CalculateIntegrationSubsteps(
        DoubleVector3 position,
        double sampleTimeStep,
        PlanetGravityHandler dominant)
    {
        if (dominant?.Planet == null || dominant.GravitationalParameter <= double.Epsilon)
        {
            return 1;
        }

        double radius = Math.Max(
            dominant.Planet.simulationScale * 0.5,
            (position - dominant.Planet.simulationPosition).Magnitude());
        double localDynamicalTime = Math.Sqrt(radius * radius * radius / dominant.GravitationalParameter);
        double maximumStableStep = Math.Max(0.02, localDynamicalTime * 0.025);
        return Math.Clamp((int)Math.Ceiling(sampleTimeStep / maximumStableStep), 1, 32);
    }

    private PlanetGravityHandler FindDominantGravity(DoubleVector3 position)
    {
        PlanetGravityHandler dominant = null;
        double strongestAcceleration = 0.0;
        foreach (PlanetGravityHandler gravity in gravitySources)
        {
            if (gravity == null || !gravity.isActiveAndEnabled || gravity.Planet == null)
            {
                continue;
            }

            double acceleration = gravity.CalculateGravityAtPosition(position);
            if (acceleration > strongestAcceleration)
            {
                strongestAcceleration = acceleration;
                dominant = gravity;
            }
        }
        return dominant;
    }

    private static double CalculatePredictionHorizon(
        DoubleVector3 position,
        DoubleVector3 velocity,
        PlanetGravityHandler dominant)
    {
        const double minimumHorizon = 600.0;
        const double maximumHorizon = 60.0 * 60.0 * 24.0 * 60.0;

        if (dominant?.Planet == null || dominant.GravitationalParameter <= double.Epsilon)
        {
            return Math.Clamp(position.Magnitude() / Math.Max(1.0, velocity.Magnitude()) * 2.0, minimumHorizon, maximumHorizon);
        }

        double radius = (position - dominant.Planet.simulationPosition).Magnitude();
        double orbitalPeriod = 2.0 * Math.PI * Math.Sqrt(radius * radius * radius / dominant.GravitationalParameter);
        double travelTime = radius / Math.Max(1.0, velocity.Magnitude());
        return Math.Clamp(Math.Max(orbitalPeriod * 1.15, travelTime * 2.0), minimumHorizon, maximumHorizon);
    }

    private static string ClassifyTrajectory(
        DoubleVector3 position,
        DoubleVector3 velocity,
        PlanetGravityHandler dominant,
        string collisionName)
    {
        if (collisionName != null)
        {
            return $"Impact course: {collisionName}";
        }

        if (dominant?.Planet == null || dominant.GravitationalParameter <= double.Epsilon)
        {
            return "Ballistic path";
        }

        double radius = (position - dominant.Planet.simulationPosition).Magnitude();
        double specificEnergy = velocity.Dot(velocity) * 0.5 - dominant.GravitationalParameter / Math.Max(1.0, radius);
        return specificEnergy < 0.0
            ? $"Bound orbit: {dominant.Planet.name}"
            : $"Unbound path: {dominant.Planet.name}";
    }

    private Vector3 ToMapPosition(DoubleVector3 position)
    {
        return (Vector3)((position - simulationOrigin) * (1.0 / metersPerMapUnit));
    }

    private float GetBodySizeFactor(PerspectiveIllusionObject body)
    {
        if (body == null || simulationOriginBody == null || simulationOriginBody.simulationScale <= double.Epsilon)
        {
            return 0f;
        }
        return Mathf.Clamp01(Mathf.Sqrt((float)(body.simulationScale / simulationOriginBody.simulationScale)));
    }

    private static Color GetBodyColor(string bodyName)
    {
        string lower = bodyName.ToLowerInvariant();
        if (lower.Contains("sun")) return new Color(1f, 0.72f, 0.18f);
        if (lower.Contains("earth")) return new Color(0.2f, 0.62f, 1f);
        if (lower.Contains("mars")) return new Color(1f, 0.32f, 0.14f);
        if (lower.Contains("moon")) return new Color(0.72f, 0.76f, 0.82f);
        return new Color(0.65f, 0.75f, 1f);
    }

    private static string FormatDistance(double meters)
    {
        double absolute = Math.Abs(meters);
        if (absolute >= 1.0e12) return $"{meters / 1.0e12:0.###} Tm";
        if (absolute >= 1.0e9) return $"{meters / 1.0e9:0.###} Gm";
        if (absolute >= 1.0e6) return $"{meters / 1.0e6:0.###} Mm";
        if (absolute >= 1.0e3) return $"{meters / 1.0e3:0.###} km";
        return $"{meters:0.###} m";
    }

    private static string FormatCoordinate(double value)
    {
        return Math.Abs(value) >= 1.0e7 ? value.ToString("0.######e+0") : value.ToString("0.###");
    }

    private RectTransform CreatePanel(
        Transform parent,
        string panelName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color)
    {
        GameObject panelObject = new(panelName);
        RectTransform rect = panelObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        panelObject.AddComponent<Image>().color = color;
        return rect;
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        string content,
        int fontSize,
        FontStyle style,
        TextAnchor alignment)
    {
        GameObject textObject = new(objectName);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.88f, 0.94f, 1f);
        text.raycastTarget = false;
        return text;
    }

    private void CreateNavigationButton(RectTransform parent, string label, float y, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new($"{label} Button");
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-20f, 34f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.15f, 0.25f, 0.94f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.14f, 0.32f, 0.5f, 1f);
        colors.pressedColor = new Color(0.1f, 0.48f, 0.68f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        Text text = CreateText(buttonObject.transform, "Label", label, 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-8f, 0f));
    }

    private void CreateCompactButton(
        RectTransform parent,
        string label,
        float x,
        float width,
        UnityEngine.Events.UnityAction action,
        out Text labelText)
    {
        GameObject buttonObject = new($"{label} Time Button");
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 0f);
        rect.sizeDelta = new Vector2(width, 30f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.17f, 0.27f, 0.98f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.14f, 0.38f, 0.55f, 1f);
        colors.pressedColor = new Color(0.1f, 0.58f, 0.72f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        labelText = CreateText(buttonObject.transform, "Label", label, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        SetRect(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void SuspendOtherCanvases()
    {
        suspendedCanvasStates.Clear();
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas == mapCanvas)
            {
                continue;
            }

            suspendedCanvasStates[canvas] = canvas.enabled;
            canvas.enabled = false;
        }
    }

    private void RestoreOtherCanvases()
    {
        foreach (KeyValuePair<Canvas, bool> entry in suspendedCanvasStates)
        {
            if (entry.Key != null)
            {
                entry.Key.enabled = entry.Value;
            }
        }
        suspendedCanvasStates.Clear();
    }

    private void OnDisable()
    {
        if (mapOpen)
        {
            SetMapVisible(false);
        }
    }

    private void OnDestroy()
    {
        if (timeInitialized)
        {
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }

        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }

        if (mapWorld != null) Destroy(mapWorld);
        if (mapCamera != null) Destroy(mapCamera.gameObject);
        if (mapCanvas != null) Destroy(mapCanvas.gameObject);
        if (flightTimeCanvas != null) Destroy(flightTimeCanvas.gameObject);
    }
}
