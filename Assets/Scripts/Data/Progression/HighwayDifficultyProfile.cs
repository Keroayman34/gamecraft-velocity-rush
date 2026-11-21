using System;
using VelocityRush.Data;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure, immutable value-type specification defining highway traffic, coin economy,
    /// and rival competition parameters for an event tier (Phase 12 Stage 4; TDD §61).
    /// </summary>
    [Serializable]
    public readonly struct HighwayDifficultyProfile : IEquatable<HighwayDifficultyProfile>
    {
        public readonly string EventId;
        public readonly float TrafficDensity;
        public readonly float TrafficMinSpeed;
        public readonly float TrafficMaxSpeed;
        public readonly float CoinArcSpacing;
        public readonly int CoinValue;
        public readonly int NearMissBonusCredits;
        public readonly float RivalBasePaceMultiplier;
        public readonly RubberBandConfig RubberBand;

        public HighwayDifficultyProfile(
            string eventId,
            float trafficDensity,
            float trafficMinSpeed,
            float trafficMaxSpeed,
            float coinArcSpacing,
            int coinValue,
            int nearMissBonusCredits,
            float rivalBasePaceMultiplier,
            RubberBandConfig rubberBand)
        {
            EventId = eventId;
            TrafficDensity = trafficDensity;
            TrafficMinSpeed = trafficMinSpeed;
            TrafficMaxSpeed = trafficMaxSpeed;
            CoinArcSpacing = coinArcSpacing;
            CoinValue = coinValue;
            NearMissBonusCredits = nearMissBonusCredits;
            RivalBasePaceMultiplier = rivalBasePaceMultiplier;
            RubberBand = rubberBand ?? RubberBandConfig.CreateDefault();
        }

        /// <summary>
        /// Validates difficulty configuration fail-closed.
        /// </summary>
        public bool TryValidate(out string reason)
        {
            if (string.IsNullOrWhiteSpace(EventId))
            {
                reason = "EventId cannot be null, empty, or whitespace.";
                return false;
            }

            if (TrafficDensity < 0.25f || TrafficDensity > 0.85f || float.IsNaN(TrafficDensity) || float.IsInfinity(TrafficDensity))
            {
                reason = $"TrafficDensity must be within [0.25, 0.85], got {TrafficDensity}.";
                return false;
            }

            if (TrafficMinSpeed <= 0f || TrafficMaxSpeed < TrafficMinSpeed ||
                float.IsNaN(TrafficMinSpeed) || float.IsNaN(TrafficMaxSpeed) ||
                float.IsInfinity(TrafficMinSpeed) || float.IsInfinity(TrafficMaxSpeed))
            {
                reason = $"TrafficMinSpeed ({TrafficMinSpeed}) and TrafficMaxSpeed ({TrafficMaxSpeed}) must be positive and TrafficMaxSpeed >= TrafficMinSpeed.";
                return false;
            }

            if (CoinArcSpacing <= 0f || float.IsNaN(CoinArcSpacing) || float.IsInfinity(CoinArcSpacing))
            {
                reason = $"CoinArcSpacing must be positive and finite, got {CoinArcSpacing}.";
                return false;
            }

            if (CoinValue <= 0)
            {
                reason = $"CoinValue must be greater than zero, got {CoinValue}.";
                return false;
            }

            if (NearMissBonusCredits < 0)
            {
                reason = $"NearMissBonusCredits must be non-negative, got {NearMissBonusCredits}.";
                return false;
            }

            if (RivalBasePaceMultiplier < 0.5f || RivalBasePaceMultiplier > 1.5f ||
                float.IsNaN(RivalBasePaceMultiplier) || float.IsInfinity(RivalBasePaceMultiplier))
            {
                reason = $"RivalBasePaceMultiplier must be within [0.5, 1.5], got {RivalBasePaceMultiplier}.";
                return false;
            }

            if (RubberBand == null)
            {
                reason = "RubberBand is null.";
                return false;
            }

            if (!RubberBand.TryValidate(out reason))
            {
                return false;
            }

            if (RubberBand.minMultiplier < 0.85f || RubberBand.maxMultiplier > 1.18f)
            {
                reason = $"RubberBand bounds [{RubberBand.minMultiplier}, {RubberBand.maxMultiplier}] must be strictly within [0.85, 1.18].";
                return false;
            }

            reason = null;
            return true;
        }

        public bool Equals(HighwayDifficultyProfile other)
        {
            return string.Equals(EventId, other.EventId, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return obj is HighwayDifficultyProfile other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EventId != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(EventId) : 0;
        }
    }
}
