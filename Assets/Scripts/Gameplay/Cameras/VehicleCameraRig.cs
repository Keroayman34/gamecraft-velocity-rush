using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// T5.3 third-person chase camera: telemetry-driven look-ahead, critically damped position and
    /// rotation smoothing, speed-scaled FOV, and sphere-cast collision avoidance (TDD §§38.1–38.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prototype scope only: one rig per player vehicle, no Cinemachine, no camera framework, no
    /// gameplay state ownership. The rig is a read-only consumer of <see cref="IVehicleTelemetry"/>
    /// (speed, grounded state) and of the follow target's pose; it never reads or writes physics
    /// state. Distances, smoothing, FOV, and look-ahead are placeholder profile values, not final
    /// presentation design (OQ-01/OQ-A06; feel gate D-023).
    /// </para>
    /// <para>
    /// Position and rotation use critically damped springs: <see cref="Vector3.SmoothDamp"/> for
    /// translation and a deterministic quaternion spring for rotation. Paused time freezes both so
    /// resuming cannot replay a pre-pause oscillation.
    /// </para>
    /// <para>
    /// Collision uses a non-allocating <see cref="Physics.SphereCastNonAlloc"/>. The serialized
    /// <see cref="ObstructionMask"/> is the seam for track/environment layers only; the prototype
    /// scene wires it to the dedicated <c>Track</c> layer, and colliders under the follow target are
    /// always skipped so the vehicle can never obstruct its own camera.
    /// </para>
    /// <para>
    /// Smoothing state is kept separate from the collision-clamped transform position. Reusing the
    /// clamped position as the next frame's smoothing source would keep the restore target at the
    /// obstructed distance, so the camera could not smoothly return to its normal follow distance.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleCameraRig : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Camera] ";
        private const float MinimumSqrDistance = 0.0001f;

        [SerializeField] private CameraProfile profile;
        [SerializeField] private Transform target;
        [Tooltip("MonoBehaviour implementing IVehicleTelemetry (the vehicle controller in the prototype).")]
        [SerializeField] private MonoBehaviour telemetrySource;
        [Tooltip("Collision layers for camera obstruction. The prototype scene uses the Track layer only.")]
        [SerializeField] private LayerMask obstructionMask = Physics.DefaultRaycastLayers;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private Camera _camera;
        private IVehicleTelemetry _telemetry;
        private Rigidbody _targetBody;
        private Vector3 _smoothedPivot;
        private Vector3 _smoothedPosition;
        private Vector3 _pivotVelocity;
        private Vector3 _positionVelocity;
        private Vector3 _rotationVelocity;
        private Quaternion _smoothedRotation = Quaternion.identity;
        private float _distanceVelocity;
        private float _currentDistance;
        private bool _initialized;

        /// <summary>The assigned placeholder profile (read-only for consumers).</summary>
        public CameraProfile Profile => profile;

        /// <summary>The followed vehicle root or marker.</summary>
        public Transform Target => target;

        /// <summary>The resolved telemetry source, or null before <c>Awake</c>.</summary>
        public IVehicleTelemetry TelemetrySource => _telemetry;

        /// <summary>Collision layers used by the obstruction sphere-cast.</summary>
        public LayerMask ObstructionMask => obstructionMask;

        /// <summary>Final camera distance from the follow pivot after collision clamping.</summary>
        public float CurrentDistance { get; private set; }

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (!TryValidateRig(out var error))
            {
                Debug.LogError(LogPrefix + error + " Camera rig disabled.", this);
                enabled = false;
                return;
            }

            _telemetry = (IVehicleTelemetry)telemetrySource;
            _targetBody = target.GetComponentInParent<Rigidbody>();
        }

        /// <summary>
        /// Rebinds the rig to a runtime-spawned vehicle (T-8.11 debug selection flow). Smoothing
        /// state is reset so the camera snaps to the new target instead of interpolating from the
        /// previous vehicle's pose.
        /// </summary>
        /// <param name="newTarget">New follow target root.</param>
        /// <param name="newTelemetrySource">MonoBehaviour implementing <see cref="IVehicleTelemetry"/>.</param>
        /// <returns>True when the new wiring is valid.</returns>
        public bool Configure(Transform newTarget, MonoBehaviour newTelemetrySource)
        {
            if (newTarget == null || !(newTelemetrySource is IVehicleTelemetry))
            {
                return false;
            }

            if (_camera == null)
            {
                _camera = GetComponent<Camera>();
            }

            target = newTarget;
            telemetrySource = newTelemetrySource;
            _telemetry = (IVehicleTelemetry)newTelemetrySource;
            _targetBody = newTarget.GetComponentInParent<Rigidbody>();
            _initialized = false;
            _currentDistance = 0f;
            CurrentDistance = 0f;
            _pivotVelocity = Vector3.zero;
            _positionVelocity = Vector3.zero;
            _rotationVelocity = Vector3.zero;
            _distanceVelocity = 0f;
            _smoothedRotation = Quaternion.identity;
            enabled = true;
            return true;
        }

        /// <summary>
        /// Validates the serialized rig wiring without entering play mode. Used by <see cref="Awake"/>
        /// and by tests; returns the first problem found.
        /// </summary>
        public bool TryValidateRig(out string error)
        {
            if (GetComponent<Camera>() == null)
            {
                error = "No Camera component on this object.";
                return false;
            }

            if (profile == null)
            {
                error = "No CameraProfile assigned.";
                return false;
            }

            if (!profile.TryValidate(out var profileError))
            {
                error = "Assigned profile is invalid: " + profileError;
                return false;
            }

            if (target == null)
            {
                error = "No follow target assigned.";
                return false;
            }

            if (telemetrySource == null)
            {
                error = "No telemetry source assigned; it must implement IVehicleTelemetry.";
                return false;
            }

            if (!(telemetrySource is IVehicleTelemetry))
            {
                error = "Assigned telemetry source does not implement IVehicleTelemetry.";
                return false;
            }

            error = null;
            return true;
        }

        private void LateUpdate()
        {
            if (_telemetry == null || profile == null || target == null || _camera == null)
            {
                return;
            }

            var forward = target.forward;
            if (forward.sqrMagnitude < MinimumSqrDistance)
            {
                forward = Vector3.forward;
            }

            var telemetry = _telemetry.Telemetry;
            var lookAhead = CameraFollowMath.LookAheadOffset(telemetry.Speed, profile.LookAheadPerSpeed);
            var maxLookAhead = Mathf.Max(0f, profile.FollowDistance - profile.MinimumDistance);
            if (lookAhead > maxLookAhead)
            {
                lookAhead = maxLookAhead;
            }

            // Pivot is the look target; the camera sits behind it by the profile follow distance.
            var pivot = target.position + (Vector3.up * profile.FollowHeight) + (forward * lookAhead);
            var desiredPosition = pivot - (forward * profile.FollowDistance);

            if (!_initialized)
            {
                _initialized = true;
                _smoothedPivot = pivot;
                _smoothedPosition = desiredPosition;
                _smoothedRotation = LookRotation(desiredPosition, pivot);
                _currentDistance = profile.FollowDistance;
                CurrentDistance = profile.FollowDistance;
                transform.SetPositionAndRotation(desiredPosition, _smoothedRotation);
                ApplyFieldOfView(telemetry.Speed);
                return;
            }

            var deltaTime = Time.deltaTime;
            _smoothedPivot = Vector3.SmoothDamp(
                _smoothedPivot,
                pivot,
                ref _pivotVelocity,
                profile.PositionSmoothing,
                Mathf.Infinity,
                deltaTime);

            _smoothedPosition = Vector3.SmoothDamp(
                _smoothedPosition,
                desiredPosition,
                ref _positionVelocity,
                profile.PositionSmoothing,
                Mathf.Infinity,
                deltaTime);

            var offset = _smoothedPosition - _smoothedPivot;
            var desiredDistance = offset.magnitude;
            var direction = desiredDistance > MinimumSqrDistance ? offset / desiredDistance : -forward;
            if (desiredDistance <= MinimumSqrDistance)
            {
                desiredDistance = 0f;
            }

            var hitDistance = CastObstruction(_smoothedPivot, direction, desiredDistance);
            var allowedDistance = CameraFollowMath.CollisionLimitedDistance(
                desiredDistance,
                hitDistance,
                profile.CollisionPadding,
                profile.MinimumDistance);

            if (allowedDistance < _currentDistance)
            {
                // Push in immediately so the camera never clips; restore is smoothed below.
                _currentDistance = allowedDistance;
                _distanceVelocity = 0f;
            }
            else if (allowedDistance > _currentDistance)
            {
                _currentDistance = Mathf.SmoothDamp(
                    _currentDistance,
                    allowedDistance,
                    ref _distanceVelocity,
                    profile.PositionSmoothing,
                    Mathf.Infinity,
                    deltaTime);
            }

            var finalDistance = Mathf.Min(desiredDistance, _currentDistance);
            var finalPosition = _smoothedPivot + (direction * finalDistance);
            var desiredRotation = LookRotation(finalPosition, _smoothedPivot);

            _smoothedRotation = CameraFollowMath.CriticallyDampedRotation(
                _smoothedRotation,
                desiredRotation,
                ref _rotationVelocity,
                profile.RotationSmoothing,
                deltaTime);

            transform.SetPositionAndRotation(finalPosition, _smoothedRotation);
            CurrentDistance = finalDistance;
            ApplyFieldOfView(telemetry.Speed);
        }

        private void ApplyFieldOfView(float speed)
        {
            _camera.fieldOfView = CameraFollowMath.FieldOfView(
                speed,
                profile.FieldOfViewAtRest,
                profile.FieldOfViewAtTopSpeed,
                profile.FieldOfViewSpeedReference);
        }

        /// <summary>
        /// Returns the nearest obstruction distance, or positive infinity when the path is clear.
        /// Uses a preallocated hit buffer, so no managed allocation occurs per frame.
        /// </summary>
        private float CastObstruction(Vector3 origin, Vector3 direction, float distance)
        {
            if (distance <= 0f || profile.CollisionRadius <= 0f)
            {
                return float.PositiveInfinity;
            }

            var count = Physics.SphereCastNonAlloc(
                origin,
                profile.CollisionRadius,
                direction,
                _hits,
                distance,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            var nearest = float.PositiveInfinity;
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];
                var collider = hit.collider;
                if (collider == null)
                {
                    continue;
                }

                // The vehicle must never obstruct its own chase camera.
                if (collider.transform.IsChildOf(target))
                {
                    continue;
                }

                if (_targetBody != null && hit.rigidbody == _targetBody)
                {
                    continue;
                }

                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                }
            }

            return nearest;
        }

        private static Quaternion LookRotation(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            if (direction.sqrMagnitude < MinimumSqrDistance)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
