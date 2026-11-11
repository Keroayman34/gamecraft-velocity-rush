using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-2 acceptance tests over the generated Phase-8 roster assets. Unlike the synthetic
    /// schema tests, these assert the actual project content: ten valid definitions, the
    /// 2/3/2/3 distribution, and exact parity with the two owner-approved tuning baselines.
    /// </summary>
    public sealed class VehicleRosterAssetTests
    {
        private const string GripBaselinePath = "Assets/Data/Tuning/CandidateA_Grip_Vehicle.asset";
        private const string LooseBaselinePath = "Assets/Data/Tuning/CandidateB_Loose_Vehicle.asset";
        private const string VelocityGtPath =
            "Assets/Data/Vehicles/Sport/sport-velocity-gt/sport-velocity-gt_Physics.asset";
        private const string SidewinderPath =
            "Assets/Data/Vehicles/Drift/drift-sidewinder/drift-sidewinder_Physics.asset";

        [Test]
        public void ProjectRoster_HasTenValidDefinitions()
        {
            var definitions = LoadProjectRoster();

            Assert.That(definitions, Has.Count.EqualTo(VehicleRosterRules.RequiredVehicleCount));
            var issues = VehicleRosterRules.ValidateRoster(definitions);
            Assert.That(issues, Is.Empty, string.Join("\n", issues));

            foreach (var definition in definitions)
            {
                Assert.That(definition.PhysicsProfile, Is.Not.Null,
                    definition.VehicleId + " must reference a physics profile.");
                Assert.That(definition.PhysicsProfile.TryValidate(out var profileError), Is.True,
                    definition.VehicleId + ": " + profileError);
            }
        }

        [Test]
        public void ProjectRoster_MatchesClassDistribution()
        {
            var definitions = LoadProjectRoster();
            var counts = new Dictionary<VehicleClass, int>
            {
                { VehicleClass.Starter, 0 },
                { VehicleClass.Sport, 0 },
                { VehicleClass.Drift, 0 },
                { VehicleClass.Supercar, 0 }
            };

            foreach (var definition in definitions)
            {
                counts[definition.Class]++;
            }

            Assert.That(counts[VehicleClass.Starter], Is.EqualTo(2));
            Assert.That(counts[VehicleClass.Sport], Is.EqualTo(3));
            Assert.That(counts[VehicleClass.Drift], Is.EqualTo(2));
            Assert.That(counts[VehicleClass.Supercar], Is.EqualTo(3));
        }

        [Test]
        public void VelocityGt_ExactlyMatchesCandidateAGripBaseline()
        {
            var expected = LoadProfile(GripBaselinePath);
            var actual = LoadProfile(VelocityGtPath);

            AssertProfilesMatch(expected, actual);
        }

        [Test]
        public void Sidewinder_ExactlyMatchesCandidateBLooseBaseline()
        {
            var expected = LoadProfile(LooseBaselinePath);
            var actual = LoadProfile(SidewinderPath);

            AssertProfilesMatch(expected, actual);
        }

        private static List<VehicleDefinition> LoadProjectRoster()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(System.StringComparer.Ordinal);

            var definitions = new List<VehicleDefinition>(paths.Count);
            foreach (var path in paths)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(path);
                Assert.That(definition, Is.Not.Null, "Could not load " + path);
                definitions.Add(definition);
            }

            return definitions;
        }

        private static VehiclePhysicsProfile LoadProfile(string path)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(path);
            Assert.That(profile, Is.Not.Null, "Could not load " + path);
            return profile;
        }

        private static void AssertProfilesMatch(VehiclePhysicsProfile expected, VehiclePhysicsProfile actual)
        {
            Assert.That(actual.Mass, Is.EqualTo(expected.Mass).Within(0.0001f), "Mass");
            Assert.That(actual.CenterOfMassOffset, Is.EqualTo(expected.CenterOfMassOffset), "CenterOfMassOffset");
            Assert.That(actual.LinearDamping, Is.EqualTo(expected.LinearDamping).Within(0.0001f), "LinearDamping");
            Assert.That(actual.AngularDamping, Is.EqualTo(expected.AngularDamping).Within(0.0001f), "AngularDamping");
            Assert.That(actual.WheelRadius, Is.EqualTo(expected.WheelRadius).Within(0.0001f), "WheelRadius");
            Assert.That(actual.WheelMass, Is.EqualTo(expected.WheelMass).Within(0.0001f), "WheelMass");
            Assert.That(actual.SuspensionDistance, Is.EqualTo(expected.SuspensionDistance).Within(0.0001f),
                "SuspensionDistance");
            Assert.That(actual.SuspensionSpring, Is.EqualTo(expected.SuspensionSpring).Within(0.0001f),
                "SuspensionSpring");
            Assert.That(actual.SuspensionDamper, Is.EqualTo(expected.SuspensionDamper).Within(0.0001f),
                "SuspensionDamper");
            Assert.That(actual.SuspensionTargetPosition,
                Is.EqualTo(expected.SuspensionTargetPosition).Within(0.0001f), "SuspensionTargetPosition");
            Assert.That(actual.ForwardFrictionStiffness,
                Is.EqualTo(expected.ForwardFrictionStiffness).Within(0.0001f), "ForwardFrictionStiffness");
            Assert.That(actual.SidewaysFrictionStiffness,
                Is.EqualTo(expected.SidewaysFrictionStiffness).Within(0.0001f), "SidewaysFrictionStiffness");
            Assert.That(actual.MotorTorque, Is.EqualTo(expected.MotorTorque).Within(0.0001f), "MotorTorque");
            Assert.That(actual.BrakeTorque, Is.EqualTo(expected.BrakeTorque).Within(0.0001f), "BrakeTorque");
            Assert.That(actual.HandbrakeTorque, Is.EqualTo(expected.HandbrakeTorque).Within(0.0001f),
                "HandbrakeTorque");
            Assert.That(actual.SteeringAngle, Is.EqualTo(expected.SteeringAngle).Within(0.0001f), "SteeringAngle");
            Assert.That(actual.BrakeBiasFront, Is.EqualTo(expected.BrakeBiasFront).Within(0.0001f), "BrakeBiasFront");

            AssertCurveMatches(expected.EngineTorqueCurve, actual.EngineTorqueCurve, "EngineTorqueCurve");
            AssertCurveMatches(expected.SteeringSpeedSensitivity, actual.SteeringSpeedSensitivity,
                "SteeringSpeedSensitivity");
        }

        private static void AssertCurveMatches(AnimationCurve expected, AnimationCurve actual, string label)
        {
            Assert.That(actual.length, Is.EqualTo(expected.length), label + " key count");
            for (var time = 0f; time <= 60f; time += 5f)
            {
                Assert.That(actual.Evaluate(time), Is.EqualTo(expected.Evaluate(time)).Within(0.0001f),
                    label + " at t=" + time);
            }
        }
    }
}
