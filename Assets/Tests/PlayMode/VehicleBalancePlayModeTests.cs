using System.Collections;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// T-8.5 deterministic balance harness: drives every Phase-8 physics profile through the same
    /// scripted manoeuvre on a stripped flat bounds-check surface and asserts that no vehicle flips
    /// or stalls and that class telemetry separates (Drift sustains more slip than Sport).
    /// </summary>
    /// <remarks>
    /// The real <c>HandlingCourse</c> scene supplies the roster catalog and the canonical
    /// prototype rig; its race, camera, audio, VFX, and HUD systems are disabled for the run so
    /// only vehicle physics contributes. Each vehicle gets a fresh clone and an identical input
    /// script, so differences come from the profile rather than from timing or scene state.
    /// </remarks>
    public sealed class VehicleBalancePlayModeTests
    {
        private const float LoadTimeoutSeconds = 30f;
        private const float SettleSeconds = 1f;
        private const float StraightSeconds = 1.5f;
        private const float TurnSeconds = 2.5f;
        private const float TurnSteering = 0.7f;
        private const float GroundHeight = -200f;
        private const float MinTravelDistance = 8f;
        private const float MinFinalUpY = 0.2f;
        private const float InvertedThreshold = -0.5f;

        private GameObject _ground;
        private GameObject _template;
        private VehicleDefinition[] _roster;
        private int _straightSteps;
        private int _turnSteps;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            RestoreTimeScale();
            Time.timeScale = 2f;
            _straightSteps = Mathf.CeilToInt(StraightSeconds / Time.fixedDeltaTime);
            _turnSteps = Mathf.CeilToInt(TurnSeconds / Time.fixedDeltaTime);

            yield return ResetToCleanHost();

            if (SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(PrototypeSceneLoader.HandlingCourseSceneName);
                yield return null;
            }

            Assert.That(PrototypeSceneLoader.TryLoadHandlingCourse(out var loadError), Is.True,
                "HandlingCourse load request failed: " + loadError);

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (!SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded,
                Is.True, "HandlingCourse did not load within " + LoadTimeoutSeconds + " s.");

            yield return null;
            yield return null;

            var catalog = Object.FindAnyObjectByType<VehicleRosterCatalog>();
            Assert.That(catalog, Is.Not.Null, "HandlingCourse has no VehicleRosterCatalog.");
            _roster = catalog.Vehicles;
            Assert.That(_roster.Length, Is.EqualTo(VehicleRosterRules.RequiredVehicleCount),
                "Roster catalog must contain exactly 10 definitions.");

            var templateController = Object.FindAnyObjectByType<VehicleController>();
            Assert.That(templateController, Is.Not.Null, "HandlingCourse has no vehicle template.");
            _template = templateController.gameObject;
            _template.SetActive(false);

            DisableAmbientSceneSystems();

            _ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _ground.name = "BalanceGround";
            _ground.transform.position = new Vector3(0f, GroundHeight, 0f);
            _ground.transform.localScale = new Vector3(60f, 1f, 60f);
            _ground.GetComponent<MeshRenderer>().enabled = false;

            yield return new WaitForFixedUpdate();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            RestoreTimeScale();

            if (_ground != null)
            {
                Object.Destroy(_ground);
            }

            if (_template != null)
            {
                Object.Destroy(_template);
            }

            if (SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(PrototypeSceneLoader.HandlingCourseSceneName);
            }

            yield return null;
        }

        private VehicleRunResult _lastResult;

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator AllTenProfiles_DriveWithoutFlippingOrStalling_AndClassesSeparate()
        {
            var results = new VehicleRunResult[_roster.Length];
            var report = new StringBuilder();

            for (var i = 0; i < _roster.Length; i++)
            {
                yield return RunProfile(_roster[i]);
                results[i] = _lastResult;
                report.Append(results[i].VehicleId)
                    .Append(" class=").Append(_roster[i].Class)
                    .Append(" dist=").Append(results[i].Distance.ToString("0.0"))
                    .Append(" top=").Append(results[i].TopSpeed.ToString("0.0"))
                    .Append(" slip=").Append(results[i].AverageTurnSlip.ToString("0.000"))
                    .Append(" up=").Append(results[i].FinalUpY.ToString("0.00"))
                    .Append('\n');
            }

            Debug.Log("[VelocityRush.Balance]\n" + report);

            var driftSlip = 0f;
            var sportSlip = 0f;
            var driftCount = 0;
            var sportCount = 0;
            var starterSpeed = 0f;
            var starterCount = 0;
            var supercarSpeed = 0f;
            var supercarCount = 0;

            for (var i = 0; i < results.Length; i++)
            {
                var result = results[i];
                var definition = _roster[i];

                Assert.That(result.Distance, Is.GreaterThan(MinTravelDistance),
                    definition.VehicleId + " stalled after " + result.Distance.ToString("0.0") + " m.");
                Assert.That(result.FinalUpY, Is.GreaterThan(MinFinalUpY),
                    definition.VehicleId + " ended on its side or roof (final up.y=" +
                    result.FinalUpY.ToString("0.00") + ").");
                Assert.That(result.FullyInverted, Is.False,
                    definition.VehicleId + " inverted below up.y=" + InvertedThreshold + ".");

                switch (definition.Class)
                {
                    case VehicleClass.Drift:
                        driftSlip += result.AverageTurnSlip;
                        driftCount++;
                        break;
                    case VehicleClass.Sport:
                        sportSlip += result.AverageTurnSlip;
                        sportCount++;
                        break;
                    case VehicleClass.Starter:
                        starterSpeed += result.TopSpeed;
                        starterCount++;
                        break;
                    case VehicleClass.Supercar:
                        supercarSpeed += result.TopSpeed;
                        supercarCount++;
                        break;
                }
            }

            var driftAverage = driftSlip / Mathf.Max(1, driftCount);
            var sportAverage = sportSlip / Mathf.Max(1, sportCount);
            var starterAverageSpeed = starterSpeed / Mathf.Max(1, starterCount);
            var supercarAverageSpeed = supercarSpeed / Mathf.Max(1, supercarCount);

            Assert.That(driftAverage, Is.GreaterThan(sportAverage),
                "Drift vehicles must sustain more slip than Sport vehicles (drift=" +
                driftAverage.ToString("0.000") + ", sport=" + sportAverage.ToString("0.000") + ").");
            Assert.That(supercarAverageSpeed, Is.GreaterThan(starterAverageSpeed),
                "Supercars must reach a higher top speed than Starter vehicles (supercar=" +
                supercarAverageSpeed.ToString("0.0") + ", starter=" +
                starterAverageSpeed.ToString("0.0") + ").");
        }

        private IEnumerator RunProfile(VehicleDefinition definition)
        {
            var clone = Object.Instantiate(_template);
            clone.name = "Balance_" + definition.VehicleId;
            clone.SetActive(false);

            var controller = clone.GetComponent<VehicleController>();
            Assert.That(controller, Is.Not.Null, definition.VehicleId + " template has no VehicleController.");

            var scripted = clone.AddComponent<ScriptedVehicleInput>();
            Assert.That(controller.TrySetInputSource(scripted, out var inputError), Is.True,
                definition.VehicleId + ": " + inputError);
            Assert.That(controller.TryApplyProfile(definition.PhysicsProfile, out var profileError), Is.True,
                definition.VehicleId + ": " + profileError);

            clone.SetActive(true);
            DisableChildSystems(clone);

            var spawn = new Vector3(0f, GroundHeight + 1.1f, 0f);
            controller.ResetVehicleState(spawn, Quaternion.identity);

            var rigidbody = clone.GetComponent<Rigidbody>();
            Assert.That(rigidbody, Is.Not.Null);

            // Settle on the surface with neutral input, then run the identical script.
            scripted.Set(0f, 0f, 0f, false);
            yield return StepFixed(Mathf.CeilToInt(SettleSeconds / Time.fixedDeltaTime));

            var start = rigidbody.position;
            var topSpeed = 0f;
            var slipSum = 0f;
            var slipSamples = 0;
            var finalUpY = 1f;
            var fullyInverted = false;

            scripted.Set(1f, 0f, 0f, false);
            for (var step = 0; step < _straightSteps; step++)
            {
                yield return new WaitForFixedUpdate();
                topSpeed = Mathf.Max(topSpeed, controller.Telemetry.Speed);
                Observe(clone.transform.up.y, ref finalUpY, ref fullyInverted);
            }

            scripted.Set(1f, 0f, TurnSteering, false);
            for (var step = 0; step < _turnSteps; step++)
            {
                yield return new WaitForFixedUpdate();
                var telemetry = controller.Telemetry;
                topSpeed = Mathf.Max(topSpeed, telemetry.Speed);
                slipSum += telemetry.WheelSlip;
                slipSamples++;
                Observe(clone.transform.up.y, ref finalUpY, ref fullyInverted);
            }

            // A couple of settled samples after the scripted input returns to neutral.
            scripted.Set(0f, 0f, 0f, false);
            yield return StepFixed(10);
            Observe(clone.transform.up.y, ref finalUpY, ref fullyInverted);

            var distance = Vector3.Distance(start, rigidbody.position);
            _lastResult = new VehicleRunResult(
                definition.VehicleId,
                distance,
                topSpeed,
                slipSamples > 0 ? slipSum / slipSamples : 0f,
                finalUpY,
                fullyInverted);

            Object.Destroy(clone);
            yield return null;
        }

        private static void Observe(float upY, ref float finalUpY, ref bool fullyInverted)
        {
            finalUpY = upY;
            if (upY < InvertedThreshold)
            {
                fullyInverted = true;
            }
        }

        private static IEnumerator StepFixed(int steps)
        {
            for (var i = 0; i < steps; i++)
            {
                yield return new WaitForFixedUpdate();
            }
        }

        private void DisableAmbientSceneSystems()
        {
            DisableAll<RaceController>();
            DisableAll<SlicePauseDriver>();
            DisableAll<SliceHudPresenter>();
            DisableAll<VehicleCameraRig>();
            DisableAll<VehicleTelemetryOverlay>();
            DisableAll<Camera>();
        }

        private static void DisableAll<T>() where T : Behaviour
        {
            var behaviours = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            for (var i = 0; i < behaviours.Length; i++)
            {
                behaviours[i].enabled = false;
            }
        }

        private static void DisableChildSystems(GameObject root)
        {
            var behaviours = root.GetComponentsInChildren<Behaviour>(true);
            for (var i = 0; i < behaviours.Length; i++)
            {
                var behaviour = behaviours[i];
                if (behaviour is VehicleController || behaviour is ScriptedVehicleInput)
                {
                    continue;
                }

                // Audio/VFX/camera components on the clone are presentation-only; disabling them
                // keeps the harness free of audio-device and render dependencies.
                behaviour.enabled = false;
            }
        }

        private static IEnumerator ResetToCleanHost()
        {
            var host = SceneManager.GetSceneByName("BalanceHost");
            if (!host.isLoaded)
            {
                host = SceneManager.CreateScene("BalanceHost");
            }

            SceneManager.SetActiveScene(host);

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == host || !scene.isLoaded)
                {
                    continue;
                }

                // The test runner owns its generated InitTestScene; everything else is a
                // leftover gameplay scene from another suite.
                if (scene.name.StartsWith("InitTestScene", System.StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }

        private static void RestoreTimeScale()
        {
            Time.timeScale = 1f;
        }

        private readonly struct VehicleRunResult
        {
            public VehicleRunResult(
                string vehicleId,
                float distance,
                float topSpeed,
                float averageTurnSlip,
                float finalUpY,
                bool fullyInverted)
            {
                VehicleId = vehicleId;
                Distance = distance;
                TopSpeed = topSpeed;
                AverageTurnSlip = averageTurnSlip;
                FinalUpY = finalUpY;
                FullyInverted = fullyInverted;
            }

            public string VehicleId { get; }
            public float Distance { get; }
            public float TopSpeed { get; }
            public float AverageTurnSlip { get; }
            public float FinalUpY { get; }
            public bool FullyInverted { get; }
        }
    }

    /// <summary>Scriptable driving input for the T-8.5 balance harness.</summary>
    public sealed class ScriptedVehicleInput : MonoBehaviour, IVehicleInput
    {
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Steering { get; private set; }
        public bool Handbrake { get; private set; }

        public void Set(float throttle, float brake, float steering, bool handbrake)
        {
            Throttle = Mathf.Clamp01(throttle);
            Brake = Mathf.Clamp01(brake);
            Steering = Mathf.Clamp(steering, -1f, 1f);
            Handbrake = handbrake;
        }
    }
}
