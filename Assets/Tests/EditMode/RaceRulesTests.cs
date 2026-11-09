using System;
using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Pure EditMode coverage for the T5.4 Race Rules core: deterministic injected time, valid and
    /// invalid transitions, ordered checkpoints, finish gating, exactly-once results, and retry
    /// reset. No scene, physics, or sleep-based waiting is involved.
    /// </summary>
    public sealed class RaceRulesTests
    {
        private const double CountdownSeconds = 3d;
        private const int CheckpointCount = 3;

        private FakeRaceClock _clock;
        private RaceRules _rules;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeRaceClock();
            _rules = new RaceRules(_clock, CountdownSeconds, CheckpointCount);
        }

        [Test]
        public void NewRace_StartsInCountdown_WithNoProgressOrResult()
        {
            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d));
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(0));
            Assert.That(_rules.RequiredCheckpointCount, Is.EqualTo(CheckpointCount));
            Assert.That(_rules.Result.HasValue, Is.False);
        }

        [Test]
        public void Constructor_RejectsInvalidArguments()
        {
            Assert.Throws<ArgumentNullException>(() => new RaceRules(null, CountdownSeconds, CheckpointCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceRules(_clock, -0.1d, CheckpointCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceRules(_clock, double.NaN, CheckpointCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceRules(_clock, double.PositiveInfinity, CheckpointCount));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceRules(_clock, CountdownSeconds, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceRules(_clock, CountdownSeconds, -1));
        }

        [Test]
        public void Tick_BeforeCountdownCompletes_StaysInCountdown()
        {
            _clock.NowSeconds = CountdownSeconds - 0.001d;

            _rules.Tick();

            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d));
        }

        [Test]
        public void Tick_AtCountdownCompletion_TransitionsToRunning_AndStartsTimerFromZero()
        {
            _clock.NowSeconds = CountdownSeconds;

            _rules.Tick();

            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d).Within(1e-9));

            _clock.NowSeconds = CountdownSeconds + 2.5d;
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(2.5d).Within(1e-9));
        }

        [Test]
        public void Timer_ExcludesCountdownTime()
        {
            _clock.NowSeconds = 100d;
            _rules.Tick();

            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d).Within(1e-9));

            _clock.NowSeconds = 104d;
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(4d).Within(1e-9));
        }

        [Test]
        public void ZeroCountdown_TransitionsToRunningOnFirstTick()
        {
            var rules = new RaceRules(_clock, 0d, CheckpointCount);

            rules.Tick();

            Assert.That(rules.State, Is.EqualTo(RaceState.Running));
            Assert.That(rules.ElapsedSeconds, Is.EqualTo(0d).Within(1e-9));
        }

        [Test]
        public void Checkpoints_MustBeAcceptedInStableOrder()
        {
            EnterRunning();

            Assert.That(_rules.TryAcceptCheckpoint(1), Is.False, "A later checkpoint must be rejected before the expected one.");
            Assert.That(_rules.TryAcceptCheckpoint(2), Is.False);
            Assert.That(_rules.TryAcceptCheckpoint(-1), Is.False);
            Assert.That(_rules.TryAcceptCheckpoint(CheckpointCount), Is.False);
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(0));

            Assert.That(_rules.TryAcceptCheckpoint(0), Is.True);
            Assert.That(_rules.TryAcceptCheckpoint(0), Is.False, "Re-entering an accepted checkpoint must not increment progress.");
            Assert.That(_rules.TryAcceptCheckpoint(2), Is.False, "Skipping the next expected checkpoint must be rejected.");
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(1));

            Assert.That(_rules.TryAcceptCheckpoint(1), Is.True);
            Assert.That(_rules.TryAcceptCheckpoint(2), Is.True);
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(CheckpointCount));
        }

        [Test]
        public void Checkpoints_AreRejectedDuringCountdownAndAfterFinish()
        {
            Assert.That(_rules.TryAcceptCheckpoint(0), Is.False);

            EnterRunning();
            AcceptAllCheckpoints();
            _clock.NowSeconds += 1d;
            Assert.That(_rules.TryFinish(), Is.True);

            Assert.That(_rules.TryAcceptCheckpoint(CheckpointCount), Is.False);
            Assert.That(_rules.TryAcceptCheckpoint(0), Is.False);
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(CheckpointCount));
        }

        [Test]
        public void Finish_IsBlockedUntilAllCheckpointsAreAccepted()
        {
            Assert.That(_rules.TryFinish(), Is.False, "Finish must be rejected during countdown.");

            EnterRunning();
            Assert.That(_rules.TryFinish(), Is.False, "Finish must be rejected with zero checkpoints.");

            Assert.That(_rules.TryAcceptCheckpoint(0), Is.True);
            Assert.That(_rules.TryAcceptCheckpoint(1), Is.True);
            Assert.That(_rules.TryFinish(), Is.False, "Finish must stay blocked while a checkpoint is outstanding.");
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));

            Assert.That(_rules.TryAcceptCheckpoint(2), Is.True);
            Assert.That(_rules.TryFinish(), Is.True);
            Assert.That(_rules.State, Is.EqualTo(RaceState.Finished));
        }

        [Test]
        public void Finish_EmitsExactlyOneResult_AndFreezesTheTimer()
        {
            var finishCount = 0;
            RaceResult? observed = null;
            _rules.RaceFinished += result =>
            {
                finishCount++;
                observed = result;
            };

            EnterRunning();
            AcceptAllCheckpoints();
            _clock.NowSeconds = CountdownSeconds + 7.25d;

            Assert.That(_rules.TryFinish(), Is.True);
            Assert.That(finishCount, Is.EqualTo(1));
            Assert.That(observed.HasValue, Is.True);
            Assert.That(_rules.Result.HasValue, Is.True);
            Assert.That(_rules.Result.Value.ElapsedSeconds, Is.EqualTo(7.25d).Within(1e-9));

            var frozen = _rules.ElapsedSeconds;
            _clock.NowSeconds += 100d;

            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(frozen).Within(1e-9));
            Assert.That(_rules.TryFinish(), Is.False, "A duplicate finish trigger must not emit a second result.");
            Assert.That(finishCount, Is.EqualTo(1));
            Assert.That(_rules.Result.Value.ElapsedSeconds, Is.EqualTo(frozen).Within(1e-9));
        }

        [Test]
        public void FinishResult_ContainsCompletionFacts()
        {
            EnterRunning();
            AcceptAllCheckpoints();
            _clock.NowSeconds = CountdownSeconds + 1.5d;

            _rules.TryFinish();
            var result = _rules.Result.Value;

            Assert.That(result.ElapsedSeconds, Is.EqualTo(1.5d).Within(1e-9));
            Assert.That(result.AcceptedCheckpointCount, Is.EqualTo(CheckpointCount));
            Assert.That(result.RequiredCheckpointCount, Is.EqualTo(CheckpointCount));
            Assert.That(result.Completed, Is.True);
        }

        [Test]
        public void ReentrantFinishSubscriber_CannotProduceASecondResult()
        {
            var finishCount = 0;
            _rules.RaceFinished += _ =>
            {
                finishCount++;
                Assert.That(_rules.TryFinish(), Is.False, "The state must already be Finished inside the notification.");
            };

            EnterRunning();
            AcceptAllCheckpoints();

            Assert.That(_rules.TryFinish(), Is.True);
            Assert.That(finishCount, Is.EqualTo(1));
        }

        [Test]
        public void Reset_ClearsStateProgressTimerAndResultGuards_WithoutEmittingOnReset()
        {
            var finishCount = 0;
            _rules.RaceFinished += _ => finishCount++;

            EnterRunning();
            AcceptAllCheckpoints();
            _clock.NowSeconds = CountdownSeconds + 4d;
            _rules.TryFinish();
            Assert.That(finishCount, Is.EqualTo(1));

            _clock.NowSeconds += 50d;
            _rules.Reset();

            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(0));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d));
            Assert.That(_rules.Result.HasValue, Is.False);
            Assert.That(finishCount, Is.EqualTo(1), "Resetting must not emit a result for the abandoned attempt.");

            // A fresh attempt in the same instance can complete and emit again, once.
            _clock.NowSeconds = 500d;
            _rules.Tick();
            _rules.TryAcceptCheckpoint(0);
            _rules.TryAcceptCheckpoint(1);
            _rules.TryAcceptCheckpoint(2);
            _clock.NowSeconds = 501d;

            Assert.That(_rules.TryFinish(), Is.True);
            Assert.That(finishCount, Is.EqualTo(2));
            Assert.That(_rules.Result.Value.ElapsedSeconds, Is.EqualTo(1d).Within(1e-9));
        }

        [Test]
        public void Reset_DuringCountdown_RestartsTheConfiguredCountdown()
        {
            _clock.NowSeconds = 2.9d;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));

            _clock.NowSeconds = 3.1d;
            _rules.Reset();
            _rules.Tick();

            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown), "Reset must restart the countdown window.");

            _clock.NowSeconds = 3.1d + CountdownSeconds + 0.001d;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));
        }

        [Test]
        public void ElapsedSeconds_IsMonotonicWhileRunning()
        {
            EnterRunning();
            var previous = _rules.ElapsedSeconds;

            for (var step = 1; step <= 10; step++)
            {
                _clock.NowSeconds = CountdownSeconds + (step * 0.1d);
                var current = _rules.ElapsedSeconds;
                Assert.That(current, Is.GreaterThanOrEqualTo(previous));
                previous = current;
            }
        }

        [Test]
        public void StopwatchRaceClock_IsMonotonic()
        {
            var clock = new StopwatchRaceClock();

            var first = clock.NowSeconds;
            var second = clock.NowSeconds;

            Assert.That(first, Is.GreaterThanOrEqualTo(0d));
            Assert.That(second, Is.GreaterThanOrEqualTo(first));
        }

        [Test]
        public void RaceResult_RejectsInvalidFacts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(-1d, 0, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(double.NaN, 0, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(double.PositiveInfinity, 0, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(1d, -1, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(1d, 2, 1, true));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RaceResult(1d, 0, -1, true));
        }

        private void EnterRunning()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));
        }

        private void AcceptAllCheckpoints()
        {
            for (var i = 0; i < CheckpointCount; i++)
            {
                Assert.That(_rules.TryAcceptCheckpoint(i), Is.True, "Checkpoint " + i + " should be accepted.");
            }
        }

        private sealed class FakeRaceClock : IRaceClock
        {
            public double NowSeconds { get; set; }
        }
    }
}
