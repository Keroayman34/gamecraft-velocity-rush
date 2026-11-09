using NUnit.Framework;
using UnityEngine;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the pure T5.3 camera rules: frame-rate-independent smoothing,
    /// speed-scaled look-ahead, and collision distance clamping/padding.
    /// </summary>
    public sealed class CameraFollowMathTests
    {
        [Test]
        public void SmoothingFactor_PausedTimeFreezes()
        {
            Assert.That(CameraFollowMath.SmoothingFactor(0.2f, 0f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.SmoothingFactor(0.2f, -0.1f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.SmoothingFactor(0.2f, float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void SmoothingFactor_ZeroSmoothingSnaps()
        {
            Assert.That(CameraFollowMath.SmoothingFactor(0f, 0.016f), Is.EqualTo(1f));
        }

        [Test]
        public void SmoothingFactor_IsInsideUnitRangeAndFrameRateIndependent()
        {
            var atSixty = CameraFollowMath.SmoothingFactor(0.2f, 1f / 60f);
            var atThirty = CameraFollowMath.SmoothingFactor(0.2f, 1f / 30f);

            Assert.That(atSixty, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(atThirty, Is.GreaterThan(atSixty), "Longer steps must blend further toward the target.");
            Assert.That(atThirty, Is.LessThan(1f));
        }

        [Test]
        public void LookAheadOffset_ScalesWithSpeedOnlyWhenPositive()
        {
            Assert.That(CameraFollowMath.LookAheadOffset(0f, 0.15f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.LookAheadOffset(-5f, 0.15f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.LookAheadOffset(20f, 0.15f), Is.EqualTo(3f).Within(0.0001f));
            Assert.That(CameraFollowMath.LookAheadOffset(float.NaN, 0.15f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.LookAheadOffset(float.PositiveInfinity, 0.15f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.LookAheadOffset(20f, 0f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.LookAheadOffset(20f, -1f), Is.EqualTo(0f));
        }

        [Test]
        public void CollisionLimitedDistance_NoHitKeepsDesiredDistance()
        {
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, float.PositiveInfinity, 0.2f, 2f),
                Is.EqualTo(7f));
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, float.NaN, 0.2f, 2f),
                Is.EqualTo(7f));
        }

        [Test]
        public void CollisionLimitedDistance_SubtractsPaddingFromHit()
        {
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, 5f, 0.2f, 2f),
                Is.EqualTo(4.8f).Within(0.0001f));
        }

        [Test]
        public void CollisionLimitedDistance_ClampsToMinimumDistance()
        {
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, 0.5f, 0.2f, 2f),
                Is.EqualTo(2f));
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, 0f, 0f, 2f),
                Is.EqualTo(2f));
        }

        [Test]
        public void CollisionLimitedDistance_NeverExceedsDesiredDistance()
        {
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(3f, 10f, 0.2f, 2f),
                Is.EqualTo(3f));
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(1f, 0.5f, 0f, 2f),
                Is.EqualTo(1f));
        }

        [Test]
        public void CollisionLimitedDistance_NegativePaddingAndMinimumAreSafe()
        {
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, 5f, -1f, 2f),
                Is.EqualTo(5f));
            Assert.That(
                CameraFollowMath.CollisionLimitedDistance(7f, 5f, 0.2f, -1f),
                Is.EqualTo(4.8f).Within(0.0001f));
        }

        [Test]
        public void CollisionLimitedDistance_ZeroOrInvalidDesiredReturnsZero()
        {
            Assert.That(CameraFollowMath.CollisionLimitedDistance(0f, 5f, 0.2f, 2f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.CollisionLimitedDistance(-1f, 5f, 0.2f, 2f), Is.EqualTo(0f));
            Assert.That(CameraFollowMath.CollisionLimitedDistance(float.NaN, 5f, 0.2f, 2f), Is.EqualTo(0f));
        }

        [Test]
        public void FieldOfView_RampsLinearlyFromRestToTopSpeed()
        {
            Assert.That(CameraFollowMath.FieldOfView(0f, 60f, 72f, 60f), Is.EqualTo(60f).Within(0.0001f));
            Assert.That(CameraFollowMath.FieldOfView(30f, 60f, 72f, 60f), Is.EqualTo(66f).Within(0.0001f));
            Assert.That(CameraFollowMath.FieldOfView(60f, 60f, 72f, 60f), Is.EqualTo(72f).Within(0.0001f));
            Assert.That(CameraFollowMath.FieldOfView(120f, 60f, 72f, 60f), Is.EqualTo(72f).Within(0.0001f));
        }

        [Test]
        public void FieldOfView_InvalidInputsFallBackToRest()
        {
            Assert.That(CameraFollowMath.FieldOfView(float.NaN, 60f, 72f, 60f), Is.EqualTo(60f));
            Assert.That(CameraFollowMath.FieldOfView(float.PositiveInfinity, 60f, 72f, 60f), Is.EqualTo(60f));
            Assert.That(CameraFollowMath.FieldOfView(-1f, 60f, 72f, 60f), Is.EqualTo(60f));
            Assert.That(CameraFollowMath.FieldOfView(30f, 60f, 72f, 0f), Is.EqualTo(60f));
            Assert.That(CameraFollowMath.FieldOfView(30f, 60f, float.NaN, 60f), Is.EqualTo(60f));
        }

        [Test]
        public void CriticallyDampedRotation_PausedTimeFreezesOrientationAndVelocity()
        {
            var current = Quaternion.Euler(0f, 10f, 5f);
            var target = Quaternion.Euler(0f, 40f, 5f);
            var velocity = new Vector3(1f, 2f, 3f);

            var result = CameraFollowMath.CriticallyDampedRotation(current, target, ref velocity, 0.2f, 0f);

            Assert.That(Quaternion.Angle(result, current), Is.EqualTo(0f).Within(0.0001f));
            Assert.That(velocity, Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }

        [Test]
        public void CriticallyDampedRotation_ZeroSmoothingSnapsAndClearsVelocity()
        {
            var target = Quaternion.Euler(0f, 30f, 0f);
            var velocity = new Vector3(1f, 1f, 1f);

            var result = CameraFollowMath.CriticallyDampedRotation(
                Quaternion.identity,
                target,
                ref velocity,
                0f,
                1f / 60f);

            Assert.That(Quaternion.Angle(result, target), Is.LessThan(0.0001f));
            Assert.That(velocity, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CriticallyDampedRotation_MovesTowardTargetWithoutOvershoot()
        {
            var target = Quaternion.Euler(0f, 30f, 0f);
            var velocity = Vector3.zero;

            var result = CameraFollowMath.CriticallyDampedRotation(
                Quaternion.identity,
                target,
                ref velocity,
                0.15f,
                1f / 60f);

            var remaining = Quaternion.Angle(result, target);
            Assert.That(remaining, Is.GreaterThan(0f), "One step must not teleport to the target.");
            Assert.That(remaining, Is.LessThan(30f), "One step must make progress toward the target.");
            Assert.That(Quaternion.Angle(result, Quaternion.identity), Is.LessThanOrEqualTo(30f),
                "A critically damped step must not overshoot the target.");
        }

        [Test]
        public void CriticallyDampedRotation_ConvergesOverRepeatedSteps()
        {
            var target = Quaternion.Euler(0f, 30f, 0f);
            var current = Quaternion.identity;
            var velocity = Vector3.zero;

            for (var i = 0; i < 120; i++)
            {
                current = CameraFollowMath.CriticallyDampedRotation(
                    current,
                    target,
                    ref velocity,
                    0.15f,
                    1f / 60f);
            }

            Assert.That(Quaternion.Angle(current, target), Is.LessThan(0.5f));
        }
    }
}
