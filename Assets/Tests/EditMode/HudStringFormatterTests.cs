using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class HudStringFormatterTests
    {
        private char[] _buffer;

        [SetUp]
        public void SetUp()
        {
            _buffer = new char[32];
        }

        [Test]
        public void WriteInt_Zero_WritesZero()
        {
            int len = HudStringFormatter.WriteInt(_buffer, 0, 0);
            Assert.That(len, Is.EqualTo(1));
            Assert.That(_buffer[0], Is.EqualTo('0'));
        }

        [Test]
        public void WriteInt_PositiveInteger_WritesCorrectDigits()
        {
            int len = HudStringFormatter.WriteInt(_buffer, 0, 245);
            Assert.That(len, Is.EqualTo(3));
            Assert.That(new string(_buffer, 0, len), Is.EqualTo("245"));
        }

        [Test]
        public void WriteIntPadded_PadsWithZeros()
        {
            int len = HudStringFormatter.WriteIntPadded(_buffer, 0, 7, 2);
            Assert.That(len, Is.EqualTo(2));
            Assert.That(new string(_buffer, 0, len), Is.EqualTo("07"));
        }

        [Test]
        public void WriteRaceTime_UnderMinute_FormatsSecondsAndHundredths()
        {
            int len = HudStringFormatter.WriteRaceTime(_buffer, 0, 45.67d);
            Assert.That(len, Is.GreaterThan(0));
            Assert.That(new string(_buffer, 0, len), Is.EqualTo("45.67"));
        }

        [Test]
        public void WriteRaceTime_OverMinute_FormatsMinutesSecondsHundredths()
        {
            int len = HudStringFormatter.WriteRaceTime(_buffer, 0, 72.34d);
            Assert.That(len, Is.GreaterThan(0));
            Assert.That(new string(_buffer, 0, len), Is.EqualTo("1:12.34"));
        }

        [Test]
        public void WritePosition_Ordinals_CorrectSuffixes()
        {
            int len1 = HudStringFormatter.WritePosition(_buffer, 0, 1);
            Assert.That(new string(_buffer, 0, len1), Is.EqualTo("1st"));

            int len2 = HudStringFormatter.WritePosition(_buffer, 0, 2);
            Assert.That(new string(_buffer, 0, len2), Is.EqualTo("2nd"));

            int len3 = HudStringFormatter.WritePosition(_buffer, 0, 3);
            Assert.That(new string(_buffer, 0, len3), Is.EqualTo("3rd"));

            int len4 = HudStringFormatter.WritePosition(_buffer, 0, 4);
            Assert.That(new string(_buffer, 0, len4), Is.EqualTo("4th"));

            int len5 = HudStringFormatter.WritePosition(_buffer, 0, 5);
            Assert.That(new string(_buffer, 0, len5), Is.EqualTo("5th"));
        }
    }
}
