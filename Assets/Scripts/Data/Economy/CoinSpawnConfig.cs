using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Configuration for road coin spawning, object pooling, and arcade collection radius (Phase 12 Stage 2; TDD §59).
    /// </summary>
    [Serializable]
    public struct CoinSpawnConfig
    {
        public const int DefaultPoolSize = 36;
        public const int DefaultCoinValue = 10;
        public const int DefaultNearMissBonusCredits = 25;
        public const float DefaultPickupRadius = 2.4f;
        public const float DefaultSpawnAheadDistance = 140f;
        public const float DefaultDespawnBehindDistance = 25f;
        public const int DefaultArcMinCoins = 3;
        public const int DefaultArcMaxCoins = 5;
        public const float DefaultCoinSpacingInArc = 7.0f;
        public const float DefaultMinArcSpacing = 45.0f;
        public const float DefaultHoverHeight = 0.75f;
        public const float DefaultSpinSpeedDegreesPerSecond = 180f;

        [Range(8, 64)]
        [SerializeField] public int poolSize;

        [Min(1)]
        [SerializeField] public int coinValue;

        [Min(0)]
        [SerializeField] public int nearMissBonusCredits;

        [Min(0.5f)]
        [SerializeField] public float pickupRadius;

        [Min(10f)]
        [SerializeField] public float spawnAheadDistance;

        [Min(5f)]
        [SerializeField] public float despawnBehindDistance;

        [Min(1)]
        [SerializeField] public int arcMinCoins;

        [Min(1)]
        [SerializeField] public int arcMaxCoins;

        [Min(1f)]
        [SerializeField] public float coinSpacingInArc;

        [Min(10f)]
        [SerializeField] public float minArcSpacing;

        [Min(0.1f)]
        [SerializeField] public float hoverHeight;

        [Min(0f)]
        [SerializeField] public float spinSpeedDegreesPerSecond;

        public static CoinSpawnConfig CreateDefault()
        {
            return new CoinSpawnConfig
            {
                poolSize = DefaultPoolSize,
                coinValue = DefaultCoinValue,
                nearMissBonusCredits = DefaultNearMissBonusCredits,
                pickupRadius = DefaultPickupRadius,
                spawnAheadDistance = DefaultSpawnAheadDistance,
                despawnBehindDistance = DefaultDespawnBehindDistance,
                arcMinCoins = DefaultArcMinCoins,
                arcMaxCoins = DefaultArcMaxCoins,
                coinSpacingInArc = DefaultCoinSpacingInArc,
                minArcSpacing = DefaultMinArcSpacing,
                hoverHeight = DefaultHoverHeight,
                spinSpeedDegreesPerSecond = DefaultSpinSpeedDegreesPerSecond
            };
        }

        public bool TryValidate(out string reason)
        {
            if (poolSize < 8 || poolSize > 64)
            {
                reason = "PoolSize must be in [8..64].";
                return false;
            }

            if (coinValue <= 0)
            {
                reason = "CoinValue must be > 0.";
                return false;
            }

            if (nearMissBonusCredits < 0)
            {
                reason = "NearMissBonusCredits must be >= 0.";
                return false;
            }

            if (pickupRadius < 0.5f || float.IsNaN(pickupRadius) || float.IsInfinity(pickupRadius))
            {
                reason = "PickupRadius must be >= 0.5m.";
                return false;
            }

            if (spawnAheadDistance <= 0f || despawnBehindDistance <= 0f ||
                float.IsNaN(spawnAheadDistance) || float.IsInfinity(spawnAheadDistance) ||
                float.IsNaN(despawnBehindDistance) || float.IsInfinity(despawnBehindDistance))
            {
                reason = "SpawnAhead and DespawnBehind distances must be positive.";
                return false;
            }

            if (arcMinCoins <= 0 || arcMaxCoins < arcMinCoins)
            {
                reason = "ArcMinCoins must be > 0 and ArcMaxCoins >= ArcMinCoins.";
                return false;
            }

            if (coinSpacingInArc <= 0f || minArcSpacing <= 0f ||
                float.IsNaN(coinSpacingInArc) || float.IsInfinity(coinSpacingInArc) ||
                float.IsNaN(minArcSpacing) || float.IsInfinity(minArcSpacing))
            {
                reason = "CoinSpacingInArc and MinArcSpacing must be positive.";
                return false;
            }

            if (hoverHeight < 0f || float.IsNaN(hoverHeight) || float.IsInfinity(hoverHeight))
            {
                reason = "HoverHeight must be non-negative.";
                return false;
            }

            if (spinSpeedDegreesPerSecond < 0f || float.IsNaN(spinSpeedDegreesPerSecond) || float.IsInfinity(spinSpeedDegreesPerSecond))
            {
                reason = "SpinSpeedDegreesPerSecond must be non-negative.";
                return false;
            }

            reason = null;
            return true;
        }
    }
}
