using System.Collections;
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
    /// PlayMode proof for T-8.11: the handling course reads <see cref="SelectionContext"/> and
    /// replaces the template with the selected data-driven rig, while the no-selection path keeps
    /// the approved vertical-slice template untouched.
    /// </summary>
    public sealed class VehicleSelectionSpawnerPlayModeTests
    {
        private const string SceneName = "HandlingCourse";
        private const string ScenePath = "Assets/Scenes/HandlingCourse.unity";
        private const string HostSceneName = "SelectionSpawnerTestHost";
        private const float LoadTimeoutSeconds = 30f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SelectionContext.Clear();
            yield return ResetToCleanHost();
            yield return UnloadCourse();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SelectionContext.Clear();
            yield return UnloadCourse();
        }

        [UnityTest]
        public IEnumerator SelectedVehicle_ReplacesTheTemplate_AndRebindsTheScene()
        {
            var definition = LoadDefinition("sport-pulse-r");
            Assert.That(SelectionContext.SetSelection(definition.VehicleId), Is.True);

            yield return LoadCourse();
            yield return null;
            yield return null;

            var spawner = Object.FindAnyObjectByType<VehicleSelectionSpawner>();
            Assert.That(spawner, Is.Not.Null, "HandlingCourse has no VehicleSelectionSpawner.");
            Assert.That(spawner.SpawnedSelection, Is.True,
                "The spawner did not apply the pre-loaded SelectionContext.");
            Assert.That(spawner.SelectedVehicleId, Is.EqualTo(definition.VehicleId));

            Assert.That(GameObject.Find("PrototypeVehicle"), Is.Null,
                "The hardcoded template must be destroyed after a selected rig spawns.");

            var selectedRoot = GameObject.Find("SelectedVehicle_" + definition.VehicleId);
            Assert.That(selectedRoot, Is.Not.Null, "Selected rig root was not created.");

            var controller = selectedRoot.GetComponentInChildren<VehicleController>();
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.Profile, Is.SameAs(definition.PhysicsProfile),
                "The spawned rig must keep its own definition's physics profile.");
            Assert.That(controller.isActiveAndEnabled, Is.True,
                "The spawned VehicleController must be enabled after activation.");
            Assert.That(controller.InputSource, Is.Not.Null, "Spawned rig lost its input binding.");

            var cameraRig = Object.FindAnyObjectByType<VehicleCameraRig>();
            Assert.That(cameraRig, Is.Not.Null);
            Assert.That(cameraRig.Target, Is.EqualTo(controller.transform),
                "Camera must follow the spawned rig, not the destroyed template.");
            Assert.That(cameraRig.TelemetrySource, Is.SameAs(controller));

            var race = Object.FindAnyObjectByType<RaceController>();
            Assert.That(race, Is.Not.Null);
            Assert.That(race.RequiredCheckpointCount, Is.GreaterThan(0),
                "Race wiring must survive the participant rebound.");

            Assert.That(controller.GetComponent<VehicleVfxController>(), Is.Not.Null,
                "Selected rig must keep the slice VFX presentation.");
            Assert.That(controller.GetComponent<ImpactAudioController>(), Is.Not.Null,
                "Selected rig must keep the impact presentation.");
        }

        [UnityTest]
        public IEnumerator NoSelection_KeepsTheSliceTemplate()
        {
            SelectionContext.Clear();
            yield return LoadCourse();
            yield return null;
            yield return null;

            var spawner = Object.FindAnyObjectByType<VehicleSelectionSpawner>();
            Assert.That(spawner, Is.Not.Null);
            Assert.That(spawner.SpawnedSelection, Is.False);

            var template = GameObject.Find("PrototypeVehicle");
            Assert.That(template, Is.Not.Null, "No-selection fallback must keep the approved template.");
            Assert.That(Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Exclude).Length,
                Is.EqualTo(1), "Fallback must not leave a second vehicle in the scene.");
        }

        private static VehicleDefinition LoadDefinition(string vehicleId)
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets(
                "t:VehicleDefinition", new[] { "Assets/Data/Vehicles" });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<VehicleDefinition>(path);
                if (definition != null && definition.VehicleId == vehicleId)
                {
                    return definition;
                }
            }
#endif
            Assert.Fail("Vehicle definition '" + vehicleId + "' was not found.");
            return null;
        }

        private static IEnumerator LoadCourse()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
#endif

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetSceneByName(SceneName).isLoaded &&
                    Object.FindAnyObjectByType<VehicleSelectionSpawner>() != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("HandlingCourse did not load within " + LoadTimeoutSeconds + " s.");
        }

        private static IEnumerator UnloadCourse()
        {
            if (SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(SceneName);
                yield return null;
            }
        }

        private static IEnumerator ResetToCleanHost()
        {
            var host = SceneManager.GetSceneByName(HostSceneName);
            if (!host.isLoaded)
            {
                host = SceneManager.CreateScene(HostSceneName);
            }

            SceneManager.SetActiveScene(host);

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == host || !scene.isLoaded)
                {
                    continue;
                }

                if (scene.name.StartsWith("InitTestScene", System.StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }
    }
}
