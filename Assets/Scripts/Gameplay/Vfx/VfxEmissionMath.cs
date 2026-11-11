using System;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure VFX budget math: converts telemetry into bounded per-tick particle counts so the slice
    /// never emits more than its declared budget (T-7.9).
    /// </summary>
    /// <remarks>
    /// All curves are thresholded and saturated, and every input is sanitized. Keeping the budget
    /// arithmetic separate from <see cref="ParticleSystem"/> means the caps can be unit-tested and
    /// the runtime controller only performs emission.
    /// </remarks>
    public static class VfxEmissionMath
    {
        /// <summary>
        /// Intensity 0..1 for a slip value above <paramref name="slipThreshold"/>, saturating at
        /// <paramref name="maxSlip"/>.
        /// </summary>
        public static float SlipIntensity01(float slip, float slipThreshold, float maxSlip)
        {
            return Ramp01(slip, slipThreshold, maxSlip);
        }

        /// <summary>
        /// Intensity 0..1 for speed above <paramref name="speedThreshold"/>, saturating at
        /// <paramref name="maxSpeed"/>.
        /// </summary>
        public static float SpeedIntensity01(float speed, float speedThreshold, float maxSpeed)
        {
            return Ramp01(speed, speedThreshold, maxSpeed);
        }

        /// <summary>
        /// Number of particles to emit this tick for an intensity in 0..1, capped at
        /// <paramref name="maxPerTick"/>. Rounds to the nearest particle.
        /// </summary>
        public static int EmissionCount(float intensity01, int maxPerTick)
        {
            var cap = maxPerTick < 0 ? 0 : maxPerTick;
            var intensity = SanitizeUnit(intensity01);
            if (cap == 0 || intensity <= 0f)
            {
                return 0;
            }

            return (int)Math.Round(intensity * cap, MidpointRounding.AwayFromZero);
        }

        /// <summary>Smoke particle count for one tick of wheel slip.</summary>
        public static int SmokeCount(float slip, float slipThreshold, float maxSlip, int maxPerTick)
        {
            return EmissionCount(SlipIntensity01(slip, slipThreshold, maxSlip), maxPerTick);
        }

        /// <summary>Dust particle count for one tick at a given speed.</summary>
        public static int DustCount(float speed, float speedThreshold, float maxSpeed, int maxPerTick)
        {
            return EmissionCount(SpeedIntensity01(speed, speedThreshold, maxSpeed), maxPerTick);
        }

        /// <summary>Spark particle count for one impact at a given relative speed.</summary>
        public static int SparkCount(float impactSpeed, float impactThreshold, float maxImpactSpeed, int maxPerTick)
        {
            return EmissionCount(SpeedIntensity01(impactSpeed, impactThreshold, maxImpactSpeed), maxPerTick);
        }

        /// <summary>
        /// Accumulates a fractional emission budget so a low particle rate still emits at a stable
        /// average instead of rounding every tick to zero.
        /// </summary>
        /// <param name="budget">Current fractional budget (updated).</param>
        /// <param name="ratePerSecond">Particles per second; negative values are treated as zero.</param>
        /// <param name="deltaSeconds">Tick duration in seconds.</param>
        /// <returns>Whole particles to emit this tick.</returns>
        public static int ConsumeBudget(ref float budget, float ratePerSecond, float deltaSeconds)
        {
            if (!IsFinite(ratePerSecond) || ratePerSecond <= 0f || !IsFinite(deltaSeconds) || deltaSeconds <= 0f)
            {
                return 0;
            }

            budget += ratePerSecond * deltaSeconds;
            if (!IsFinite(budget) || budget < 1f)
            {
                return 0;
            }

            var whole = (int)budget;
            budget -= whole;
            return whole;
        }

        private static float Ramp01(float value, float threshold, float ceiling)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                return 0f;
            }

            var low = IsFinite(threshold) && threshold > 0f ? threshold : 0f;
            var high = IsFinite(ceiling) && ceiling > low ? ceiling : low + 1f;

            if (value <= low)
            {
                return 0f;
            }

            var ramp = (value - low) / (high - low);
            return ramp > 1f ? 1f : ramp;
        }

        private static float SanitizeUnit(float value)
        {
            if (!IsFinite(value) || value <= 0f)
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
