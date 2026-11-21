using System;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Gameplay.Economy
{
    /// <summary>
    /// Coordinates collectible road coin placement in 4-lane highway arcs and evaluates zero-allocation
    /// player proximity collection (Phase 12 Stage 2; TDD §59).
    /// </summary>
    public sealed class RoadCoinManager : MonoBehaviour
    {
        private const int DefaultSeed = 42;
        private const float TrafficClearanceMargin = 6.0f;

        [SerializeField] private CoinSpawnConfig _config;
        [SerializeField] private TrafficLaneConfig _laneConfig;

        private CoinPickupPool _pool;
        private TrackPath _trackPath;
        private TrafficLaneManager _trafficManager;
        private Transform _playerTransform;
        private System.Random _prng;
        private int _initialSeed = DefaultSeed;

        private float _spawnCursorDistance;
        private int _sessionCoinsCollected;
        private int _sessionCoinCredits;

        public CoinPickupPool Pool => _pool;
        public CoinSpawnConfig Config => _config;
        public int SessionCoinsCollected => _sessionCoinsCollected;
        public int SessionCoinCredits => _sessionCoinCredits;

        public event Action<CoinCollectedEvent> OnCoinCollected;

        public void Initialize(
            TrackPath trackPath,
            TrafficLaneConfig laneConfig,
            CoinSpawnConfig coinConfig,
            TrafficLaneManager trafficManager = null,
            Transform playerTransform = null,
            int seed = DefaultSeed)
        {
            _trackPath = trackPath ?? throw new ArgumentNullException(nameof(trackPath));
            _laneConfig = laneConfig;
            _config = coinConfig;
            _trafficManager = trafficManager;
            _playerTransform = playerTransform;
            _initialSeed = seed;

            if (!_config.TryValidate(out var error))
            {
                Debug.LogWarning($"[RoadCoinManager] Invalid config ({error}), falling back to defaults.");
                _config = CoinSpawnConfig.CreateDefault();
            }

            _pool = new CoinPickupPool();
            _pool.Initialize(transform, _config);

            ResetForRetry();
        }

        /// <summary>
        /// Dynamically updates and validates coin spawn configuration for the active event (Phase 12 Stage 4).
        /// </summary>
        public void ApplyConfig(CoinSpawnConfig newConfig, TrafficLaneConfig? newLaneConfig = null)
        {
            if (newConfig.TryValidate(out var error))
            {
                _config = newConfig;
            }
            else
            {
                Debug.LogWarning($"[RoadCoinManager] ApplyConfig validation failed: {error}");
            }

            if (newLaneConfig.HasValue && newLaneConfig.Value.TryValidate(out _))
            {
                _laneConfig = newLaneConfig.Value;
            }
        }

        private void Update()
        {
            if (_playerTransform == null || _trackPath == null || _pool == null)
            {
                return;
            }

            float playerDist = ResolvePlayerTrackDistance();
            Tick(Time.deltaTime, _playerTransform.position, playerDist);
        }

        public void Tick(float deltaSeconds, Vector3 playerPos, float playerTrackDistance)
        {
            if (_pool == null || _trackPath == null)
            {
                return;
            }

            float despawnHorizon = playerTrackDistance - _config.despawnBehindDistance;
            float pickupRadiusSq = _config.pickupRadius * _config.pickupRadius;

            // Reverse for-loop for safe zero-allocation removal
            var activeCoins = _pool.ActiveCoins;
            int activeCount = _pool.ActiveCount;

            for (int i = activeCount - 1; i >= 0; i--)
            {
                var coin = activeCoins[i];
                if (coin == null || !coin.IsActive)
                {
                    continue;
                }

                if (coin.TrackDistance < despawnHorizon)
                {
                    // Passed behind player: recycle to pool
                    _pool.Return(coin);
                }
                else
                {
                    float distSq = (coin.WorldPosition - playerPos).sqrMagnitude;
                    if (distSq <= pickupRadiusSq)
                    {
                        // Proximity collection!
                        int lane = coin.LaneIndex;
                        _pool.Return(coin);

                        _sessionCoinsCollected++;
                        _sessionCoinCredits += _config.coinValue;

                        OnCoinCollected?.Invoke(new CoinCollectedEvent(
                            _config.coinValue,
                            _sessionCoinsCollected,
                            _sessionCoinCredits,
                            lane));
                    }
                    else
                    {
                        coin.TickSpin(deltaSeconds, _config.spinSpeedDegreesPerSecond);
                    }
                }
            }

            // Maintain coin arcs ahead of player
            MaintainSpawnHorizon(playerTrackDistance);
        }

        private void MaintainSpawnHorizon(float playerTrackDistance)
        {
            float horizon = playerTrackDistance + _config.spawnAheadDistance;
            if (_trackPath.IsClosedLoop)
            {
                horizon = Mathf.Min(horizon, playerTrackDistance + _trackPath.TotalLength);
            }
            else
            {
                horizon = Mathf.Min(horizon, _trackPath.TotalLength - 10f);
            }

            while (_spawnCursorDistance < horizon && _pool.AvailableCount >= _config.arcMinCoins)
            {
                SpawnCoinArc();
            }

            if (_spawnCursorDistance < playerTrackDistance + 10f)
            {
                _spawnCursorDistance = playerTrackDistance + 10f;
            }
        }

        private void SpawnCoinArc()
        {
            int laneCount = Mathf.Max(1, _laneConfig.laneCount);
            int selectedLane = _prng.Next(0, laneCount);
            int arcLength = _prng.Next(_config.arcMinCoins, _config.arcMaxCoins + 1);

            float lateralOffset = _laneConfig.GetLaneLateralOffset(selectedLane);

            for (int i = 0; i < arcLength; i++)
            {
                float coinDist = _spawnCursorDistance + i * _config.coinSpacingInArc;

                // Traffic overlap avoidance: avoid spawning embedded in active traffic
                if (IsBlockedByTraffic(selectedLane, coinDist))
                {
                    continue;
                }

                if (_pool.TryRent(out var coin))
                {
                    var sample = _trackPath.SampleAtDistance(coinDist, lateralOffset);
                    Vector3 worldPos = sample.Position + Vector3.up * _config.hoverHeight;
                    coin.Activate(selectedLane, coinDist, worldPos);
                }
                else
                {
                    break;
                }
            }

            _spawnCursorDistance += arcLength * _config.coinSpacingInArc + _config.minArcSpacing;
        }

        private bool IsBlockedByTraffic(int laneIndex, float distance)
        {
            if (_trafficManager == null || _trafficManager.Pool == null)
            {
                return false;
            }

            var activeTraffic = _trafficManager.Pool.ActiveVehicles;
            int count = _trafficManager.Pool.ActiveCount;

            for (int i = 0; i < count; i++)
            {
                var v = activeTraffic[i];
                if (v != null && v.IsActive && v.LaneIndex == laneIndex)
                {
                    if (Mathf.Abs(v.TrackDistance - distance) < TrafficClearanceMargin)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private float ResolvePlayerTrackDistance()
        {
            if (_playerTransform == null || _trackPath == null)
            {
                return 0f;
            }

            return _trackPath.ProjectDistance(_playerTransform.position);
        }

        public void ResetForRetry()
        {
            if (_pool != null)
            {
                _pool.ResetAll();
            }

            _sessionCoinsCollected = 0;
            _sessionCoinCredits = 0;
            _prng = new System.Random(_initialSeed);

            float playerDist = ResolvePlayerTrackDistance();
            _spawnCursorDistance = playerDist + 20f;

            if (_trackPath != null && _pool != null)
            {
                MaintainSpawnHorizon(playerDist);
            }
        }

        public void Teardown()
        {
            if (_pool != null)
            {
                _pool.Teardown();
                _pool = null;
            }

            _sessionCoinsCollected = 0;
            _sessionCoinCredits = 0;
            OnCoinCollected = null;
        }

        private void OnDestroy()
        {
            Teardown();
        }
    }
}
