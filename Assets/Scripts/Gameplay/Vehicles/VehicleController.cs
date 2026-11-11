using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// First drivable four-wheel prototype: consumes <see cref="IVehicleInput"/>, applies
    /// WheelCollider motor/brake/steer in <c>FixedUpdate</c>, publishes polled
    /// <see cref="IVehicleTelemetry"/>, and copies the physics wheel pose onto the visual wheels
    /// (TDD §§36.2/36.6/36.9/36.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Prototype scope only (D-033 provisional): rear-wheel drive, no assists, no reverse gear, and
    /// no per-surface friction. Engine torque falloff, brake bias, and steering speed sensitivity
    /// are profile-driven response shaping; their curves and values remain placeholder tuning
    /// surfaces for the D-023 playtest (OQ-02).
    /// </para>
    /// <para>
    /// The controller never polls a device API; it reads the assigned <see cref="IVehicleInput"/>
    /// (the scene's keyboard adapter in this prototype). Missing or invalid wiring disables the
    /// component with an explicit error rather than failing silently in <c>FixedUpdate</c>.
    /// </para>
    /// <para>
    /// The controller is also the telemetry source because it already owns the authoritative
    /// physics state; exposing a separate provider would duplicate wheel queries. External systems
    /// depend only on <see cref="IVehicleTelemetry"/>, never on this concrete type.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VehicleController : MonoBehaviour, IVehicleTelemetry, IVehicleWheelTelemetry
    {
        private const string LogPrefix = "[VelocityRush.Vehicle] ";

        [SerializeField] private VehiclePhysicsProfile profile;
        [SerializeField] private Rigidbody chassis;
        [SerializeField] private MonoBehaviour inputSource;

        [Header("Wheel bindings (physics + visual pairs)")]
        [SerializeField] private WheelBinding frontLeft = new WheelBinding();
        [SerializeField] private WheelBinding frontRight = new WheelBinding();
        [SerializeField] private WheelBinding rearLeft = new WheelBinding();
        [SerializeField] private WheelBinding rearRight = new WheelBinding();

        private readonly WheelBinding[] _wheels = new WheelBinding[4];
        private readonly WheelTelemetry[] _wheelTelemetry = new WheelTelemetry[4];
        private IVehicleInput _input;

        /// <summary>Referenced handling profile (sole source of physics values).</summary>
        public VehiclePhysicsProfile Profile => profile;

        /// <summary>The bound driving input source, or null before runtime binding.</summary>
        public MonoBehaviour InputSource => inputSource;

        /// <inheritdoc />
        public VehicleTelemetry Telemetry { get; private set; }

        /// <inheritdoc />
        public int WheelCount => _wheelTelemetry.Length;

        /// <inheritdoc />
        public WheelTelemetry GetWheel(int index)
        {
            if ((uint)index >= (uint)_wheelTelemetry.Length)
            {
                return default;
            }

            return _wheelTelemetry[index];
        }

        private void Awake()
        {
            _wheels[0] = frontLeft;
            _wheels[1] = frontRight;
            _wheels[2] = rearLeft;
            _wheels[3] = rearRight;

            if (inputSource == null)
            {
                Debug.LogError(LogPrefix + "No input source assigned; assign a MonoBehaviour implementing IVehicleInput. Vehicle controller disabled.", this);
                enabled = false;
                return;
            }

            _input = inputSource as IVehicleInput;
            if (_input == null)
            {
                Debug.LogError(LogPrefix + "Assigned input source does not implement IVehicleInput. Vehicle controller disabled.", this);
                enabled = false;
                return;
            }

            if (!TryValidateRig(out var error))
            {
                Debug.LogError(LogPrefix + error + " Vehicle controller disabled.", this);
                enabled = false;
                return;
            }

            ConfigureRig();
        }

        /// <summary>
        /// Validates the serialized vehicle wiring without requiring play mode. Used by <see cref="Awake"/>
        /// and by EditMode prefab tests; returns the first problem found.
        /// </summary>
        public bool TryValidateRig(out string error)
        {
            if (profile == null)
            {
                error = "No VehiclePhysicsProfile assigned.";
                return false;
            }

            if (!profile.TryValidate(out var profileError))
            {
                error = "Assigned profile is invalid: " + profileError;
                return false;
            }

            if (chassis == null)
            {
                error = "No chassis Rigidbody assigned.";
                return false;
            }

            if (!ValidateWheel("FrontLeft", frontLeft, out error)
                || !ValidateWheel("FrontRight", frontRight, out error)
                || !ValidateWheel("RearLeft", rearLeft, out error)
                || !ValidateWheel("RearRight", rearRight, out error))
            {
                return false;
            }

            if (frontLeft.Collider == frontRight.Collider
                || frontLeft.Collider == rearLeft.Collider
                || frontLeft.Collider == rearRight.Collider
                || frontRight.Collider == rearLeft.Collider
                || frontRight.Collider == rearRight.Collider
                || rearLeft.Collider == rearRight.Collider)
            {
                error = "Wheel bindings must reference four distinct WheelColliders.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Validates and applies a different handling profile to an existing rig. Used by the
        /// Phase-8 balance harness and by future vehicle-selection flows; the profile remains the
        /// only source of handling values (TDD §36.7).
        /// </summary>
        /// <param name="newProfile">Validated profile to apply.</param>
        /// <param name="error">Diagnostic when the profile is null or invalid.</param>
        /// <returns>True when the profile was applied and the rig reconfigured.</returns>
        public bool TryApplyProfile(VehiclePhysicsProfile newProfile, out string error)
        {
            if (newProfile == null)
            {
                error = "Cannot apply a null VehiclePhysicsProfile.";
                return false;
            }

            if (!newProfile.TryValidate(out var profileError))
            {
                error = "Cannot apply an invalid VehiclePhysicsProfile: " + profileError;
                return false;
            }

            profile = newProfile;
            error = null;

            if (isActiveAndEnabled && chassis != null)
            {
                ConfigureRig();
            }

            return true;
        }

        /// <summary>
        /// Replaces the driving input source at runtime after validating that it implements
        /// <see cref="IVehicleInput"/>. The balance harness uses this to drive one rig with a
        /// scripted input without changing the scene's keyboard adapter.
        /// </summary>
        /// <param name="source">MonoBehaviour implementing <see cref="IVehicleInput"/>.</param>
        /// <param name="error">Diagnostic when the source is missing or incompatible.</param>
        /// <returns>True when the input source was bound.</returns>
        public bool TrySetInputSource(MonoBehaviour source, out string error)
        {
            if (source == null)
            {
                error = "Cannot bind a null input source.";
                return false;
            }

            var candidate = source as IVehicleInput;
            if (candidate == null)
            {
                error = "Assigned input source does not implement IVehicleInput.";
                return false;
            }

            inputSource = source;
            _input = candidate;
            error = null;
            return true;
        }

        /// <summary>
        /// Teleports the chassis to a known pose and clears motion and drive state for an in-scene
        /// prototype retry (T5.4; TDD §35.5). Does not reload the scene or rebuild the rig.
        /// </summary>
        /// <param name="position">World position for the chassis Rigidbody.</param>
        /// <param name="rotation">World rotation for the chassis Rigidbody.</param>
        public void ResetVehicleState(Vector3 position, Quaternion rotation)
        {
            if (chassis == null)
            {
                return;
            }

            chassis.linearVelocity = Vector3.zero;
            chassis.angularVelocity = Vector3.zero;
            chassis.position = position;
            chassis.rotation = rotation;

            for (var i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i];
                if (wheel == null || wheel.Collider == null)
                {
                    continue;
                }

                wheel.Collider.motorTorque = 0f;
                wheel.Collider.brakeTorque = 0f;
                wheel.Collider.steerAngle = 0f;
            }

            // Make the teleport visible to trigger queries in the same frame as the reset.
            Physics.SyncTransforms();
            Telemetry = default;
            for (var i = 0; i < _wheelTelemetry.Length; i++)
            {
                _wheelTelemetry[i] = default;
            }
        }

        private bool ValidateWheel(string label, WheelBinding binding, out string error)
        {
            if (binding == null || binding.Collider == null)
            {
                error = label + " wheel has no WheelCollider assigned.";
                return false;
            }

            if (binding.Visual == null)
            {
                error = label + " wheel has no visual Transform assigned.";
                return false;
            }

            if (binding.Visual == binding.Collider.transform)
            {
                error = label + " visual Transform must be separate from the WheelCollider transform.";
                return false;
            }

            var wheelTransform = binding.Collider.transform;
            if (wheelTransform != chassis.transform && !wheelTransform.IsChildOf(chassis.transform))
            {
                error = label + " WheelCollider must live under the chassis Rigidbody hierarchy.";
                return false;
            }

            error = null;
            return true;
        }

        private void ConfigureRig()
        {
            chassis.mass = profile.Mass;
            chassis.centerOfMass = profile.CenterOfMassOffset;
            chassis.interpolation = RigidbodyInterpolation.Interpolate;
            chassis.collisionDetectionMode = CollisionDetectionMode.Discrete;
            chassis.linearDamping = profile.LinearDamping;
            chassis.angularDamping = profile.AngularDamping;
            // A driven vehicle must never sleep (TDD §36.5); 0 disables the sleep threshold.
            chassis.sleepThreshold = 0f;

            for (var i = 0; i < _wheels.Length; i++)
            {
                ConfigureWheel(_wheels[i].Collider);
            }
        }

        private void ConfigureWheel(WheelCollider wheel)
        {
            wheel.mass = profile.WheelMass;
            wheel.radius = profile.WheelRadius;
            wheel.suspensionDistance = profile.SuspensionDistance;

            var spring = wheel.suspensionSpring;
            spring.spring = profile.SuspensionSpring;
            spring.damper = profile.SuspensionDamper;
            spring.targetPosition = profile.SuspensionTargetPosition;
            wheel.suspensionSpring = spring;

            var forward = wheel.forwardFriction;
            forward.stiffness = profile.ForwardFrictionStiffness;
            wheel.forwardFriction = forward;

            var sideways = wheel.sidewaysFriction;
            sideways.stiffness = profile.SidewaysFrictionStiffness;
            wheel.sidewaysFriction = sideways;
        }

        private void FixedUpdate()
        {
            if (_input == null)
            {
                return;
            }

            var velocity = chassis.linearVelocity;
            var speed = velocity.magnitude;
            var forwardSpeed = Vector3.Dot(velocity, chassis.transform.forward);
            var speedForCurves = Mathf.Abs(forwardSpeed);

            // Publish state once per physics step; consumers poll this snapshot in LateUpdate or
            // the next frame. No events and no managed allocation (TDD §36.10).
            SampleTelemetry(velocity, speed, forwardSpeed);

            var torqueMultiplier = profile.EngineTorqueCurve.Evaluate(speedForCurves);
            var motorTorque = VehicleControlMath.MotorTorque(_input.Throttle, profile.MotorTorque, torqueMultiplier);

            var frontBrakeTorque = VehicleControlMath.BrakeTorqueForAxle(
                _input.Brake,
                profile.BrakeTorque,
                profile.BrakeBiasFront);
            var rearPedalBrakeTorque = VehicleControlMath.BrakeTorqueForAxle(
                _input.Brake,
                profile.BrakeTorque,
                1f - profile.BrakeBiasFront);
            var handbrakeTorque = VehicleControlMath.HandbrakeTorque(_input.Handbrake, profile.HandbrakeTorque);
            var rearBrakeTorque = VehicleControlMath.RearBrakeTorque(rearPedalBrakeTorque, handbrakeTorque);

            var steeringMultiplier = profile.SteeringSpeedSensitivity.Evaluate(speedForCurves);
            var steerAngle = VehicleControlMath.SteeringAngle(
                _input.Steering,
                profile.SteeringAngle,
                steeringMultiplier);

            // Provisional rear-wheel drive; drivetrain layout is an OQ-02 tuning surface, not decided here.
            frontLeft.Collider.motorTorque = 0f;
            frontRight.Collider.motorTorque = 0f;
            rearLeft.Collider.motorTorque = motorTorque;
            rearRight.Collider.motorTorque = motorTorque;

            frontLeft.Collider.brakeTorque = frontBrakeTorque;
            frontRight.Collider.brakeTorque = frontBrakeTorque;
            rearLeft.Collider.brakeTorque = rearBrakeTorque;
            rearRight.Collider.brakeTorque = rearBrakeTorque;

            frontLeft.Collider.steerAngle = steerAngle;
            frontRight.Collider.steerAngle = steerAngle;
            rearLeft.Collider.steerAngle = 0f;
            rearRight.Collider.steerAngle = 0f;
        }

        /// <summary>
        /// Fills the aggregate and per-wheel snapshots from the current Rigidbody velocity and
        /// grounded wheel hits. Uses preallocated bindings and struct-only wheel queries, so it
        /// does not allocate (TDD §36.10/§36.12).
        /// </summary>
        private void SampleTelemetry(Vector3 velocity, float speed, float forwardSpeed)
        {
            var lateralSpeed = Vector3.Dot(velocity, chassis.transform.right);
            var isGrounded = false;
            var maxSlip = 0f;

            for (var i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i].Collider;
                if (wheel == null)
                {
                    _wheelTelemetry[i] = default;
                    continue;
                }

                var grounded = wheel.isGrounded;
                var slip = 0f;
                var compression = 0f;

                if (grounded && wheel.GetGroundHit(out var hit))
                {
                    var sidewaysSlip = Mathf.Abs(hit.sidewaysSlip);
                    var longitudinalSlip = Mathf.Abs(hit.forwardSlip);
                    slip = sidewaysSlip > longitudinalSlip ? sidewaysSlip : longitudinalSlip;

                    // The hit point is the tire contact patch; subtracting the radius leaves
                    // remaining suspension travel, which maps to a 0..1 compression value.
                    var downDistance = Vector3.Dot(hit.point - wheel.transform.position, -wheel.transform.up);
                    var travel = downDistance - wheel.radius;
                    var suspensionDistance = Mathf.Max(0.0001f, wheel.suspensionDistance);
                    compression = 1f - Mathf.Clamp01(travel / suspensionDistance);
                }

                if (grounded)
                {
                    isGrounded = true;
                }

                if (slip > maxSlip)
                {
                    maxSlip = slip;
                }

                _wheelTelemetry[i] = new WheelTelemetry(slip, grounded, compression);
            }

            Telemetry = new VehicleTelemetry(speed, forwardSpeed, lateralSpeed, maxSlip, isGrounded);
        }

        private void Update()
        {
            // Visual pose sync only: no physics writes here and no per-frame allocation.
            for (var i = 0; i < _wheels.Length; i++)
            {
                var wheel = _wheels[i];
                wheel.Collider.GetWorldPose(out var position, out var rotation);
                wheel.Visual.SetPositionAndRotation(position, rotation);
            }
        }
    }
}
