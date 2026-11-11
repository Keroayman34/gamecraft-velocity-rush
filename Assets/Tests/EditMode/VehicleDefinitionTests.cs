using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Schema-integrity tests for <see cref="VehicleDefinition"/>. Every test builds transient
    /// assets in memory, so the suite never depends on owner-authored roster content (OQ-14).
    /// </summary>
    public sealed class VehicleDefinitionTests
    {
        private const string ArabicName = "ستارتر ون";

        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        [Test]
        public void DefaultInstance_FailsForMissingIdentity()
        {
            var definition = ScriptableObject.CreateInstance<VehicleDefinition>();
            _created.Add(definition);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("VehicleId", error);
        }

        [Test]
        public void ValidDefinition_PassesValidation()
        {
            var definition = CreateDefinition();

            Assert.That(definition.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void DefaultSchemaVersion_IsCurrent()
        {
            var definition = CreateDefinition();

            Assert.That(definition.SchemaVersion, Is.EqualTo(VehicleDefinition.CurrentSchemaVersion));
        }

        [Test]
        public void UnsupportedSchemaVersion_FailsLoudly()
        {
            var definition = CreateDefinition();
            SetInt(definition, "schemaVersion", VehicleDefinition.CurrentSchemaVersion + 1);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("SchemaVersion", error);
        }

        [TestCase("starter-01")]
        [TestCase("sport-2")]
        [TestCase("drift-03")]
        [TestCase("supercar-10")]
        [TestCase("a")]
        public void ValidVehicleId_Passes(string id)
        {
            var definition = CreateDefinition(id: id);

            Assert.That(definition.TryValidate(out var error), Is.True, error);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase("Starter-01")]
        [TestCase("starter_01")]
        [TestCase("starter 01")]
        [TestCase("-starter")]
        [TestCase("starter-")]
        [TestCase("starter--01")]
        [TestCase("1starter")]
        [TestCase("starter.01")]
        public void InvalidVehicleId_Fails(string id)
        {
            var definition = CreateDefinition(id: id);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("VehicleId", error);
        }

        [Test]
        public void VehicleIdAtMaximumLength_Passes()
        {
            var id = "a" + new string('b', VehicleDefinition.MaxVehicleIdLength - 1);
            var definition = CreateDefinition(id: id);

            Assert.That(definition.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void VehicleIdOverMaximumLength_Fails()
        {
            var id = "a" + new string('b', VehicleDefinition.MaxVehicleIdLength);
            var definition = CreateDefinition(id: id);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("VehicleId", error);
        }

        [TestCase("", ArabicName, "DisplayNameEn")]
        [TestCase("Starter One", "", "DisplayNameAr")]
        [TestCase("   ", ArabicName, "DisplayNameEn")]
        [TestCase("Starter One", "   ", "DisplayNameAr")]
        [TestCase(" Starter One", ArabicName, "DisplayNameEn")]
        [TestCase("Starter One", ArabicName + " ", "DisplayNameAr")]
        public void MissingOrUntrimmedLocalizedName_Fails(string english, string arabic, string expectedField)
        {
            var definition = CreateDefinition(displayNameEn: english, displayNameAr: arabic);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains(expectedField, error);
        }

        [Test]
        public void DisplayNameOverMaximumLength_Fails()
        {
            var definition = CreateDefinition(
                displayNameEn: new string('A', VehicleDefinition.MaxDisplayNameLength + 1));

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("DisplayNameEn", error);
        }

        [Test]
        public void ControlCharacterInDisplayName_Fails()
        {
            var definition = CreateDefinition(displayNameEn: "Starter\nOne");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("control", error);
        }

        [Test]
        public void NullPhysicsProfile_Fails()
        {
            var definition = CreateDefinition(includePhysicsProfile: false);

            SetString(definition, "vehicleId", "starter-01");
            SetString(definition, "displayNameEn", "Starter One");
            SetString(definition, "displayNameAr", ArabicName);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("PhysicsProfile", error);
        }

        [Test]
        public void InvalidPhysicsProfile_Fails()
        {
            var profile = CreatePhysicsProfile();
            SetFloat(profile, "mass", 0f);
            var definition = CreateDefinition(physicsProfile: profile);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("PhysicsProfile", error);
        }

        [Test]
        public void UndefinedVehicleClass_Fails()
        {
            var definition = CreateDefinition();
            SetInt(definition, "vehicleClass", 99);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("VehicleClass", error);
        }

        private VehicleDefinition CreateDefinition(
            string id = "starter-01",
            string displayNameEn = "Starter One",
            string displayNameAr = ArabicName,
            VehicleClass vehicleClass = VehicleClass.Starter,
            VehiclePhysicsProfile physicsProfile = null,
            bool includePhysicsProfile = true)
        {
            var definition = ScriptableObject.CreateInstance<VehicleDefinition>();
            _created.Add(definition);

            var profile = physicsProfile;
            if (includePhysicsProfile && profile == null)
            {
                profile = CreatePhysicsProfile();
            }

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("vehicleId").stringValue = id;
            serialized.FindProperty("displayNameEn").stringValue = displayNameEn;
            serialized.FindProperty("displayNameAr").stringValue = displayNameAr;
            serialized.FindProperty("vehicleClass").intValue = (int)vehicleClass;
            serialized.FindProperty("physicsProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private VehiclePhysicsProfile CreatePhysicsProfile()
        {
            var profile = ScriptableObject.CreateInstance<VehiclePhysicsProfile>();
            _created.Add(profile);
            return profile;
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
