using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Runtime telemetry and persistent double-precision teleport locations for the
/// FloatPrecision demo. The UI is generated at runtime so existing scenes keep
/// working while their legacy HUD remains available as a serialized fallback.
/// </summary>
[DisallowMultipleComponent]
public sealed class FloatPrecisionHudController : MonoBehaviour
{
    private const string SaveKey = "TechArtLibrary.FloatPrecision.SavedLocations.v1";
    private const float CardWidth = 360f;
    private const float CardHeight = 306f;
    private const float LocationsWidth = 470f;
    private const float LocationsHeight = 570f;

    private static readonly Color PanelColor = new(0.025f, 0.065f, 0.105f, 0.94f);
    private static readonly Color PanelSecondary = new(0.045f, 0.105f, 0.16f, 0.98f);
    private static readonly Color Accent = new(0.10f, 0.78f, 0.92f, 1f);
    private static readonly Color TextPrimary = new(0.90f, 0.96f, 1f, 1f);
    private static readonly Color TextSecondary = new(0.55f, 0.69f, 0.78f, 1f);
    private static readonly Color Danger = new(0.83f, 0.24f, 0.25f, 1f);

    [Serializable]
    public sealed class SavedLocation
    {
        public string id;
        public string displayName;
        public string x;
        public string y;
        public string z;
        public float rotationX;
        public float rotationY;
        public float rotationZ;
        public float rotationW;
        public string savedUtc;

        public DoubleVector3 Position => new(
            ParseDouble(x),
            ParseDouble(y),
            ParseDouble(z));

        public Quaternion Rotation => new(rotationX, rotationY, rotationZ, rotationW);

        private static double ParseDouble(string value)
        {
            return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
                ? result
                : 0.0;
        }
    }

    [Serializable]
    private sealed class SavedLocationCollection
    {
        public List<SavedLocation> locations = new();
    }

    private FloatPrecisionPlayer player;
    private SimulationMapController mapController;
    private PerspectiveIllusionObject nearestBody;
    private readonly List<PerspectiveIllusionObject> celestialBodies = new();
    private SavedLocationCollection saved = new();
    private readonly List<Graphic> hiddenLegacyGraphics = new();

    private GameObject legacyInfo;
    private GameObject hudRoot;
    private GameObject locationsPanel;
    private RectTransform locationsContent;
    private TextMeshProUGUI telemetryText;
    private TextMeshProUGUI locationsButtonText;
    private TextMeshProUGUI statusText;
    private TMP_InputField nameInput;
    private bool locationsOpen;

    public int SavedLocationCount => saved.locations.Count;
    public bool LocationsPanelOpen => locationsOpen;

    private void Awake()
    {
        player = GetComponent<FloatPrecisionPlayer>();
        mapController = GetComponent<SimulationMapController>();
        LoadLocations();
        FindCelestialBodies();
        BuildHud();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame && !IsEditingName())
        {
            SetLocationsPanelVisible(!locationsOpen);
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && locationsOpen)
        {
            SetLocationsPanelVisible(false);
        }

        bool mapOpen = mapController != null && mapController.IsOpen;
        if (hudRoot != null)
        {
            hudRoot.SetActive(!mapOpen);
        }

        player.FlightInputEnabled = !mapOpen && !locationsOpen && !IsEditingName();
        UpdateTelemetry();
    }

    private void OnDestroy()
    {
        foreach (Graphic graphic in hiddenLegacyGraphics)
        {
            if (graphic != null)
            {
                graphic.enabled = true;
            }
        }

        hiddenLegacyGraphics.Clear();
    }

    public string SaveCurrentLocation(string requestedName)
    {
        string locationName = MakeUniqueName(string.IsNullOrWhiteSpace(requestedName)
            ? $"Location {saved.locations.Count + 1}"
            : requestedName.Trim());

        Quaternion rotation = player.transform.rotation;
        SavedLocation location = new()
        {
            id = Guid.NewGuid().ToString("N"),
            displayName = locationName,
            x = player.playerPosition.x.ToString("R", CultureInfo.InvariantCulture),
            y = player.playerPosition.y.ToString("R", CultureInfo.InvariantCulture),
            z = player.playerPosition.z.ToString("R", CultureInfo.InvariantCulture),
            rotationX = rotation.x,
            rotationY = rotation.y,
            rotationZ = rotation.z,
            rotationW = rotation.w,
            savedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        };

        saved.locations.Add(location);
        PersistLocations();
        RebuildLocationRows();
        SetStatus($"Saved {location.displayName}", Accent);
        return location.id;
    }

    public bool TeleportToLocation(string id)
    {
        SavedLocation location = saved.locations.Find(item => item.id == id);
        if (location == null)
        {
            SetStatus("Location no longer exists", Danger);
            return false;
        }

        player.playerPosition = location.Position;
        player.SetVelocity(new DoubleVector3(0, 0, 0));
        player.transform.rotation = NormalizeSafe(location.Rotation);
        Physics.SyncTransforms();
        SetStatus($"Teleported to {location.displayName}", Accent);
        return true;
    }

    public bool DeleteLocation(string id)
    {
        int removed = saved.locations.RemoveAll(item => item.id == id);
        if (removed == 0)
        {
            return false;
        }

        PersistLocations();
        RebuildLocationRows();
        SetStatus("Location deleted", TextSecondary);
        return true;
    }

    public string GetSavedLocationId(int index)
    {
        return index >= 0 && index < saved.locations.Count ? saved.locations[index].id : string.Empty;
    }

    public string GetSavedLocationName(int index)
    {
        return index >= 0 && index < saved.locations.Count ? saved.locations[index].displayName : string.Empty;
    }

    private void FindCelestialBodies()
    {
        celestialBodies.Clear();
        foreach (PerspectiveIllusionObject body in FindObjectsOfType<PerspectiveIllusionObject>())
        {
            if (body.simulationScale > 0.0)
            {
                celestialBodies.Add(body);
            }
        }
    }

    private void UpdateTelemetry()
    {
        if (telemetryText == null || player == null)
        {
            return;
        }

        nearestBody = FindNearestBody(out double centerDistance, out double altitude);
        string bodyName = nearestBody != null ? nearestBody.name : "Deep space";
        double speed = player.VelocityActive ? player.GetVelocity().Magnitude() : player.MovementSpeed;
        string mode = player.VelocityActive ? "VELOCITY SIMULATION" : "DIRECT FLIGHT";
        DoubleVector3 position = player.playerPosition;

        telemetryText.text =
            $"<color=#{ToHex(TextSecondary)}>NEAREST BODY</color>   <b>{bodyName.ToUpperInvariant()}</b>\n" +
            $"<color=#{ToHex(TextSecondary)}>ALTITUDE</color>          <b>{FormatDistance(altitude)}</b>\n" +
            $"<color=#{ToHex(TextSecondary)}>CENTER RANGE</color>   <b>{FormatDistance(centerDistance)}</b>\n\n" +
            $"<color=#{ToHex(TextSecondary)}>SIMULATION POSITION</color>\n" +
            $"X  {FormatCoordinate(position.x)}\n" +
            $"Y  {FormatCoordinate(position.y)}\n" +
            $"Z  {FormatCoordinate(position.z)}\n\n" +
            $"<color=#{ToHex(TextSecondary)}>SPEED</color>   <b>{FormatSpeed(speed)}</b>    " +
            $"<color=#{ToHex(Accent)}>{mode}</color>";
    }

    private PerspectiveIllusionObject FindNearestBody(out double centerDistance, out double altitude)
    {
        PerspectiveIllusionObject closest = null;
        centerDistance = double.PositiveInfinity;
        altitude = double.PositiveInfinity;

        foreach (PerspectiveIllusionObject body in celestialBodies)
        {
            double distance = (body.simulationPosition - player.playerPosition).Magnitude();
            double surface = distance - body.simulationScale * 0.5;
            if (surface < altitude)
            {
                closest = body;
                centerDistance = distance;
                altitude = surface;
            }
        }

        return closest;
    }

    private void BuildHud()
    {
        Canvas canvas = null;
        foreach (Canvas candidate in FindObjectsOfType<Canvas>(true))
        {
            if (candidate.transform.Find("Info") != null)
            {
                canvas = candidate;
                break;
            }
        }

        canvas ??= FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new("Float Precision HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Transform legacy = canvas.transform.Find("Info");
        if (legacy != null)
        {
            legacyInfo = legacy.gameObject;
            // Serialized telemetry UnityEvents still target TextChanger components in
            // this hierarchy. Keep those behaviours active and hide only the visuals.
            foreach (Graphic graphic in legacyInfo.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.enabled)
                {
                    hiddenLegacyGraphics.Add(graphic);
                    graphic.enabled = false;
                }
            }
        }

        hudRoot = CreateRect("Flight HUD", canvas.transform).gameObject;
        Stretch(hudRoot.GetComponent<RectTransform>());

        BuildInfoCard(hudRoot.transform);
        BuildLocationsPanel(hudRoot.transform);
        SetLocationsPanelVisible(false);
    }

    private void BuildInfoCard(Transform parent)
    {
        RectTransform card = CreatePanel("Telemetry", parent, PanelColor);
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0, 1);
        card.anchoredPosition = new Vector2(18, -18);
        card.sizeDelta = new Vector2(CardWidth, CardHeight);

        RectTransform accent = CreatePanel("Accent", card, Accent);
        SetRect(accent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 0), new Vector2(5, CardHeight));

        TextMeshProUGUI title = CreateText("Title", card, "FLIGHT COMPUTER", 20, FontStyles.Bold, TextPrimary);
        SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -14), new Vector2(-125, 28));

        TextMeshProUGUI subtitle = CreateText("Subtitle", card, "LOCAL TELEMETRY  •  DOUBLE PRECISION", 10, FontStyles.Bold, Accent);
        subtitle.characterSpacing = 1.3f;
        SetRect(subtitle.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -42), new Vector2(-30, 18));

        Button locationsButton = CreateButton("Locations", card, "LOCATIONS  [L]", Accent, new Color(0.04f, 0.19f, 0.25f, 1f), 11);
        SetRect(locationsButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-12, -13), new Vector2(112, 30));
        locationsButton.onClick.AddListener(() => SetLocationsPanelVisible(!locationsOpen));
        locationsButtonText = locationsButton.GetComponentInChildren<TextMeshProUGUI>();

        telemetryText = CreateText("Telemetry Readout", card, string.Empty, 13, FontStyles.Normal, TextPrimary);
        telemetryText.richText = true;
        telemetryText.lineSpacing = 2f;
        SetRect(telemetryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(20, -70), new Vector2(-30, 178));

        TextMeshProUGUI speedLabel = CreateText("Speed Label", card, "FLIGHT SPEED", 10, FontStyles.Bold, TextSecondary);
        SetRect(speedLabel.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 50), new Vector2(100, 18));

        CreateSpeedButton(card, "300 m/s", 300f, 20);
        CreateSpeedButton(card, "10 km/s", 10000f, 100);
        CreateSpeedButton(card, "100 km/s", 100000f, 180);
        CreateSpeedButton(card, "10 Mm/s", 10000000f, 260);
    }

    private void CreateSpeedButton(RectTransform card, string label, float speed, float x)
    {
        Button button = CreateButton(label, card, label, TextPrimary, PanelSecondary, 11);
        SetRect(button.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(x, 12), new Vector2(74, 34));
        button.onClick.AddListener(() => player.SetSpeed(speed));
    }

    private void BuildLocationsPanel(Transform parent)
    {
        RectTransform panel = CreatePanel("Saved Locations Panel", parent, PanelColor);
        locationsPanel = panel.gameObject;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1, 1);
        // Leave room for the global time controls in the scene's upper-right corner.
        panel.anchoredPosition = new Vector2(-18, -64);
        panel.sizeDelta = new Vector2(LocationsWidth, LocationsHeight);

        RectTransform accent = CreatePanel("Accent", panel, Accent);
        SetRect(accent, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 4));

        TextMeshProUGUI title = CreateText("Title", panel, "SAVED LOCATIONS", 22, FontStyles.Bold, TextPrimary);
        SetRect(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(22, -18), new Vector2(-80, 30));

        TextMeshProUGUI help = CreateText("Help", panel, "Store exact simulation coordinates and player orientation", 11, FontStyles.Normal, TextSecondary);
        SetRect(help.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(22, -51), new Vector2(-44, 20));

        Button close = CreateButton("Close", panel, "×", TextSecondary, Color.clear, 22);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-14, -14), new Vector2(38, 38));
        close.onClick.AddListener(() => SetLocationsPanelVisible(false));

        nameInput = CreateInputField(panel);
        SetRect(nameInput.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(22, -88), new Vector2(-164, 42));

        Button saveButton = CreateButton("Save", panel, "SAVE HERE", PanelColor, Accent, 12);
        SetRect(saveButton.GetComponent<RectTransform>(), new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-22, -88), new Vector2(112, 42));
        saveButton.onClick.AddListener(SaveFromInput);

        statusText = CreateText("Status", panel, string.Empty, 11, FontStyles.Normal, TextSecondary);
        SetRect(statusText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(22, -136), new Vector2(-44, 20));

        BuildScrollView(panel);
        RebuildLocationRows();
    }

    private void BuildScrollView(RectTransform panel)
    {
        RectTransform scrollRectTransform = CreateRect("Locations Scroll", panel);
        Stretch(scrollRectTransform);
        scrollRectTransform.offsetMin = new Vector2(22, 22);
        scrollRectTransform.offsetMax = new Vector2(-22, -164);
        ScrollRect scroll = scrollRectTransform.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 24f;

        RectTransform viewport = CreateRect("Viewport", scrollRectTransform);
        Stretch(viewport);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        locationsContent = CreateRect("Content", viewport);
        locationsContent.anchorMin = new Vector2(0, 1);
        locationsContent.anchorMax = new Vector2(1, 1);
        locationsContent.pivot = new Vector2(0.5f, 1);
        locationsContent.anchoredPosition = Vector2.zero;
        locationsContent.sizeDelta = Vector2.zero;

        VerticalLayoutGroup layout = locationsContent.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        ContentSizeFitter fitter = locationsContent.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = locationsContent;
    }

    private void RebuildLocationRows()
    {
        if (locationsContent == null)
        {
            return;
        }

        for (int i = locationsContent.childCount - 1; i >= 0; --i)
        {
            Destroy(locationsContent.GetChild(i).gameObject);
        }

        if (locationsButtonText != null)
        {
            locationsButtonText.text = $"LOCATIONS  {saved.locations.Count}  [L]";
        }

        if (saved.locations.Count == 0)
        {
            TextMeshProUGUI empty = CreateText("Empty", locationsContent, "No saved locations yet.\nName this position above and save it.", 13, FontStyles.Normal, TextSecondary);
            empty.alignment = TextAlignmentOptions.Center;
            LayoutElement emptyLayout = empty.gameObject.AddComponent<LayoutElement>();
            emptyLayout.preferredHeight = 90;
            return;
        }

        foreach (SavedLocation location in saved.locations)
        {
            CreateLocationRow(location);
        }
    }

    private void CreateLocationRow(SavedLocation location)
    {
        RectTransform row = CreatePanel($"Location - {location.displayName}", locationsContent, PanelSecondary);
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 76;
        layout.minHeight = 76;

        TextMeshProUGUI name = CreateText("Name", row, location.displayName, 15, FontStyles.Bold, TextPrimary);
        SetRect(name.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(14, -10), new Vector2(-175, 24));

        DoubleVector3 position = location.Position;
        TextMeshProUGUI coordinates = CreateText("Coordinates", row,
            $"X {FormatCoordinate(position.x)}   Y {FormatCoordinate(position.y)}   Z {FormatCoordinate(position.z)}",
            10, FontStyles.Normal, TextSecondary);
        coordinates.enableWordWrapping = false;
        SetRect(coordinates.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(14, 10), new Vector2(-172, 22));

        Button teleport = CreateButton("Teleport", row, "TELEPORT", PanelColor, Accent, 11);
        SetRect(teleport.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-48, 0), new Vector2(100, 38));
        string locationId = location.id;
        teleport.onClick.AddListener(() => TeleportToLocation(locationId));

        Button delete = CreateButton("Delete", row, "×", TextPrimary, Danger, 18);
        SetRect(delete.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(32, 38));
        delete.onClick.AddListener(() => DeleteLocation(locationId));
    }

    private void SaveFromInput()
    {
        SaveCurrentLocation(nameInput.text);
        nameInput.text = string.Empty;
        EventSystem.current?.SetSelectedGameObject(null);
    }

    public void SetLocationsPanelVisible(bool visible)
    {
        locationsOpen = visible;
        if (locationsPanel != null)
        {
            locationsPanel.SetActive(visible);
        }

        if (!visible)
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    private bool IsEditingName()
    {
        return nameInput != null && nameInput.isFocused;
    }

    private void LoadLocations()
    {
        string json = PlayerPrefs.GetString(SaveKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                saved = JsonUtility.FromJson<SavedLocationCollection>(json) ?? new SavedLocationCollection();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not load FloatPrecision saved locations: {exception.Message}");
                saved = new SavedLocationCollection();
            }
        }

        saved.locations ??= new List<SavedLocation>();
        saved.locations.RemoveAll(location => location == null || string.IsNullOrEmpty(location.id));
    }

    private void PersistLocations()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saved));
        PlayerPrefs.Save();
    }

    private string MakeUniqueName(string requested)
    {
        string candidate = requested;
        int suffix = 2;
        while (saved.locations.Exists(item => string.Equals(item.displayName, candidate, StringComparison.OrdinalIgnoreCase)))
        {
            candidate = $"{requested} ({suffix++})";
        }

        return candidate;
    }

    private void SetStatus(string message, Color color)
    {
        if (statusText == null)
        {
            return;
        }

        statusText.text = message;
        statusText.color = color;
    }

    private static Quaternion NormalizeSafe(Quaternion rotation)
    {
        float magnitude = Mathf.Sqrt(
            rotation.x * rotation.x + rotation.y * rotation.y +
            rotation.z * rotation.z + rotation.w * rotation.w);
        if (magnitude < 0.0001f)
        {
            return Quaternion.identity;
        }

        float inverse = 1f / magnitude;
        return new Quaternion(rotation.x * inverse, rotation.y * inverse, rotation.z * inverse, rotation.w * inverse);
    }

    private static string FormatDistance(double metres)
    {
        if (double.IsInfinity(metres)) return "—";
        double absolute = Math.Abs(metres);
        if (absolute >= 1_000_000_000.0) return $"{metres / 1_000_000_000.0:0.###} Gm";
        if (absolute >= 1_000_000.0) return $"{metres / 1_000_000.0:0.###} Mm";
        if (absolute >= 1_000.0) return $"{metres / 1_000.0:0.###} km";
        return $"{metres:0.##} m";
    }

    private static string FormatSpeed(double metresPerSecond)
    {
        double absolute = Math.Abs(metresPerSecond);
        return absolute >= 1_000_000.0
            ? $"{metresPerSecond / 1_000_000.0:0.###} Mm/s"
            : absolute >= 1_000.0
                ? $"{metresPerSecond / 1_000.0:0.###} km/s"
                : $"{metresPerSecond:0.##} m/s";
    }

    private static string FormatCoordinate(double value)
    {
        double absolute = Math.Abs(value);
        return absolute >= 1_000_000_000.0
            ? value.ToString("0.000000E+0", CultureInfo.InvariantCulture)
            : value.ToString("N2", CultureInfo.InvariantCulture);
    }

    private static string ToHex(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float size, FontStyles style, Color color)
    {
        RectTransform rect = CreateRect(name, parent);
        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = size;
        label.fontStyle = style;
        label.color = color;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableWordWrapping = true;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(string name, Transform parent, string label, Color textColor, Color backgroundColor, float fontSize)
    {
        RectTransform rect = CreatePanel(name, parent, backgroundColor);
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.14f, 1.14f, 1.14f, 1f);
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", rect, label, fontSize, FontStyles.Bold, textColor);
        Stretch(text.rectTransform, 4);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        return button;
    }

    private static TMP_InputField CreateInputField(Transform parent)
    {
        RectTransform root = CreatePanel("Location Name", parent, PanelSecondary);
        TMP_InputField input = root.gameObject.AddComponent<TMP_InputField>();

        RectTransform viewport = CreateRect("Text Area", root);
        Stretch(viewport, 12);
        viewport.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI text = CreateText("Text", viewport, string.Empty, 13, FontStyles.Normal, TextPrimary);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.raycastTarget = true;

        TextMeshProUGUI placeholder = CreateText("Placeholder", viewport, "Location name…", 13, FontStyles.Italic, TextSecondary);
        Stretch(placeholder.rectTransform);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;

        input.textViewport = viewport;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 48;
        return input;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Stretch(RectTransform rect, float inset = 0)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
}
