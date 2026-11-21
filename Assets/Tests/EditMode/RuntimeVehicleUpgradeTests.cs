using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class RuntimeVehicleUpgradeTests
    {
        private GameObject _holder;
        private VehicleController _controller;
        private VehiclePhysicsProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestVehicle_Upgrades");
            var rb = _holder.AddComponent<Rigidbody>();

            // Create temporary in-memory ScriptableObject profile
            _profile = ScriptableObject.CreateInstance<VehiclePhysicsProfile>();

            _controller = _holder.AddComponent<VehicleController>();

            // Create mock wheels
            var flCol = CreateWheelCollider("FL");
            var frCol = CreateWheelCollider("FR");
            var rlCol = CreateWheelCollider("RL");
            var rrCol = CreateWheelCollider("RR");

            var flVis = new GameObject("FL_Vis").transform;
            flVis.parent = _holder.transform;
            var frVis = new GameObject("FR_Vis").transform;
            frVis.parent = _holder.transform;
            var rlVis = new GameObject("RL_Vis").transform;
            rlVis.parent = _holder.transform;
            var rrVis = new GameObject("RR_Vis").transform;
            rrVis.parent = _holder.transform;

            SetBinding("frontLeft", flCol, flVis);
            SetBinding("frontRight", frCol, frVis);
            SetBinding("rearLeft", rlCol, rlVis);
            SetBinding("rearRight", rrCol, rrVis);

            SetField("chassis", rb);
            SetField("profile", _profile);

            var dummyInput = _holder.AddComponent<DummyInputAdapter>();
            SetField("inputSource", dummyInput);

            _controller.TryApplyProfile(_profile, out _);
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null) Object.DestroyImmediate(_holder);
            if (_profile != null) Object.DestroyImmediate(_profile);
        }

        [Test]
        public void ApplyUpgradeModifiers_AltersRigParameters_WithoutMutatingBaseProfileAsset()
        {
            float baseMotorTorque = _profile.MotorTorque;
            float baseBrakeTorque = _profile.BrakeTorque;
            float baseForwardStiffness = _profile.ForwardFrictionStiffness;
            float baseSuspensionSpring = _profile.SuspensionSpring;

            // Tier 3 engine (+9%), tires (+9%), brakes (+12%), suspension (+9%)
            var upgradeState = new VehicleUpgradeState(
                engineTier: 3,
                tiresTier: 3,
                brakesTier: 3,
                suspensionTier: 3,
                transmissionTier: 3,
                aeroTier: 3);

            var modifiers = UpgradeCalculator.ComputeModifiers(upgradeState);

            Assert.That(modifiers.TorqueMultiplier, Is.GreaterThan(1f));
            Assert.That(modifiers.FrictionMultiplier, Is.GreaterThan(1f));
            Assert.That(modifiers.BrakeTorqueMultiplier, Is.GreaterThan(1f));

            _controller.ApplyUpgradeModifiers(modifiers);

            Assert.That(_controller.UpgradeModifiers, Is.EqualTo(modifiers));

            // CRITICAL Non-mutation check: base profile fields MUST remain unchanged
            Assert.That(_profile.MotorTorque, Is.EqualTo(baseMotorTorque));
            Assert.That(_profile.BrakeTorque, Is.EqualTo(baseBrakeTorque));
            Assert.That(_profile.ForwardFrictionStiffness, Is.EqualTo(baseForwardStiffness));
            Assert.That(_profile.SuspensionSpring, Is.EqualTo(baseSuspensionSpring));
        }

        private WheelCollider CreateWheelCollider(string name)
        {
            var go = new GameObject(name);
            go.transform.parent = _holder.transform;
            return go.AddComponent<WheelCollider>();
        }

        private void SetBinding(string fieldName, WheelCollider col, Transform vis)
        {
            var field = typeof(VehicleController).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                var binding = new WheelBinding();
                SetSubField(binding, "collider", col);
                SetSubField(binding, "visual", vis);
                field.SetValue(_controller, binding);
            }
        }

        private void SetSubField(object obj, string name, object value)
        {
            var field = obj.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private void SetField(string name, object value)
        {
            var field = typeof(VehicleController).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_controller, value);
        }

        private class DummyInputAdapter : MonoBehaviour, Core.IVehicleInput
        {
            public float Throttle => 1f;
            public float Brake => 0f;
            public float Steering => 0f;
            public bool Handbrake => false;
            public void ResetInput() { }
        }
    }
}
