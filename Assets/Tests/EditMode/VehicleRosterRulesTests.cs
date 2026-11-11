using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Roster-level tests for the D-014 2/3/2/3 distribution, unique ids, and entry validity.
    /// Synthetic in-memory definitions keep the suite independent of OQ-14/OQ-06 content.
    /// </summary>
    public sealed class VehicleRosterRulesTests
    {
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
        public void RequiredDistribution_IsTwoThreeTwoThree()
        {
            Assert.That(VehicleRosterRules.RequiredVehicleCount, Is.EqualTo(10));
            Assert.That(VehicleRosterRules.GetRequiredCount(VehicleClass.Starter), Is.EqualTo(2));
            Assert.That(VehicleRosterRules.GetRequiredCount(VehicleClass.Sport), Is.EqualTo(3));
            Assert.That(VehicleRosterRules.GetRequiredCount(VehicleClass.Drift), Is.EqualTo(2));
            Assert.That(VehicleRosterRules.GetRequiredCount(VehicleClass.Supercar), Is.EqualTo(3));
        }

        [Test]
        public void CompleteRoster_PassesValidation()
        {
            var roster = CreateCompleteRoster();

            var issues = VehicleRosterRules.ValidateRoster(roster);

            Assert.That(issues, Is.Empty, Join(issues));
            Assert.That(VehicleRosterRules.TryValidateRoster(roster, out var error), Is.True, error);
        }

        [Test]
        public void IncompleteRoster_FailsTotalAndClassCounts()
        {
            var roster = CreateCompleteRoster();
            roster.RemoveAt(roster.Count - 1);

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("exactly 10", text);
            StringAssert.Contains("Supercar count must be 3", text);
        }

        [Test]
        public void WrongClassDistribution_Fails()
        {
            var roster = CreateCompleteRoster();
            SetInt(roster[2], "vehicleClass", (int)VehicleClass.Starter);

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("Starter count must be 2 (found 3)", text);
            StringAssert.Contains("Sport count must be 3 (found 2)", text);
        }

        [Test]
        public void DuplicateVehicleId_Fails()
        {
            var roster = CreateCompleteRoster();
            SetString(roster[2], "vehicleId", "starter-01");

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("Duplicate VehicleId 'starter-01'", text);
        }

        [Test]
        public void DuplicateVehicleId_IsCaseInsensitive()
        {
            var roster = CreateCompleteRoster();
            SetString(roster[2], "vehicleId", "STARTER-01");

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("Duplicate VehicleId", text);
        }

        [Test]
        public void NullEntry_Fails()
        {
            var roster = CreateCompleteRoster();
            roster[4] = null;

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("entry 4 is null", text);
        }

        [Test]
        public void InvalidEntry_FailsWithFieldDiagnostic()
        {
            var roster = CreateCompleteRoster();
            SetString(roster[0], "displayNameAr", string.Empty);

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("DisplayNameAr", text);
        }

        [Test]
        public void UndefinedClass_Fails()
        {
            var roster = CreateCompleteRoster();
            SetInt(roster[0], "vehicleClass", 99);

            var issues = VehicleRosterRules.ValidateRoster(roster);
            var text = Join(issues);

            StringAssert.Contains("VehicleClass value 99", text);
        }

        [Test]
        public void TryValidateRoster_ReturnsFirstIssue()
        {
            var roster = CreateCompleteRoster();
            SetString(roster[0], "vehicleId", string.Empty);

            Assert.That(VehicleRosterRules.TryValidateRoster(roster, out var error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void NullRoster_Fails()
        {
            var issues = VehicleRosterRules.ValidateRoster(null);

            Assert.That(issues, Has.Count.EqualTo(1));
            StringAssert.Contains("null", issues[0]);
        }

        private List<VehicleDefinition> CreateCompleteRoster()
        {
            return new List<VehicleDefinition>
            {
                CreateEntry("starter-01", VehicleClass.Starter),
                CreateEntry("starter-02", VehicleClass.Starter),
                CreateEntry("sport-01", VehicleClass.Sport),
                CreateEntry("sport-02", VehicleClass.Sport),
                CreateEntry("sport-03", VehicleClass.Sport),
                CreateEntry("drift-01", VehicleClass.Drift),
                CreateEntry("drift-02", VehicleClass.Drift),
                CreateEntry("supercar-01", VehicleClass.Supercar),
                CreateEntry("supercar-02", VehicleClass.Supercar),
                CreateEntry("supercar-03", VehicleClass.Supercar)
            };
        }

        private VehicleDefinition CreateEntry(string id, VehicleClass vehicleClass)
        {
            var profile = ScriptableObject.CreateInstance<VehiclePhysicsProfile>();
            _created.Add(profile);

            var definition = ScriptableObject.CreateInstance<VehicleDefinition>();
            _created.Add(definition);

            var serialized = new SerializedObject(definition);
            serialized.FindProperty("vehicleId").stringValue = id;
            serialized.FindProperty("displayNameEn").stringValue = "Vehicle " + id;
            serialized.FindProperty("displayNameAr").stringValue = "مركبة " + id;
            serialized.FindProperty("vehicleClass").intValue = (int)vehicleClass;
            serialized.FindProperty("physicsProfile").objectReferenceValue = profile;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static string Join(IReadOnlyList<string> issues)
        {
            return string.Join("\n", issues);
        }
    }
}
