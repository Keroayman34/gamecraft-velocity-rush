using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the pure T5.2.2 control mapping: normalized input clamping, motor/brake
    /// torque conversion, handbrake routing to rear wheels, and steering sign/clamp.
    /// </summary>
    public sealed class VehicleControlMathTests
    {
        [Test]
        public void Clamp01_MapsOutOfRangeAndNaNToNeutralOrBounds()
        {
            Assert.That(VehicleControlMath.Clamp01(-1f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.Clamp01(0.25f), Is.EqualTo(0.25f));
            Assert.That(VehicleControlMath.Clamp01(2f), Is.EqualTo(1f));
            Assert.That(VehicleControlMath.Clamp01(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void ClampSigned_MapsOutOfRangeAndNaNAndKeepsDirection()
        {
            Assert.That(VehicleControlMath.ClampSigned(-2f), Is.EqualTo(-1f));
            Assert.That(VehicleControlMath.ClampSigned(-0.5f), Is.EqualTo(-0.5f));
            Assert.That(VehicleControlMath.ClampSigned(2f), Is.EqualTo(1f));
            Assert.That(VehicleControlMath.ClampSigned(float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void MotorTorque_ScalesThrottleByMaxAndClamps()
        {
            Assert.That(VehicleControlMath.MotorTorque(0f, 1000f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.MotorTorque(0.5f, 1000f), Is.EqualTo(500f));
            Assert.That(VehicleControlMath.MotorTorque(1f, 1000f), Is.EqualTo(1000f));
            Assert.That(VehicleControlMath.MotorTorque(2f, 1000f), Is.EqualTo(1000f));
            Assert.That(VehicleControlMath.MotorTorque(-1f, 1000f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.MotorTorque(float.NaN, 1000f), Is.EqualTo(0f));
        }

        [Test]
        public void MotorTorque_NegativeMax_IsTreatedAsZero()
        {
            Assert.That(VehicleControlMath.MotorTorque(1f, -500f), Is.EqualTo(0f));
        }

        [Test]
        public void BrakeTorque_ScalesBrakeByMaxAndClamps()
        {
            Assert.That(VehicleControlMath.BrakeTorque(0f, 2000f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.BrakeTorque(0.5f, 2000f), Is.EqualTo(1000f));
            Assert.That(VehicleControlMath.BrakeTorque(1f, 2000f), Is.EqualTo(2000f));
            Assert.That(VehicleControlMath.BrakeTorque(2f, 2000f), Is.EqualTo(2000f));
        }

        [Test]
        public void HandbrakeTorque_IsZeroUnlessEngaged()
        {
            Assert.That(VehicleControlMath.HandbrakeTorque(false, 4000f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.HandbrakeTorque(true, 4000f), Is.EqualTo(4000f));
            Assert.That(VehicleControlMath.HandbrakeTorque(true, -1f), Is.EqualTo(0f));
        }

        [Test]
        public void SteeringAngle_PreservesSignAndClampsToMax()
        {
            Assert.That(VehicleControlMath.SteeringAngle(0f, 30f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.SteeringAngle(1f, 30f), Is.EqualTo(30f));
            Assert.That(VehicleControlMath.SteeringAngle(-1f, 30f), Is.EqualTo(-30f));
            Assert.That(VehicleControlMath.SteeringAngle(0.5f, 30f), Is.EqualTo(15f));
            Assert.That(VehicleControlMath.SteeringAngle(-2f, 30f), Is.EqualTo(-30f));
            Assert.That(VehicleControlMath.SteeringAngle(float.NaN, 30f), Is.EqualTo(0f));
        }

        [Test]
        public void RearBrakeTorque_UsesStrongerOfPedalAndHandbrake()
        {
            Assert.That(VehicleControlMath.RearBrakeTorque(0f, 0f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.RearBrakeTorque(1000f, 0f), Is.EqualTo(1000f));
            Assert.That(VehicleControlMath.RearBrakeTorque(0f, 4000f), Is.EqualTo(4000f));
            Assert.That(VehicleControlMath.RearBrakeTorque(1000f, 4000f), Is.EqualTo(4000f));
            Assert.That(VehicleControlMath.RearBrakeTorque(4000f, 1000f), Is.EqualTo(4000f));
        }

        [Test]
        public void MotorTorque_AppliesEngineMultiplierWithinCap()
        {
            Assert.That(VehicleControlMath.MotorTorque(0.5f, 1000f, 0.55f), Is.EqualTo(275f).Within(0.0001f));
            Assert.That(VehicleControlMath.MotorTorque(1f, 1000f, 0.55f), Is.EqualTo(550f).Within(0.0001f));
            Assert.That(VehicleControlMath.MotorTorque(1f, 1000f, 2f), Is.EqualTo(1000f),
                "A multiplier above 1 must not exceed the profile torque cap.");
            Assert.That(VehicleControlMath.MotorTorque(1f, 1000f, -1f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.MotorTorque(1f, 1000f, float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void SteeringAngle_AppliesSpeedSensitivityWithinMaxAngle()
        {
            Assert.That(VehicleControlMath.SteeringAngle(1f, 30f, 0.45f), Is.EqualTo(13.5f).Within(0.0001f));
            Assert.That(VehicleControlMath.SteeringAngle(-1f, 30f, 0.45f), Is.EqualTo(-13.5f).Within(0.0001f));
            Assert.That(VehicleControlMath.SteeringAngle(1f, 30f, 2f), Is.EqualTo(30f),
                "Sensitivity above 1 must not exceed the profile steering angle.");
            Assert.That(VehicleControlMath.SteeringAngle(1f, 30f, -1f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.SteeringAngle(1f, 30f, float.NaN), Is.EqualTo(0f));
        }

        [Test]
        public void BrakeTorqueForAxle_DistributesBiasAndKeepsTotalStable()
        {
            const float maxPerWheel = 2000f;

            var frontHalf = VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 0.5f);
            var rearHalf = VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 0.5f);
            Assert.That(frontHalf, Is.EqualTo(2000f).Within(0.0001f));
            Assert.That(frontHalf + rearHalf, Is.EqualTo(4000f).Within(0.0001f));

            var frontBiased = VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 0.6f);
            var rearBiased = VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 0.4f);
            Assert.That(frontBiased, Is.EqualTo(2400f).Within(0.0001f));
            Assert.That(rearBiased, Is.EqualTo(1600f).Within(0.0001f));
            Assert.That(frontBiased + rearBiased, Is.EqualTo(frontHalf + rearHalf).Within(0.0001f));

            Assert.That(VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 0f), Is.EqualTo(0f));
            Assert.That(VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, 2f), Is.EqualTo(4000f),
                "An out-of-range share clamps to a full axle.");
            Assert.That(VehicleControlMath.BrakeTorqueForAxle(1f, maxPerWheel, float.NaN), Is.EqualTo(0f));
        }
    }
}
