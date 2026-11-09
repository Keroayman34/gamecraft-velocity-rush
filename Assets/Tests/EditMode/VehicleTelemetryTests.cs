using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the polled telemetry value type (T5.2): neutral defaults and
    /// deterministic sanitizing of non-finite or negative physics readings.
    /// </summary>
    public sealed class VehicleTelemetryTests
    {
        [Test]
        public void Default_IsNeutralAndGroundedFalse()
        {
            var telemetry = default(VehicleTelemetry);

            Assert.That(telemetry.Speed, Is.EqualTo(0f));
            Assert.That(telemetry.ForwardSpeed, Is.EqualTo(0f));
            Assert.That(telemetry.LateralSpeed, Is.EqualTo(0f));
            Assert.That(telemetry.WheelSlip, Is.EqualTo(0f));
            Assert.That(telemetry.IsGrounded, Is.False);
        }

        [Test]
        public void Constructor_ExposesValues()
        {
            var telemetry = new VehicleTelemetry(12.5f, 11f, -2f, 0.3f, true);

            Assert.That(telemetry.Speed, Is.EqualTo(12.5f));
            Assert.That(telemetry.ForwardSpeed, Is.EqualTo(11f));
            Assert.That(telemetry.LateralSpeed, Is.EqualTo(-2f));
            Assert.That(telemetry.WheelSlip, Is.EqualTo(0.3f));
            Assert.That(telemetry.IsGrounded, Is.True);
        }

        [Test]
        public void Constructor_SanitizesNonFiniteAndNegativeValues()
        {
            var telemetry = new VehicleTelemetry(
                float.NaN,
                float.PositiveInfinity,
                float.NegativeInfinity,
                -1f,
                true);

            Assert.That(telemetry.Speed, Is.EqualTo(0f));
            Assert.That(telemetry.ForwardSpeed, Is.EqualTo(0f));
            Assert.That(telemetry.LateralSpeed, Is.EqualTo(0f));
            Assert.That(telemetry.WheelSlip, Is.EqualTo(0f));
            Assert.That(telemetry.IsGrounded, Is.True);
        }

        [Test]
        public void Telemetry_IsAValueType()
        {
            Assert.That(typeof(VehicleTelemetry).IsValueType, Is.True);
            Assert.That(typeof(IVehicleTelemetry).IsInterface, Is.True);
        }
    }
}
