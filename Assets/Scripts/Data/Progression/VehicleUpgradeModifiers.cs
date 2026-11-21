using System;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure value type carrying computed physics modifier multipliers (D-068, 0 B heap allocation).
    /// </summary>
    /// <remarks>
    /// Evaluated dynamically by the vehicle controller or spawner without ever mutating base
    /// <see cref="VehiclePhysicsProfile"/> ScriptableObject assets.
    /// </remarks>
    public readonly struct VehicleUpgradeModifiers : IEquatable<VehicleUpgradeModifiers>
    {
        public float TorqueMultiplier { get; }
        public float FrictionMultiplier { get; }
        public float BrakeTorqueMultiplier { get; }
        public float SuspensionMultiplier { get; }
        public float TransmissionRetentionMultiplier { get; }
        public float DragMultiplier { get; }

        public static VehicleUpgradeModifiers Default => new VehicleUpgradeModifiers(1f, 1f, 1f, 1f, 1f, 1f);

        public VehicleUpgradeModifiers(
            float torqueMultiplier,
            float frictionMultiplier,
            float brakeTorqueMultiplier,
            float suspensionMultiplier,
            float transmissionRetentionMultiplier,
            float dragMultiplier)
        {
            TorqueMultiplier = torqueMultiplier;
            FrictionMultiplier = frictionMultiplier;
            BrakeTorqueMultiplier = brakeTorqueMultiplier;
            SuspensionMultiplier = suspensionMultiplier;
            TransmissionRetentionMultiplier = transmissionRetentionMultiplier;
            DragMultiplier = dragMultiplier;
        }

        public bool Equals(VehicleUpgradeModifiers other)
        {
            return UnityEngine.Mathf.Approximately(TorqueMultiplier, other.TorqueMultiplier) &&
                   UnityEngine.Mathf.Approximately(FrictionMultiplier, other.FrictionMultiplier) &&
                   UnityEngine.Mathf.Approximately(BrakeTorqueMultiplier, other.BrakeTorqueMultiplier) &&
                   UnityEngine.Mathf.Approximately(SuspensionMultiplier, other.SuspensionMultiplier) &&
                   UnityEngine.Mathf.Approximately(TransmissionRetentionMultiplier, other.TransmissionRetentionMultiplier) &&
                   UnityEngine.Mathf.Approximately(DragMultiplier, other.DragMultiplier);
        }

        public override bool Equals(object obj)
        {
            return obj is VehicleUpgradeModifiers other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = TorqueMultiplier.GetHashCode();
                hash = (hash * 397) ^ FrictionMultiplier.GetHashCode();
                hash = (hash * 397) ^ BrakeTorqueMultiplier.GetHashCode();
                hash = (hash * 397) ^ SuspensionMultiplier.GetHashCode();
                hash = (hash * 397) ^ TransmissionRetentionMultiplier.GetHashCode();
                hash = (hash * 397) ^ DragMultiplier.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(VehicleUpgradeModifiers left, VehicleUpgradeModifiers right) => left.Equals(right);
        public static bool operator !=(VehicleUpgradeModifiers left, VehicleUpgradeModifiers right) => !left.Equals(right);
    }
}
