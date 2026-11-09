using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode structural validation of the T5.2.2 vehicle prefab: one chassis Rigidbody, exactly
    /// four WheelColliders, complete/symmetric wheel bindings, separate physics/visual wheel
    /// objects, a valid placeholder profile, and no missing scripts.
    /// </summary>
    public sealed class PrototypeVehiclePrefabTests
    {
        private const string PrefabPath = "Assets/Prefabs/PrototypeVehicle.prefab";
        private const string ProfilePath = "Assets/Data/PrototypeVehicleProfile.asset";

        private static readonly string[] PhysicsWheelNames =
        {
            "Wheel FL", "Wheel FR", "Wheel RL", "Wheel RR",
        };

        private static readonly string[] VisualWheelNames =
        {
            "Wheel FL Visual", "Wheel FR Visual", "Wheel RL Visual", "Wheel RR Visual",
        };

        private GameObject _prefab;
        private VehicleController _controller;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(_prefab, Is.Not.Null, "Missing prototype vehicle prefab at " + PrefabPath + ".");

            _controller = _prefab.GetComponentsInChildren<VehicleController>(true).SingleOrDefault();
        }

        [Test]
        public void Prefab_HasOneRigidbodyAndExactlyFourWheelColliders()
        {
            Assert.That(_prefab.name, Is.EqualTo("PrototypeVehicle"));
            Assert.That(_prefab.GetComponentsInChildren<Rigidbody>(true).Length, Is.EqualTo(1));
            Assert.That(_prefab.GetComponentsInChildren<WheelCollider>(true).Length, Is.EqualTo(4));
            Assert.That(_prefab.GetComponentsInChildren<VehicleController>(true).Length, Is.EqualTo(1));
        }

        [Test]
        public void Controller_RigWiring_IsCompleteAndValid()
        {
            Assert.That(_controller, Is.Not.Null);
            Assert.That(_controller.TryValidateRig(out var error), Is.True, error);
            Assert.That(_controller.GetComponent<Rigidbody>(), Is.Not.Null);
        }

        [Test]
        public void Controller_ExposesTelemetryThroughTheCoreInterface()
        {
            Assert.That(_controller, Is.InstanceOf<IVehicleTelemetry>(),
                "Camera/HUD consumers must depend on IVehicleTelemetry, not the concrete controller.");
        }

        [Test]
        public void Controller_Profile_IsTheValidatedPlaceholderAsset()
        {
            Assert.That(_controller, Is.Not.Null);
            Assert.That(_controller.Profile, Is.Not.Null);

            var authored = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(ProfilePath);
            Assert.That(authored, Is.Not.Null);
            Assert.That(_controller.Profile, Is.SameAs(authored));
            Assert.That(_controller.Profile.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void WheelLayout_IsSymmetricAndFrontRearCorrect()
        {
            var frontLeft = FindWheel("Wheel FL");
            var frontRight = FindWheel("Wheel FR");
            var rearLeft = FindWheel("Wheel RL");
            var rearRight = FindWheel("Wheel RR");

            AssertLeftRight(frontLeft, frontRight);
            AssertLeftRight(rearLeft, rearRight);

            Assert.That(frontLeft.transform.localPosition.z, Is.GreaterThan(0f), "FL must be a front wheel.");
            Assert.That(frontRight.transform.localPosition.z, Is.GreaterThan(0f), "FR must be a front wheel.");
            Assert.That(rearLeft.transform.localPosition.z, Is.LessThan(0f), "RL must be a rear wheel.");
            Assert.That(rearRight.transform.localPosition.z, Is.LessThan(0f), "RR must be a rear wheel.");

            Assert.That(frontLeft.transform.localPosition.z, Is.EqualTo(-rearLeft.transform.localPosition.z).Within(0.01f));
            Assert.That(frontRight.transform.localPosition.z, Is.EqualTo(-rearRight.transform.localPosition.z).Within(0.01f));
            Assert.That(frontLeft.transform.localPosition.z - rearLeft.transform.localPosition.z,
                Is.GreaterThan(1f), "Wheelbase looks implausibly short.");
        }

        [Test]
        public void WheelColliders_UseProfileDimensions()
        {
            Assert.That(_controller, Is.Not.Null);
            var profile = _controller.Profile;

            foreach (var name in PhysicsWheelNames)
            {
                var wheel = FindWheel(name);
                Assert.That(wheel.radius, Is.EqualTo(profile.WheelRadius).Within(0.0001f), name + " radius drifted from profile.");
                Assert.That(wheel.suspensionDistance, Is.EqualTo(profile.SuspensionDistance).Within(0.0001f), name + " suspension distance drifted from profile.");
                Assert.That(wheel.mass, Is.EqualTo(profile.WheelMass).Within(0.0001f), name + " mass drifted from profile.");
            }
        }

        [Test]
        public void WheelVisuals_AreSeparateObjectsWithoutColliders()
        {
            foreach (var name in VisualWheelNames)
            {
                var visual = FindTransform(name);
                Assert.That(visual, Is.Not.Null, "Missing visual wheel '" + name + "'.");
                Assert.That(visual.GetComponentsInChildren<WheelCollider>(true).Length, Is.EqualTo(0));
                Assert.That(visual.GetComponentsInChildren<Collider>(true).Length, Is.EqualTo(0),
                    name + " must be visual-only; WheelColliders are raycast-based.");
                Assert.That(visual.GetComponentsInChildren<Renderer>(true).Length, Is.GreaterThanOrEqualTo(1),
                    name + " has no visible mesh.");
            }

            foreach (var name in PhysicsWheelNames)
            {
                var wheel = FindWheel(name);
                Assert.That(wheel.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(0),
                    name + " physics wheel should not render.");
            }
        }

        [Test]
        public void ChassisBody_HasBoxCollider()
        {
            var body = FindTransform("ChassisBody");
            Assert.That(body, Is.Not.Null);
            Assert.That(body.GetComponent<BoxCollider>(), Is.Not.Null);
            Assert.That(body.GetComponent<Renderer>(), Is.Not.Null);
        }

        [Test]
        public void Prefab_HasNoMissingScripts()
        {
            foreach (var component in _prefab.GetComponentsInChildren<Component>(true))
            {
                Assert.That(component, Is.Not.Null, "Missing script in prototype vehicle prefab.");
            }
        }

        private WheelCollider FindWheel(string name)
        {
            var match = _prefab.GetComponentsInChildren<WheelCollider>(true)
                .SingleOrDefault(candidate => candidate.name == name);
            Assert.That(match, Is.Not.Null, "Missing physics wheel '" + name + "'.");
            return match;
        }

        private Transform FindTransform(string name)
        {
            return _prefab.GetComponentsInChildren<Transform>(true)
                .SingleOrDefault(candidate => candidate.name == name);
        }

        private static void AssertLeftRight(WheelCollider left, WheelCollider right)
        {
            Assert.That(left.transform.localPosition.x, Is.LessThan(0f), left.name + " must be on the left.");
            Assert.That(right.transform.localPosition.x, Is.GreaterThan(0f), right.name + " must be on the right.");
            Assert.That(left.transform.localPosition.x, Is.EqualTo(-right.transform.localPosition.x).Within(0.01f));
        }
    }
}
