using System;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Gameplay.Traffic
{
    /// <summary>
    /// Zero-allocation 4-lane highway traffic manager (Phase 12 Stage 1; TDD §58).
    /// Manages pre-warmed pooling, anti-wall safe-gap spawning, kinematic lane movement,
    /// and non-blocking collision penalties.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrafficLaneManager : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Traffic] ";
        public const float DefaultCollisionCooldown = 0.5f;

        [Header("Traffic Configuration")]
        [SerializeField] private TrafficLaneConfig config = TrafficLaneConfig.CreateDefault();
        [SerializeField] private GameObject customVehiclePrefab;

        private TrackPath _trackPath;
        private VehicleController _playerVehicle;
        private Transform _playerTransform;
        private TrafficVehiclePool _pool;
        private float _lastCollisionTime = -10f;
        private float _spawnCursorDistance;
        private bool _isInitialized;
        private int _pseudoRandomSeed = 1337;

        public TrafficLaneConfig Config => config;
        public TrafficVehiclePool Pool => _pool;
        public bool IsInitialized => _isInitialized;
        public float PlayerTrackDistance => ResolvePlayerTrackDistance();

        /// <summary>Raised whenever the player collides with a traffic vehicle.</summary>
        public event TrafficCollisionHandler OnTrafficCollision;

        /// <summary>
        /// Dynamically updates and validates traffic configuration for the active event (Phase 12 Stage 4).
        /// </summary>
        public void ApplyConfig(TrafficLaneConfig newConfig)
        {
            if (newConfig.TryValidate(out var error))
            {
                config = newConfig;
            }
            else
            {
                Debug.LogWarning(LogPrefix + "ApplyConfig validation failed: " + error);
            }
        }

        private void Awake()
        {
            if (!config.TryValidate(out var error))
            {
                Debug.LogWarning(LogPrefix + "Config validation failed: " + error + ". Restoring defaults.");
                config = TrafficLaneConfig.CreateDefault();
            }
        }

        private void OnDestroy()
        {
            Teardown();
        }

        /// <summary>
        /// Initializes the traffic manager with the active track path and player reference.
        /// Pre-warms the pool immediately.
        /// </summary>
        public void Initialize(TrackPath path, VehicleController player, GameObject prefabOverride = null)
        {
            if (_isInitialized)
            {
                ResetTraffic();
                return;
            }

            _trackPath = path;
            _playerVehicle = player;
            _playerTransform = player != null ? player.transform : null;

            if (prefabOverride != null)
            {
                customVehiclePrefab = prefabOverride;
            }

            var poolRoot = new GameObject("TrafficPool_Container").transform;
            poolRoot.SetParent(transform, false);

            _pool = new TrafficVehiclePool(config.poolSize);
            _pool.Initialize(poolRoot, this, customVehiclePrefab);

            _spawnCursorDistance = ResolvePlayerTrackDistance() + 20f;
            _isInitialized = true;

            // Pre-seed traffic ahead of player
            SeedInitialTraffic();
        }

        /// <summary>
        /// Explicit initialization for tests with custom config.
        /// </summary>
        public void InitializeForTesting(TrackPath path, Transform playerTransform, TrafficLaneConfig customConfig)
        {
            config = customConfig;
            _trackPath = path;
            _playerTransform = playerTransform;

            var poolRoot = new GameObject("TrafficPool_TestContainer").transform;
            poolRoot.SetParent(transform, false);

            _pool = new TrafficVehiclePool(config.poolSize);
            _pool.Initialize(poolRoot, this, null);

            _spawnCursorDistance = ResolvePlayerTrackDistance() + 20f;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized || _trackPath == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            TickTraffic(dt);
        }

        /// <summary>
        /// Per-frame traffic simulation tick. Public for deterministic test harness execution.
        /// </summary>
        public void TickTraffic(float deltaSeconds)
        {
            if (!_isInitialized || _trackPath == null)
            {
                return;
            }

            float playerDist = ResolvePlayerTrackDistance();

            // 1. Advance active traffic vehicles & recycle those falling behind
            var activeVehicles = _pool.ActiveVehicles;
            for (int i = activeVehicles.Length - 1; i >= 0; i--)
            {
                var vehicle = activeVehicles[i];
                if (vehicle == null || !vehicle.IsActive)
                {
                    continue;
                }

                // Advance kinematic movement
                float lateralOffset = config.GetLaneLateralOffset(vehicle.LaneIndex);
                vehicle.Advance(deltaSeconds, _trackPath, lateralOffset);

                // Recycle if behind player or past track end
                if ((playerDist - vehicle.TrackDistance > config.despawnBehindDistance) ||
                    (vehicle.TrackDistance >= _trackPath.TotalLength - 10f))
                {
                    _pool.Return(vehicle);
                }
            }

            // 2. Spawn traffic ahead up to spawnAheadDistance
            float spawnHorizon = playerDist + config.spawnAheadDistance;
            if (_trackPath.IsClosedLoop)
            {
                spawnHorizon = Mathf.Min(spawnHorizon, playerDist + _trackPath.TotalLength);
            }
            else
            {
                spawnHorizon = Mathf.Min(spawnHorizon, _trackPath.TotalLength - 20f);
            }

            float minSpacing = config.minSameLaneSpacing;
            float step = Mathf.Max(10f, minSpacing / Mathf.Max(0.1f, config.density));

            while (_spawnCursorDistance < spawnHorizon && _pool.AvailableCount > 0)
            {
                int lane = SelectFairLane(_spawnCursorDistance);
                if (lane >= 0)
                {
                    float speed = EvaluateTrafficSpeed(lane, _spawnCursorDistance);
                    SpawnVehicle(lane, _spawnCursorDistance, speed);
                }

                _spawnCursorDistance += step;
            }

            // Maintain cursor ahead of player
            if (_spawnCursorDistance < playerDist + 20f)
            {
                _spawnCursorDistance = playerDist + 20f;
            }
        }

        /// <summary>
        /// Anti-Wall Fairness Check:
        /// Verifies whether spawning at candidate distance in candidate lane preserves
        /// at least one open, passable lane across the highway slice.
        /// Checks both the candidate slice and all surrounding vehicle slices.
        /// </summary>
        public bool CanSpawnAt(int candidateLane, float candidateDistance)
        {
            if (candidateLane < 0 || candidateLane >= config.laneCount)
            {
                return false;
            }

            float sameLaneMinGap = config.minSameLaneSpacing;
            float wallWindow = config.minSameLaneSpacing * 0.75f;

            var activeVehicles = _pool.ActiveVehicles;
            int activeCount = activeVehicles.Length;

            // 1. Check same-lane minimum spacing
            for (int i = 0; i < activeCount; i++)
            {
                var v = activeVehicles[i];
                if (v.LaneIndex == candidateLane && Mathf.Abs(v.TrackDistance - candidateDistance) < sameLaneMinGap)
                {
                    return false;
                }
            }

            int allLanesMask = (1 << config.laneCount) - 1;

            // 2. Check longitudinal wall avoidance across all lanes centered at candidateDistance
            int candidateMask = (1 << candidateLane);
            for (int i = 0; i < activeCount; i++)
            {
                var v = activeVehicles[i];
                if (Mathf.Abs(v.TrackDistance - candidateDistance) < wallWindow)
                {
                    candidateMask |= (1 << v.LaneIndex);
                }
            }

            if ((candidateMask & allLanesMask) == allLanesMask)
            {
                // Impassable wall detected at candidate slice! Reject spawn.
                return false;
            }

            // 3. Bidirectional neighbor check: ensure adding candidate does not cause any
            // active vehicle's existing slice to become a complete 4-lane wall.
            for (int i = 0; i < activeCount; i++)
            {
                var v = activeVehicles[i];
                if (Mathf.Abs(v.TrackDistance - candidateDistance) < wallWindow)
                {
                    int vWindowMask = (1 << candidateLane) | (1 << v.LaneIndex);
                    for (int j = 0; j < activeCount; j++)
                    {
                        var other = activeVehicles[j];
                        if (Mathf.Abs(other.TrackDistance - v.TrackDistance) < wallWindow)
                        {
                            vWindowMask |= (1 << other.LaneIndex);
                        }
                    }

                    if ((vWindowMask & allLanesMask) == allLanesMask)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Selects an eligible lane that passes the anti-wall guarantee.
        /// Returns -1 if no lane is currently safe.
        /// </summary>
        public int SelectFairLane(float distance)
        {
            int laneCount = config.laneCount;
            int startOffset = NextPseudoRandom(0, laneCount);

            for (int i = 0; i < laneCount; i++)
            {
                int candidateLane = (startOffset + i) % laneCount;
                if (CanSpawnAt(candidateLane, distance))
                {
                    return candidateLane;
                }
            }

            return -1;
        }

        private void SpawnVehicle(int laneIndex, float distance, float speed)
        {
            var vehicle = _pool.Rent();
            if (vehicle == null)
            {
                return;
            }

            float lateralOffset = config.GetLaneLateralOffset(laneIndex);
            var sample = _trackPath.SampleAtDistance(distance, lateralOffset);
            var rot = sample.Tangent.sqrMagnitude > 0.001f ? Quaternion.LookRotation(sample.Tangent, Vector3.up) : Quaternion.identity;

            vehicle.Spawn(laneIndex, distance, speed, sample.Position, rot);
        }

        private float EvaluateTrafficSpeed(int laneIndex, float distance)
        {
            // Deterministic uniform highway pace across active lanes preserves rigid anti-wall spacing
            // and guarantees zero inter-traffic pile-ups along the spline.
            return Mathf.Lerp(config.trafficMinSpeed, config.trafficMaxSpeed, 0.5f);
        }

        private void SeedInitialTraffic()
        {
            float playerDist = ResolvePlayerTrackDistance();
            _spawnCursorDistance = playerDist + 25f;
            float horizon = playerDist + config.spawnAheadDistance;
            float step = config.minSameLaneSpacing / Mathf.Max(0.2f, config.density);

            while (_spawnCursorDistance < horizon && _pool.AvailableCount > 0)
            {
                int lane = SelectFairLane(_spawnCursorDistance);
                if (lane >= 0)
                {
                    float speed = EvaluateTrafficSpeed(lane, _spawnCursorDistance);
                    SpawnVehicle(lane, _spawnCursorDistance, speed);
                }

                _spawnCursorDistance += step;
            }
        }

        public void HandleTrafficCollision(TrafficVehicle traffic, VehicleController player, Vector3 contactPoint)
        {
            if (traffic == null || !traffic.IsActive)
            {
                return;
            }

            if (Time.time - _lastCollisionTime < DefaultCollisionCooldown)
            {
                return;
            }

            _lastCollisionTime = Time.time;
            traffic.HasCollidedWithPlayer = true;

            float playerSpeed = player != null ? player.Telemetry.Speed : 0f;

            // Non-blocking arcade penalty: scale down forward velocity by 25% without dead-stop
            if (player != null && player.Chassis != null)
            {
                player.Chassis.linearVelocity *= 0.75f;

                // Mild lateral nudge away from traffic center
                Vector3 away = (player.transform.position - traffic.transform.position);
                away.y = 0f;
                if (away.sqrMagnitude > 0.001f)
                {
                    player.Chassis.AddForce(away.normalized * 3f, ForceMode.VelocityChange);
                }
            }

            OnTrafficCollision?.Invoke(new TrafficCollisionEvent(
                traffic.VehicleId,
                traffic.LaneIndex,
                traffic.Speed,
                playerSpeed,
                contactPoint));
        }

        /// <summary>
        /// Resets all traffic cleanly during in-scene fast retry without allocations or scene reloading.
        /// </summary>
        public void ResetTraffic()
        {
            if (_pool != null)
            {
                _pool.ResetAll();
            }

            _lastCollisionTime = -10f;
            _spawnCursorDistance = ResolvePlayerTrackDistance() + 20f;

            if (_trackPath != null)
            {
                SeedInitialTraffic();
            }
        }

        /// <summary>
        /// Tears down the traffic pool upon scene exit leaving zero retained objects.
        /// </summary>
        public void Teardown()
        {
            if (_pool != null)
            {
                _pool.Teardown();
                _pool = null;
            }

            _isInitialized = false;
        }

        private float ResolvePlayerTrackDistance()
        {
            if (_trackPath == null)
            {
                return 0f;
            }

            if (_playerTransform != null)
            {
                return _trackPath.ProjectDistance(_playerTransform.position);
            }

            return 0f;
        }

        private int NextPseudoRandom(int min, int max)
        {
            _pseudoRandomSeed = (_pseudoRandomSeed * 1103515245 + 12345) & 0x7fffffff;
            return min + (_pseudoRandomSeed % (max - min));
        }
    }
}
