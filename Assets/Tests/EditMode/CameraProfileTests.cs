using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the T5.3 placeholder camera template: default values validate, the
    /// authored asset validates, and every field rejects out-of-range or non-finite values with a
    /// field-specific diagnostic.
    /// </summary>
    public sealed class CameraProfileTests
    {
        private const string AuthoredProfilePath = "Assets/Data/PrototypeCameraProfile.asset";

        private CameraProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = ScriptableObject.CreateInstance<CameraProfile>();
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
            var asset = AssetDatabase.LoadAssetAtPath<CameraProfile>(AuthoredProfilePath);

            Assert.That(asset, Is.Not.Null, "Missing prototype camera profile at " + AuthoredProfilePath + ".");
            Assert.That(asset.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void FollowDistance_Zero_IsRejected()
        {
            SetFloat("followDistance", 0f);

            AssertInvalid("FollowDistance");
        }

        [Test]
        public void FollowHeight_Negative_IsRejected()
        {
            SetFloat("followHeight", -1f);

            AssertInvalid("FollowHeight");
        }

        [Test]
        public void PositionSmoothing_Negative_IsRejected()
        {
            SetFloat("positionSmoothing", -0.1f);

            AssertInvalid("PositionSmoothing");
        }

        [Test]
        public void RotationSmoothing_Negative_IsRejected()
        {
            SetFloat("rotationSmoothing", -0.1f);

            AssertInvalid("RotationSmoothing");
        }

        [Test]
        public void LookAheadPerSpeed_Negative_IsRejected()
        {
            SetFloat("lookAheadPerSpeed", -0.1f);

            AssertInvalid("LookAheadPerSpeed");
        }

        [Test]
        public void FieldOfViewAtRest_OutsideOpenRange_IsRejected()
        {
            SetFloat("fieldOfViewAtRest", 1f);
            AssertInvalid("FieldOfViewAtRest");

            SetFloat("fieldOfViewAtRest", 179f);
            AssertInvalid("FieldOfViewAtRest");
        }

        [Test]
        public void FieldOfViewAtTopSpeed_BelowRest_IsRejected()
        {
            SetFloat("fieldOfViewAtRest", 60f);
            SetFloat("fieldOfViewAtTopSpeed", 50f);

            AssertInvalid("FieldOfViewAtTopSpeed");
        }

        [Test]
        public void FieldOfViewSpeedReference_Zero_IsRejected()
        {
            SetFloat("fieldOfViewSpeedReference", 0f);

            AssertInvalid("FieldOfViewSpeedReference");
        }

        [Test]
        public void AuthoredPrototypeAsset_ExposesPackagedFieldOfViewValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<CameraProfile>(AuthoredProfilePath);
            Assert.That(asset, Is.Not.Null);

            Assert.That(asset.FieldOfViewAtRest, Is.EqualTo(60f).Within(0.001f));
            Assert.That(asset.FieldOfViewAtTopSpeed, Is.EqualTo(72f).Within(0.001f));
            Assert.That(asset.FieldOfViewSpeedReference, Is.EqualTo(60f).Within(0.001f));
        }

        [Test]
        public void CollisionRadius_Zero_IsRejected()
        {
            SetFloat("collisionRadius", 0f);

            AssertInvalid("CollisionRadius");
        }

        [Test]
        public void CollisionPadding_Negative_IsRejected()
        {
            SetFloat("collisionPadding", -0.1f);

            AssertInvalid("CollisionPadding");
        }

        [Test]
        public void MinimumDistance_Zero_IsRejected()
        {
            SetFloat("minimumDistance", 0f);

            AssertInvalid("MinimumDistance");
        }

        [Test]
        public void MinimumDistance_LargerThanFollowDistance_IsRejected()
        {
            SetFloat("followDistance", 3f);
            SetFloat("minimumDistance", 4f);

            AssertInvalid("MinimumDistance");
        }

        [Test]
        public void FollowDistance_NaN_IsRejected()
        {
            SetFloat("followDistance", float.NaN);

            AssertInvalid("FollowDistance");
        }

        [Test]
        public void Values_AreExposedThroughReadOnlyProperties()
        {
            Assert.That(_profile.FollowDistance, Is.GreaterThan(0f));
            Assert.That(_profile.FollowHeight, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_profile.PositionSmoothing, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_profile.RotationSmoothing, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_profile.LookAheadPerSpeed, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_profile.FieldOfViewAtRest, Is.GreaterThan(1f).And.LessThan(179f));
            Assert.That(_profile.FieldOfViewAtTopSpeed, Is.GreaterThanOrEqualTo(_profile.FieldOfViewAtRest));
            Assert.That(_profile.FieldOfViewSpeedReference, Is.GreaterThan(0f));
            Assert.That(_profile.CollisionRadius, Is.GreaterThan(0f));
            Assert.That(_profile.CollisionPadding, Is.GreaterThanOrEqualTo(0f));
            Assert.That(_profile.MinimumDistance, Is.GreaterThan(0f));
        }

        private void SetFloat(string fieldName, float value)
        {
            var serialized = new SerializedObject(_profile);
            serialized.FindProperty(fieldName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void AssertInvalid(string fieldName)
        {
            Assert.That(_profile.TryValidate(out var error), Is.False);
            Assert.That(error, Does.Contain(fieldName));
        }
    }
}
