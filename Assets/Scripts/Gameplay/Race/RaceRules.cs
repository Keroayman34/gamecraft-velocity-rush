using System;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure prototype Race Rules core: countdown, monotonic timer, ordered checkpoints, and the
    /// exactly-once finish result (TDD §29 S5 / §35.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately free of Unity scene types and device input so the complete race lifecycle is
    /// EditMode-testable with an injected <see cref="IRaceClock"/>. The track only reports typed
    /// signals; judging stays here, matching TDD §29 S4/S5.
    /// </para>
    /// <para>
    /// Exactly-once rule: <see cref="TryFinish"/> transitions to <see cref="RaceState.Finished"/>
    /// before raising <see cref="RaceFinished"/>, so a duplicate finish trigger (or a re-entrant
    /// subscriber) can never produce a second result for the same attempt. <see cref="Reset"/>
    /// starts a fresh attempt and deliberately emits no result.
    /// </para>
    /// </remarks>
    public sealed class RaceRules
    {
        private readonly IRaceClock _clock;
        private readonly double _countdownSeconds;
        private readonly int _checkpointCount;

        private double _countdownStartedAt;
        private double _runningStartedAt;
        private double _frozenElapsedSeconds;
        private double _pausedAt;
        private double _pausedCountdownRemaining;
        private int _acceptedCheckpointCount;
        private RaceResult? _result;

        /// <summary>Current lifecycle state. A new instance starts in Countdown.</summary>
        public RaceState State { get; private set; }

        /// <summary>
        /// True while the attempt is paused. A paused attempt keeps its state, progress, and
        /// elapsed value frozen; no checkpoint, finish, or countdown transition is accepted.
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>Next expected checkpoint index (also the number accepted so far).</summary>
        public int AcceptedCheckpointCount => _acceptedCheckpointCount;

        /// <summary>Checkpoint count required before the finish trigger is accepted.</summary>
        public int RequiredCheckpointCount => _checkpointCount;

        /// <summary>
        /// Elapsed race seconds. Zero during countdown, live while running, and permanently frozen
        /// once finished. Countdown time is never included.
        /// </summary>
        public double ElapsedSeconds
        {
            get
            {
                if (IsPaused)
                {
                    return _frozenElapsedSeconds;
                }

                if (State == RaceState.Running)
                {
                    return Math.Max(0d, _clock.NowSeconds - _runningStartedAt);
                }

                return _frozenElapsedSeconds;
            }
        }

        /// <summary>
        /// Remaining countdown seconds, or zero once the run is live or finished. While paused the
        /// value captured at the pause transition is reported, so the HUD cannot advance.
        /// </summary>
        public double CountdownRemainingSeconds
        {
            get
            {
                if (State != RaceState.Countdown)
                {
                    return 0d;
                }

                if (IsPaused)
                {
                    return _pausedCountdownRemaining;
                }

                return Math.Max(0d, _countdownSeconds - (_clock.NowSeconds - _countdownStartedAt));
            }
        }

        /// <summary>The completed attempt's immutable result, or null before a valid finish.</summary>
        public RaceResult? Result => _result;

        /// <summary>
        /// Raised exactly once per completed attempt, after the state becomes
        /// <see cref="RaceState.Finished"/> and the result is frozen.
        /// </summary>
        public event Action<RaceResult> RaceFinished;

        /// <param name="clock">Monotonic time source; never null.</param>
        /// <param name="countdownSeconds">Configurable countdown duration; finite and non-negative.</param>
        /// <param name="checkpointCount">Positive required checkpoint count.</param>
        public RaceRules(IRaceClock clock, double countdownSeconds, int checkpointCount)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (double.IsNaN(countdownSeconds) || double.IsInfinity(countdownSeconds) || countdownSeconds < 0d)
            {
                throw new ArgumentOutOfRangeException(nameof(countdownSeconds), "Countdown duration must be finite and non-negative.");
            }

            if (checkpointCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpointCount), "At least one checkpoint is required.");
            }

            _clock = clock;
            _countdownSeconds = countdownSeconds;
            _checkpointCount = checkpointCount;

            Reset();
        }

        /// <summary>
        /// Advances the countdown. Once the configured duration has elapsed, the state becomes
        /// Running and the race timer starts at that exact transition.
        /// </summary>
        public void Tick()
        {
            if (IsPaused || State != RaceState.Countdown)
            {
                return;
            }

            var now = _clock.NowSeconds;
            if (now - _countdownStartedAt < _countdownSeconds)
            {
                return;
            }

            State = RaceState.Running;
            _runningStartedAt = now;
            _frozenElapsedSeconds = 0d;
        }

        /// <summary>
        /// Accepts only the next expected checkpoint while running. Out-of-order, duplicate,
        /// negative, and post-finish indices are rejected without changing progress.
        /// </summary>
        public bool TryAcceptCheckpoint(int checkpointIndex)
        {
            if (IsPaused || State != RaceState.Running)
            {
                return false;
            }

            if (checkpointIndex < 0 || checkpointIndex >= _checkpointCount)
            {
                return false;
            }

            if (checkpointIndex != _acceptedCheckpointCount)
            {
                return false;
            }

            _acceptedCheckpointCount++;
            return true;
        }

        /// <summary>
        /// Finishes only when running with every checkpoint accepted. Raises
        /// <see cref="RaceFinished"/> exactly once and freezes the elapsed time.
        /// </summary>
        public bool TryFinish()
        {
            if (IsPaused || State != RaceState.Running || _acceptedCheckpointCount < _checkpointCount)
            {
                return false;
            }

            var now = _clock.NowSeconds;
            _frozenElapsedSeconds = Math.Max(0d, now - _runningStartedAt);
            State = RaceState.Finished;

            var result = new RaceResult(
                _frozenElapsedSeconds,
                _acceptedCheckpointCount,
                _checkpointCount,
                completed: true);

            _result = result;
            RaceFinished?.Invoke(result);
            return true;
        }

        /// <summary>
        /// Freezes the attempt in Countdown or Running. Returns false when already paused or when
        /// the attempt is finished (a finished result is immutable and must not be pausable).
        /// </summary>
        public bool Pause()
        {
            if (IsPaused || State == RaceState.Finished)
            {
                return false;
            }

            var now = _clock.NowSeconds;
            IsPaused = true;
            _pausedAt = now;

            if (State == RaceState.Running)
            {
                _frozenElapsedSeconds = Math.Max(0d, now - _runningStartedAt);
            }
            else
            {
                _pausedCountdownRemaining = Math.Max(0d, _countdownSeconds - (now - _countdownStartedAt));
            }

            return true;
        }

        /// <summary>
        /// Resumes a paused attempt. The countdown and run origins shift by the paused duration so
        /// no paused time is ever counted. Returns false when the attempt is not paused.
        /// </summary>
        public bool Resume()
        {
            if (!IsPaused)
            {
                return false;
            }

            var now = _clock.NowSeconds;
            var pausedSeconds = Math.Max(0d, now - _pausedAt);
            _countdownStartedAt += pausedSeconds;
            _runningStartedAt += pausedSeconds;

            IsPaused = false;
            _pausedAt = 0d;
            _pausedCountdownRemaining = 0d;

            if (State == RaceState.Running)
            {
                _frozenElapsedSeconds = 0d;
            }

            return true;
        }

        /// <summary>
        /// Starts a fresh attempt in the same session: Countdown state, zero progress, zero
        /// elapsed time, and no result. Does not raise a result or perform any scene work.
        /// </summary>
        public void Reset()
        {
            State = RaceState.Countdown;
            _countdownStartedAt = _clock.NowSeconds;
            _runningStartedAt = 0d;
            _frozenElapsedSeconds = 0d;
            _pausedAt = 0d;
            _pausedCountdownRemaining = 0d;
            _acceptedCheckpointCount = 0;
            _result = null;
            IsPaused = false;
        }
    }
}
