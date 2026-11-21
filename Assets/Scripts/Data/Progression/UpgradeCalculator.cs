using System;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure, stateless mathematical calculation of upgrade costs and modifiers (D-011, D-068).
    /// </summary>
    public static class UpgradeCalculator
    {
        public const int MaxTier = 3;

        public const float EngineDeltaPerTier = 0.03f;         // +3% torque per tier
        public const float TiresDeltaPerTier = 0.03f;          // +3% friction per tier
        public const float BrakesDeltaPerTier = 0.04f;         // +4% brake torque per tier
        public const float SuspensionDeltaPerTier = 0.03f;     // +3% spring/damper rate per tier
        public const float TransmissionDeltaPerTier = 0.04f;   // +4% torque retention per tier
        public const float AeroDragDeltaPerTier = -0.03f;      // -3% drag (higher top speed) per tier

        /// <summary>
        /// Returns the credit cost to purchase the specified target tier (1, 2, or 3) for a vehicle class.
        /// Returns 0 for tier 0 (stock) and -1 for invalid tiers.
        /// </summary>
        public static int GetUpgradeCost(VehicleClass vehicleClass, int targetTier)
        {
            if (targetTier <= 0) return 0;
            if (targetTier > MaxTier) return -1;

            int baseCost;
            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    baseCost = 250;
                    break;
                case VehicleClass.Sport:
                    baseCost = 500;
                    break;
                case VehicleClass.Drift:
                    baseCost = 750;
                    break;
                case VehicleClass.Supercar:
                    baseCost = 1000;
                    break;
                default:
                    baseCost = 500;
                    break;
            }

            return baseCost * targetTier;
        }

        /// <summary>
        /// Computes the exact <see cref="VehicleUpgradeModifiers"/> struct from an upgrade state (0 heap allocation).
        /// </summary>
        public static VehicleUpgradeModifiers ComputeModifiers(in VehicleUpgradeState state)
        {
            float torque = 1f + (state.EngineTier * EngineDeltaPerTier);
            float friction = 1f + (state.TiresTier * TiresDeltaPerTier);
            float brakes = 1f + (state.BrakesTier * BrakesDeltaPerTier);
            float suspension = 1f + (state.SuspensionTier * SuspensionDeltaPerTier);
            float transmission = 1f + (state.TransmissionTier * TransmissionDeltaPerTier);
            float drag = 1f + (state.AeroTier * AeroDragDeltaPerTier);

            return new VehicleUpgradeModifiers(torque, friction, brakes, suspension, transmission, drag);
        }

        /// <summary>
        /// Returns the modifier percentage bonus for a specific category and tier.
        /// </summary>
        public static float GetModifierDelta(UpgradeCategory category, int tier)
        {
            int clampedTier = Math.Max(0, Math.Min(MaxTier, tier));
            switch (category)
            {
                case UpgradeCategory.Engine: return clampedTier * EngineDeltaPerTier;
                case UpgradeCategory.Tires: return clampedTier * TiresDeltaPerTier;
                case UpgradeCategory.Brakes: return clampedTier * BrakesDeltaPerTier;
                case UpgradeCategory.Suspension: return clampedTier * SuspensionDeltaPerTier;
                case UpgradeCategory.Transmission: return clampedTier * TransmissionDeltaPerTier;
                case UpgradeCategory.Aerodynamics: return clampedTier * -AeroDragDeltaPerTier; // Reported as positive bonus
                default: return 0f;
            }
        }
    }
}
