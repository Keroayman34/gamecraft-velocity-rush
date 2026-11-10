using System;
using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Fixed-point formatting and allocation checks for the development overlay.</summary>
    public sealed class TelemetryOverlayFormatTests
    {
        [Test]
        public void WriteFixed_PadsAndRoundsToRequestedWidth()
        {
            var buffer = new char[10];

            TelemetryOverlayFormat.WriteFixed(buffer, 12.345f, 1, 10);

            Assert.That(new string(buffer), Is.EqualTo("      12.3"));
        }

        [Test]
        public void WriteFixed_KeepsNegativeSignAndWidth()
        {
            var buffer = new char[8];

            TelemetryOverlayFormat.WriteFixed(buffer, -3.5f, 1, 8);

            Assert.That(new string(buffer), Is.EqualTo("    -3.5"));
        }

        [Test]
        public void WriteFixed_SanitizesNonFiniteValues()
        {
            var buffer = new char[8];

            TelemetryOverlayFormat.WriteFixed(buffer, float.NaN, 1, 8);
            Assert.That(new string(buffer), Is.EqualTo("     0.0"));

            TelemetryOverlayFormat.WriteFixed(buffer, float.PositiveInfinity, 1, 8);
            Assert.That(new string(buffer), Is.EqualTo("     0.0"));
        }

        [Test]
        public void WriteFixed_TruncatesIntoAvailableWidth()
        {
            var buffer = new char[4];

            var written = TelemetryOverlayFormat.WriteFixed(buffer, 12345.6f, 1, 4);

            Assert.That(written, Is.EqualTo(4));
            Assert.That(new string(buffer), Is.EqualTo("99.9"));
        }

        [Test]
        public void SlipAngle_IsZeroWhenStraight()
        {
            Assert.That(TelemetryOverlayFormat.SlipAngleDegrees(12f, 0f), Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void SlipAngle_IsFortyFiveAtEqualAxes()
        {
            Assert.That(TelemetryOverlayFormat.SlipAngleDegrees(10f, 10f), Is.EqualTo(45f).Within(1e-3f));
        }

        [Test]
        public void SlipAngle_IsNinetyAtPureLateral()
        {
            Assert.That(TelemetryOverlayFormat.SlipAngleDegrees(0f, 10f), Is.EqualTo(90f).Within(1e-3f));
        }

        [Test]
        public void SlipAngle_SanitizesNonFiniteInput()
        {
            Assert.That(TelemetryOverlayFormat.SlipAngleDegrees(float.NaN, 5f), Is.EqualTo(0f));
            Assert.That(TelemetryOverlayFormat.SlipAngleDegrees(5f, float.PositiveInfinity), Is.EqualTo(0f));
        }

        [Test]
        public void WriteFixed_DoesNotAllocateAfterWarmup()
        {
            var buffer = new char[10];
            for (var i = 0; i < 256; i++)
            {
                TelemetryOverlayFormat.WriteFixed(buffer, i * 0.25f, 2, 10);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++)
            {
                TelemetryOverlayFormat.WriteFixed(buffer, i * 0.25f, 2, 10);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L), "Fixed-point formatting must not allocate.");
        }
    }
}
