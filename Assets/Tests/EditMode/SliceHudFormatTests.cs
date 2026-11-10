using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Display-formatting rules for the vertical-slice HUD.</summary>
    public sealed class SliceHudFormatTests
    {
        [Test]
        public void SplitRaceTime_SplitsWholeSecondsAndHundredths()
        {
            SliceHudFormat.SplitRaceTime(12.346d, out var whole, out var fraction);

            Assert.That(whole, Is.EqualTo(12));
            Assert.That(fraction, Is.EqualTo(35));
        }

        [Test]
        public void SplitRaceTime_ClampsInvalidInputToZero()
        {
            SliceHudFormat.SplitRaceTime(-1d, out var whole, out var fraction);
            Assert.That(whole, Is.EqualTo(0));
            Assert.That(fraction, Is.EqualTo(0));

            SliceHudFormat.SplitRaceTime(double.NaN, out whole, out fraction);
            Assert.That(whole, Is.EqualTo(0));
            Assert.That(fraction, Is.EqualTo(0));
        }

        [Test]
        public void SplitRaceTime_SaturatesOverflow()
        {
            SliceHudFormat.SplitRaceTime(5000d, out var whole, out var fraction);

            Assert.That(whole, Is.EqualTo(SliceHudFormat.MaxWholeSeconds));
            Assert.That(fraction, Is.EqualTo(99));
        }

        [Test]
        public void SpeedToKph_ConvertsAndRounds()
        {
            Assert.That(SliceHudFormat.SpeedToKph(0f), Is.EqualTo(0));
            Assert.That(SliceHudFormat.SpeedToKph(10f), Is.EqualTo(36));
            Assert.That(SliceHudFormat.SpeedToKph(25f), Is.EqualTo(90));
        }

        [Test]
        public void SpeedToKph_ClampsInvalidAndOverflow()
        {
            Assert.That(SliceHudFormat.SpeedToKph(-10f), Is.EqualTo(0));
            Assert.That(SliceHudFormat.SpeedToKph(float.NaN), Is.EqualTo(0));
            Assert.That(SliceHudFormat.SpeedToKph(1000f), Is.EqualTo(SliceHudFormat.MaxSpeedKph));
        }
    }
}
