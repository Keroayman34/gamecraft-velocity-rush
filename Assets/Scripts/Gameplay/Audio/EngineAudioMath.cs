using System;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure engine-audio response curves: speed to simulated RPM, and RPM to loop pitch/volume
    /// (T-7.11).
    /// </summary>
    /// <remarks>
    /// The slice has no gearbox simulation, so audible gear steps are synthesized from normalized
    /// speed: each gear sweeps the loop pitch upward and the next gear drops it back. Keeping this
    /// math free of Unity types makes the response curve unit-testable and keeps the audio
    /// controller limited to applying values to a loop source.
    /// </remarks>
    public static class EngineAudioMath
    {
        /// <summary>Default number of audible gear steps across the speed range.</summary>
        public const int DefaultGearCount = 5;

        /// <summary>RPM fraction at standstill (idle rumble).</summary>
        public const float IdleRpmFraction = 0.12f;

        /// <summary>
        /// Normalized speed in 0..1 for a finite, non-negative speed and positive max speed.
        /// Non-finite or non-positive inputs map to zero.
        /// </summary>
        public static float Speed01(float speed, float maxSpeed)
        {
            if (!IsFinite(speed) || !IsFinite(maxSpeed) || speed <= 0f || maxSpeed <= 0f)
            {
                return 0f;
            }

            var value = speed / maxSpeed;
            return value > 1f ? 1f : value;
        }

        /// <summary>
        /// Zero-based simulated gear for normalized speed. Returns 0 for non-finite input.
        /// </summary>
        public static int GearIndex(float speed01, int gearCount)
        {
            var gears = gearCount < 1 ? 1 : gearCount;
            var clamped = SanitizeUnit(speed01);

            // At exactly top speed the last gear is reported, never an out-of-range index.
            var index = (int)(clamped * gears);
            return index >= gears ? gears - 1 : index;
        }

        /// <summary>
        /// Position within the current simulated gear, 0 (shift point) to 1 (next shift).
        /// </summary>
        public static float GearFraction(float speed01, int gearCount)
        {
            var gears = gearCount < 1 ? 1 : gearCount;
            var clamped = SanitizeUnit(speed01);
            var scaled = clamped * gears;
            var fraction = scaled - (float)Math.Floor(scaled);

            // Top of the range must sweep fully instead of snapping to zero.
            if (scaled >= gears)
            {
                return 1f;
            }

            return fraction;
        }

        /// <summary>
        /// Simulated RPM fraction in 0..1: idle at standstill, then a sawtooth per gear.
        /// </summary>
        public static float Rpm01(float speed, float maxSpeed, int gearCount)
        {
            var gears = gearCount < 1 ? 1 : gearCount;
            var speed01 = Speed01(speed, maxSpeed);
            var gearFraction = GearFraction(speed01, gears);
            var sweep = IdleRpmFraction + ((1f - IdleRpmFraction) * gearFraction);

            return speed01 <= 0f ? IdleRpmFraction : sweep;
        }

        /// <summary>Linearly maps RPM fraction onto a loop pitch range. Input is clamped.</summary>
        public static float Pitch(float rpm01, float minPitch, float maxPitch)
        {
            var low = SanitizePositive(minPitch, 0.01f);
            var high = SanitizePositive(maxPitch, low);
            if (high < low)
            {
                var swap = low;
                low = high;
                high = swap;
            }

            return low + ((high - low) * SanitizeUnit(rpm01));
        }

        /// <summary>Linearly maps RPM fraction onto a volume range. Input is clamped.</summary>
        public static float Volume(float rpm01, float minVolume, float maxVolume)
        {
            var low = SanitizeUnit(minVolume);
            var high = SanitizeUnit(maxVolume);
            if (high < low)
            {
                var swap = low;
                low = high;
                high = swap;
            }

            return low + ((high - low) * SanitizeUnit(rpm01));
        }

        private static float SanitizeUnit(float value)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static float SanitizePositive(float value, float fallback)
        {
            return IsFinite(value) && value > 0f ? value : fallback;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
