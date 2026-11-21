using System;

namespace VelocityRush.Data
{
    /// <summary>
    /// Immutable value type representing the upgrade tier levels [0..3] across all six categories (D-011, D-068).
    /// </summary>
    public readonly struct VehicleUpgradeState : IEquatable<VehicleUpgradeState>
    {
        public const int MaxTier = 3;

        public int EngineTier { get; }
        public int TiresTier { get; }
        public int BrakesTier { get; }
        public int SuspensionTier { get; }
        public int TransmissionTier { get; }
        public int AeroTier { get; }

        public static VehicleUpgradeState Stock => new VehicleUpgradeState(0, 0, 0, 0, 0, 0);

        public VehicleUpgradeState(
            int engineTier,
            int tiresTier,
            int brakesTier,
            int suspensionTier,
            int transmissionTier,
            int aeroTier)
        {
            EngineTier = ClampTier(engineTier);
            TiresTier = ClampTier(tiresTier);
            BrakesTier = ClampTier(brakesTier);
            SuspensionTier = ClampTier(suspensionTier);
            TransmissionTier = ClampTier(transmissionTier);
            AeroTier = ClampTier(aeroTier);
        }

        private static int ClampTier(int tier)
        {
            if (tier < 0) return 0;
            if (tier > MaxTier) return MaxTier;
            return tier;
        }

        public int GetTier(UpgradeCategory category)
        {
            switch (category)
            {
                case UpgradeCategory.Engine: return EngineTier;
                case UpgradeCategory.Tires: return TiresTier;
                case UpgradeCategory.Brakes: return BrakesTier;
                case UpgradeCategory.Suspension: return SuspensionTier;
                case UpgradeCategory.Transmission: return TransmissionTier;
                case UpgradeCategory.Aerodynamics: return AeroTier;
                default: return 0;
            }
        }

        public VehicleUpgradeState WithTier(UpgradeCategory category, int newTier)
        {
            int c = ClampTier(newTier);
            switch (category)
            {
                case UpgradeCategory.Engine:
                    return new VehicleUpgradeState(c, TiresTier, BrakesTier, SuspensionTier, TransmissionTier, AeroTier);
                case UpgradeCategory.Tires:
                    return new VehicleUpgradeState(EngineTier, c, BrakesTier, SuspensionTier, TransmissionTier, AeroTier);
                case UpgradeCategory.Brakes:
                    return new VehicleUpgradeState(EngineTier, TiresTier, c, SuspensionTier, TransmissionTier, AeroTier);
                case UpgradeCategory.Suspension:
                    return new VehicleUpgradeState(EngineTier, TiresTier, BrakesTier, c, TransmissionTier, AeroTier);
                case UpgradeCategory.Transmission:
                    return new VehicleUpgradeState(EngineTier, TiresTier, BrakesTier, SuspensionTier, c, AeroTier);
                case UpgradeCategory.Aerodynamics:
                    return new VehicleUpgradeState(EngineTier, TiresTier, BrakesTier, SuspensionTier, TransmissionTier, c);
                default:
                    return this;
            }
        }

        public bool Equals(VehicleUpgradeState other)
        {
            return EngineTier == other.EngineTier &&
                   TiresTier == other.TiresTier &&
                   BrakesTier == other.BrakesTier &&
                   SuspensionTier == other.SuspensionTier &&
                   TransmissionTier == other.TransmissionTier &&
                   AeroTier == other.AeroTier;
        }

        public override bool Equals(object obj)
        {
            return obj is VehicleUpgradeState other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = EngineTier;
                hash = (hash * 397) ^ TiresTier;
                hash = (hash * 397) ^ BrakesTier;
                hash = (hash * 397) ^ SuspensionTier;
                hash = (hash * 397) ^ TransmissionTier;
                hash = (hash * 397) ^ AeroTier;
                return hash;
            }
        }

        public static bool operator ==(VehicleUpgradeState left, VehicleUpgradeState right) => left.Equals(right);
        public static bool operator !=(VehicleUpgradeState left, VehicleUpgradeState right) => !left.Equals(right);
    }
}
