namespace VelocityRush.Core
{
    /// <summary>
    /// Optional, additive read-only per-wheel telemetry surface (TDD §36.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Consumers that need per-wheel slip, contact, or suspension bars can probe for this
    /// interface. Keeping it separate from <see cref="IVehicleTelemetry"/> avoids changing the
    /// established aggregate contract and its tests.
    /// </para>
    /// <para>
    /// Implementations must be polled, event-free, and allocation-free on the physics path.
    /// Out-of-range indices return <c>default</c> rather than throwing on the hot path.
    /// </para>
    /// </remarks>
    public interface IVehicleWheelTelemetry
    {
        /// <summary>Number of wheels currently exposed by this source.</summary>
        int WheelCount { get; }

        /// <summary>Returns the most recent sample for a wheel, or <c>default</c> when out of range.</summary>
        WheelTelemetry GetWheel(int index);
    }
}
