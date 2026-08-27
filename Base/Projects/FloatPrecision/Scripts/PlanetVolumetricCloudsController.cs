using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Connects physical cloud settings to a perspective-compressed planet proxy.
/// The cloud renderer continues to work in metres while its render sphere follows
/// the exact center and scale used by <see cref="PerspectiveIllusionObject"/>.
/// </summary>
[DefaultExecutionOrder(100)]
public sealed class PlanetVolumetricCloudsController : MonoBehaviour
{
    [Header("Planet")]
    [SerializeField] private PerspectiveIllusionObject planet;
    [SerializeField] private bool cloudsEnabled = true;

    [Header("Earth Cloud Layer (metres)")]
    [SerializeField, Min(0f)] private float bottomAltitude = 1500f;
    [SerializeField, Min(100f)] private float altitudeRange = 5500f;
    [SerializeField, Range(0f, 1f)] private float density = 0.30f;

    [Header("Shape")]
    [SerializeField] private VolumetricClouds.CloudPresets preset = VolumetricClouds.CloudPresets.Cloudy;
    [SerializeField, Range(0f, 1f)] private float shapeFactor = 0.72f;
    [SerializeField, Min(0.1f)] private float shapeScale = 1.35f;
    [Tooltip("Breaks the repeating local noise into cloud fields with different sizes and silhouettes.")]
    [SerializeField, Range(0f, 1f)] private float localShapeVariation = 0.82f;
    [SerializeField, Range(0.1f, 4f)] private float macroShapeScale = 0.62f;
    [SerializeField, Range(0.2f, 48f)] private float cumulusScale = 32f;
    [SerializeField, Range(0f, 1f)] private float cumulusStrength = 0.82f;
    [SerializeField, Range(0f, 1f)] private float verticalDevelopment = 0.8f;
    [SerializeField, Range(0f, 1f)] private float detailStrength = 0.62f;
    [SerializeField, Range(0f, 1f)] private float edgeHardness = 0.96f;

    [Header("Planet-Wide Weather")]
    [SerializeField, Range(0.25f, 8f)] private float planetaryCoverageScale = 1.45f;
    [SerializeField, Range(0f, 1f)] private float planetaryCoverage = 0.58f;
    [SerializeField, Range(0f, 1f)] private float planetaryCoverageContrast = 0.75f;
    [SerializeField, Range(0f, 1f)] private float planetaryCoverageInfluence = 1f;
    [Tooltip("Optional equirectangular satellite-style coverage map. Red controls coverage and green controls density; a grayscale map works for both.")]
    [SerializeField] private Texture2D weatherMapOverride;
    [SerializeField, Range(128, 1024)] private int weatherMapWidth = 1024;
    [SerializeField] private int weatherSeed = 15873;
    [SerializeField, Min(0f)] private float detailFadeStartAltitude = 35000f;
    [SerializeField, Min(1f)] private float detailFadeEndAltitude = 240000f;

    [SerializeField, Range(0f, 1f)] private float erosionFactor = 0.32f;
    [SerializeField, Min(1f)] private float erosionScale = 55f;
    [SerializeField] private bool microErosion = false;
    [SerializeField, Range(0f, 360f)] private float windOrientation = 65f;
    [SerializeField] private float windSpeedKph = 35f;

    [Header("Lighting and Quality")]
    [SerializeField, Range(0f, 2f)] private float ambientLight = 0.8f;
    [SerializeField, Range(0f, 2f)] private float sunlight = 1.15f;
    [SerializeField, Range(0.0005f, 0.02f)] private float extinctionCoefficient = 0.0045f;
    [SerializeField, Range(0f, 2f)] private float silverLiningIntensity = 0.70f;
    [SerializeField, Range(24, 256)] private int primarySteps = 128;
    [SerializeField, Range(1, 16)] private int lightSteps = 4;
    [SerializeField, Range(25f, 500f)] private float baseStepSize = 90f;
    [SerializeField, Range(0f, 0.05f)] private float adaptiveStepSizeFactor = 0.008f;
    [SerializeField, Range(250f, 3000f)] private float maximumStepSize = 1200f;
    [SerializeField, Range(0f, 1f)] private float temporalAccumulation = 0.86f;

    [Header("Ground Cloud Shadows")]
    [SerializeField] private bool groundShadows = true;
    [SerializeField] private VolumetricClouds.CloudShadowResolution groundShadowResolution =
        VolumetricClouds.CloudShadowResolution.Medium256;
    [Tooltip("Width in metres of the camera-centred cloud-shadow coverage area.")]
    [SerializeField, Min(1000f)] private float groundShadowDistance = 24000f;
    [SerializeField, Range(0f, 1f)] private float groundShadowOpacity = 0.72f;
    [SerializeField, Range(6, 32)] private int groundShadowSamples = 16;
    [Tooltip("Cloud shadows are fully active below this altitude in metres.")]
    [SerializeField, Min(0f)] private float groundShadowFadeStartAltitude = 25000f;
    [Tooltip("Cloud shadows are completely disabled above this altitude in metres.")]
    [SerializeField, Min(1000f)] private float groundShadowFadeEndAltitude = 60000f;

    [Header("Orbital Cloud Proxy")]
    [Tooltip("Optical density of the satellite-scale cloud shell used after local volumetric detail fades out.")]
    [SerializeField, Range(0.25f, 5f)] private float orbitalProxyOpacity = 0.75f;
    [SerializeField] private Color orbitalCloudTint = Color.white;
    [SerializeField, Range(0f, 1f)] private float orbitalAmbient = 0.12f;

    private GameObject volumeObject;
    private VolumeProfile runtimeProfile;
    private VolumetricClouds clouds;
    private Texture2D proceduralWeatherMap;

    private static readonly Vector4[] WeatherVortices =
    {
        new(0.12f, 0.28f, 0.09f, 1.30f),
        new(0.24f, 0.47f, 0.21f, 2.25f),
        new(0.43f, 0.70f, 0.13f, -1.35f),
        new(0.67f, 0.36f, 0.25f, 1.90f),
        new(0.88f, 0.63f, 0.16f, -2.10f)
    };

    private void OnEnable()
    {
        CreateRuntimeVolume();
        CreateProceduralWeatherMap();
        ApplySettings();
    }

    private void LateUpdate()
    {
        if (!cloudsEnabled || planet == null || planet.player == null ||
            planet.simulationScale <= double.Epsilon)
        {
            VolumetricCloudsURP.ClearPlanetRenderState();
            return;
        }

        planet.CalculateRenderState(
            out DoubleVector3 renderedCenter,
            out double renderedDiameter,
            out _,
            out _);

        double unitsPerMeter = renderedDiameter / planet.simulationScale;
        if (renderedDiameter <= double.Epsilon || unitsPerMeter <= double.Epsilon)
        {
            VolumetricCloudsURP.ClearPlanetRenderState();
            return;
        }

        VolumetricCloudsURP.SetPlanetRenderState(
            (Vector3)renderedCenter,
            Mathf.Max(0.000001f, (float)(renderedDiameter * 0.5)),
            Mathf.Max(0.000000001f, (float)unitsPerMeter));
    }

    private void CreateRuntimeVolume()
    {
        if (volumeObject != null)
        {
            return;
        }

        volumeObject = new GameObject("Earth Volumetric Clouds Volume");
        volumeObject.transform.SetParent(transform, false);

        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 50f;

        runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        runtimeProfile.name = "Earth Volumetric Clouds (Runtime)";
        volume.sharedProfile = runtimeProfile;
        clouds = runtimeProfile.Add<VolumetricClouds>(true);
    }

    private void ApplySettings()
    {
        if (clouds == null)
        {
            return;
        }

        clouds.active = true;
        clouds.state.Override(cloudsEnabled);
        clouds.localClouds.Override(true);
        clouds.cloudPreset = preset;
        clouds.bottomAltitude.Override(bottomAltitude);
        clouds.altitudeRange.Override(altitudeRange);
        clouds.densityMultiplier.Override(density);
        clouds.shapeFactor.Override(shapeFactor);
        clouds.shapeScale.Override(shapeScale);
        clouds.localShapeVariation.Override(localShapeVariation);
        clouds.macroShapeScale.Override(macroShapeScale);
        clouds.cumulusScale.Override(cumulusScale);
        clouds.cumulusStrength.Override(cumulusStrength);
        clouds.verticalDevelopment.Override(verticalDevelopment);
        clouds.detailStrength.Override(detailStrength);
        clouds.edgeHardness.Override(edgeHardness);
        clouds.planetaryCoverageScale.Override(planetaryCoverageScale);
        clouds.planetaryCoverage.Override(planetaryCoverage);
        clouds.planetaryCoverageContrast.Override(planetaryCoverageContrast);
        clouds.planetaryCoverageInfluence.Override(planetaryCoverageInfluence);
        clouds.planetaryWeatherMap.Override(
            weatherMapOverride != null ? weatherMapOverride : proceduralWeatherMap);
        clouds.planetaryDetailFadeStart.Override(detailFadeStartAltitude);
        clouds.planetaryDetailFadeEnd.Override(detailFadeEndAltitude);
        clouds.orbitalProxyOpacity.Override(orbitalProxyOpacity);
        clouds.orbitalProxyTint.Override(orbitalCloudTint);
        clouds.orbitalProxyAmbient.Override(orbitalAmbient);
        clouds.erosionFactor.Override(erosionFactor);
        clouds.erosionScale.Override(erosionScale);
        clouds.microErosion.Override(microErosion);
        clouds.globalOrientation.Override(windOrientation);
        clouds.globalSpeed.Override(windSpeedKph);
        clouds.ambientLightProbeDimmer.Override(ambientLight);
        clouds.sunLightDimmer.Override(sunlight);
        clouds.extinctionCoefficient.Override(extinctionCoefficient);
        clouds.silverLiningIntensity.Override(silverLiningIntensity);
        clouds.numPrimarySteps.Override(primarySteps);
        clouds.numLightSteps.Override(lightSteps);
        clouds.baseStepSize.Override(baseStepSize);
        clouds.adaptiveStepSizeFactor.Override(adaptiveStepSizeFactor);
        clouds.maximumStepSize.Override(maximumStepSize);
        clouds.temporalAccumulationFactor.Override(temporalAccumulation);
        clouds.perceptualBlending.Override(1f);

        clouds.shadows.Override(groundShadows);
        clouds.shadowResolution.Override(groundShadowResolution);
        clouds.shadowDistance.Override(groundShadowDistance);
        clouds.shadowOpacity.Override(groundShadowOpacity);
        clouds.shadowOpacityFallback.Override(0f);
        clouds.shadowSampleCount.Override(groundShadowSamples);
        clouds.shadowFadeStartAltitude.Override(groundShadowFadeStartAltitude);
        clouds.shadowFadeEndAltitude.Override(
            Mathf.Max(groundShadowFadeStartAltitude + 1000f, groundShadowFadeEndAltitude));
    }

    private void CreateProceduralWeatherMap()
    {
        DestroyProceduralWeatherMap();

        int width = Mathf.Clamp(weatherMapWidth, 128, 1024);
        int height = Mathf.Max(64, width / 2);
        proceduralWeatherMap = new Texture2D(width, height, TextureFormat.RGBA32, true, true)
        {
            name = "Earth Procedural Weather",
            filterMode = FilterMode.Trilinear,
            wrapModeU = TextureWrapMode.Repeat,
            wrapModeV = TextureWrapMode.Clamp,
            anisoLevel = 1
        };

        int pixelCount = width * height;
        Color32[] pixels = new Color32[pixelCount];
        float[] coverageSignals = new float[pixelCount];
        float[] shapeSignals = new float[pixelCount];
        float[] fineDetailSignals = new float[pixelCount];
        float seedX = Mathf.Abs(weatherSeed % 997) * 0.01931f + 11.7f;
        float seedY = Mathf.Abs(weatherSeed % 619) * 0.02713f + 37.1f;
        float frequencyMultiplier = Mathf.Clamp(planetaryCoverageScale / 1.45f, 0.5f, 2.5f);
        int broadFrequencyX = Mathf.Max(2, Mathf.RoundToInt(5f * frequencyMultiplier));
        int broadFrequencyY = Mathf.Max(2, Mathf.RoundToInt(3f * frequencyMultiplier));
        int frontFrequencyX = Mathf.Max(3, Mathf.RoundToInt(9f * frequencyMultiplier));
        int frontFrequencyY = Mathf.Max(2, Mathf.RoundToInt(5f * frequencyMultiplier));
        int wispFrequencyX = Mathf.Max(6, Mathf.RoundToInt(18f * frequencyMultiplier));
        int wispFrequencyY = Mathf.Max(4, Mathf.RoundToInt(10f * frequencyMultiplier));
        int filamentFrequencyX = Mathf.Max(12, Mathf.RoundToInt(42f * frequencyMultiplier));
        int filamentFrequencyY = Mathf.Max(7, Mathf.RoundToInt(23f * frequencyMultiplier));
        int microFrequencyX = Mathf.Max(24, Mathf.RoundToInt(110f * frequencyMultiplier));
        int microFrequencyY = Mathf.Max(12, Mathf.RoundToInt(62f * frequencyMultiplier));

        for (int y = 0; y < height; y++)
        {
            float v = (y + 0.5f) / height;
            for (int x = 0; x < width; x++)
            {
                float u = (x + 0.5f) / width;
                float warpU = PeriodicFbm(u, v, 2, 2, seedX + 17.3f, seedY + 41.9f, 4) - 0.5f;
                float warpV = PeriodicFbm(u, v, 3, 2, seedX + 73.1f, seedY + 9.7f, 4) - 0.5f;
                float macroU = Mathf.Repeat(u + warpU * 0.11f, 1f);
                float macroV = Mathf.Clamp01(v + warpV * 0.075f);
                float weatherU = Mathf.Repeat(u + warpU * 0.19f, 1f);
                float weatherV = Mathf.Clamp01(v + warpV * 0.12f);
                ApplyWeatherVortices(ref weatherU, ref weatherV);

                // Large coverage is deliberately not vortex-warped. Cyclones alter
                // their internal fronts, not the entire planetary coverage field.
                float broad = PeriodicFbm(macroU, macroV, broadFrequencyX, broadFrequencyY, seedX, seedY, 4);
                float mesoscale = PeriodicFbm(
                    macroU,
                    macroV,
                    frontFrequencyX,
                    frontFrequencyY,
                    seedX + 101.7f,
                    seedY + 53.4f,
                    4);
                float wisps = PeriodicFbm(
                    weatherU,
                    weatherV,
                    wispFrequencyX,
                    wispFrequencyY,
                    seedX + 211.2f,
                    seedY + 137.6f,
                    3);
                float filamentNoise = PeriodicFbm(
                    weatherU,
                    weatherV,
                    filamentFrequencyX,
                    filamentFrequencyY,
                    seedX + 307.4f,
                    seedY + 271.8f,
                    2);
                float microNoise = PeriodicFbm(
                    weatherU,
                    weatherV,
                    microFrequencyX,
                    microFrequencyY,
                    seedX + 839.2f,
                    seedY + 751.6f,
                    2);
                float filaments = 1f - Mathf.Abs(filamentNoise * 2f - 1f);
                filaments = Mathf.Pow(Mathf.Clamp01(filaments), 4.2f);

                float frontNoise = PeriodicFbm(
                    weatherU,
                    weatherV,
                    frontFrequencyX,
                    frontFrequencyY,
                    seedX + 427.6f,
                    seedY + 331.2f,
                    3);
                float frontRidge = 1f - Mathf.Abs(frontNoise * 2f - 1f);
                frontRidge = Mathf.Pow(Mathf.Clamp01(frontRidge), 2.0f);
                float frontGate = Mathf.SmoothStep(0.38f, 0.67f, broad)
                    * Mathf.Lerp(0.25f, 1f, wisps);
                float brokenFronts = frontRidge * frontGate;

                float clearingNoise = PeriodicFbm(
                    macroU,
                    macroV,
                    Mathf.Max(2, broadFrequencyX + 1),
                    Mathf.Max(2, broadFrequencyY + 1),
                    seedX + 619.3f,
                    seedY + 487.1f,
                    3);
                float drySlot = Mathf.SmoothStep(0.56f, 0.76f, clearingNoise);
                float convectiveCells = Mathf.SmoothStep(0.58f, 0.78f, wisps)
                    * Mathf.SmoothStep(0.34f, 0.68f, mesoscale);

                float latitude = Mathf.Abs(v * 2f - 1f);
                float zonalFlow = 0.5f + 0.5f * Mathf.Sin(
                    (weatherV * 8f + broad * 1.1f + mesoscale * 0.35f) * Mathf.PI);
                float macroWeather = broad * 0.56f + mesoscale * 0.32f + zonalFlow * 0.12f;
                float synopticEnvelope = Mathf.SmoothStep(0.40f, 0.62f, macroWeather);
                float cloudMass = synopticEnvelope * Mathf.Lerp(
                    0.48f,
                    1f,
                    Mathf.SmoothStep(0.30f, 0.72f, mesoscale));

                // Large synoptic systems establish the silhouette; narrow fronts,
                // convective patches and dry slots keep it from becoming a few soft
                // white continents when viewed from orbit.
                float frontalEnvelope = Mathf.SmoothStep(
                    0.22f,
                    0.82f,
                    synopticEnvelope + broad * 0.24f);
                float frontSignal = brokenFronts * frontalEnvelope;
                // Red is the synoptic envelope only. Keeping high-frequency ridges
                // out of coverage prevents the white contour/ribbon look at orbit.
                float weatherSignal = cloudMass * Mathf.Lerp(0.62f, 1f, mesoscale)
                    + frontSignal * 0.12f
                    + convectiveCells * synopticEnvelope * 0.06f;
                weatherSignal -= drySlot
                    * Mathf.Lerp(0.18f, 0.50f, 1f - frontSignal)
                    * Mathf.SmoothStep(0.12f, 0.88f, cloudMass);
                weatherSignal = Mathf.Clamp01((weatherSignal - 0.035f) * 1.10f);
                weatherSignal = Mathf.Lerp(
                    weatherSignal,
                    Mathf.Max(weatherSignal, cloudMass * 0.90f + frontSignal * 0.10f),
                    latitude * 0.14f);

                float gatedFilaments = filaments * frontSignal
                    * Mathf.SmoothStep(0.45f, 0.72f, wisps);
                float fineWisps = Mathf.SmoothStep(0.28f, 0.74f, filamentNoise)
                    * Mathf.Lerp(0.35f, 1f, wisps);
                float microPatches = Mathf.SmoothStep(0.34f, 0.70f, microNoise)
                    * Mathf.Lerp(0.25f, 1f, wisps);
                float shapeSignal = Mathf.Clamp01(
                    cloudMass * 0.10f
                    + mesoscale * 0.08f
                    + wisps * 0.18f
                    + fineWisps * 0.22f
                    + microPatches * 0.22f
                    + frontSignal * 0.10f
                    + convectiveCells * 0.05f
                    + gatedFilaments * 0.05f
                    - drySlot * cloudMass * 0.12f);
                float fineDetailSignal = Mathf.Clamp01(
                    microNoise * 0.48f
                    + filamentNoise * 0.30f
                    + wisps * 0.14f
                    + filaments * 0.08f);
                // Green is continuous internal density detail. It deliberately uses
                // a wider range than coverage so orbital clouds retain satellite-like
                // wisps and translucent gaps instead of becoming flat white shapes.
                shapeSignal = Mathf.Lerp(0.28f, 1f, Mathf.Pow(shapeSignal, 0.92f));
                int pixelIndex = y * width + x;
                coverageSignals[pixelIndex] = weatherSignal;
                shapeSignals[pixelIndex] = shapeSignal;
                fineDetailSignals[pixelIndex] = fineDetailSignal;
            }
        }

        // Procedural noise ranges vary with seed and frequency. A percentile remap
        // prevents a valid seed from placing the entire map below the shader's
        // coverage threshold (which would make every cloud disappear).
        NormalizeWeatherCoverage(coverageSignals);
        NormalizeWeatherShape(shapeSignals);
        NormalizeWeatherShape(fineDetailSignals);
        for (int pixelIndex = 0; pixelIndex < pixelCount; pixelIndex++)
        {
            float coverageSignal = coverageSignals[pixelIndex];
            float shapeSignal = Mathf.Lerp(
                shapeSignals[pixelIndex] * 0.92f,
                shapeSignals[pixelIndex],
                coverageSignal);
            byte coverageByte = (byte)Mathf.RoundToInt(coverageSignal * 255f);
            byte shapeByte = (byte)Mathf.RoundToInt(Mathf.Clamp01(shapeSignal) * 255f);
            byte fineDetailByte = (byte)Mathf.RoundToInt(
                Mathf.Clamp01(fineDetailSignals[pixelIndex]) * 255f);
            pixels[pixelIndex] = new Color32(
                coverageByte,
                shapeByte,
                fineDetailByte,
                255);
        }

        proceduralWeatherMap.SetPixels32(pixels);
        proceduralWeatherMap.Apply(true, true);
    }

    private static void NormalizeWeatherCoverage(float[] values)
    {
        const int HistogramSize = 512;
        const float LowPercentile = 0.03f;
        const float HighPercentile = 0.97f;

        int[] histogram = new int[HistogramSize];
        for (int index = 0; index < values.Length; index++)
        {
            int bin = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Clamp01(values[index]) * (HistogramSize - 1)),
                0,
                HistogramSize - 1);
            histogram[bin]++;
        }

        int lowTarget = Mathf.RoundToInt(values.Length * LowPercentile);
        int highTarget = Mathf.RoundToInt(values.Length * HighPercentile);
        int cumulative = 0;
        int lowBin = 0;
        int highBin = HistogramSize - 1;
        bool foundLow = false;
        for (int bin = 0; bin < HistogramSize; bin++)
        {
            cumulative += histogram[bin];
            if (!foundLow && cumulative >= lowTarget)
            {
                lowBin = bin;
                foundLow = true;
            }

            if (cumulative >= highTarget)
            {
                highBin = bin;
                break;
            }
        }

        float low = lowBin / (float)(HistogramSize - 1);
        float high = Mathf.Max(low + 0.001f, highBin / (float)(HistogramSize - 1));
        for (int index = 0; index < values.Length; index++)
        {
            float normalized = Mathf.InverseLerp(low, high, values[index]);
            // A subtle S-curve sharpens fronts without turning intermediate wisps
            // into a binary mask.
            values[index] = Mathf.Lerp(normalized, Mathf.SmoothStep(0f, 1f, normalized), 0.32f);
        }
    }

    private static void NormalizeWeatherShape(float[] values)
    {
        const int HistogramSize = 512;
        const float LowPercentile = 0.01f;
        const float HighPercentile = 0.99f;

        int[] histogram = new int[HistogramSize];
        for (int index = 0; index < values.Length; index++)
        {
            int bin = Mathf.Clamp(
                Mathf.FloorToInt(Mathf.Clamp01(values[index]) * (HistogramSize - 1)),
                0,
                HistogramSize - 1);
            histogram[bin]++;
        }

        int lowTarget = Mathf.RoundToInt(values.Length * LowPercentile);
        int highTarget = Mathf.RoundToInt(values.Length * HighPercentile);
        int cumulative = 0;
        int lowBin = 0;
        int highBin = HistogramSize - 1;
        bool foundLow = false;
        for (int bin = 0; bin < HistogramSize; bin++)
        {
            cumulative += histogram[bin];
            if (!foundLow && cumulative >= lowTarget)
            {
                lowBin = bin;
                foundLow = true;
            }

            if (cumulative >= highTarget)
            {
                highBin = bin;
                break;
            }
        }

        float low = lowBin / (float)(HistogramSize - 1);
        float high = Mathf.Max(low + 0.001f, highBin / (float)(HistogramSize - 1));
        for (int index = 0; index < values.Length; index++)
        {
            // Keep a small floor inside broad weather systems, but use most of
            // the channel range for fronts, wisps and translucent clearings.
            float normalized = Mathf.InverseLerp(low, high, values[index]);
            float contrasted = Mathf.Lerp(
                normalized,
                Mathf.SmoothStep(0f, 1f, normalized),
                0.45f);
            values[index] = Mathf.Lerp(0.03f, 1f, Mathf.Pow(contrasted, 1.05f));
        }
    }

    private static float PeriodicFbm(
        float u,
        float v,
        int baseFrequencyX,
        int baseFrequencyY,
        float offsetX,
        float offsetY,
        int octaves)
    {
        float value = 0f;
        float weight = 0f;
        float amplitude = 1f;
        int frequencyX = baseFrequencyX;
        int frequencyY = baseFrequencyY;

        for (int octave = 0; octave < octaves; octave++)
        {
            value += PeriodicPerlin(u, v, frequencyX, frequencyY, offsetX, offsetY) * amplitude;
            weight += amplitude;
            amplitude *= 0.5f;
            frequencyX *= 2;
            frequencyY *= 2;
            offsetX += 19.19f;
            offsetY += 37.37f;
        }

        return value / Mathf.Max(weight, 0.0001f);
    }

    private static float PeriodicPerlin(
        float u,
        float v,
        int frequencyX,
        int frequencyY,
        float offsetX,
        float offsetY)
    {
        u = Mathf.Repeat(u, 1f);
        v = Mathf.Clamp01(v);
        float blend = u * u * (3f - 2f * u);
        float sampleY = v * frequencyY + offsetY;
        float right = Mathf.PerlinNoise(u * frequencyX + offsetX, sampleY);
        float left = Mathf.PerlinNoise((u - 1f) * frequencyX + offsetX, sampleY);
        return Mathf.Lerp(right, left, blend);
    }

    private static void ApplyWeatherVortices(ref float u, ref float v)
    {
        foreach (Vector4 vortex in WeatherVortices)
        {
            float deltaU = u - vortex.x;
            deltaU -= Mathf.Round(deltaU);
            float deltaV = (v - vortex.y) * 0.72f;
            float distance = Mathf.Sqrt(deltaU * deltaU + deltaV * deltaV);
            if (distance >= vortex.z)
            {
                continue;
            }

            float influence = 1f - distance / vortex.z;
            float angle = influence * influence * vortex.w;
            float cosine = Mathf.Cos(angle);
            float sine = Mathf.Sin(angle);
            float rotatedU = deltaU * cosine - deltaV * sine;
            float rotatedV = deltaU * sine + deltaV * cosine;
            u = Mathf.Repeat(vortex.x + rotatedU, 1f);
            v = Mathf.Clamp01(vortex.y + rotatedV / 0.72f);
        }
    }

    private void DestroyProceduralWeatherMap()
    {
        if (proceduralWeatherMap == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(proceduralWeatherMap);
        }
        else
        {
            DestroyImmediate(proceduralWeatherMap);
        }

        proceduralWeatherMap = null;
    }

    private void OnDisable()
    {
        VolumetricCloudsURP.ClearPlanetRenderState();
        DestroyProceduralWeatherMap();

        if (volumeObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(volumeObject);
            }
            else
            {
                DestroyImmediate(volumeObject);
            }
        }

        if (runtimeProfile != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeProfile);
            }
            else
            {
                DestroyImmediate(runtimeProfile);
            }
        }

        volumeObject = null;
        runtimeProfile = null;
        clouds = null;
    }

    private void OnValidate()
    {
        bottomAltitude = Mathf.Max(0f, bottomAltitude);
        altitudeRange = Mathf.Max(100f, altitudeRange);
        shapeScale = Mathf.Max(0.1f, shapeScale);
        localShapeVariation = Mathf.Clamp01(localShapeVariation);
        macroShapeScale = Mathf.Clamp(macroShapeScale, 0.1f, 4f);
        cumulusScale = Mathf.Clamp(cumulusScale, 0.2f, 48f);
        cumulusStrength = Mathf.Clamp01(cumulusStrength);
        verticalDevelopment = Mathf.Clamp01(verticalDevelopment);
        detailStrength = Mathf.Clamp01(detailStrength);
        edgeHardness = Mathf.Clamp01(edgeHardness);
        planetaryCoverageScale = Mathf.Clamp(planetaryCoverageScale, 0.25f, 8f);
        weatherMapWidth = Mathf.Clamp(weatherMapWidth, 128, 1024);
        detailFadeStartAltitude = Mathf.Max(0f, detailFadeStartAltitude);
        detailFadeEndAltitude = Mathf.Max(detailFadeStartAltitude + 1f, detailFadeEndAltitude);
        erosionScale = Mathf.Max(1f, erosionScale);
        primarySteps = Mathf.Clamp(primarySteps, 24, 256);
        lightSteps = Mathf.Clamp(lightSteps, 1, 16);
        baseStepSize = Mathf.Clamp(baseStepSize, 25f, 500f);
        adaptiveStepSizeFactor = Mathf.Clamp(adaptiveStepSizeFactor, 0f, 0.05f);
        maximumStepSize = Mathf.Clamp(maximumStepSize, 250f, 3000f);
        groundShadowDistance = Mathf.Max(1000f, groundShadowDistance);
        groundShadowSamples = Mathf.Clamp(groundShadowSamples, 6, 32);
        groundShadowFadeStartAltitude = Mathf.Max(0f, groundShadowFadeStartAltitude);
        groundShadowFadeEndAltitude = Mathf.Max(
            groundShadowFadeStartAltitude + 1000f,
            groundShadowFadeEndAltitude);
        extinctionCoefficient = Mathf.Clamp(extinctionCoefficient, 0.0005f, 0.02f);
        silverLiningIntensity = Mathf.Clamp(silverLiningIntensity, 0f, 2f);
        orbitalProxyOpacity = Mathf.Clamp(orbitalProxyOpacity, 0.25f, 5f);

        if (Application.isPlaying)
        {
            if (weatherMapOverride == null)
            {
                CreateProceduralWeatherMap();
            }

            ApplySettings();
        }
    }
}
