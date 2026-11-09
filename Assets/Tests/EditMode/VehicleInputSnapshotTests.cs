using System.Globalization;
using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the normalized driving-input value type (T5.1):
    /// neutral defaults, declared ranges, clamping, and deterministic NaN handling.
    /// </summary>
    public sealed class VehicleInputSnapshotTests
    {
        [Test]
        public void Default_IsNeutral()
        {
            var snapshot = default(VehicleInputSnapshot);

            Assert.That(snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(snapshot.Brake, Is.EqualTo(0f));
            Assert.That(snapshot.Steering, Is.EqualTo(0f));
            Assert.That(snapshot.Handbrake, Is.False);
            Assert.That(snapshot.IsNeutral, Is.True);
        }

        [Test]
        public void Neutral_MatchesDefaultState()
        {
            var neutral = VehicleInputSnapshot.Neutral;

            Assert.That(neutral.Throttle, Is.EqualTo(VehicleInputSnapshot.MinThrottle));
            Assert.That(neutral.Brake, Is.EqualTo(VehicleInputSnapshot.MinThrottle));
            Assert.That(neutral.Steering, Is.EqualTo(0f));
            Assert.That(neutral.Handbrake, Is.False);
            Assert.That(neutral.IsNeutral, Is.True);
        }

        [Test]
        public void Constructor_ClampsChannelsToDeclaredRanges()
        {
            var over = new VehicleInputSnapshot(2f, 2f, 3f, handbrake: true);
            var under = new VehicleInputSnapshot(-2f, -2f, -3f, handbrake: false);

            Assert.That(over.Throttle, Is.EqualTo(VehicleInputSnapshot.MaxThrottle));
            Assert.That(over.Brake, Is.EqualTo(VehicleInputSnapshot.MaxThrottle));
            Assert.That(over.Steering, Is.EqualTo(VehicleInputSnapshot.MaxSteering));
            Assert.That(over.Handbrake, Is.True);

            Assert.That(under.Throttle, Is.EqualTo(VehicleInputSnapshot.MinThrottle));
            Assert.That(under.Brake, Is.EqualTo(VehicleInputSnapshot.MinThrottle));
            Assert.That(under.Steering, Is.EqualTo(VehicleInputSnapshot.MinSteering));
            Assert.That(under.Handbrake, Is.False);
        }

        [Test]
        public void Constructor_AcceptsBoundaryValuesExactly()
        {
            var snapshot = new VehicleInputSnapshot(1f, 0f, -1f, handbrake: true);

            Assert.That(snapshot.Throttle, Is.EqualTo(1f));
            Assert.That(snapshot.Brake, Is.EqualTo(0f));
            Assert.That(snapshot.Steering, Is.EqualTo(-1f));
            Assert.That(snapshot.Handbrake, Is.True);
        }

        [Test]
        public void Constructor_SanitizesNaNToNeutralZero()
        {
            var snapshot = new VehicleInputSnapshot(float.NaN, float.NaN, float.NaN, handbrake: false);

            Assert.That(snapshot.Throttle, Is.EqualTo(0f));
            Assert.That(snapshot.Brake, Is.EqualTo(0f));
            Assert.That(snapshot.Steering, Is.EqualTo(0f));
            Assert.That(snapshot.IsNeutral, Is.True);
        }

        [Test]
        public void Constructor_ClampsInfinitiesToDeclaredBounds()
        {
            var positive = new VehicleInputSnapshot(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity, handbrake: false);
            var negative = new VehicleInputSnapshot(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity, handbrake: false);

            Assert.That(positive.Throttle, Is.EqualTo(VehicleInputSnapshot.MaxThrottle));
            Assert.That(positive.Steering, Is.EqualTo(VehicleInputSnapshot.MaxSteering));
            Assert.That(negative.Throttle, Is.EqualTo(VehicleInputSnapshot.MinThrottle));
            Assert.That(negative.Steering, Is.EqualTo(VehicleInputSnapshot.MinSteering));
        }

        [TestCase(0.5f, 0f, 0f, false)]
        [TestCase(0f, 0.5f, 0f, false)]
        [TestCase(0f, 0f, 0.5f, false)]
        [TestCase(0f, 0f, -0.5f, false)]
        [TestCase(0f, 0f, 0f, true)]
        public void IsNeutral_IsFalse_WhenAnyChannelIsActive(float throttle, float brake, float steering, bool handbrake)
        {
            var snapshot = new VehicleInputSnapshot(throttle, brake, steering, handbrake);

            Assert.That(snapshot.IsNeutral, Is.False);
        }

        [Test]
        public void ToString_IsDeterministicAndCultureIndependent()
        {
            var snapshot = new VehicleInputSnapshot(1f, 0.25f, -0.5f, handbrake: true);
            var previousCulture = CultureInfo.CurrentCulture;

            try
            {
                // A comma-decimal culture proves the invariant formatter is actually used; under
                // the default culture a regression to CurrentCulture could still pass unnoticed.
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                Assert.That(snapshot.ToString(), Is.EqualTo("throttle=1.00 brake=0.25 steer=-0.50 handbrake=True"));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }
    }
}
