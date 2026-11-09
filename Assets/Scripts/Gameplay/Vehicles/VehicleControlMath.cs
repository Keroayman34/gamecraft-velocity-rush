namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure, allocation-free mapping from normalized <see cref="VelocityRush.Core.IVehicleInput"/>
    /// values to WheelCollider drive quantities (TDD §36.6/§36.9).
    /// </summary>
    /// <remarks>
    /// Deliberately free of Unity APIs so the mapping is EditMode-testable and the controller stays
    /// a thin application layer. Speed-dependent response is passed in as an already-evaluated
    /// 0..1 multiplier from the profile curves, so this layer stays deterministic and the curves
    /// remain authoring data. Stability assists are not implemented here: they are a Phase-6
    /// tuning surface (OQ-02), not a prototype decision.
    /// </remarks>
    public static class VehicleControlMath
    {
        /// <summary>Number of driven axles a per-wheel brake cap is distributed across.</summary>
        private const float AxlesPerVehicle = 2f;

        /// <summary>Clamps a normalized sample to 0..1; NaN maps to neutral (0).</summary>
        public static float Clamp01(float value)
        {
            if (float.IsNaN(value))
            {
                return 0f;
            }

            if (value <= 0f)
            {
                return 0f;
            }

            return value >= 1f ? 1f : value;
        }

        /// <summary>Clamps a signed sample to -1..1; NaN maps to neutral (0).</summary>
        public static float ClampSigned(float value)
        {
            if (float.IsNaN(value))
            {
                return 0f;
            }

            if (value <= -1f)
            {
                return -1f;
            }

            return value >= 1f ? 1f : value;
        }

        /// <summary>Converts a 0..1 throttle sample into motor torque for one driven wheel.</summary>
        public static float MotorTorque(float throttle, float maxMotorTorque)
        {
            return Clamp01(throttle) * NonNegative(maxMotorTorque);
        }

        /// <summary>
        /// Converts a throttle sample into motor torque scaled by an already-evaluated engine
        /// multiplier. The multiplier is clamped to 0..1 so the profile torque cap always holds.
        /// </summary>
        public static float MotorTorque(float throttle, float maxMotorTorque, float torqueMultiplier)
        {
            return MotorTorque(throttle, maxMotorTorque) * Clamp01(torqueMultiplier);
        }

        /// <summary>Converts a 0..1 brake sample into brake torque for one wheel.</summary>
        public static float BrakeTorque(float brake, float maxBrakeTorque)
        {
            return Clamp01(brake) * NonNegative(maxBrakeTorque);
        }

        /// <summary>Converts the handbrake state into brake torque for one rear wheel.</summary>
        public static float HandbrakeTorque(bool handbrake, float maxHandbrakeTorque)
        {
            return handbrake ? NonNegative(maxHandbrakeTorque) : 0f;
        }

        /// <summary>Converts a -1..1 steering sample into a front-wheel angle in degrees.</summary>
        public static float SteeringAngle(float steering, float maxSteeringAngle)
        {
            return ClampSigned(steering) * NonNegative(maxSteeringAngle);
        }

        /// <summary>
        /// Converts a steering sample into a wheel angle scaled by an already-evaluated speed
        /// sensitivity multiplier. The multiplier is clamped to 0..1 so steering never exceeds the
        /// profile's maximum angle.
        /// </summary>
        public static float SteeringAngle(float steering, float maxSteeringAngle, float sensitivityMultiplier)
        {
            return SteeringAngle(steering, maxSteeringAngle) * Clamp01(sensitivityMultiplier);
        }

        /// <summary>
        /// Distributes pedal brake torque to one axle. <paramref name="axleShare"/> is the profile
        /// bias for that axle; 0.5 reproduces the per-wheel cap and the two axle shares must sum to
        /// 1, so front + rear torque stays constant for any bias.
        /// </summary>
        public static float BrakeTorqueForAxle(float brake, float maxBrakeTorquePerWheel, float axleShare)
        {
            return BrakeTorque(brake, maxBrakeTorquePerWheel) * (Clamp01(axleShare) * AxlesPerVehicle);
        }

        /// <summary>
        /// Combines pedal braking with the handbrake on a rear wheel. The stronger request wins so
        /// the two inputs cannot stack into an unintended torque spike.
        /// </summary>
        public static float RearBrakeTorque(float pedalBrakeTorque, float handbrakeTorque)
        {
            return pedalBrakeTorque > handbrakeTorque ? pedalBrakeTorque : handbrakeTorque;
        }

        private static float NonNegative(float value)
        {
            if (float.IsNaN(value) || value <= 0f)
            {
                return 0f;
            }

            return value;
        }
    }
}
