using System;
using NUnit.Framework;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Projection rules from vehicle/race state to HUD values.</summary>
    public sealed class SliceHudModelTests
    {
        [Test]
        public void Evaluate_MapsSpeedTimeAndCheckpoints()
        {
            var telemetry = new VehicleTelemetry(25f, 25f, 0f, 0f, true);

            var snapshot = SliceHudModel.Evaluate(telemetry, RaceState.Running, 5.5d, 2, 3);

            Assert.That(snapshot.SpeedKph, Is.EqualTo(90));
            Assert.That(snapshot.TimeWholeSeconds, Is.EqualTo(5));
            Assert.That(snapshot.TimeHundredths, Is.EqualTo(50));
            Assert.That(snapshot.AcceptedCheckpoints, Is.EqualTo(2));
            Assert.That(snapshot.RequiredCheckpoints, Is.EqualTo(3));
            Assert.That(snapshot.State, Is.EqualTo(RaceState.Running));
        }

        [Test]
        public void Evaluate_ClampsNegativeCheckpointCounts()
        {
            var telemetry = new VehicleTelemetry(0f, 0f, 0f, 0f, false);

            var snapshot = SliceHudModel.Evaluate(telemetry, RaceState.Countdown, 0d, -2, -1);

            Assert.That(snapshot.AcceptedCheckpoints, Is.EqualTo(0));
            Assert.That(snapshot.RequiredCheckpoints, Is.EqualTo(0));
            Assert.That(snapshot.State, Is.EqualTo(RaceState.Countdown));
        }

        [Test]
        public void Evaluate_PropagatesFinishedState()
        {
            var telemetry = new VehicleTelemetry(5f, 5f, 0f, 0f, true);

            var snapshot = SliceHudModel.Evaluate(telemetry, RaceState.Finished, 12.346d, 3, 3);

            Assert.That(snapshot.State, Is.EqualTo(RaceState.Finished));
            Assert.That(snapshot.TimeWholeSeconds, Is.EqualTo(12));
            Assert.That(snapshot.TimeHundredths, Is.EqualTo(35));
        }

        [Test]
        public void Evaluate_RepeatedCallsDoNotAllocate()
        {
            var telemetry = new VehicleTelemetry(10f, 10f, 0f, 0f, true);
            SliceHudModel.Evaluate(telemetry, RaceState.Running, 1d, 0, 3);

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++)
            {
                SliceHudModel.Evaluate(telemetry, RaceState.Running, i * 0.01d, 0, 3);
            }

            var after = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(after - before, Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_CountdownUsesCeilingAndIsZeroOutsideCountdown()
        {
            var telemetry = new VehicleTelemetry(0f, 0f, 0f, 0f, false);

            Assert.That(
                SliceHudModel.Evaluate(telemetry, RaceState.Countdown, 0d, 0, 3, 2.1d).CountdownSeconds,
                Is.EqualTo(3));
            Assert.That(
                SliceHudModel.Evaluate(telemetry, RaceState.Countdown, 0d, 0, 3, 0.001d).CountdownSeconds,
                Is.EqualTo(1));
            Assert.That(
                SliceHudModel.Evaluate(telemetry, RaceState.Countdown, 0d, 0, 3, 500d).CountdownSeconds,
                Is.EqualTo(SliceHudModel.MaxCountdownSeconds));
            Assert.That(
                SliceHudModel.Evaluate(telemetry, RaceState.Running, 0d, 0, 3, 2d).CountdownSeconds,
                Is.EqualTo(0));
        }

        [Test]
        public void Evaluate_PropagatesPauseState()
        {
            var telemetry = new VehicleTelemetry(0f, 0f, 0f, 0f, false);

            var snapshot = SliceHudModel.Evaluate(telemetry, RaceState.Running, 4d, 1, 3, 0d, isPaused: true);

            Assert.That(snapshot.IsPaused, Is.True);
            Assert.That(snapshot.TimeWholeSeconds, Is.EqualTo(4));
        }

        [Test]
        public void CountdownSeconds_HandlesNonFiniteAndNegativeValues()
        {
            Assert.That(SliceHudModel.CountdownSeconds(RaceState.Countdown, double.NaN), Is.EqualTo(0));
            Assert.That(SliceHudModel.CountdownSeconds(RaceState.Countdown, double.PositiveInfinity), Is.EqualTo(0));
            Assert.That(SliceHudModel.CountdownSeconds(RaceState.Countdown, -1d), Is.EqualTo(0));
            Assert.That(SliceHudModel.CountdownSeconds(RaceState.Finished, 2d), Is.EqualTo(0));
        }
    }
}
