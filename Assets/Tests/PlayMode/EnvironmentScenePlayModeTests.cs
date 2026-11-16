using System.Collections;
using System.Collections.Generic;
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
    /// PlayMode proof for T-9.11–T-9.13: every generated environment scene loads additively with
    /// the complete approved race stack wired, and unloads without leaving scene objects behind.
    /// </summary>
    /// <remarks>
    /// The scenes are opened one at a time. Loading two at once would create duplicate cameras and
    /// AudioListeners, which is not a supported runtime configuration and would mask wiring defects.
    /// </remarks>
    public sealed class EnvironmentScenePlayModeTests
    {
        private const string HostSceneName = "EnvironmentSceneTestHost";
        private const float LoadTimeoutSeconds = 30f;

        private static readonly string[] EnvironmentIds =
        {
            "desert-signature",
            "coastal-signature",
            "mountain-pass-signature",
            "neon-city-signature",
            "world-arena-signature"
        };

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SelectionContext.Clear();
            Time.timeScale = 1f;
            yield return ResetToCleanHost();
            yield return UnloadAllEnvironments();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SelectionContext.Clear();
            Time.timeScale = 1f;
            yield return UnloadAllEnvironments();
        }

        [UnityTest]
        public IEnumerator EveryEnvironmentScene_LoadsWithTheCompleteRaceStack()
        {
            for (var i = 0; i < EnvironmentIds.Length; i++)
            {
                var id = EnvironmentIds[i];
                yield return LoadEnvironment(id);

                var scene = SceneManager.GetSceneByName(id);
                Assert.That(scene.isLoaded, Is.True, id + " did not load.");

                var roots = scene.GetRootGameObjects();
                var context = FindSingle<EnvironmentSceneContext>(roots);
                Assert.That(context, Is.Not.Null, id + " has no EnvironmentSceneContext.");
                Assert.That(context.Definition, Is.Not.Null, id + " context lost its definition.");
                Assert.That(context.Definition.EnvironmentId, Is.EqualTo(id));
                Assert.That(context.TryValidate(out var contextError), Is.True, id + ": " + contextError);

                var race = FindSingle<RaceController>(roots);
                Assert.That(race, Is.Not.Null, id + " has no RaceController.");
                Assert.That(
                    race.RequiredCheckpointCount,
                    Is.EqualTo(EnvironmentRaceRules.CheckpointCount),
                    id + " checkpoint count");

                var checkpoints = FindAll<CheckpointTrigger>(roots);
                Assert.That(
                    checkpoints.Count,
                    Is.EqualTo(EnvironmentRaceRules.CheckpointCount),
                    id + " checkpoint gates");
                for (var checkpointIndex = 0; checkpointIndex < checkpoints.Count; checkpointIndex++)
                {
                    Assert.That(checkpoints[checkpointIndex].Index, Is.EqualTo(checkpointIndex));
                }

                Assert.That(FindSingle<FinishTrigger>(roots), Is.Not.Null, id + " has no FinishTrigger.");
                Assert.That(FindSingle<SlicePauseDriver>(roots), Is.Not.Null, id + " has no pause driver.");

                var camera = FindSingle<Camera>(roots);
                Assert.That(camera, Is.Not.Null, id + " has no camera.");
                Assert.That(FindSingle<AudioListener>(roots), Is.Not.Null, id + " has no AudioListener.");
                var cameraRig = FindSingle<VehicleCameraRig>(roots);
                Assert.That(cameraRig, Is.Not.Null, id + " has no VehicleCameraRig.");
                Assert.That(cameraRig.Target, Is.Not.Null, id + " camera rig lost its target.");
                Assert.That(cameraRig.Profile, Is.Not.Null, id + " camera rig lost its profile.");
                Assert.That(cameraRig.TelemetrySource, Is.Not.Null, id + " camera rig lost telemetry.");

                Assert.That(FindSingle<SliceHudPresenter>(roots), Is.Not.Null, id + " has no HUD presenter.");
                Assert.That(FindSingle<UnityEngine.UIElements.UIDocument>(roots), Is.Not.Null,
                    id + " has no UIDocument.");
                Assert.That(FindSingle<VehicleTelemetryOverlay>(roots), Is.Not.Null,
                    id + " has no telemetry overlay.");
                Assert.That(FindSingle<PrototypePerformanceProbe>(roots), Is.Not.Null,
                    id + " has no performance probe.");

                Assert.That(FindSingle<EngineAudioController>(roots), Is.Not.Null, id + " has no engine audio.");
                Assert.That(FindSingle<TyreAudioController>(roots), Is.Not.Null, id + " has no tyre audio.");
                Assert.That(FindSingle<ImpactAudioController>(roots), Is.Not.Null, id + " has no impact audio.");
                Assert.That(FindSingle<SliceUiAudioController>(roots), Is.Not.Null, id + " has no UI audio.");

                Assert.That(FindSingle<VehicleVfxController>(roots), Is.Not.Null, id + " has no VFX.");
                Assert.That(FindSingle<VehicleSelectionSpawner>(roots), Is.Not.Null, id + " has no spawner.");
                Assert.That(FindSingle<VehicleRosterCatalog>(roots), Is.Not.Null, id + " has no roster catalog.");
                Assert.That(FindSingle<VehicleRigCatalog>(roots), Is.Not.Null, id + " has no rig catalog.");
                Assert.That(FindSingle<VehicleController>(roots), Is.Not.Null, id + " has no vehicle.");
                Assert.That(FindSingle<Light>(roots), Is.Not.Null, id + " has no directional light.");

                yield return UnloadEnvironment(id);
                Assert.That(SceneManager.GetSceneByName(id).isLoaded, Is.False, id + " did not unload.");
            }
        }

        [UnityTest]
        public IEnumerator EnvironmentScene_UnloadRemovesEverySceneObject()
        {
            const string id = "desert-signature";
            yield return LoadEnvironment(id);

            var roots = SceneManager.GetSceneByName(id).GetRootGameObjects();
            Assert.That(roots.Length, Is.GreaterThan(0));
            var rootNames = new List<string>(roots.Length);
            for (var i = 0; i < roots.Length; i++)
            {
                rootNames.Add(roots[i].name);
            }

            yield return UnloadEnvironment(id);
            yield return null;

            Assert.That(SceneManager.GetSceneByName(id).isLoaded, Is.False);
            Assert.That(Object.FindAnyObjectByType<EnvironmentSceneContext>(), Is.Null,
                "EnvironmentSceneContext survived unload.");
            Assert.That(Object.FindAnyObjectByType<VehicleSelectionSpawner>(), Is.Null,
                "VehicleSelectionSpawner survived unload.");
            for (var i = 0; i < rootNames.Count; i++)
            {
                Assert.That(GameObject.Find(rootNames[i]), Is.Null,
                    "Scene root '" + rootNames[i] + "' survived unload.");
            }
        }

        [UnityTest]
        public IEnumerator TenCycleLoadUnload_LeavesNoRetainedSceneObjects()
        {
            const int cycles = 10;
            for (var cycle = 1; cycle <= cycles; cycle++)
            {
                for (var i = 0; i < EnvironmentIds.Length; i++)
                {
                    var id = EnvironmentIds[i];
                    yield return LoadEnvironment(id);

                    var scene = SceneManager.GetSceneByName(id);
                    Assert.That(scene.isLoaded, Is.True, "Cycle " + cycle + ": " + id + " did not load.");
                    var roots = scene.GetRootGameObjects();
                    Assert.That(roots.Length, Is.GreaterThan(0), "Cycle " + cycle + ": " + id + " has no roots.");

                    var rootNames = new List<string>(roots.Length);
                    for (var rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    {
                        rootNames.Add(roots[rootIndex].name);
                    }

                    yield return UnloadEnvironment(id);
                    yield return null;

                    Assert.That(
                        SceneManager.GetSceneByName(id).isLoaded,
                        Is.False,
                        "Cycle " + cycle + ": " + id + " did not unload.");
                    Assert.That(
                        Object.FindAnyObjectByType<EnvironmentSceneContext>(),
                        Is.Null,
                        "Cycle " + cycle + ": " + id + " retained an EnvironmentSceneContext.");
                    for (var rootIndex = 0; rootIndex < rootNames.Count; rootIndex++)
                    {
                        Assert.That(
                            GameObject.Find(rootNames[rootIndex]),
                            Is.Null,
                            "Cycle " + cycle + ": " + id + " retained root '" + rootNames[rootIndex] + "'.");
                    }
                }
            }
        }

        private static IEnumerator LoadEnvironment(string id)
        {
            var path = "Assets/Scenes/Environments/" + id + ".unity";
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                path, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(id, LoadSceneMode.Additive);
#endif

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                if (SceneManager.GetSceneByName(id).isLoaded &&
                    Object.FindAnyObjectByType<VehicleSelectionSpawner>() != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(id + " did not load within " + LoadTimeoutSeconds + " s.");
        }

        private static IEnumerator UnloadEnvironment(string id)
        {
            if (SceneManager.GetSceneByName(id).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(id);
                yield return null;
            }
        }

        private static IEnumerator UnloadAllEnvironments()
        {
            for (var i = 0; i < EnvironmentIds.Length; i++)
            {
                yield return UnloadEnvironment(EnvironmentIds[i]);
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

        private static T FindSingle<T>(GameObject[] roots) where T : Component
        {
            var matches = FindAll<T>(roots);
            return matches.Count > 0 ? matches[0] : null;
        }

        private static List<T> FindAll<T>(GameObject[] roots) where T : Component
        {
            var results = new List<T>();
            for (var i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results;
        }
    }
}
