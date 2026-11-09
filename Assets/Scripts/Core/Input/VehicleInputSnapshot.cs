using System.Globalization;
using UnityEngine;

namespace VelocityRush.Core
{
    /// <summary>
    /// Immutable, normalized driving-input sample (TDD §37.4). One instance fully describes a
    /// frame's driving surface, so adapters and consumers agree on ranges and neutral defaults.
    /// </summary>
    /// <remarks>
    /// Values are clamped at construction: <see cref="Throttle"/> and <see cref="Brake"/> to 0..1,
    /// <see cref="Steering"/> to -1..1. NaN is sanitized to 0 (neutral); infinities clamp to the
    /// declared bounds. <see cref="Neutral"/> is the exact all-zero state.
    /// </remarks>
    public readonly struct VehicleInputSnapshot
    {
        /// <summary>Minimum throttle/brake sample (neutral).</summary>
        public const float MinThrottle = 0f;

        /// <summary>Maximum throttle/brake sample (full).</summary>
        public const float MaxThrottle = 1f;

        /// <summary>Minimum steering sample (full left).</summary>
        public const float MinSteering = -1f;

        /// <summary>Maximum steering sample (full right).</summary>
        public const float MaxSteering = 1f;

        /// <summary>Throttle sample. Range 0..1.</summary>
        public float Throttle { get; }

        /// <summary>Brake sample. Range 0..1.</summary>
        public float Brake { get; }

        /// <summary>Steering sample. Range -1..1; negative steers left.</summary>
        public float Steering { get; }

        /// <summary>Handbrake state.</summary>
        public bool Handbrake { get; }

        /// <summary>All channels neutral: no throttle, no brake, no steering, handbrake released.</summary>
        public static VehicleInputSnapshot Neutral => default;

        /// <summary>True when every channel equals its neutral value.</summary>
        public bool IsNeutral => Throttle == 0f && Brake == 0f && Steering == 0f && !Handbrake;

        /// <param name="throttle">Throttle sample in 0..1.</param>
        /// <param name="brake">Brake sample in 0..1.</param>
        /// <param name="steering">Steering sample in -1..1 (negative = left).</param>
        /// <param name="handbrake">Handbrake state.</param>
        public VehicleInputSnapshot(float throttle, float brake, float steering, bool handbrake)
        {
            Throttle = Clamp(throttle, MinThrottle, MaxThrottle);
            Brake = Clamp(brake, MinThrottle, MaxThrottle);
            Steering = Clamp(steering, MinSteering, MaxSteering);
            Handbrake = handbrake;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "throttle={0:0.00} brake={1:0.00} steer={2:0.00} handbrake={3}",
                Throttle,
                Brake,
                Steering,
                Handbrake);
        }

        private static float Clamp(float value, float min, float max)
        {
            // NaN has no meaningful direction; treat it as neutral rather than propagating it
            // into physics. Infinities clamp to the declared bounds below.
            if (float.IsNaN(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, min, max);
        }
    }
}
