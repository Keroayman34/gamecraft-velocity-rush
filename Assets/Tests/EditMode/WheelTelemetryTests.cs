using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Sanitization and immutability checks for the additive per-wheel telemetry struct.</summary>
    public sealed class WheelTelemetryTests
    {
        [Test]
        public void Default_HasNoSlip_NoCompression_NotGrounded()
        {
            var sample = default(WheelTelemetry);

            Assert.That(sample.Slip, Is.EqualTo(0f));
            Assert.That(sample.SuspensionCompression, Is.EqualTo(0f));
            Assert.That(sample.IsGrounded, Is.False);
        }

        [Test]
        public void Preserves_ValidValues()
        {
            var sample = new WheelTelemetry(0.75f, true, 0.4f);

            Assert.That(sample.Slip, Is.EqualTo(0.75f));
            Assert.That(sample.IsGrounded, Is.True);
            Assert.That(sample.SuspensionCompression, Is.EqualTo(0.4f));
        }

        [Test]
        public void NegativeOrNonFiniteSlip_BecomesZero()
        {
            Assert.That(new WheelTelemetry(-3f, true, 0.5f).Slip, Is.EqualTo(0f));
            Assert.That(new WheelTelemetry(float.NaN, true, 0.5f).Slip, Is.EqualTo(0f));
            Assert.That(new WheelTelemetry(float.PositiveInfinity, true, 0.5f).Slip, Is.EqualTo(0f));
        }

        [Test]
        public void Compression_IsClampedToUnitRange()
        {
            Assert.That(new WheelTelemetry(0.1f, true, 2f).SuspensionCompression, Is.EqualTo(1f));
            Assert.That(new WheelTelemetry(0.1f, true, -2f).SuspensionCompression, Is.EqualTo(0f));
            Assert.That(new WheelTelemetry(0.1f, true, float.NaN).SuspensionCompression, Is.EqualTo(0f));
            Assert.That(new WheelTelemetry(0.1f, true, float.PositiveInfinity).SuspensionCompression, Is.EqualTo(0f));
        }
    }
}
