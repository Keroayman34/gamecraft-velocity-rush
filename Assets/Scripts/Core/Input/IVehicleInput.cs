namespace VelocityRush.Core
{
    /// <summary>
    /// Device-independent driving surface consumed by gameplay systems (TDD §37.1).
    /// Implementations may read any device; consumers must never touch a device API directly.
    /// </summary>
    /// <remarks>
    /// Value ranges are fixed and normalized:
    /// <see cref="Throttle"/> and <see cref="Brake"/> are 0..1 (neutral 0),
    /// <see cref="Steering"/> is -1..1 (neutral 0, negative = left), and
    /// <see cref="Handbrake"/> is a boolean (neutral false).
    /// Response shaping, dead zones, and speed sensitivity belong to the vehicle domain —
    /// the adapter reports the raw normalized sample (TDD §37.4).
    /// </remarks>
    public interface IVehicleInput
    {
        /// <summary>Throttle sample. Range 0..1; neutral is 0.</summary>
        float Throttle { get; }

        /// <summary>Brake sample. Range 0..1; neutral is 0.</summary>
        float Brake { get; }

        /// <summary>Steering sample. Range -1..1; neutral is 0; negative steers left.</summary>
        float Steering { get; }

        /// <summary>Handbrake state. Neutral is <c>false</c>.</summary>
        bool Handbrake { get; }
    }
}
