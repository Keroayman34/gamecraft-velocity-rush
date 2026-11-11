using System;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure projection from race/vehicle state to HUD values. Separated from the view so the
    /// display rules are unit-testable without UI Toolkit or a running race (TDD §42).
    /// </summary>
    public static class SliceHudModel
    {
        /// <summary>Largest countdown value the single-digit HUD cell can display.</summary>
        public const int MaxCountdownSeconds = 99;

        /// <summary>
        /// Builds a clamped, render-ready snapshot. Counts are clamped to non-negative values;
        /// speed and time are sanitised by <see cref="SliceHudFormat"/>.
        /// </summary>
        /// <param name="telemetry">Latest vehicle telemetry sample.</param>
        /// <param name="state">Current race lifecycle state.</param>
        /// <param name="elapsedSeconds">Elapsed race time in seconds.</param>
        /// <param name="acceptedCheckpoints">Accepted checkpoint count.</param>
        /// <param name="requiredCheckpoints">Required checkpoint count.</param>
        /// <param name="countdownRemainingSeconds">Remaining countdown seconds (zero when running).</param>
        /// <param name="isPaused">True while the attempt is paused.</param>
        public static SliceHudSnapshot Evaluate(
            in VehicleTelemetry telemetry,
            RaceState state,
            double elapsedSeconds,
            int acceptedCheckpoints,
            int requiredCheckpoints,
            double countdownRemainingSeconds = 0d,
            bool isPaused = false)
        {
            SliceHudFormat.SplitRaceTime(elapsedSeconds, out var wholeSeconds, out var hundredths);

            return new SliceHudSnapshot(
                SliceHudFormat.SpeedToKph(telemetry.Speed),
                wholeSeconds,
                hundredths,
                Math.Max(0, acceptedCheckpoints),
                Math.Max(0, requiredCheckpoints),
                state,
                CountdownSeconds(state, countdownRemainingSeconds),
                isPaused);
        }

        /// <summary>
        /// Converts a remaining countdown duration into a whole-second display value. Non-finite,
        /// negative, and post-countdown values map to zero; the result is capped at
        /// <see cref="MaxCountdownSeconds"/>.
        /// </summary>
        public static int CountdownSeconds(RaceState state, double remainingSeconds)
        {
            if (state != RaceState.Countdown ||
                double.IsNaN(remainingSeconds) ||
                double.IsInfinity(remainingSeconds) ||
                remainingSeconds <= 0d)
            {
                return 0;
            }

            var ceil = (int)Math.Ceiling(remainingSeconds);
            return ceil > MaxCountdownSeconds ? MaxCountdownSeconds : ceil;
        }
    }
}
