using System.Diagnostics;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Monotonic elapsed-time source for the prototype race timer (TDD §29 S5).
    /// </summary>
    /// <remarks>
    /// Injected into <see cref="RaceRules"/> so the pure race logic is deterministic in EditMode
    /// tests and never accumulates frame deltas. The runtime implementation uses
    /// <see cref="Stopwatch"/>, which is a monotonic high-resolution clock and does not jump with
    /// wall-clock adjustments.
    /// </remarks>
    public interface IRaceClock
    {
        /// <summary>Monotonic elapsed seconds since this clock was created.</summary>
        double NowSeconds { get; }
    }

    /// <summary>
    /// Runtime <see cref="IRaceClock"/> backed by <see cref="Stopwatch"/>.
    /// </summary>
    public sealed class StopwatchRaceClock : IRaceClock
    {
        private readonly long _originTimestamp;

        public StopwatchRaceClock()
        {
            _originTimestamp = Stopwatch.GetTimestamp();
        }

        /// <inheritdoc />
        public double NowSeconds
        {
            get
            {
                var elapsedTicks = Stopwatch.GetTimestamp() - _originTimestamp;
                return elapsedTicks / (double)Stopwatch.Frequency;
            }
        }
    }
}
