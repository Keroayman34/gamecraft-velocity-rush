namespace VelocityRush.Core
{
    /// <summary>
    /// Read-only, polled source of vehicle state for external systems (TDD §§36.2/36.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Camera, HUD, audio, and race rules consume this interface instead of reaching into the
    /// physics controller or reading a <c>Rigidbody</c> directly. The implementation is expected to
    /// refresh its snapshot during the physics step; consumers may poll at any time and receive the
    /// most recent sample.
    /// </para>
    /// <para>
    /// Deliberately event-free: per-frame state must not allocate delegates or closures, and a
    /// polling consumer cannot accumulate stale subscriptions (TDD §36.10).
    /// </para>
    /// </remarks>
    public interface IVehicleTelemetry
    {
        /// <summary>The most recent telemetry sample for this vehicle.</summary>
        VehicleTelemetry Telemetry { get; }
    }
}
