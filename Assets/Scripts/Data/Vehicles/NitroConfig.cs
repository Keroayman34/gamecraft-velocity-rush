using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Configuration for vehicle nitro boost mechanics (Phase 12 Stage 3; TDD §60).
    /// Pure serializable data model with fail-closed validation.
    /// </summary>
    [Serializable]
    public sealed class NitroConfig
    {
        [Tooltip("Maximum nitro charge capacity (arbitrary units, typically 100).")]
        [Min(10f)]
        public float maxCharge = 100f;

        [Tooltip("Minimum charge required to trigger nitro activation (prevents stutter spam).")]
        [Min(1f)]
        public float minActivationCharge = 15f;

        [Tooltip("Rate at which nitro charge drains per second while actively boosting.")]
        [Min(1f)]
        public float drainRatePerSecond = 28f;

        [Tooltip("Passive charge regeneration rate per second while moving at speed.")]
        [Min(0f)]
        public float passiveRegenPerSecond = 2.5f;

        [Tooltip("Instant bonus charge awarded for high-speed near-miss passes.")]
        [Min(0f)]
        public float nearMissChargeBonus = 22f;

        [Tooltip("Charge accumulated per second during controlled vehicle drifts.")]
        [Min(0f)]
        public float driftChargePerSecond = 14f;

        [Tooltip("Engine motor torque multiplier applied during active nitro boost.")]
        [Range(1.05f, 2.5f)]
        public float boostTorqueMultiplier = 1.45f;

        [Tooltip("Linear damping reduction factor expanding top speed ceiling during boost.")]
        [Range(1.05f, 2.0f)]
        public float boostMaxSpeedMultiplier = 1.22f;

        /// <summary>
        /// Creates a validated default nitro configuration.
        /// </summary>
        public static NitroConfig CreateDefault()
        {
            return new NitroConfig();
        }

        /// <summary>
        /// Validates parameters fail-closed.
        /// </summary>
        public bool TryValidate(out string reason)
        {
            if (maxCharge < 10f)
            {
                reason = "maxCharge must be at least 10.";
                return false;
            }

            if (minActivationCharge <= 0f || minActivationCharge > maxCharge)
            {
                reason = "minActivationCharge must be > 0 and <= maxCharge.";
                return false;
            }

            if (drainRatePerSecond <= 0f)
            {
                reason = "drainRatePerSecond must be > 0.";
                return false;
            }

            if (passiveRegenPerSecond < 0f || nearMissChargeBonus < 0f || driftChargePerSecond < 0f)
            {
                reason = "Regeneration and bonus charge rates cannot be negative.";
                return false;
            }

            if (boostTorqueMultiplier < 1.0f)
            {
                reason = "boostTorqueMultiplier must be at least 1.0.";
                return false;
            }

            if (boostMaxSpeedMultiplier < 1.0f)
            {
                reason = "boostMaxSpeedMultiplier must be at least 1.0.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
