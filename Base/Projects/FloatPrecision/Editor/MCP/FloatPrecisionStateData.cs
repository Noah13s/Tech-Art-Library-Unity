#nullable enable
using System.ComponentModel;

namespace AIGD
{
    public sealed class FloatPrecisionStateData
    {
        [Description("Active Unity scene path.")]
        public string ScenePath { get; set; } = string.Empty;

        [Description("Whether the Unity Editor is currently in Play mode.")]
        public bool IsPlaying { get; set; }

        [Description("Whether Play mode is paused.")]
        public bool IsPaused { get; set; }

        [Description("True when the FloatPrecisionPlayer component was found.")]
        public bool PlayerFound { get; set; }

        [Description("Player simulation-space position in metres.")]
        public string PlayerPosition { get; set; } = string.Empty;

        [Description("Player velocity in metres per second.")]
        public string PlayerVelocity { get; set; } = string.Empty;

        [Description("Player speed in metres per second.")]
        public double PlayerSpeed { get; set; }

        [Description("Whether physics velocity mode is active.")]
        public bool VelocityMode { get; set; }

        [Description("Main camera position, forward vector, field of view and clip planes.")]
        public string Camera { get; set; } = string.Empty;

        [Description("One summary per PerspectiveIllusionObject in the active scene.")]
        public string[] Planets { get; set; } = System.Array.Empty<string>();

        [Description("Actionable scene/setup warnings. Empty when the inspected state is healthy.")]
        public string[] Warnings { get; set; } = System.Array.Empty<string>();
    }
}
