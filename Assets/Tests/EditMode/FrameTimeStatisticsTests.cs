using System;
using NUnit.Framework;
using VelocityRush.Core.Diagnostics;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Rules for the fixed-capacity frame-time window used by the profiling harness.</summary>
    public sealed class FrameTimeStatisticsTests
    {
        [Test]
        public void Constructor_RejectsNonPositiveCapacity()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTimeStatistics(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new FrameTimeStatistics(-1));
        }

        [Test]
        public void Add_ComputesMeanMinimumAndMaximum()
        {
            var statistics = new FrameTimeStatistics(8);
            statistics.Add(10f);
            statistics.Add(20f);
            statistics.Add(30f);

            Assert.That(statistics.Count, Is.EqualTo(3));
            Assert.That(statistics.MeanMilliseconds, Is.EqualTo(20f).Within(0.001f));
            Assert.That(statistics.MinMilliseconds, Is.EqualTo(10f).Within(0.001f));
            Assert.That(statistics.MaxMilliseconds, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Add_WrapsAtCapacityAndRetainsNewestWindow()
        {
            var statistics = new FrameTimeStatistics(3);
            statistics.Add(10f);
            statistics.Add(20f);
            statistics.Add(30f);
            statistics.Add(40f);

            Assert.That(statistics.Count, Is.EqualTo(3));
            Assert.That(statistics.MinMilliseconds, Is.EqualTo(20f).Within(0.001f));
            Assert.That(statistics.MaxMilliseconds, Is.EqualTo(40f).Within(0.001f));
            Assert.That(statistics.MeanMilliseconds, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Percentile_InterpolatesBetweenRanks()
        {
            var statistics = new FrameTimeStatistics(5);
            for (var i = 1; i <= 5; i++)
            {
                statistics.Add(i);
            }

            Assert.That(statistics.PercentileMilliseconds(0.5f), Is.EqualTo(3f).Within(0.001f));
            Assert.That(statistics.PercentileMilliseconds(0.95f), Is.EqualTo(4.8f).Within(0.001f));
            Assert.That(statistics.PercentileMilliseconds(0f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(statistics.PercentileMilliseconds(1f), Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Percentile_RejectsOutOfRangeInput()
        {
            var statistics = new FrameTimeStatistics(2);
            statistics.Add(16f);

            Assert.That(statistics.PercentileMilliseconds(-0.1f), Is.EqualTo(0f));
            Assert.That(statistics.PercentileMilliseconds(1.1f), Is.EqualTo(0f));
            Assert.That(statistics.PercentileMilliseconds(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void Add_SanitizesInvalidSamples()
        {
            var statistics = new FrameTimeStatistics(4);
            statistics.Add(float.NaN);
            statistics.Add(float.PositiveInfinity);
            statistics.Add(-5f);

            Assert.That(statistics.MeanMilliseconds, Is.EqualTo(0f));
            Assert.That(statistics.MinMilliseconds, Is.EqualTo(0f));
            Assert.That(statistics.MaxMilliseconds, Is.EqualTo(0f));
        }

        [Test]
        public void FramesPerSecond_AreDerivedFromMilliseconds()
        {
            var statistics = new FrameTimeStatistics(4);
            statistics.Add(16.6667f);

            Assert.That(statistics.MeanFramesPerSecond, Is.EqualTo(60f).Within(0.01f));
            Assert.That(statistics.MinimumFramesPerSecond, Is.EqualTo(60f).Within(0.01f));
            Assert.That(statistics.MaximumFramesPerSecond, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void Clear_ResetsTheWindow()
        {
            var statistics = new FrameTimeStatistics(2);
            statistics.Add(10f);
            statistics.Clear();

            Assert.That(statistics.IsEmpty, Is.True);
            Assert.That(statistics.MeanMilliseconds, Is.EqualTo(0f));
        }
    }
}
