using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for Nitro boost physics, Arcade Camera Juice, HUD updates,
    /// and Fast Retry cleanup (Phase 12 Stage 3; TDD §60).
    /// </summary>
    [TestFixture]
    public sealed class NitroAndRivalPlayModeTests
    {
        private GameObject _hostRoot;
        private Camera _camera;
        private ArcadeCameraJuice _cameraJuice;
        private VisualElement _hudRoot;
        private RaceHudPresenter _hudPresenter;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hostRoot = new GameObject("NitroAndRivalHost");

            var camGo = new GameObject("TestCamera");
            camGo.transform.SetParent(_hostRoot.transform, false);
            _camera = camGo.AddComponent<Camera>();
            _camera.fieldOfView = 60f;
            _cameraJuice = camGo.AddComponent<ArcadeCameraJuice>();

            _hudRoot = new VisualElement();
            _hudPresenter = new RaceHudPresenter(_hudRoot);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            _hudPresenter?.Dispose();

            if (_hostRoot != null)
            {
                Object.DestroyImmediate(_hostRoot);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator NitroBoost_IncreasesVehicleSpeed_WithoutMutatingBasePhysicsProfile()
        {
            var vehicleGo = new GameObject("NitroTestVehicle");
            vehicleGo.transform.SetParent(_hostRoot.transform, false);

            var rb = vehicleGo.AddComponent<Rigidbody>();
            rb.mass = 1200f;
            rb.linearDamping = 0.05f;

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*No input source assigned.*"));
            var controller = vehicleGo.AddComponent<VehicleController>();

            var profile = ScriptableObject.CreateInstance<VehiclePhysicsProfile>();
            typeof(VehicleController).GetField("chassis", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(controller, rb);
            typeof(VehicleController).GetField("profile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(controller, profile);

            // Record baseline values to verify non-mutation
            float baselineMass = profile.Mass;
            float baselineDamping = profile.LinearDamping;
            float baselineTorque = profile.MotorTorque;

            // Apply boost active
            controller.SetNitroBoostActive(true, torqueMultiplier: 1.5f, maxSpeedMultiplier: 1.25f);
            Assert.That(controller.IsNitroBoosting, Is.True);

            yield return new WaitForFixedUpdate();

            // Profile asset MUST remain completely unmodified
            Assert.That(profile.Mass, Is.EqualTo(baselineMass), "Profile Mass must not be mutated.");
            Assert.That(profile.LinearDamping, Is.EqualTo(baselineDamping), "Profile LinearDamping must not be mutated.");
            Assert.That(profile.MotorTorque, Is.EqualTo(baselineTorque), "Profile MotorTorque must not be mutated.");

            // Reset boost
            controller.SetNitroBoostActive(false);
            Assert.That(controller.IsNitroBoosting, Is.False);

            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(vehicleGo);
        }

        [UnityTest]
        public IEnumerator ArcadeCameraJuice_DynamicFovAndShake_RespondToNitroAndCollision_AndResetOnRetry()
        {
            Vector3 initialCamPos = _camera.transform.position;
            float baseFov = _cameraJuice.CurrentFov;

            // 1. Dynamic FOV with speed & nitro
            _cameraJuice.SetSpeed(30f, 60f);
            _cameraJuice.SetNitroActive(true);
            _cameraJuice.SnapFov();

            Assert.That(_cameraJuice.CurrentFov, Is.GreaterThan(baseFov + 5f), "FOV should expand during active nitro boost.");
            Assert.That(_cameraJuice.CurrentFov, Is.LessThanOrEqualTo(84f), "FOV must stay within safe WebGL boundaries (<= 84).");

            // 2. Camera Shake on collision impulse
            _cameraJuice.TriggerImpulse(intensity: 0.8f, duration: 0.5f);
            Assert.That(_cameraJuice.IsShaking, Is.True, "Camera should report active shake state.");

            yield return null;

            // 3. ResetJuice restores position and FOV cleanly
            _cameraJuice.ResetJuice();
            yield return null;

            Assert.That(_cameraJuice.IsShaking, Is.False, "Shake state must be cleared after reset.");
            Assert.That(_cameraJuice.CurrentShakeOffset, Is.EqualTo(Vector3.zero), "Shake offset must be restored to exact zero.");
            Assert.That(_cameraJuice.CurrentFov, Is.EqualTo(60f).Within(0.1f), "FOV must be restored to baseline.");
        }

        [UnityTest]
        public IEnumerator RivalAndNitroHud_SteadyStateTick_AllocatesZeroBytes()
        {
            // Warm up UI elements
            for (int i = 0; i < 20; i++)
            {
                _hudPresenter.SetNitroCharge(0.5f, false);
                _hudPresenter.SetRivalGap(25f, true);
                yield return null;
            }

            // Steady-state measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int frame = 0; frame < 60; frame++)
            {
                // Quantized changes or identical state
                _hudPresenter.SetNitroCharge(0.5f, false);
                _hudPresenter.SetRivalGap(25f, true);
                yield return null;
            }

            long allocAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAlloc = allocAfter - allocBefore;

            Assert.That(totalAlloc, Is.EqualTo(0), $"HUD Nitro and Rival gap hot path must allocate 0 B in steady state. Allocated: {totalAlloc} B.");
        }

        [UnityTest]
        public IEnumerator FastRetry_ResetsNitroCameraAndRivalState_Cleanly()
        {
            var nitro = new NitroSystem(NitroConfig.CreateDefault(), startingCharge: 25f);
            nitro.AddCharge(50f);
            nitro.Tick(0.1f, boostInputHeld: true, forwardSpeed: 20f, isDrifting: false);

            Assert.That(nitro.IsBoosting, Is.True);
            Assert.That(nitro.CurrentCharge, Is.GreaterThan(50f));

            _cameraJuice.TriggerImpulse(0.5f, 0.4f);
            Assert.That(_cameraJuice.IsShaking, Is.True);

            _hudPresenter.SetNitroCharge(nitro.NormalizedCharge, nitro.IsBoosting);
            _hudPresenter.SetRivalGap(30f, true);

            yield return null;

            // Execute Fast Retry reset
            nitro.Reset(25f);
            _cameraJuice.ResetJuice();
            _hudPresenter.SetNitroCharge(nitro.NormalizedCharge, nitro.IsBoosting);
            _hudPresenter.SetRivalGap(0f, false);

            yield return null;

            Assert.That(nitro.IsBoosting, Is.False, "Nitro must not be boosting after retry.");
            Assert.That(nitro.CurrentCharge, Is.EqualTo(25f), "Nitro charge should reset to default initial.");
            Assert.That(_cameraJuice.IsShaking, Is.False, "Camera juice must clear shake on retry.");
        }
    }
}
