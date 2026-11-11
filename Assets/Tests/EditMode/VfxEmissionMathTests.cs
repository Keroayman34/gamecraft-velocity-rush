using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Pure particle-budget coverage for the Stage-2 slice VFX.</summary>
    public sealed class VfxEmissionMathTests
    {
        [Test]
        public void SlipIntensity_IsThresholdedAndSaturated()
        {
            Assert.That(VfxEmissionMath.SlipIntensity01(1f, 1.4f, 6f), Is.EqualTo(0f));
            Assert.That(VfxEmissionMath.SlipIntensity01(3.7f, 1.4f, 6f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(VfxEmissionMath.SlipIntensity01(9f, 1.4f, 6f), Is.EqualTo(1f));
            Assert.That(VfxEmissionMath.SlipIntensity01(float.NaN, 1.4f, 6f), Is.EqualTo(0f));
        }

        [Test]
        public void EmissionCount_RoundsAndCaps()
        {
            Assert.That(VfxEmissionMath.EmissionCount(0f, 10), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.EmissionCount(0.5f, 10), Is.EqualTo(5));
            Assert.That(VfxEmissionMath.EmissionCount(0.55f, 10), Is.EqualTo(6));
            Assert.That(VfxEmissionMath.EmissionCount(1f, 10), Is.EqualTo(10));
            Assert.That(VfxEmissionMath.EmissionCount(2f, 10), Is.EqualTo(10));
            Assert.That(VfxEmissionMath.EmissionCount(0.5f, 0), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.EmissionCount(0.5f, -4), Is.EqualTo(0));
        }

        [Test]
        public void ConsumeBudget_AccumulatesFractionsWithoutBursts()
        {
            var budget = 0f;

            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, 10f, 0.05f), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, 10f, 0.06f), Is.EqualTo(1));
            Assert.That(budget, Is.EqualTo(0.1f).Within(1e-4f));

            for (var i = 0; i < 20; i++)
            {
                VfxEmissionMath.ConsumeBudget(ref budget, 10f, 0.05f);
            }

            Assert.That(budget, Is.InRange(0f, 1f));
        }

        [Test]
        public void ConsumeBudget_IgnoresInvalidRates()
        {
            var budget = 0f;
            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, 0f, 0.1f), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, -5f, 0.1f), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, float.NaN, 0.1f), Is.EqualTo(0));
            Assert.That(VfxEmissionMath.ConsumeBudget(ref budget, 30f, 0f), Is.EqualTo(0));
        }

        [Test]
        public void SmokeDustAndSparkCounts_AreBoundedByTheirBudget()
        {
            Assert.That(VfxEmissionMath.SmokeCount(9f, 1.4f, 6f, 4), Is.EqualTo(4));
            Assert.That(VfxEmissionMath.DustCount(100f, 8f, 40f, 6), Is.EqualTo(6));
            Assert.That(VfxEmissionMath.SparkCount(100f, 3f, 16f, 14), Is.EqualTo(14));
            Assert.That(VfxEmissionMath.SparkCount(1f, 3f, 16f, 14), Is.EqualTo(0));
        }
    }
}
