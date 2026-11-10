using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// Immutable per-wheel state published for debug and tuning consumers (TDD §36.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is an additive companion to <see cref="VehicleTelemetry"/>: the aggregate contract is
    /// deliberately unchanged so existing camera, HUD, and race consumers keep working.
    /// </para>
    /// <para>
    /// <see cref="Slip"/> is a non-negative magnitude and <see cref="SuspensionCompression"/> is a
    /// normalized 0..1 value. Non-finite inputs are sanitized instead of propagating NaN into the
    /// debug overlay or future HUD.
    /// </para>
    /// </remarks>
    public readonly struct WheelTelemetry
    {
        /// <summary>Creates a sanitized per-wheel sample.</summary>
        /// <param name="slip">Largest absolute longitudinal/lateral slip; negative or non-finite maps to 0.</param>
        /// <param name="isGrounded">True when this wheel currently has ground contact.</param>
        /// <param name="suspensionCompression">Normalized compression 0..1; non-finite maps to 0.</param>
        public WheelTelemetry(float slip, bool isGrounded, float suspensionCompression)
        {
            Slip = SanitizeNonNegative(slip);
            IsGrounded = isGrounded;
            SuspensionCompression = SanitizeUnit(suspensionCompression);
        }

        /// <summary>Largest absolute slip for this wheel (0 = full grip).</summary>
        public float Slip { get; }

        /// <summary>True when this wheel currently has ground contact.</summary>
        public bool IsGrounded { get; }

        /// <summary>Normalized suspension compression (0 = fully extended, 1 = fully compressed).</summary>
        public float SuspensionCompression { get; }

        private static float SanitizeNonNegative(float value)
        {
            return IsFinite(value) && value > 0f ? value : 0f;
        }

        private static float SanitizeUnit(float value)
        {
            if (!IsFinite(value))
            {
                return 0f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
