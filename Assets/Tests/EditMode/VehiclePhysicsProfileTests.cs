using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the T5.2.2 prototype tuning template: default placeholders validate,
    /// the authored asset validates, and every field rejects out-of-range or non-finite values
    /// with a field-specific diagnostic.
    /// </summary>
    public sealed class VehiclePhysicsProfileTests
    {
        private const string AuthoredProfilePath = "Assets/Data/PrototypeVehicleProfile.asset";

        private VehiclePhysicsProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<VehiclePhysicsProfile>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_profile);
        }

        [Test]
        public void DefaultPlaceholderValues_AreValid()
        {
            Assert.That(_profile.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void AuthoredPrototypeAsset_ExistsAndValidates()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(AuthoredProfilePath);

            Assert.That(asset, Is.Not.Null, "Missing prototype profile asset at " + AuthoredProfilePath + ".");
            Assert.That(asset.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void AuthoredPrototypeAsset_ExposesPackagedResponseValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(AuthoredProfilePath);
            Assert.That(asset, Is.Not.Null);

            // Phase 6 (T-6.1) promoted CandidateA_Grip values into the prototype default after the
            // owner-accepted driving-feel playtest (OQ-02 remains the owning question for further tuning).
            Assert.That(asset.BrakeBiasFront, Is.EqualTo(0.55f).Within(0.0001f));
            Assert.That(asset.ForwardFrictionStiffness, Is.EqualTo(1.9f).Within(0.0001f));
            Assert.That(asset.SidewaysFrictionStiffness, Is.EqualTo(1.95f).Within(0.0001f));
            Assert.That(asset.EngineTorqueCurve, Is.Not.Null);
            Assert.That(asset.EngineTorqueCurve.Evaluate(0f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(asset.EngineTorqueCurve.Evaluate(60f), Is.EqualTo(0.55f).Within(0.001f));
            Assert.That(asset.SteeringSpeedSensitivity, Is.Not.Null);
            Assert.That(asset.SteeringSpeedSensitivity.Evaluate(0f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(asset.SteeringSpeedSensitivity.Evaluate(60f), Is.EqualTo(0.62f).Within(0.001f));
        }

        [Test]
        public void Mass_Zero_IsRejected()
        {
            SetFloat("mass", 0f);

            AssertInvalid("Mass");
        }

        [Test]
        public void Mass_NaN_IsRejected()
        {
            SetFloat("mass", float.NaN);

            AssertInvalid("Mass");
        }

        [Test]
        public void WheelRadius_Zero_IsRejected()
        {
            SetFloat("wheelRadius", 0f);

            AssertInvalid("WheelRadius");
        }

        [Test]
        public void WheelMass_Zero_IsRejected()
        {
            SetFloat("wheelMass", 0f);

            AssertInvalid("WheelMass");
        }

        [Test]
        public void SuspensionDistance_Zero_IsRejected()
        {
            SetFloat("suspensionDistance", 0f);

            AssertInvalid("SuspensionDistance");
        }

        [Test]
        public void SuspensionSpring_Zero_IsRejected()
        {
            SetFloat("suspensionSpring", 0f);

            AssertInvalid("SuspensionSpring");
        }

        [Test]
        public void SuspensionDamper_Negative_IsRejected()
        {
            SetFloat("suspensionDamper", -1f);

            AssertInvalid("SuspensionDamper");
        }

        [Test]
        public void SuspensionTargetPosition_OutsideUnitRange_IsRejected()
        {
            SetFloat("suspensionTargetPosition", 1.1f);

            AssertInvalid("SuspensionTargetPosition");
        }

        [Test]
        public void ForwardFrictionStiffness_Zero_IsRejected()
        {
            SetFloat("forwardFrictionStiffness", 0f);

            AssertInvalid("ForwardFrictionStiffness");
        }

        [Test]
        public void SidewaysFrictionStiffness_Zero_IsRejected()
        {
            SetFloat("sidewaysFrictionStiffness", 0f);

            AssertInvalid("SidewaysFrictionStiffness");
        }

        [Test]
        public void MotorTorque_Negative_IsRejected()
        {
            SetFloat("motorTorque", -1f);

            AssertInvalid("MotorTorque");
        }

        [Test]
        public void BrakeTorque_Zero_IsRejected()
        {
            SetFloat("brakeTorque", 0f);

            AssertInvalid("BrakeTorque");
        }

        [Test]
        public void HandbrakeTorque_Negative_IsRejected()
        {
            SetFloat("handbrakeTorque", -1f);

            AssertInvalid("HandbrakeTorque");
        }

        [Test]
        public void SteeringAngle_Zero_IsRejected()
        {
            SetFloat("steeringAngle", 0f);

            AssertInvalid("SteeringAngle");
        }

        [Test]
        public void SteeringAngle_AboveNinety_IsRejected()
        {
            SetFloat("steeringAngle", 91f);

            AssertInvalid("SteeringAngle");
        }

        [Test]
        public void BrakeBiasFront_Negative_IsRejected()
        {
            SetFloat("brakeBiasFront", -0.1f);

            AssertInvalid("BrakeBiasFront");
        }

        [Test]
        public void BrakeBiasFront_AboveOne_IsRejected()
        {
            SetFloat("brakeBiasFront", 1.1f);

            AssertInvalid("BrakeBiasFront");
        }

        [Test]
        public void EngineTorqueCurve_Null_IsRejected()
        {
            SetCurve("engineTorqueCurve", null);

            AssertInvalid("EngineTorqueCurve");
        }

        [Test]
        public void EngineTorqueCurve_Empty_IsRejected()
        {
            SetCurve("engineTorqueCurve", new AnimationCurve());

            AssertInvalid("EngineTorqueCurve");
        }

        [Test]
        public void EngineTorqueCurve_NonFiniteKeyTime_IsRejected()
        {
            SetCurve("engineTorqueCurve", new AnimationCurve(
                new Keyframe(float.PositiveInfinity, 1f),
                new Keyframe(60f, 0.5f)));

            AssertInvalid("EngineTorqueCurve");
        }

        [Test]
        public void EngineTorqueCurve_NegativeValue_IsRejected()
        {
            SetCurve("engineTorqueCurve", AnimationCurve.Linear(0f, -0.1f, 60f, 0.5f));

            AssertInvalid("EngineTorqueCurve");
        }

        [Test]
        public void SteeringSpeedSensitivity_AboveOne_IsRejected()
        {
            SetCurve("steeringSpeedSensitivity", AnimationCurve.Linear(0f, 1.2f, 60f, 0.45f));

            AssertInvalid("SteeringSpeedSensitivity");
        }

        [Test]
        public void CenterOfMass_NaN_IsRejected()
        {
            var serialized = new SerializedObject(_profile);
            serialized.FindProperty("centerOfMassOffset").vector3Value = new Vector3(float.NaN, 0f, 0f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Assert.That(_profile.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain("CenterOfMassOffset"));
        }

        [Test]
        public void Values_AreExposedThroughReadOnlyProperties()
        {
            Assert.That(_profile.Mass, Is.GreaterThan(0f));
            Assert.That(_profile.WheelRadius, Is.GreaterThan(0f));
            Assert.That(_profile.SuspensionDistance, Is.GreaterThan(0f));
            Assert.That(_profile.SuspensionSpring, Is.GreaterThan(0f));
            Assert.That(_profile.MotorTorque, Is.GreaterThan(0f));
            Assert.That(_profile.BrakeTorque, Is.GreaterThan(0f));
            Assert.That(_profile.HandbrakeTorque, Is.GreaterThan(0f));
            Assert.That(_profile.SteeringAngle, Is.GreaterThan(0f));
            Assert.That(_profile.BrakeBiasFront, Is.InRange(0f, 1f));
            Assert.That(_profile.EngineTorqueCurve, Is.Not.Null);
            Assert.That(_profile.SteeringSpeedSensitivity, Is.Not.Null);
        }

        private void SetFloat(string fieldName, float value)
        {
            var serialized = new SerializedObject(_profile);
            serialized.FindProperty(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void SetCurve(string fieldName, AnimationCurve curve)
        {
            var field = typeof(VehiclePhysicsProfile).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(field, Is.Not.Null, "Missing serialized field " + fieldName + ".");
            field.SetValue(_profile, curve);
        }

        private void AssertInvalid(string fieldName)
        {
            Assert.That(_profile.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain(fieldName));
        }
    }
}
