#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AIGD;
using com.IvanMurzak.McpPlugin;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Editor.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.IvanMurzak.Unity.MCP.Editor.API
{
    [AiToolType]
    public partial class Tool_FloatPrecision
    {
        private const string InspectToolId = "float-precision-inspect";
        private const string PlaceToolId = "float-precision-place-player";
        private const string PresetToolId = "float-precision-apply-preset";

        [AiTool(InspectToolId, Title = "Float Precision / Inspect", IdempotentHint = true)]
        [Description("Inspect the active FloatPrecision scene, including double-precision player state, planet distances, camera settings, and actionable setup warnings. Call before and after visual changes.")]
        public FloatPrecisionStateData Inspect
        (
            [Description("Include every PerspectiveIllusionObject in the result. Keep true for normal diagnostics.")]
            bool includePlanets = true
        )
        {
            return MainThread.Instance.Run(() => BuildState(includePlanets));
        }

        [AiTool(PlaceToolId, Title = "Float Precision / Place Player")]
        [Description("Place the player at an exact planet-relative latitude, longitude, and altitude in simulation space. Optionally configure a circular orbit, frame the planet in the flight camera, and pause for a deterministic screenshot. Requires Play mode.")]
        public FloatPrecisionStateData PlacePlayer
        (
            [Description("Planet GameObject name, normally Earth, Mars, Moon, or Sun.")]
            string planetName = "Earth",
            [Description("Altitude above the planet's nominal radius in metres. Must be zero or greater.")]
            double altitudeMeters = 1000.0,
            [Description("Planetocentric latitude in degrees, clamped to -90 through 90.")]
            double latitudeDegrees = 0.0,
            [Description("Longitude in degrees. Any finite value is accepted.")]
            double longitudeDegrees = 180.0,
            [Description("Enable velocity mode and assign the local circular-orbit speed. Leave false for stationary visual inspection.")]
            bool circularOrbit = false,
            [Description("Frame the selected planet with the flight camera.")]
            bool framePlanet = true,
            [Description("Pause Play mode after placement for a stable screenshot.")]
            bool pauseAfterPlacement = true
        )
        {
            if (string.IsNullOrWhiteSpace(planetName))
                throw new ArgumentException("Planet name cannot be empty.", nameof(planetName));
            if (!double.IsFinite(altitudeMeters) || altitudeMeters < 0.0)
                throw new ArgumentOutOfRangeException(nameof(altitudeMeters), "Altitude must be a finite value greater than or equal to zero.");
            if (!double.IsFinite(latitudeDegrees) || !double.IsFinite(longitudeDegrees))
                throw new ArgumentException("Latitude and longitude must be finite values.");

            return MainThread.Instance.Run(() =>
            {
                RequirePlayMode();

                FloatPrecisionPlayer player = FindPlayer();
                PerspectiveIllusionObject planet = FindPlanet(planetName);
                DoubleVector3 outward = DirectionFromCoordinates(latitudeDegrees, longitudeDegrees);
                double radius = planet.simulationScale * 0.5;

                player.playerPosition = planet.simulationPosition + outward * (radius + altitudeMeters);
                ConfigureVelocity(player, planet, outward, radius + altitudeMeters, circularOrbit);

                foreach (SphereSurfacePatchGenerator surfacePatch in
                    UnityEngine.Object.FindObjectsOfType<SphereSurfacePatchGenerator>(true))
                {
                    surfacePatch.ResetPlayerMotionHistory();
                }

                if (framePlanet)
                    FramePlanet(player, planet);

                EditorApplication.isPaused = pauseAfterPlacement;
                EditorApplication.QueuePlayerLoopUpdate();
                EditorUtils.RepaintAllEditorWindows();

                return BuildState(true);
            });
        }

        [AiTool(PresetToolId, Title = "Float Precision / Apply Visual Preset")]
        [Description("Apply a named, repeatable FloatPrecision inspection preset. Presets: ground (100 m), clouds (12 km), atmosphere (80 km), low-orbit (400 km circular orbit), and planet (20,000 km). Requires Play mode.")]
        public FloatPrecisionStateData ApplyPreset
        (
            [Description("One of: ground, clouds, atmosphere, low-orbit, planet.")]
            string preset = "planet",
            [Description("Planet GameObject name. Earth is the default reference body.")]
            string planetName = "Earth",
            [Description("Latitude used by the preset in degrees.")]
            double latitudeDegrees = 0.0,
            [Description("Longitude used by the preset in degrees.")]
            double longitudeDegrees = 180.0,
            [Description("Pause after applying the preset so screenshots are deterministic.")]
            bool pauseAfterPlacement = true
        )
        {
            if (string.IsNullOrWhiteSpace(preset))
                throw new ArgumentException("Preset cannot be empty.", nameof(preset));

            string normalized = preset.Trim().ToLowerInvariant();
            double altitude;
            bool orbit;

            switch (normalized)
            {
                case "ground":
                    altitude = 100.0;
                    orbit = false;
                    break;
                case "clouds":
                    altitude = 12000.0;
                    orbit = false;
                    break;
                case "atmosphere":
                    altitude = 80000.0;
                    orbit = false;
                    break;
                case "low-orbit":
                case "orbit":
                    altitude = 400000.0;
                    orbit = true;
                    break;
                case "planet":
                case "full-planet":
                    altitude = 20000000.0;
                    orbit = false;
                    break;
                default:
                    throw new ArgumentException($"Unknown preset '{preset}'. Use ground, clouds, atmosphere, low-orbit, or planet.", nameof(preset));
            }

            return PlacePlayer(
                planetName,
                altitude,
                latitudeDegrees,
                longitudeDegrees,
                orbit,
                true,
                pauseAfterPlacement);
        }

        private static FloatPrecisionStateData BuildState(bool includePlanets)
        {
            var warnings = new List<string>();
            FloatPrecisionPlayer? player = UnityEngine.Object.FindObjectOfType<FloatPrecisionPlayer>(true);
            Camera? camera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>(true);

            if (player == null)
                warnings.Add("FloatPrecisionPlayer was not found. Open the FloatPrecision scene.");
            if (camera == null)
                warnings.Add("No active camera was found.");

            PerspectiveIllusionObject[] bodies = includePlanets
                ? UnityEngine.Object.FindObjectsOfType<PerspectiveIllusionObject>(true)
                    .OrderBy(body => body.name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : Array.Empty<PerspectiveIllusionObject>();

            if (includePlanets && bodies.Length == 0)
                warnings.Add("No PerspectiveIllusionObject was found in the active scene.");

            string[] planetSummaries = player == null
                ? bodies.Select(body => $"{body.name}: player reference unavailable").ToArray()
                : bodies.Select(DescribePlanet).ToArray();

            DoubleVector3 position = player?.GetPosition() ?? DoubleVector3.Zero;
            DoubleVector3 velocity = player?.GetVelocity() ?? DoubleVector3.Zero;

            return new FloatPrecisionStateData
            {
                ScenePath = SceneManager.GetActiveScene().path,
                IsPlaying = EditorApplication.isPlaying,
                IsPaused = EditorApplication.isPaused,
                PlayerFound = player != null,
                PlayerPosition = FormatVector(position, "m"),
                PlayerVelocity = FormatVector(velocity, "m/s"),
                PlayerSpeed = velocity.Magnitude(),
                VelocityMode = player != null && player.VelocityActive,
                Camera = DescribeCamera(camera),
                Planets = planetSummaries,
                Warnings = warnings.ToArray()
            };
        }

        private static string DescribePlanet(PerspectiveIllusionObject planet)
        {
            planet.CalculateRenderState(
                out DoubleVector3 renderPosition,
                out double renderScale,
                out double centerDistance,
                out double surfaceDistance);

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: center={1:F3} m; radius={2:F3} m; altitude={3:F3} m; proxyPosition={4}; proxyDiameter={5:F3}",
                planet.name,
                centerDistance,
                planet.simulationScale * 0.5,
                surfaceDistance,
                FormatVector(renderPosition, "Unity units"),
                renderScale);
        }

        private static string DescribeCamera(Camera? camera)
        {
            if (camera == null)
                return "not found";

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}: position={1}; forward={2}; FOV={3:F2}; clip={4:F3}..{5:F3}",
                camera.name,
                camera.transform.position,
                camera.transform.forward,
                camera.fieldOfView,
                camera.nearClipPlane,
                camera.farClipPlane);
        }

        private static string FormatVector(DoubleVector3 value, string unit)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "({0:R}, {1:R}, {2:R}) {3}",
                value.x,
                value.y,
                value.z,
                unit);
        }

        private static FloatPrecisionPlayer FindPlayer()
        {
            return UnityEngine.Object.FindObjectOfType<FloatPrecisionPlayer>(true)
                ?? throw new InvalidOperationException("FloatPrecisionPlayer was not found. Open the FloatPrecision scene before using this tool.");
        }

        private static PerspectiveIllusionObject FindPlanet(string planetName)
        {
            PerspectiveIllusionObject? planet = UnityEngine.Object
                .FindObjectsOfType<PerspectiveIllusionObject>(true)
                .FirstOrDefault(body => string.Equals(body.name, planetName, StringComparison.OrdinalIgnoreCase));

            return planet
                ?? throw new ArgumentException($"PerspectiveIllusionObject '{planetName}' was not found in the active scene.", nameof(planetName));
        }

        private static DoubleVector3 DirectionFromCoordinates(double latitudeDegrees, double longitudeDegrees)
        {
            double latitude = Math.Max(-90.0, Math.Min(90.0, latitudeDegrees)) * Math.PI / 180.0;
            double longitude = longitudeDegrees * Math.PI / 180.0;
            double cosLatitude = Math.Cos(latitude);

            return new DoubleVector3(
                cosLatitude * Math.Cos(longitude),
                Math.Sin(latitude),
                cosLatitude * Math.Sin(longitude));
        }

        private static void ConfigureVelocity(
            FloatPrecisionPlayer player,
            PerspectiveIllusionObject planet,
            DoubleVector3 outward,
            double centerDistance,
            bool circularOrbit)
        {
            var serializedPlayer = new SerializedObject(player);
            SerializedProperty velocityActive = serializedPlayer.FindProperty("velocityActive")
                ?? throw new MissingFieldException(nameof(FloatPrecisionPlayer), "velocityActive");

            velocityActive.boolValue = circularOrbit;
            serializedPlayer.ApplyModifiedPropertiesWithoutUndo();

            if (!circularOrbit)
            {
                player.SetVelocity(DoubleVector3.Zero);
                return;
            }

            PlanetGravityHandler? gravity = planet.GetComponent<PlanetGravityHandler>();
            if (gravity == null)
                throw new InvalidOperationException($"Planet '{planet.name}' has no PlanetGravityHandler, so a circular orbit cannot be calculated.");

            DoubleVector3 reference = Math.Abs(outward.y) < 0.95
                ? new DoubleVector3(0.0, 1.0, 0.0)
                : new DoubleVector3(1.0, 0.0, 0.0);
            DoubleVector3 tangent = reference.Cross(outward).Normalized();
            double orbitSpeed = Math.Sqrt(gravity.GravitationalParameter / centerDistance);
            player.SetVelocity(tangent * orbitSpeed);
        }

        private static void FramePlanet(FloatPrecisionPlayer player, PerspectiveIllusionObject planet)
        {
            Camera? camera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>(true);
            if (camera == null)
                return;

            Vector3 direction = (Vector3)(planet.simulationPosition - player.playerPosition).Normalized();
            if (direction.sqrMagnitude < 0.5f)
                return;

            player.transform.rotation = Quaternion.identity;

            OrbitCamera? orbitCamera = camera.GetComponent<OrbitCamera>();
            if (orbitCamera != null)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f)) * Mathf.Rad2Deg;

                typeof(OrbitCamera).GetField("currentRotationX", flags)?.SetValue(orbitCamera, yaw);
                typeof(OrbitCamera).GetField("currentRotationY", flags)?.SetValue(orbitCamera, pitch);
                typeof(OrbitCamera).GetField("distance", flags)?.SetValue(orbitCamera, 10f);
                orbitCamera.lockCamera = false;
            }

            Vector3 target = player.transform.position;
            camera.transform.SetPositionAndRotation(
                target - direction * 10f,
                Quaternion.LookRotation(direction, Vector3.up));
        }

        private static void RequirePlayMode()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("This operation changes runtime simulation state and therefore requires Play mode. Start Play mode with editor-application-set-state first.");
        }
    }
}
