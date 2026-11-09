using System;
using System.Globalization;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Immutable outcome of one completed prototype race attempt (TDD §29 S5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only snapshot: Race Rules creates it once at the finish transition and never mutates
    /// it. It deliberately contains only prototype facts (elapsed time, checkpoint progress,
    /// completion) and no rewards, scoring, progression, or save data.
    /// </para>
    /// <para>
    /// The commercial result schema is expected to add a session-unique race-instance id for the
    /// exactly-once progression handoff described in TDD §30.5; that id is intentionally absent
    /// from this prototype slice because no progression handoff exists yet.
    /// </para>
    /// </remarks>
    public readonly struct RaceResult
    {
        /// <summary>Elapsed race time in seconds, excluding countdown time.</summary>
        public double ElapsedSeconds { get; }

        /// <summary>Checkpoint count accepted before the finish trigger.</summary>
        public int AcceptedCheckpointCount { get; }

        /// <summary>Checkpoint count required to complete the run.</summary>
        public int RequiredCheckpointCount { get; }

        /// <summary>True when the attempt reached a valid finish.</summary>
        public bool Completed { get; }

        /// <param name="elapsedSeconds">Non-negative elapsed race time in seconds.</param>
        /// <param name="acceptedCheckpointCount">Accepted checkpoints at the finish transition.</param>
        /// <param name="requiredCheckpointCount">Checkpoints required by this race.</param>
        /// <param name="completed">Whether the attempt completed.</param>
        public RaceResult(
            double elapsedSeconds,
            int acceptedCheckpointCount,
            int requiredCheckpointCount,
            bool completed)
        {
            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(elapsedSeconds), "Elapsed time must be finite and non-negative.");
            }

            if (requiredCheckpointCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredCheckpointCount), "Required checkpoint count must be non-negative.");
            }

            if (acceptedCheckpointCount < 0 || acceptedCheckpointCount > requiredCheckpointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(acceptedCheckpointCount), "Accepted checkpoint count must be within the required range.");
            }

            ElapsedSeconds = elapsedSeconds;
            AcceptedCheckpointCount = acceptedCheckpointCount;
            RequiredCheckpointCount = requiredCheckpointCount;
            Completed = completed;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "elapsed={0:0.000}s checkpoints={1}/{2} completed={3}",
                ElapsedSeconds,
                AcceptedCheckpointCount,
                RequiredCheckpointCount,
                Completed);
        }
    }
}
