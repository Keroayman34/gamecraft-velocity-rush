namespace VelocityRush.Core
{
    /// <summary>
    /// Immutable, allocation-free vehicle state published for polled consumers (TDD §36.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Per-frame telemetry is deliberately pull-based: consumers read the current value when they
    /// need it instead of subscribing to events, which keeps the hot path free of delegate and
    /// closure allocations (TDD §32.3/§36.10). The struct contains only value types, so copying it
    /// never allocates.
    /// </para>
    /// <para>
    /// All lengths use meters and all speeds use meters per second, matching Unity's physics units.
    /// <see cref="ForwardSpeed"/> and <see cref="LateralSpeed"/> are signed in chassis-local axes;
    /// <see cref="Speed"/> and <see cref="WheelSlip"/> are non-negative magnitudes.
    /// </para>
    /// </remarks>
    public readonly struct VehicleTelemetry
    {
        /// <summary>Creates a sanitized telemetry sample.</summary>
        /// <param name="speed">Velocity magnitude in m/s; negative or non-finite maps to 0.</param>
        /// <param name="forwardSpeed">Signed speed along the chassis forward axis in m/s.</param>
        /// <param name="lateralSpeed">Signed speed along the chassis right axis in m/s.</param>
        /// <param name="wheelSlip">Largest absolute wheel slip; negative or non-finite maps to 0.</param>
        /// <param name="isGrounded">True when at least one wheel currently has ground contact.</param>
        public VehicleTelemetry(float speed, float forwardSpeed, float lateralSpeed, float wheelSlip, bool isGrounded)
        {
            Speed = SanitizeNonNegative(speed);
            ForwardSpeed = SanitizeFinite(forwardSpeed);
            LateralSpeed = SanitizeFinite(lateralSpeed);
            WheelSlip = SanitizeNonNegative(wheelSlip);
            IsGrounded = isGrounded;
        }

        /// <summary>Velocity magnitude in m/s.</summary>
        public float Speed { get; }

        /// <summary>Signed speed along the chassis forward axis in m/s (negative = reversing).</summary>
        public float ForwardSpeed { get; }

        /// <summary>Signed speed along the chassis right axis in m/s (used for drift detection).</summary>
        public float LateralSpeed { get; }

        /// <summary>Largest absolute wheel slip across grounded wheels (0 = full grip).</summary>
        public float WheelSlip { get; }

        /// <summary>True when at least one wheel has ground contact.</summary>
        public bool IsGrounded { get; }

        private static float SanitizeNonNegative(float value)
        {
            return IsFinite(value) && value > 0f ? value : 0f;
        }

        private static float SanitizeFinite(float value)
        {
            return IsFinite(value) ? value : 0f;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
