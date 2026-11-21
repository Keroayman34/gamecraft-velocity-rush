using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay.AI
{
    /// <summary>
    /// Kinematic AI opponent driver (T-10.2.1–T-10.2.5, D-059/D-061).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The driver integrates a pose along a precomputed <see cref="TrackPath"/> cursor. It never
    /// reads or writes a <see cref="WheelCollider"/>, never adds a rigidbody solver, and never
    /// changes <c>VehicleController</c>. Commands are still exposed as normalized
    /// <see cref="IVehicleInput"/> values through <see cref="AIVehicleInput"/> so a future physics
    /// backend could consume the same controller without redesigning the AI.
    /// </para>
    /// <para>
    /// <b>Speed:</b> the path's curvature-limited target speed is shaped by an optional per-opponent
    /// pace scale and then by bounded rubber-banding. Rubber-banding is a strict ±10% multiplier on
    /// the target speed only (D-061): no teleporting, no input cheating, and no position changes.
    /// The player's progress is re-projected onto the path at a fixed cadence using a coarse/fine
    /// waypoint scan, so the projection is deterministic and allocation-free.
    /// </para>
    /// <para>
    /// <b>Interaction:</b> an overlap capsule detects nearby vehicles and applies a bounded,
    /// decaying lateral/forward push offset to the AI. A physics-driven player receives a capped
    /// velocity nudge instead of a collision impulse. AI colliders are query-only triggers
    /// (see <see cref="AIVisualRig"/>), so no solver contact ever occurs (D-061).
    /// </para>
    /// <para>
    /// <see cref="Tick"/> is the single testable step. It uses only struct math, cached references,
    /// and a preallocated <see cref="Collider"/> buffer, so it allocates 0 B per call after
    /// initialization (D-060).
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class AIDriver : MonoBehaviour
    {
        [Header("Path following")]
        [SerializeField, Min(0f)] private float lookaheadBase = 8f;
        [SerializeField, Min(0f)] private float lookaheadPerSpeed = 0.45f;
        [SerializeField, Min(1f)] private float maxLookahead = 40f;
        [SerializeField, Min(1f)] private float maxSteeringAngleDegrees = 30f;
        [SerializeField, Min(0f)] private float rideHeight = 0.35f;

        [Header("Speed shaping")]
        [SerializeField, Range(0.5f, 1.1f)] private float paceScale = 1f;
        [SerializeField, Min(0f)] private float throttleBand = 4f;
        [SerializeField, Min(0f)] private float brakeBand = 6f;
        [SerializeField, Min(0f)] private float minimumCruiseThrottle = 0.15f;

        [Header("Bounded rubber-banding (D-061)")]
        [SerializeField, Range(0f, 0.1f)] private float rubberBandLimit = 0.1f;
        [SerializeField, Min(0f)] private float rubberBandGainPerMeter = 0.005f;
        [SerializeField, Min(0.05f)] private float playerProjectionInterval = 0.25f;

        [Header("Soft push (D-061)")]
        [SerializeField, Min(0f)] private float softPushRadius = 1f;
        [SerializeField, Min(0.5f)] private float softPushMinSeparation = 2.4f;
        [SerializeField, Min(0f)] private float softPushMaxOffset = 1.5f;
        [SerializeField, Min(0f)] private float softPushRecoveryPerSecond = 3f;
        [SerializeField, Min(0f)] private float playerNudgeSpeed = 0.6f;
        [SerializeField] private LayerMask softPushMask = ~0;

        private readonly Collider[] _overlapBuffer = new Collider[8];

        private TrackPath _path;
        private TrackPathCursor _cursor;
        private AIVisualRig _visualRig;
        private Rigidbody _rigidbody;
        private Transform _playerTarget;

        private bool _initialized;
        private bool _startHold;
        private float _lateralBias;
        private float _targetLateralBias;
        private float _speed;
        private float _previousSpeed;
        private float _distanceTravelled;
        private float _throttle;
        private float _brake;
        private float _steering;
        private float _rubberBandScale = 1f;
        private float _targetSpeed;
        private float _projectionTimer;
        private float _playerDistance;
        private bool _hasPlayerDistance;
        private Vector3 _pushOffset;
        private Quaternion _rotation = Quaternion.identity;
        private RubberBandConfig _smartRubberBandConfig;
        private VelocityRush.Gameplay.Traffic.TrafficLaneManager _trafficManager;
        private VelocityRush.Data.TrafficLaneConfig _trafficLaneConfig;

        /// <summary>Whether <see cref="Initialize"/> has completed.</summary>
        public bool IsInitialized => _initialized;

        /// <summary>
        /// True while the synchronized start gate is holding this car on the grid (T-10.3.2).
        /// A held driver keeps its initialized pose and does not integrate or write commands.
        /// </summary>
        public bool IsStartHeld => _startHold;

        /// <summary>The path being followed, or null before initialization.</summary>
        public TrackPath Path => _path;

        /// <summary>Current planar path distance in meters.</summary>
        public float PathDistance => _cursor.Distance;

        /// <summary>Total distance travelled since initialization, including loop wraps.</summary>
        public float DistanceTravelled => _distanceTravelled;

        /// <summary>Current forward speed in m/s.</summary>
        public float Speed => _speed;

        /// <summary>Current shaped target speed in m/s (after pace and rubber-banding).</summary>
        public float TargetSpeed => _targetSpeed;

        /// <summary>Applied rubber-band multiplier, strictly within 0.9..1.1.</summary>
        public float RubberBandScale => _rubberBandScale;

        /// <summary>Applied per-opponent lateral bias in meters.</summary>
        public float LateralBias => _lateralBias;

        /// <summary>Current normalized throttle command (0..1).</summary>
        public float Throttle => _throttle;

        /// <summary>Current normalized brake command (0..1).</summary>
        public float Brake => _brake;

        /// <summary>Current normalized steering command (-1..1; positive = right).</summary>
        public float Steering => _steering;

        /// <summary>AI never requests a handbrake in Stage 2.</summary>
        public bool Handbrake => false;

        /// <summary>Current world rotation applied to the rig.</summary>
        public Quaternion Rotation => _rotation;

        /// <summary>Current soft-push offset applied on top of the racing line.</summary>
        public Vector3 PushOffset => _pushOffset;

        /// <summary>
        /// Binds the driver to a path and start pose. Safe to call again for a grid reset.
        /// </summary>
        /// <param name="path">Validated path to follow.</param>
        /// <param name="startDistance">Initial distance along the path in meters.</param>
        /// <param name="lateralBias">Per-opponent lane offset in meters (clamped to the road).</param>
        /// <param name="playerTarget">
        /// Optional player transform used only for bounded rubber-banding. Null disables it.
        /// </param>
        /// <param name="startSpeed">Initial forward speed in m/s.</param>
        public void Initialize(
            TrackPath path,
            float startDistance = 0f,
            float lateralBias = 0f,
            Transform playerTarget = null,
            float startSpeed = 0f)
        {
            if (path == null)
            {
                throw new System.ArgumentNullException(nameof(path));
            }

            _path = path;
            _cursor = path.CreateCursor(startDistance);
            _playerTarget = playerTarget;
            _lateralBias = Mathf.Clamp(lateralBias, -path.MaxLateralOffset, path.MaxLateralOffset);
            _speed = Mathf.Clamp(startSpeed, 0f, path.Settings.MaxSpeed);
            _previousSpeed = _speed;
            _distanceTravelled = 0f;
            _throttle = 0f;
            _brake = 0f;
            _steering = 0f;
            _rubberBandScale = 1f;
            _targetSpeed = 0f;
            _projectionTimer = playerProjectionInterval;
            _playerDistance = 0f;
            _hasPlayerDistance = false;
            _pushOffset = Vector3.zero;
            _startHold = false;
            _visualRig = GetComponent<AIVisualRig>();
            _rigidbody = GetComponent<Rigidbody>();
            _initialized = true;

            if (_visualRig != null)
            {
                _visualRig.Bind();
                _visualRig.ResetVisualState();
            }

            ApplyPose(0f);
        }

        /// <summary>
        /// Enables smart rubber-banding using Phase 12 dead-zone and bounded catch-up/slow-down rules (Phase 12 Stage 3; TDD §60).
        /// </summary>
        public void SetSmartRubberBand(RubberBandConfig config)
        {
            _smartRubberBandConfig = config;
        }

        /// <summary>
        /// Updates the base target pace multiplier for this rival (Phase 12 Stage 4).
        /// Clamped strictly within safe driving boundaries [0.5, 1.3].
        /// </summary>
        public void SetPaceScale(float scale)
        {
            paceScale = Mathf.Clamp(scale, 0.5f, 1.3f);
        }

        /// <summary>
        /// Enables traffic-aware lane weaving, scanning active traffic ahead and transitioning lateral bias to clear lanes.
        /// </summary>
        public void SetTrafficManager(VelocityRush.Gameplay.Traffic.TrafficLaneManager manager, VelocityRush.Data.TrafficLaneConfig laneConfig)
        {
            _trafficManager = manager;
            _trafficLaneConfig = laneConfig;
        }

        /// <summary>
        /// Holds or releases the car for the synchronized grid start (T-10.3.2). While held,
        /// <see cref="Tick"/> is a no-op and the commands are neutralized so a held car cannot creep
        /// forward. Releasing does not add speed or move the car; the normal path step starts on the
        /// next tick.
        /// </summary>
        /// <param name="held">True to hold on the grid, false to allow driving.</param>
        public void SetStartHold(bool held)
        {
            if (_startHold == held)
            {
                return;
            }

            _startHold = held;
            if (!held)
            {
                return;
            }

            _speed = 0f;
            _previousSpeed = 0f;
            _throttle = 0f;
            _brake = 0f;
            _steering = 0f;
            if (_initialized)
            {
                ApplyPose(0f);
            }
        }

        /// <summary>Resets the driver to a distance on the same path without rebuilding it.</summary>
        public void ResetTo(float startDistance, float startSpeed = 0f)
        {
            if (!_initialized)
            {
                return;
            }

            _cursor.Reset(startDistance);
            _speed = Mathf.Clamp(startSpeed, 0f, _path.Settings.MaxSpeed);
            _previousSpeed = _speed;
            _distanceTravelled = 0f;
            _pushOffset = Vector3.zero;
            _projectionTimer = playerProjectionInterval;
            _hasPlayerDistance = false;
            _rubberBandScale = 1f;
            if (_visualRig != null)
            {
                _visualRig.ResetVisualState();
            }

            ApplyPose(0f);
        }

        /// <summary>
        /// Advances the AI by one fixed step. This is the only AI simulation entry point; it is
        /// deterministic for identical inputs and allocates nothing.
        /// </summary>
        /// <param name="deltaTime">Step in seconds; non-positive values are ignored.</param>
        public void Tick(float deltaTime)
        {
            if (!_initialized || deltaTime <= 0f || _startHold)
            {
                return;
            }

            if (_playerTarget != null)
            {
                _projectionTimer += deltaTime;
                if (_projectionTimer >= playerProjectionInterval)
                {
                    _projectionTimer = 0f;
                    ProjectPlayerAndUpdateRubberBand();
                }
            }
            else
            {
                _rubberBandScale = 1f;
            }

            // Phase 12 Stage 3: Smooth traffic avoidance lane weaving
            if (_trafficManager != null && _trafficLaneConfig.laneCount > 0)
            {
                _targetLateralBias = RivalTrafficAvoidance.EvaluateTargetLateralBias(
                    _lateralBias,
                    _cursor.Distance,
                    _trafficManager,
                    _trafficLaneConfig);
                _lateralBias = Mathf.MoveTowards(_lateralBias, _targetLateralBias, 5f * deltaTime);
            }

            if (!_cursor.TrySample(_lateralBias, out var current) ||
                !_cursor.TrySampleAhead(CurrentLookahead(), _lateralBias, out var ahead))
            {
                return;
            }

            _targetSpeed = ShapeTargetSpeed(current, ahead);

            var acceleration = _targetSpeed >= _speed
                ? _path.Settings.ForwardAcceleration
                : _path.Settings.BrakingDeceleration;
            _previousSpeed = _speed;
            _speed = Mathf.MoveTowards(_speed, _targetSpeed, acceleration * deltaTime);

            var speedError = _targetSpeed - _speed;
            _throttle = Mathf.Clamp01(speedError / Mathf.Max(0.1f, throttleBand));
            if (_speed > 0.5f && _throttle < minimumCruiseThrottle)
            {
                _throttle = minimumCruiseThrottle;
            }

            _brake = Mathf.Clamp01(-speedError / Mathf.Max(0.1f, brakeBand));

            var toAhead = ahead.Position - current.Position;
            var forward = Vector3.Dot(toAhead, current.Tangent);
            var lateral = Vector3.Dot(toAhead, current.Right);
            var steeringAngle = Mathf.Atan2(lateral, Mathf.Max(0.1f, forward)) * Mathf.Rad2Deg;
            _steering = Mathf.Clamp(
                steeringAngle / Mathf.Max(1f, maxSteeringAngleDegrees),
                -1f,
                1f);

            var advance = _speed * deltaTime;
            _cursor.Advance(advance);
            _distanceTravelled += advance;

            if (!_path.IsClosedLoop && _cursor.Distance >= _path.TotalLength - 0.001f)
            {
                _speed = 0f;
            }

            ApplyPose(deltaTime);
        }

        private void FixedUpdate()
        {
            if (_initialized)
            {
                Tick(Time.fixedDeltaTime);
            }
        }

        private float CurrentLookahead()
        {
            return Mathf.Clamp(
                lookaheadBase + (lookaheadPerSpeed * _speed),
                lookaheadBase,
                Mathf.Max(lookaheadBase, maxLookahead));
        }

        private float ShapeTargetSpeed(in PathSample current, in PathSample ahead)
        {
            // A closed loop is never "ending"; an open path decelerates into its endpoint.
            if (!_path.IsClosedLoop)
            {
                var remaining = _path.TotalLength - _cursor.Distance;
                if (remaining <= CurrentLookahead())
                {
                    return 0f;
                }
            }

            var pathSpeed = Mathf.Min(current.TargetSpeed, ahead.TargetSpeed);
            var shaped = pathSpeed * paceScale * _rubberBandScale;
            var ceiling = _path.Settings.MaxSpeed;
            return Mathf.Clamp(shaped, 0f, ceiling);
        }

        private void ProjectPlayerAndUpdateRubberBand()
        {
            var playerPosition = _playerTarget.position;
            var count = _path.Count;
            if (count < 2)
            {
                _rubberBandScale = 1f;
                return;
            }

            // Coarse pass over the whole path, then a local refinement. This stays deterministic
            // and allocation-free while being robust to a player that spins out or falls behind.
            var stride = 8;
            var bestIndex = 0;
            var bestDistanceSquared = float.MaxValue;
            for (var i = 0; i < count; i += stride)
            {
                var center = _path.WaypointAt(i).Center;
                var dx = center.x - playerPosition.x;
                var dz = center.z - playerPosition.z;
                var distanceSquared = (dx * dx) + (dz * dz);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestIndex = i;
                }
            }

            var from = Mathf.Max(0, bestIndex - stride);
            var to = Mathf.Min(count - 1, bestIndex + stride);
            for (var i = from; i <= to; i++)
            {
                var center = _path.WaypointAt(i).Center;
                var dx = center.x - playerPosition.x;
                var dz = center.z - playerPosition.z;
                var distanceSquared = (dx * dx) + (dz * dz);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestIndex = i;
                }
            }

            _playerDistance = _path.WaypointAt(bestIndex).Distance;
            _hasPlayerDistance = true;

            if (_smartRubberBandConfig != null)
            {
                _rubberBandScale = RubberBandCalculator.CalculateMultiplier(_cursor.Distance, _playerDistance, 1f, _smartRubberBandConfig);
                return;
            }

            var delta = _playerDistance - _cursor.Distance;
            if (_path.IsClosedLoop && _path.TotalLength > 0.001f)
            {
                var half = _path.TotalLength * 0.5f;
                while (delta > half)
                {
                    delta -= _path.TotalLength;
                }

                while (delta < -half)
                {
                    delta += _path.TotalLength;
                }
            }

            var scale = 1f + (rubberBandGainPerMeter * delta);
            _rubberBandScale = Mathf.Clamp(scale, 1f - rubberBandLimit, 1f + rubberBandLimit);
        }

        private void ApplyPose(float deltaTime)
        {
            if (!_cursor.TrySample(_lateralBias, out var sample))
            {
                return;
            }

            if (softPushRadius > 0f)
            {
                ResolveSoftPush(sample);
            }

            if (softPushRecoveryPerSecond > 0f && deltaTime > 0f)
            {
                _pushOffset = Vector3.MoveTowards(
                    _pushOffset,
                    Vector3.zero,
                    softPushRecoveryPerSecond * deltaTime);
            }

            var tangent = sample.Tangent;
            if (tangent.sqrMagnitude > 0.000001f)
            {
                _rotation = Quaternion.LookRotation(tangent, Vector3.up);
            }

            transform.SetPositionAndRotation(
                sample.Position + (Vector3.up * rideHeight) + _pushOffset,
                _rotation);

            if (_visualRig != null)
            {
                var lateralAcceleration = _speed * _speed * sample.Curvature;
                var longitudinalAcceleration = deltaTime > 0.000001f
                    ? (_speed - _previousSpeed) / deltaTime
                    : 0f;
                _visualRig.Apply(
                    deltaTime,
                    _speed,
                    _steering,
                    lateralAcceleration,
                    longitudinalAcceleration,
                    sample.Position.y);
            }
        }

        private void ResolveSoftPush(in PathSample sample)
        {
            // Use the pose that is actually in the world for separation; the path sample is only
            // the desired point. Mixing the two makes two overlapping cars push the same way.
            var selfPosition = transform.position;
            var groundY = sample.Position.y;
            var bottom = new Vector3(selfPosition.x, groundY + 0.2f, selfPosition.z);
            var top = new Vector3(selfPosition.x, groundY + 1.4f, selfPosition.z);
            var count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                softPushRadius,
                _overlapBuffer,
                softPushMask,
                QueryTriggerInteraction.Collide);

            for (var i = 0; i < count; i++)
            {
                var hit = _overlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                var otherBody = hit.attachedRigidbody;
                if (otherBody == null || otherBody == _rigidbody)
                {
                    continue;
                }

                var otherCenter = hit.bounds.center;
                var dx = selfPosition.x - otherCenter.x;
                var dz = selfPosition.z - otherCenter.z;
                var distanceSquared = (dx * dx) + (dz * dz);
                if (distanceSquared >= softPushMinSeparation * softPushMinSeparation)
                {
                    continue;
                }

                var distance = Mathf.Sqrt(distanceSquared);
                float directionX;
                float directionZ;
                if (distance > 0.0001f)
                {
                    directionX = dx / distance;
                    directionZ = dz / distance;
                }
                else
                {
                    // Deterministic tie-break so two cars spawned on the same point still separate:
                    // compare the same object kind on both sides, which gives each car the
                    // opposite sign. Hashes are stable within a run.
                    var sign = gameObject.GetHashCode() < otherBody.gameObject.GetHashCode()
                        ? 1f
                        : -1f;
                    directionX = sample.Right.x * sign;
                    directionZ = sample.Right.z * sign;
                    distance = 0f;
                }

                var penetration = softPushMinSeparation - distance;
                _pushOffset.x += directionX * penetration * 0.5f;
                _pushOffset.z += directionZ * penetration * 0.5f;
                if (_pushOffset.sqrMagnitude > softPushMaxOffset * softPushMaxOffset)
                {
                    _pushOffset = _pushOffset.normalized * softPushMaxOffset;
                }

                // Any dynamic, non-AI body is treated as the player or a pushable prop. AI-to-AI
                // separation stays symmetric through each driver's own offset.
                if (playerNudgeSpeed > 0f && otherBody.GetComponent<AIDriver>() == null)
                {
                    NudgePlayer(otherBody, -directionX, -directionZ);
                }
            }
        }

        private void NudgePlayer(Rigidbody playerBody, float directionX, float directionZ)
        {
            // Never fight an already-moving player: the nudge only rescues a slow or stationary
            // car, and the resulting horizontal speed is capped at playerNudgeSpeed.
            var velocity = playerBody.linearVelocity;
            var horizontalSpeed = Mathf.Sqrt((velocity.x * velocity.x) + (velocity.z * velocity.z));
            if (horizontalSpeed >= playerNudgeSpeed)
            {
                return;
            }

            var along = (velocity.x * directionX) + (velocity.z * directionZ);
            if (along >= playerNudgeSpeed)
            {
                return;
            }

            var delta = playerNudgeSpeed - along;
            velocity.x += directionX * delta;
            velocity.z += directionZ * delta;

            var pushedSpeed = Mathf.Sqrt((velocity.x * velocity.x) + (velocity.z * velocity.z));
            if (pushedSpeed > playerNudgeSpeed)
            {
                var scale = playerNudgeSpeed / pushedSpeed;
                velocity.x *= scale;
                velocity.z *= scale;
            }

            playerBody.linearVelocity = velocity;
        }

        /// <summary>
        /// Test/telemetry hook: the projected player path distance used by the last rubber-band
        /// update, or 0 when no player is bound.
        /// </summary>
        public float PlayerPathDistance => _hasPlayerDistance ? _playerDistance : 0f;
    }
}
