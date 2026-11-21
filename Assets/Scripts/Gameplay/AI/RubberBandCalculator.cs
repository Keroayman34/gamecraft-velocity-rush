using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Gameplay.AI
{
    /// <summary>
    /// Pure C# stateless rubber-banding calculator (Phase 12 Stage 3; TDD §60).
    /// Enforces fair, thrilling arcade competition without teleporting or stalling.
    /// </summary>
    public static class RubberBandCalculator
    {
        /// <summary>
        /// Calculates the bounded rubber-banding pace multiplier.
        /// </summary>
        /// <param name="rivalTrackDistance">Rival distance along track in meters.</param>
        /// <param name="playerTrackDistance">Player distance along track in meters.</param>
        /// <param name="basePaceMultiplier">Rival base pace scale (e.g. 1.0).</param>
        /// <param name="config">Validated rubber band parameters.</param>
        /// <returns>Pace multiplier strictly clamped to [config.minMultiplier, config.maxMultiplier].</returns>
        public static float CalculateMultiplier(
            float rivalTrackDistance,
            float playerTrackDistance,
            float basePaceMultiplier,
            RubberBandConfig config)
        {
            if (config == null)
            {
                return Mathf.Clamp(basePaceMultiplier, 0.85f, 1.18f);
            }

            // Positive = rival is ahead; negative = rival is behind player
            float gap = rivalTrackDistance - playerTrackDistance;
            float absGap = Mathf.Abs(gap);

            // Fair competition dead-zone: exact 1.0 multiplier
            if (absGap <= config.deadZone)
            {
                return Mathf.Clamp(basePaceMultiplier, config.minMultiplier, config.maxMultiplier);
            }

            float multiplierFactor = 1.0f;

            if (gap < -config.deadZone)
            {
                // Rival is behind player: catch-up boost kicks in
                float trailingDistance = -gap;
                if (trailingDistance >= config.catchUpStartGap)
                {
                    // Linear ramp over a 40m window from catchUpStartGap up to maxCatchUpBoost
                    float ramp = Mathf.Clamp01((trailingDistance - config.catchUpStartGap) / 40f);
                    multiplierFactor = Mathf.Lerp(1.0f, config.maxCatchUpBoost, ramp);
                }
                else
                {
                    // Smooth transition from deadZone boundary to catchUpStartGap
                    float subRamp = Mathf.Clamp01((trailingDistance - config.deadZone) / Mathf.Max(1f, config.catchUpStartGap - config.deadZone));
                    multiplierFactor = Mathf.Lerp(1.0f, 1.02f, subRamp);
                }
            }
            else if (gap > config.deadZone)
            {
                // Rival is ahead of player: slow-down damping kicks in if gap exceeds slowDownStartGap
                float leadingDistance = gap;
                if (leadingDistance >= config.slowDownStartGap)
                {
                    // Linear ramp over a 40m window from slowDownStartGap down to maxSlowDownFactor
                    float ramp = Mathf.Clamp01((leadingDistance - config.slowDownStartGap) / 40f);
                    multiplierFactor = Mathf.Lerp(1.0f, config.maxSlowDownFactor, ramp);
                }
                else
                {
                    // Smooth transition from deadZone boundary to slowDownStartGap
                    float subRamp = Mathf.Clamp01((leadingDistance - config.deadZone) / Mathf.Max(1f, config.slowDownStartGap - config.deadZone));
                    multiplierFactor = Mathf.Lerp(1.0f, 0.99f, subRamp);
                }
            }

            float finalMultiplier = basePaceMultiplier * multiplierFactor;
            return Mathf.Clamp(finalMultiplier, config.minMultiplier, config.maxMultiplier);
        }
    }
}
