using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure conversion from digital key states to a normalized <see cref="VehicleInputSnapshot"/>.
    /// Contains no Unity API calls, so mapping, range, and reset behavior are EditMode-testable;
    /// the Unity adapter only polls keys and feeds this state.
    /// </summary>
    /// <remarks>
    /// Digital keys map to full-scale samples (pressed = 1). Opposite steering keys pressed
    /// together deterministically cancel to 0. Throttle and brake are reported independently;
    /// blending them is a vehicle-domain decision (TDD §36.6).
    /// </remarks>
    public sealed class KeyboardInputState
    {
        /// <summary>Most recently sampled snapshot; neutral until the first sample.</summary>
        public VehicleInputSnapshot Snapshot { get; private set; } = VehicleInputSnapshot.Neutral;

        /// <summary>True when the current snapshot is fully neutral.</summary>
        public bool IsNeutral => Snapshot.IsNeutral;

        /// <summary>
        /// Converts the current digital key states into the normalized snapshot.
        /// </summary>
        public void Sample(
            bool throttleDown,
            bool brakeDown,
            bool steerLeftDown,
            bool steerRightDown,
            bool handbrakeDown)
        {
            var steering = 0f;
            if (steerLeftDown)
            {
                steering -= 1f;
            }

            if (steerRightDown)
            {
                steering += 1f;
            }

            Snapshot = new VehicleInputSnapshot(
                throttleDown ? VehicleInputSnapshot.MaxThrottle : VehicleInputSnapshot.MinThrottle,
                brakeDown ? VehicleInputSnapshot.MaxThrottle : VehicleInputSnapshot.MinThrottle,
                steering,
                handbrakeDown);
        }

        /// <summary>Returns every channel to its neutral default (stale-input guard).</summary>
        public void Reset()
        {
            Snapshot = VehicleInputSnapshot.Neutral;
        }
    }
}
