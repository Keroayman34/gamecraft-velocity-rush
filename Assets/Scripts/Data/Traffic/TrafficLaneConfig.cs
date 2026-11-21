using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Configuration data for the 4-lane highway traffic system (TDD §58; Phase 12 Stage 1).
    /// Maps parallel lanes onto procedural/authored track paths and governs spawning, pooling,
    /// and density parameters with fail-closed validation.
    /// </summary>
    [Serializable]
    public struct TrafficLaneConfig
    {
        public const int DefaultLaneCount = 4;
        public const float DefaultLaneWidth = 3.25f;
        public const int DefaultPoolSize = 20;
        public const float DefaultSpawnAheadDistance = 150f;
        public const float DefaultDespawnBehindDistance = 35f;
        public const float DefaultMinSameLaneSpacing = 28f;
        public const float DefaultTrafficMinSpeed = 12f; // ~43.2 km/h
        public const float DefaultTrafficMaxSpeed = 22f; // ~79.2 km/h
        public const float DefaultDensity = 0.5f;

        [Range(2, 8)]
        [SerializeField] public int laneCount;

        [Min(0.5f)]
        [SerializeField] public float laneWidth;

        [Range(4, 64)]
        [SerializeField] public int poolSize;

        [Min(20f)]
        [SerializeField] public float spawnAheadDistance;

        [Min(5f)]
        [SerializeField] public float despawnBehindDistance;

        [Min(5f)]
        [SerializeField] public float minSameLaneSpacing;

        [Min(1f)]
        [SerializeField] public float trafficMinSpeed;

        [Min(1f)]
        [SerializeField] public float trafficMaxSpeed;

        [Range(0f, 1f)]
        [SerializeField] public float density;

        /// <summary>Creates a default 4-lane traffic configuration.</summary>
        public static TrafficLaneConfig CreateDefault()
        {
            return new TrafficLaneConfig
            {
                laneCount = DefaultLaneCount,
                laneWidth = DefaultLaneWidth,
                poolSize = DefaultPoolSize,
                spawnAheadDistance = DefaultSpawnAheadDistance,
                despawnBehindDistance = DefaultDespawnBehindDistance,
                minSameLaneSpacing = DefaultMinSameLaneSpacing,
                trafficMinSpeed = DefaultTrafficMinSpeed,
                trafficMaxSpeed = DefaultTrafficMaxSpeed,
                density = DefaultDensity
            };
        }

        /// <summary>
        /// Validates traffic configuration fail-closed.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (laneCount < 2 || laneCount > 8)
            {
                error = "LaneCount must be between 2 and 8 (default 4).";
                return false;
            }

            if (laneWidth <= 0.5f || float.IsNaN(laneWidth) || float.IsInfinity(laneWidth))
            {
                error = "LaneWidth must be a positive finite value > 0.5m.";
                return false;
            }

            if (poolSize < 4 || poolSize > 64)
            {
                error = "PoolSize must be between 4 and 64.";
                return false;
            }

            if (spawnAheadDistance <= 20f || float.IsNaN(spawnAheadDistance) || float.IsInfinity(spawnAheadDistance))
            {
                error = "SpawnAheadDistance must be > 20m.";
                return false;
            }

            if (despawnBehindDistance <= 5f || float.IsNaN(despawnBehindDistance) || float.IsInfinity(despawnBehindDistance))
            {
                error = "DespawnBehindDistance must be > 5m.";
                return false;
            }

            if (minSameLaneSpacing <= 5f || float.IsNaN(minSameLaneSpacing) || float.IsInfinity(minSameLaneSpacing))
            {
                error = "MinSameLaneSpacing must be > 5m.";
                return false;
            }

            if (trafficMinSpeed <= 0f || trafficMaxSpeed < trafficMinSpeed ||
                float.IsNaN(trafficMinSpeed) || float.IsNaN(trafficMaxSpeed) ||
                float.IsInfinity(trafficMinSpeed) || float.IsInfinity(trafficMaxSpeed))
            {
                error = "Traffic speeds must be positive with maxSpeed >= minSpeed.";
                return false;
            }

            if (density < 0f || density > 1f || float.IsNaN(density) || float.IsInfinity(density))
            {
                error = "Density must be between 0.0 and 1.0.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Computes the lateral offset in meters relative to the track centerline for the given lane index (0..laneCount-1).
        /// Negative values are to the left; positive values are to the right.
        /// </summary>
        public float GetLaneLateralOffset(int laneIndex)
        {
            int clamped = Mathf.Clamp(laneIndex, 0, Mathf.Max(0, laneCount - 1));
            float centeredIndex = clamped - (laneCount - 1) * 0.5f;
            return centeredIndex * laneWidth;
        }

        /// <summary>
        /// Resolves the closest lane index for a given lateral offset from the track centerline.
        /// </summary>
        public int GetNearestLaneIndex(float lateralOffset)
        {
            if (laneWidth <= 0.001f)
            {
                return 0;
            }

            float centeredIndex = lateralOffset / laneWidth;
            int index = Mathf.RoundToInt(centeredIndex + (laneCount - 1) * 0.5f);
            return Mathf.Clamp(index, 0, laneCount - 1);
        }
    }
}
