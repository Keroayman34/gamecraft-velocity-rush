using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Pure response-curve coverage for the Stage-2 engine and tyre audio.</summary>
    public sealed class SliceAudioMathTests
    {
        [Test]
        public void EngineSpeed01_ClampsAndSanitizes()
        {
            Assert.That(EngineAudioMath.Speed01(0f, 50f), Is.EqualTo(0f));
            Assert.That(EngineAudioMath.Speed01(25f, 50f), Is.EqualTo(0.5f));
            Assert.That(EngineAudioMath.Speed01(75f, 50f), Is.EqualTo(1f));
            Assert.That(EngineAudioMath.Speed01(-5f, 50f), Is.EqualTo(0f));
            Assert.That(EngineAudioMath.Speed01(float.NaN, 50f), Is.EqualTo(0f));
            Assert.That(EngineAudioMath.Speed01(10f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void EngineRpm_IdlesAtStandstill_AndSweepsWithinAGear()
        {
            const float maxSpeed = 10f;
            const int gears = 5;

            Assert.That(EngineAudioMath.Rpm01(0f, maxSpeed, gears), Is.EqualTo(EngineAudioMath.IdleRpmFraction));

            var early = EngineAudioMath.Rpm01(0.5f, maxSpeed, gears);
            var late = EngineAudioMath.Rpm01(1.5f, maxSpeed, gears);
            Assert.That(late, Is.GreaterThan(early));
            Assert.That(early, Is.GreaterThanOrEqualTo(EngineAudioMath.IdleRpmFraction));
            Assert.That(late, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void EngineRpm_DropsAtTheNextGear()
        {
            const float maxSpeed = 10f;
            const int gears = 5;

            var endOfFirst = EngineAudioMath.Rpm01(1.9f, maxSpeed, gears);
            var startOfSecond = EngineAudioMath.Rpm01(2.1f, maxSpeed, gears);

            Assert.That(startOfSecond, Is.LessThan(endOfFirst));
        }

        [Test]
        public void EngineGearIndex_NeverExceedsLastGear()
        {
            Assert.That(EngineAudioMath.GearIndex(0f, 5), Is.EqualTo(0));
            Assert.That(EngineAudioMath.GearIndex(0.5f, 5), Is.EqualTo(2));
            Assert.That(EngineAudioMath.GearIndex(1f, 5), Is.EqualTo(4));
            Assert.That(EngineAudioMath.GearIndex(2f, 5), Is.EqualTo(4));
            Assert.That(EngineAudioMath.GearIndex(float.NaN, 5), Is.EqualTo(0));
        }

        [Test]
        public void EnginePitchAndVolume_MapAcrossTheRange()
        {
            Assert.That(EngineAudioMath.Pitch(0f, 0.8f, 1.8f), Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(EngineAudioMath.Pitch(1f, 0.8f, 1.8f), Is.EqualTo(1.8f).Within(1e-5f));
            Assert.That(EngineAudioMath.Pitch(2f, 0.8f, 1.8f), Is.EqualTo(1.8f).Within(1e-5f));
            Assert.That(EngineAudioMath.Volume(0.5f, 0.2f, 0.8f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(EngineAudioMath.Volume(-1f, 0.2f, 0.8f), Is.EqualTo(0.2f).Within(1e-5f));
        }

        [Test]
        public void TyreIntensity_IsSilentBelowThreshold_AndSaturates()
        {
            Assert.That(TyreAudioMath.Intensity01(0.5f, 1.2f, 6f), Is.EqualTo(0f));
            Assert.That(TyreAudioMath.Intensity01(1.2f, 1.2f, 6f), Is.EqualTo(0f));
            Assert.That(TyreAudioMath.Intensity01(3.6f, 1.2f, 6f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(TyreAudioMath.Intensity01(9f, 1.2f, 6f), Is.EqualTo(1f));
            Assert.That(TyreAudioMath.Intensity01(float.NaN, 1.2f, 6f), Is.EqualTo(0f));
        }

        [Test]
        public void TyreVolumeAndPitch_FollowIntensity()
        {
            Assert.That(TyreAudioMath.Volume(0.5f, 0.5f), Is.EqualTo(0.25f).Within(1e-5f));
            Assert.That(TyreAudioMath.Pitch(0f, 0.8f, 1.3f), Is.EqualTo(1.3f).Within(1e-5f));
            Assert.That(TyreAudioMath.Pitch(1f, 0.8f, 1.3f), Is.EqualTo(0.8f).Within(1e-5f));
        }
    }
}
