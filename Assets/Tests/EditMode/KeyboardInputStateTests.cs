using NUnit.Framework;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the pure keyboard mapping (T5.1): digital-to-normalized conversion,
    /// steering normalization, range invariants, and deterministic reset behavior.
    /// No Unity input API is exercised here (EditMode cannot poll real keys).
    /// </summary>
    public sealed class KeyboardInputStateTests
    {
        [Test]
        public void NewState_IsNeutral()
        {
            var state = new KeyboardInputState();

            Assert.That(state.IsNeutral, Is.True);
            Assert.That(state.Snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Brake, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Handbrake, Is.False);
        }

        [Test]
        public void Sample_MapsDigitalKeysToFullScaleChannels()
        {
            var state = new KeyboardInputState();

            state.Sample(throttleDown: true, brakeDown: false, steerLeftDown: false, steerRightDown: true, handbrakeDown: true);

            Assert.That(state.Snapshot.Throttle, Is.EqualTo(1f));
            Assert.That(state.Snapshot.Brake, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(1f));
            Assert.That(state.Snapshot.Handbrake, Is.True);
            Assert.That(state.IsNeutral, Is.False);
        }

        [Test]
        public void Sample_MapsThrottleAndBrakeIndependently()
        {
            var state = new KeyboardInputState();

            state.Sample(throttleDown: true, brakeDown: true, steerLeftDown: false, steerRightDown: false, handbrakeDown: false);

            Assert.That(state.Snapshot.Throttle, Is.EqualTo(1f));
            Assert.That(state.Snapshot.Brake, Is.EqualTo(1f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
        }

        [Test]
        public void Sample_OppositeSteeringKeysCancelToNeutralSteering()
        {
            var state = new KeyboardInputState();

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: true, steerRightDown: true, handbrakeDown: false);

            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
        }

        [Test]
        public void Sample_SteeringLeftIsNegativeAndRightIsPositive()
        {
            var state = new KeyboardInputState();

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: true, steerRightDown: false, handbrakeDown: false);
            Assert.That(state.Snapshot.Steering, Is.EqualTo(VehicleInputSnapshot.MinSteering));

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: false, steerRightDown: true, handbrakeDown: false);
            Assert.That(state.Snapshot.Steering, Is.EqualTo(VehicleInputSnapshot.MaxSteering));
        }

        [Test]
        public void Sample_AllKeyCombinations_StayWithinDeclaredRanges()
        {
            var state = new KeyboardInputState();

            for (var mask = 0; mask < 32; mask++)
            {
                var throttle = (mask & 1) != 0;
                var brake = (mask & 2) != 0;
                var left = (mask & 4) != 0;
                var right = (mask & 8) != 0;
                var handbrake = (mask & 16) != 0;

                state.Sample(throttle, brake, left, right, handbrake);

                Assert.That(state.Snapshot.Throttle, Is.InRange(VehicleInputSnapshot.MinThrottle, VehicleInputSnapshot.MaxThrottle), "Throttle out of range for mask " + mask);
                Assert.That(state.Snapshot.Brake, Is.InRange(VehicleInputSnapshot.MinThrottle, VehicleInputSnapshot.MaxThrottle), "Brake out of range for mask " + mask);
                Assert.That(state.Snapshot.Steering, Is.InRange(VehicleInputSnapshot.MinSteering, VehicleInputSnapshot.MaxSteering), "Steering out of range for mask " + mask);
                Assert.That(state.Snapshot.Handbrake, Is.EqualTo(handbrake), "Handbrake mismatch for mask " + mask);
                Assert.That(state.Snapshot.Throttle, Is.EqualTo(throttle ? 1f : 0f), "Throttle mapping mismatch for mask " + mask);
                Assert.That(state.Snapshot.Brake, Is.EqualTo(brake ? 1f : 0f), "Brake mapping mismatch for mask " + mask);
            }
        }

        [Test]
        public void Reset_ClearsAllChannelsAfterNonNeutralSample()
        {
            var state = new KeyboardInputState();
            state.Sample(throttleDown: true, brakeDown: true, steerLeftDown: true, steerRightDown: false, handbrakeDown: true);
            Assert.That(state.IsNeutral, Is.False);

            state.Reset();

            Assert.That(state.IsNeutral, Is.True);
            Assert.That(state.Snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Brake, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Handbrake, Is.False);
        }

        [Test]
        public void Reset_IsIdempotent_AndStateCanBeSampledAgain()
        {
            var state = new KeyboardInputState();
            state.Sample(throttleDown: true, brakeDown: false, steerLeftDown: false, steerRightDown: false, handbrakeDown: false);

            state.Reset();
            state.Reset();

            Assert.That(state.IsNeutral, Is.True);

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: false, steerRightDown: true, handbrakeDown: false);

            Assert.That(state.Snapshot.Steering, Is.EqualTo(1f));
            Assert.That(state.IsNeutral, Is.False);
        }

        [Test]
        public void Sample_AllKeysReleased_IsNeutral()
        {
            var state = new KeyboardInputState();
            state.Sample(throttleDown: true, brakeDown: true, steerLeftDown: true, steerRightDown: false, handbrakeDown: true);
            Assert.That(state.IsNeutral, Is.False);

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: false, steerRightDown: false, handbrakeDown: false);

            Assert.That(state.Snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Brake, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Handbrake, Is.False);
            Assert.That(state.IsNeutral, Is.True);
        }

        [Test]
        public void Sample_PressThenRelease_ReturnsToNeutral()
        {
            var state = new KeyboardInputState();

            state.Sample(throttleDown: true, brakeDown: false, steerLeftDown: true, steerRightDown: false, handbrakeDown: false);
            Assert.That(state.IsNeutral, Is.False);
            Assert.That(state.Snapshot.Steering, Is.EqualTo(VehicleInputSnapshot.MinSteering));

            state.Sample(throttleDown: false, brakeDown: false, steerLeftDown: false, steerRightDown: false, handbrakeDown: false);

            Assert.That(state.Snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(state.Snapshot.Steering, Is.EqualTo(0f));
            Assert.That(state.IsNeutral, Is.True);
        }
    }
}
