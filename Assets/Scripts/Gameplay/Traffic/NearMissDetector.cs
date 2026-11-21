using System;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Gameplay.Traffic
{
    /// <summary>
    /// Zero-allocation Near-Miss detector for high-speed highway traffic overtakes (Phase 12 Stage 1; TDD §58).
    /// Detects close lateral proximity at high speeds without collision, tracks combos, and emits non-allocating events.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NearMissDetector : MonoBehaviour
    {
        public const float DefaultMinNearMissSpeed = 15f; // ~54 km/h
        public const float DefaultNearMissLateralRadius = 2.8f; // Clearance boundary
        public const float DefaultCollisionClearanceMargin = 0.8f; // Below this is collision
        public const float DefaultLongitudinalWindow = 4.0f; // Along-track pass window
        public const float DefaultComboWindow = 3.5f;

        [Header("Detection Tuning")]
        [SerializeField, Min(1f)] private float minNearMissSpeed = DefaultMinNearMissSpeed;
        [SerializeField, Min(0.5f)] private float nearMissLateralRadius = DefaultNearMissLateralRadius;
        [SerializeField, Min(0.1f)] private float collisionMargin = DefaultCollisionClearanceMargin;
        [SerializeField, Min(1f)] private float longitudinalWindow = DefaultLongitudinalWindow;
        [SerializeField, Min(0.5f)] private float comboWindow = DefaultComboWindow;

        private TrafficLaneManager _trafficManager;
        private VehicleController _playerVehicle;
        private Transform _playerTransform;
        private int _comboCount;
        private float _lastNearMissTime = -10f;
        private bool _isInitialized;

        public int CurrentCombo => _comboCount;
        public float MinNearMissSpeed => minNearMissSpeed;
        public float NearMissLateralRadius => nearMissLateralRadius;

        /// <summary>Raised whenever a valid high-speed near-miss overtake occurs.</summary>
        public event NearMissHandler OnNearMiss;

        public void Initialize(TrafficLaneManager trafficManager, VehicleController playerVehicle)
        {
            _trafficManager = trafficManager;
            _playerVehicle = playerVehicle;
            _playerTransform = playerVehicle != null ? playerVehicle.transform : null;
            _comboCount = 0;
            _lastNearMissTime = -10f;
            _isInitialized = true;
        }

        public void InitializeForTesting(TrafficLaneManager trafficManager, Transform playerTransform, float testMinSpeed = 15f)
        {
            _trafficManager = trafficManager;
            _playerTransform = playerTransform;
            minNearMissSpeed = testMinSpeed;
            _comboCount = 0;
            _lastNearMissTime = -10f;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized || _trafficManager == null || _playerTransform == null)
            {
                return;
            }

            float currentSpeed = _playerVehicle != null ? _playerVehicle.Telemetry.Speed : 0f;
            EvaluateNearMisses(currentSpeed);
        }

        /// <summary>
        /// Evaluates active traffic vehicles for near-miss conditions. Public for deterministic test harnesses.
        /// Zero allocations.
        /// </summary>
        public void EvaluateNearMisses(float playerSpeed)
        {
            if (!_isInitialized || _trafficManager == null || _trafficManager.Pool == null || _playerTransform == null)
            {
                return;
            }

            // Near miss requires minimum threshold speed
            if (playerSpeed < minNearMissSpeed)
            {
                if (Time.time - _lastNearMissTime > comboWindow)
                {
                    _comboCount = 0;
                }
                return;
            }

            float playerTrackDist = _trafficManager.PlayerTrackDistance;
            Vector3 playerPos = _playerTransform.position;
            Vector3 playerForward = _playerTransform.forward;
            Vector3 playerRight = _playerTransform.right;

            var activeVehicles = _trafficManager.Pool.ActiveVehicles;
            int activeCount = activeVehicles.Length;

            for (int i = 0; i < activeCount; i++)
            {
                var vehicle = activeVehicles[i];
                if (vehicle == null || !vehicle.IsActive)
                {
                    continue;
                }

                // If collision occurred with this vehicle or near-miss was already triggered, skip
                if (vehicle.HasCollidedWithPlayer || vehicle.HasNearMissTriggered)
                {
                    continue;
                }

                // Longitudinal proximity check along track
                float deltaDist = playerTrackDist - vehicle.TrackDistance;
                if (Mathf.Abs(deltaDist) > longitudinalWindow)
                {
                    continue;
                }

                // Lateral clearance check using player-relative local space
                Vector3 toTraffic = vehicle.transform.position - playerPos;
                float lateralOffset = Mathf.Abs(Vector3.Dot(toTraffic, playerRight));
                float forwardOffset = Mathf.Abs(Vector3.Dot(toTraffic, playerForward));

                if (forwardOffset <= longitudinalWindow &&
                    lateralOffset <= nearMissLateralRadius &&
                    lateralOffset >= collisionMargin)
                {
                    // Valid near miss pass!
                    vehicle.HasNearMissTriggered = true;

                    // Update combo
                    if (Time.time - _lastNearMissTime <= comboWindow)
                    {
                        _comboCount++;
                    }
                    else
                    {
                        _comboCount = 1;
                    }

                    _lastNearMissTime = Time.time;

                    OnNearMiss?.Invoke(new NearMissEvent(
                        vehicle.VehicleId,
                        playerSpeed,
                        lateralOffset,
                        _comboCount));
                }
            }
        }

        public void ResetCombo()
        {
            _comboCount = 0;
            _lastNearMissTime = -10f;
        }
    }
}
