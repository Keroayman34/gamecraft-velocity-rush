using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure serializable data model defining smart rival rubber-banding boundaries (Phase 12 Stage 3; TDD §60).
    /// </summary>
    [Serializable]
    public sealed class RubberBandConfig
    {
        [Tooltip("Longitudinal distance in meters behind player before catch-up boost begins.")]
        [Min(0f)]
        public float catchUpStartGap = 25f;

        [Tooltip("Maximum speed multiplier boost for trailing rivals (e.g. 1.12 = +12%).")]
        [Range(1.0f, 1.25f)]
        public float maxCatchUpBoost = 1.12f;

        [Tooltip("Longitudinal distance in meters ahead of player before lead rival deceleration begins.")]
        [Min(0f)]
        public float slowDownStartGap = 35f;

        [Tooltip("Minimum speed multiplier factor for leading rivals (e.g. 0.92 = -8%).")]
        [Range(0.80f, 1.0f)]
        public float maxSlowDownFactor = 0.92f;

        [Tooltip("Dead-zone around player in meters where pace multiplier remains strictly 1.0f for pure fair racing.")]
        [Min(0f)]
        public float deadZone = 15f;

        [Tooltip("Hard minimum pace multiplier clamp preventing rival stalls.")]
        [Range(0.70f, 0.95f)]
        public float minMultiplier = 0.85f;

        [Tooltip("Hard maximum pace multiplier clamp preventing rival teleportation.")]
        [Range(1.05f, 1.30f)]
        public float maxMultiplier = 1.18f;

        public static RubberBandConfig CreateDefault()
        {
            return new RubberBandConfig();
        }

        public bool TryValidate(out string reason)
        {
            if (catchUpStartGap <= 0f)
            {
                reason = "catchUpStartGap must be > 0.";
                return false;
            }

            if (slowDownStartGap <= 0f)
            {
                reason = "slowDownStartGap must be > 0.";
                return false;
            }

            if (deadZone < 0f || deadZone >= catchUpStartGap || deadZone >= slowDownStartGap)
            {
                reason = "deadZone must be >= 0 and strictly less than catchUpStartGap and slowDownStartGap.";
                return false;
            }

            if (maxCatchUpBoost < 1.0f || maxSlowDownFactor > 1.0f)
            {
                reason = "maxCatchUpBoost must be >= 1.0 and maxSlowDownFactor must be <= 1.0.";
                return false;
            }

            if (minMultiplier <= 0f || maxMultiplier <= minMultiplier)
            {
                reason = "Invalid multiplier bounds.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
