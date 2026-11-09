using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Temporary tuning data for the first WheelCollider prototype (TDD §36.7; D-035 template).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value here is an <b>engineering placeholder</b> for the T5.2.2 four-wheel rig. None of
    /// it is final, tuned, or approved: the Simcade handling model is OQ-02, per-vehicle stats are
    /// OQ-06/OQ-14, and the WheelCollider direction stays provisional until the D-023 driving-feel
    /// gate and WebGL profiling (D-033). Do not cite these numbers as design targets.
    /// </para>
    /// <para>
    /// The asset is a read-only definition template (D-035): runtime code must never mutate it.
    /// <see cref="TryValidate"/> is the single validation seam used by the controller and tests,
    /// so invalid authoring fails loudly instead of producing silently broken physics.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Vehicle Physics Profile", fileName = "VehiclePhysicsProfile")]
    public sealed class VehiclePhysicsProfile : ScriptableObject
    {
        private const string LogPrefix = "[VelocityRush.VehicleProfile] ";

        [Header("Rigidbody (placeholder — not final tuning)")]
        [SerializeField, Min(1f)] private float mass = 1200f;
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.25f, 0.05f);
        [SerializeField, Min(0f)] private float linearDamping = 0.02f;
        [SerializeField, Min(0f)] private float angularDamping = 0.05f;

        [Header("Wheels and suspension (placeholder — not final tuning)")]
        [SerializeField, Min(0.01f)] private float wheelRadius = 0.35f;
        [SerializeField, Min(0.01f)] private float wheelMass = 20f;
        [SerializeField, Min(0.01f)] private float suspensionDistance = 0.2f;
        [SerializeField, Min(0f)] private float suspensionSpring = 35000f;
        [SerializeField, Min(0f)] private float suspensionDamper = 4500f;
        [SerializeField, Range(0f, 1f)] private float suspensionTargetPosition = 0.5f;
        [SerializeField, Min(0f)] private float forwardFrictionStiffness = 1.5f;
        [SerializeField, Min(0f)] private float sidewaysFrictionStiffness = 1.5f;

        [Header("Drive (placeholder — not final tuning)")]
        [SerializeField, Min(0f)] private float motorTorque = 1000f;
        [SerializeField, Min(0f)] private float brakeTorque = 2500f;
        [SerializeField, Min(0f)] private float handbrakeTorque = 4000f;
        [SerializeField, Range(0f, 90f)] private float steeringAngle = 30f;

        [Header("Response shaping (placeholder — not final tuning)")]
        [Tooltip("Share of pedal brake torque sent to the front axle; the rear receives the remainder.")]
        [SerializeField, Range(0f, 1f)] private float brakeBiasFront = 0.5f;
        [Tooltip("Motor torque multiplier over absolute forward speed in m/s. Empty curves are invalid.")]
        [SerializeField] private AnimationCurve engineTorqueCurve = AnimationCurve.Linear(0f, 1f, 60f, 0.55f);
        [Tooltip("Steering authority multiplier over absolute forward speed in m/s. Values are 0..1.")]
        [SerializeField] private AnimationCurve steeringSpeedSensitivity = AnimationCurve.Linear(0f, 1f, 60f, 0.45f);

        /// <summary>Chassis mass in kilograms. Placeholder.</summary>
        public float Mass => mass;

        /// <summary>Provisional center-of-mass offset in chassis-local space (stability bias).</summary>
        public Vector3 CenterOfMassOffset => centerOfMassOffset;

        /// <summary>Rigidbody linear damping. Placeholder.</summary>
        public float LinearDamping => linearDamping;

        /// <summary>Rigidbody angular damping. Placeholder.</summary>
        public float AngularDamping => angularDamping;

        /// <summary>Wheel radius in meters. Placeholder.</summary>
        public float WheelRadius => wheelRadius;

        /// <summary>Per-wheel mass used by the WheelCollider suspension. Placeholder.</summary>
        public float WheelMass => wheelMass;

        /// <summary>Maximum suspension travel in meters. Placeholder.</summary>
        public float SuspensionDistance => suspensionDistance;

        /// <summary>Suspension spring rate (N/m). Placeholder.</summary>
        public float SuspensionSpring => suspensionSpring;

        /// <summary>Suspension damper rate (N·s/m). Placeholder.</summary>
        public float SuspensionDamper => suspensionDamper;

        /// <summary>Suspension rest target in 0..1 (0 = extended, 1 = compressed). Placeholder.</summary>
        public float SuspensionTargetPosition => suspensionTargetPosition;

        /// <summary>Forward friction stiffness multiplier. Placeholder.</summary>
        public float ForwardFrictionStiffness => forwardFrictionStiffness;

        /// <summary>Sideways friction stiffness multiplier. Placeholder.</summary>
        public float SidewaysFrictionStiffness => sidewaysFrictionStiffness;

        /// <summary>Motor torque per driven wheel (N·m). Placeholder.</summary>
        public float MotorTorque => motorTorque;

        /// <summary>Brake torque per wheel (N·m). Placeholder.</summary>
        public float BrakeTorque => brakeTorque;

        /// <summary>Handbrake torque per rear wheel (N·m). Placeholder.</summary>
        public float HandbrakeTorque => handbrakeTorque;

        /// <summary>Maximum front-wheel steering angle in degrees. Placeholder.</summary>
        public float SteeringAngle => steeringAngle;

        /// <summary>
        /// Fraction of pedal brake torque sent to the front axle (0..1); the rear axle receives
        /// <c>1 - BrakeBiasFront</c>. Placeholder.
        /// </summary>
        public float BrakeBiasFront => brakeBiasFront;

        /// <summary>
        /// Engine torque multiplier evaluated at absolute forward speed (m/s). Placeholder.
        /// </summary>
        public AnimationCurve EngineTorqueCurve => engineTorqueCurve;

        /// <summary>
        /// Steering authority multiplier evaluated at absolute forward speed (m/s). Placeholder.
        /// </summary>
        public AnimationCurve SteeringSpeedSensitivity => steeringSpeedSensitivity;

        /// <summary>
        /// Checks that every serialized value is finite and inside its physically meaningful range.
        /// Returns false with a field-specific description on the first problem found.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!IsPositive(mass))
            {
                return Fail("Mass", "must be finite and greater than 0", mass, out error);
            }

            if (!IsFinite(centerOfMassOffset.x) || !IsFinite(centerOfMassOffset.y) || !IsFinite(centerOfMassOffset.z))
            {
                error = "VehiclePhysicsProfile: CenterOfMassOffset must contain finite values.";
                return false;
            }

            if (!IsFinite(linearDamping) || linearDamping < 0f)
            {
                return Fail("LinearDamping", "must be finite and at least 0", linearDamping, out error);
            }

            if (!IsFinite(angularDamping) || angularDamping < 0f)
            {
                return Fail("AngularDamping", "must be finite and at least 0", angularDamping, out error);
            }

            if (!IsPositive(wheelRadius))
            {
                return Fail("WheelRadius", "must be finite and greater than 0", wheelRadius, out error);
            }

            if (!IsPositive(wheelMass))
            {
                return Fail("WheelMass", "must be finite and greater than 0", wheelMass, out error);
            }

            if (!IsPositive(suspensionDistance))
            {
                return Fail("SuspensionDistance", "must be finite and greater than 0", suspensionDistance, out error);
            }

            if (!IsPositive(suspensionSpring))
            {
                return Fail("SuspensionSpring", "must be finite and greater than 0", suspensionSpring, out error);
            }

            if (!IsFinite(suspensionDamper) || suspensionDamper < 0f)
            {
                return Fail("SuspensionDamper", "must be finite and at least 0", suspensionDamper, out error);
            }

            if (!IsFinite(suspensionTargetPosition) || suspensionTargetPosition < 0f || suspensionTargetPosition > 1f)
            {
                return Fail("SuspensionTargetPosition", "must be finite and inside 0..1", suspensionTargetPosition, out error);
            }

            if (!IsPositive(forwardFrictionStiffness))
            {
                return Fail("ForwardFrictionStiffness", "must be finite and greater than 0", forwardFrictionStiffness, out error);
            }

            if (!IsPositive(sidewaysFrictionStiffness))
            {
                return Fail("SidewaysFrictionStiffness", "must be finite and greater than 0", sidewaysFrictionStiffness, out error);
            }

            if (!IsFinite(motorTorque) || motorTorque < 0f)
            {
                return Fail("MotorTorque", "must be finite and at least 0", motorTorque, out error);
            }

            if (!IsPositive(brakeTorque))
            {
                return Fail("BrakeTorque", "must be finite and greater than 0", brakeTorque, out error);
            }

            if (!IsFinite(handbrakeTorque) || handbrakeTorque < 0f)
            {
                return Fail("HandbrakeTorque", "must be finite and at least 0", handbrakeTorque, out error);
            }

            if (!IsFinite(steeringAngle) || steeringAngle <= 0f || steeringAngle > 90f)
            {
                return Fail("SteeringAngle", "must be finite and inside (0, 90] degrees", steeringAngle, out error);
            }

            if (!IsFinite(brakeBiasFront) || brakeBiasFront < 0f || brakeBiasFront > 1f)
            {
                return Fail("BrakeBiasFront", "must be finite and inside 0..1", brakeBiasFront, out error);
            }

            if (!TryValidateCurve(engineTorqueCurve, "EngineTorqueCurve", out error))
            {
                return false;
            }

            if (!TryValidateCurve(steeringSpeedSensitivity, "SteeringSpeedSensitivity", out error))
            {
                return false;
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            // Authoring-time guard: surface invalid values in the inspector immediately. The
            // controller still validates at Awake because assets can change after import.
            if (!TryValidate(out var error))
            {
                Debug.LogWarning(LogPrefix + error, this);
            }
        }

        private static bool Fail(string field, string requirement, float value, out string error)
        {
            error = "VehiclePhysicsProfile: " + field + " " + requirement + " (was " + value + ").";
            return false;
        }

        /// <summary>
        /// Validates a speed-mapped multiplier curve: at least two keys, non-decreasing finite
        /// times, and values inside 0..1 so a curve can only attenuate a profile-capped force.
        /// </summary>
        private static bool TryValidateCurve(AnimationCurve curve, string field, out string error)
        {
            if (curve == null || curve.length < 2)
            {
                error = "VehiclePhysicsProfile: " + field + " must contain at least two keys.";
                return false;
            }

            var previousTime = -1f;
            for (var i = 0; i < curve.length; i++)
            {
                // The indexer reads a single key without allocating a Keyframe[] copy.
                var key = curve[i];

                if (!IsFinite(key.time) || key.time < 0f)
                {
                    error = "VehiclePhysicsProfile: " + field + " has a non-finite or negative key time.";
                    return false;
                }

                if (key.time < previousTime)
                {
                    error = "VehiclePhysicsProfile: " + field + " key times must be in non-decreasing order.";
                    return false;
                }

                if (!IsFinite(key.value) || key.value < 0f || key.value > 1f)
                {
                    error = "VehiclePhysicsProfile: " + field + " key values must be finite and inside 0..1.";
                    return false;
                }

                previousTime = key.time;
            }

            error = null;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsPositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }
    }
}
