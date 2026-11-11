using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Pure EditMode coverage for the Stage-2 pause contract in <see cref="RaceRules"/>: frozen
    /// countdown/elapsed time, excluded paused duration, blocked progress while paused, and the
    /// immutable finished result.
    /// </summary>
    public sealed class RacePauseRulesTests
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
        public void NewRace_IsNotPaused()
        {
            Assert.That(_rules.IsPaused, Is.False);
        }

        [Test]
        public void Pause_DuringCountdown_FreezesCountdownAndBlocksTick()
        {
            _clock.NowSeconds = 1d;
            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(2d).Within(1e-9));

            Assert.That(_rules.Pause(), Is.True);
            Assert.That(_rules.IsPaused, Is.True);

            _clock.NowSeconds = 9d;
            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(2d).Within(1e-9));

            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));
        }

        [Test]
        public void Resume_ExcludesPausedCountdownTime()
        {
            _clock.NowSeconds = 1d;
            _rules.Pause();

            _clock.NowSeconds = 6d;
            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(2d).Within(1e-9));
            Assert.That(_rules.Resume(), Is.True);

            _clock.NowSeconds = 7d;
            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(1d).Within(1e-9));

            _clock.NowSeconds = 8d;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));
        }

        [Test]
        public void Pause_DuringRun_FreezesElapsedTime_AndResumeExcludesPausedTime()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));

            _clock.NowSeconds = CountdownSeconds + 2d;
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(2d).Within(1e-9));

            _clock.NowSeconds = CountdownSeconds + 2d;
            Assert.That(_rules.Pause(), Is.True);

            _clock.NowSeconds = CountdownSeconds + 12d;
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(2d).Within(1e-9));

            _rules.Resume();
            Assert.That(_rules.IsPaused, Is.False);
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(2d).Within(1e-9));

            _clock.NowSeconds = CountdownSeconds + 14d;
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(4d).Within(1e-9));
        }

        [Test]
        public void Pause_IsIdempotent_AndResumeWhenNotPausedIsRefused()
        {
            Assert.That(_rules.Resume(), Is.False);
            Assert.That(_rules.IsPaused, Is.False);

            Assert.That(_rules.Pause(), Is.True);
            Assert.That(_rules.Pause(), Is.False);
            Assert.That(_rules.IsPaused, Is.True);
        }

        [Test]
        public void ProgressAndFinish_AreRefusedWhilePaused()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();
            Assert.That(_rules.State, Is.EqualTo(RaceState.Running));

            _rules.Pause();

            Assert.That(_rules.TryAcceptCheckpoint(0), Is.False);
            Assert.That(_rules.TryAcceptCheckpoint(1), Is.False);
            Assert.That(_rules.TryAcceptCheckpoint(2), Is.False);
            Assert.That(_rules.TryFinish(), Is.False);
            Assert.That(_rules.AcceptedCheckpointCount, Is.EqualTo(0));
            Assert.That(_rules.Result.HasValue, Is.False);
        }

        [Test]
        public void Pause_IsRefusedAfterAValidFinish()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();

            Assert.That(_rules.TryAcceptCheckpoint(0), Is.True);
            Assert.That(_rules.TryAcceptCheckpoint(1), Is.True);
            Assert.That(_rules.TryAcceptCheckpoint(2), Is.True);

            _clock.NowSeconds = CountdownSeconds + 4d;
            Assert.That(_rules.TryFinish(), Is.True);
            Assert.That(_rules.State, Is.EqualTo(RaceState.Finished));
            Assert.That(_rules.Result.HasValue, Is.True);

            Assert.That(_rules.Pause(), Is.False);
            Assert.That(_rules.IsPaused, Is.False);
            Assert.That(_rules.Result.Value.ElapsedSeconds, Is.EqualTo(4d).Within(1e-9));
        }

        [Test]
        public void Reset_ClearsPause_AndStartsAFreshCountdown()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();
            _rules.Pause();
            Assert.That(_rules.IsPaused, Is.True);

            _clock.NowSeconds = CountdownSeconds + 5d;
            _rules.Reset();

            Assert.That(_rules.IsPaused, Is.False);
            Assert.That(_rules.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(_rules.ElapsedSeconds, Is.EqualTo(0d));
            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(CountdownSeconds).Within(1e-9));
        }

        [Test]
        public void CountdownRemaining_IsZeroAfterCountdown()
        {
            _clock.NowSeconds = CountdownSeconds;
            _rules.Tick();

            Assert.That(_rules.CountdownRemainingSeconds, Is.EqualTo(0d));
        }

        private sealed class FakeRaceClock : IRaceClock
        {
            public double NowSeconds { get; set; }
        }
    }
}
