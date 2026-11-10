using System;
using NUnit.Framework;
using UnityEngine.UIElements;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Digit rendering rules for the zero-allocation HUD.</summary>
    public sealed class DigitDisplayTests
    {
        [Test]
        public void SetValue_SuppressesLeadingZeros()
        {
            var display = new DigitDisplay(new VisualElement(), 3);

            display.SetValue(7);

            Assert.That(display.TextAt(0), Is.EqualTo(string.Empty));
            Assert.That(display.TextAt(1), Is.EqualTo(string.Empty));
            Assert.That(display.TextAt(2), Is.EqualTo("7"));
        }

        [Test]
        public void SetValue_ShowsFixedWidthWhenRequested()
        {
            var display = new DigitDisplay(new VisualElement(), 2, false);

            display.SetValue(5);

            Assert.That(display.TextAt(0), Is.EqualTo("0"));
            Assert.That(display.TextAt(1), Is.EqualTo("5"));
        }

        [Test]
        public void SetValue_SaturatesOverflowAndClampsNegativeValues()
        {
            var display = new DigitDisplay(new VisualElement(), 3);

            display.SetValue(12345);
            Assert.That(display.TextAt(0), Is.EqualTo("9"));
            Assert.That(display.TextAt(1), Is.EqualTo("9"));
            Assert.That(display.TextAt(2), Is.EqualTo("9"));

            display.SetValue(-5);
            Assert.That(display.TextAt(0), Is.EqualTo(string.Empty));
            Assert.That(display.TextAt(1), Is.EqualTo(string.Empty));
            Assert.That(display.TextAt(2), Is.EqualTo("0"));
        }

        [Test]
        public void SetValue_RepeatedUpdatesDoNotAllocate()
        {
            var display = new DigitDisplay(new VisualElement(), 3);
            for (var i = 0; i < 1000; i++)
            {
                display.SetValue(i % 1000);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 10000; i++)
            {
                display.SetValue(i % 1000);
            }

            var after = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(after - before, Is.EqualTo(0));
        }
    }
}
