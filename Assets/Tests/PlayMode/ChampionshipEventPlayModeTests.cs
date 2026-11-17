using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Stage-3 runtime proof (T-10.8.6/T-10.8.7): every authored event launches in its
    /// environment/configuration scene, Circuit Races compose the approved four-AI grid, Time
    /// Trials stay solo, reverse scenes accept progress in checkpoint order, and ten load/unload
    /// cycles retain no runtime object.
    /// </summary>
    /// <remarks>
    /// The committed event scenes are loaded read-only. The runtime seam is a transient object that
    /// is destroyed with the scene; no scene asset is ever saved, so the signature and derived
    /// reverse scenes stay byte-identical to the reviewed content (D-064).
    /// </remarks>
    public sealed class ChampionshipEventPlayModeTests
    {
        private const string HostSceneName = "ChampionshipEventTestHost";
        private const string CatalogSceneName = "ChampionshipCatalog";
        private const string CatalogScenePath = "Assets/Scenes/ChampionshipCatalog.unity";
        private const string AiPrefabPath = "Assets/Prefabs/Events/AIOpponent.prefab";
        private const string EnvironmentSceneFolder = "Assets/Scenes/Environments/";
        private const float LoadTimeoutSeconds = 30f;
        private const float CountdownTimeoutSeconds = 10f;

        private ChampionshipCatalog _championshipCatalog;
        private EnvironmentCatalog _environmentCatalog;
        private EventCatalog _eventCatalog;
        private GameObject _aiPrefab;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return ResetToCleanHost();
            yield return UnloadEventScenes();
            yield return UnloadCatalogScene();

            yield return LoadCatalogScene();

            _aiPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(AiPrefabPath);
            Assert.That(_aiPrefab, Is.Not.Null, "Missing the preauthored AI opponent prefab.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadEventScenes();
            yield return UnloadCatalogScene();
            yield return null;
        }

        [UnityTest]
        public IEnumerator EveryAuthoredEvent_LaunchesInItsScene()
        {
            var events = _eventCatalog.Events;
            Assert.That(events.Length, Is.EqualTo(ChampionshipContentPolicy.RequiredTotalEvents));

            for (var i = 0; i < events.Length; i++)
            {
                var definition = events[i];
                var mode = definition.EventType == RaceEventType.CircuitRace
                    ? RaceLaunchMode.Grid
                    : RaceLaunchMode.Solo;

                yield return LaunchAndAssert(definition, mode);
            }
        }

        [UnityTest]
        public IEnumerator GridEvent_FinishesAfterOrderedCheckpointsOnReverseTrack()
        {
            var definition = _eventCatalog.FindById("c2-apex-challenger-round-1");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.LayoutVariant, Is.EqualTo(EnvironmentLayoutVariant.Reverse));
            Assert.That(definition.EventType, Is.EqualTo(RaceEventType.CircuitRace));

            yield return LaunchAndAssert(definition, RaceLaunchMode.Grid, false);
            var runtime = Object.FindAnyObjectByType<RaceEventRuntime>();
            Assert.That(runtime, Is.Not.Null);

            var race = runtime.Race;
            var playerBody = runtime.Player.GetComponent<Rigidbody>();
            yield return WaitForRunning(race);

            for (var index = 0; index < EnvironmentRaceRules.CheckpointCount; index++)
            {
                race.ReportCheckpoint(index, playerBody);
                Assert.That(race.AcceptedCheckpointCount, Is.EqualTo(index + 1),
                    "Checkpoint " + index + " was not accepted in order.");
            }

            race.ReportFinish(playerBody);
            Assert.That(race.State, Is.EqualTo(RaceState.Finished));
            Assert.That(race.Result.HasValue, Is.True, "The player finish must produce a result.");
        }

        [UnityTest]
        public IEnumerator SoloTimeTrial_FinishesAfterOrderedCheckpointsOnReverseTrack()
        {
            var definition = _eventCatalog.FindById("c1-ignition-series-round-3");
            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.LayoutVariant, Is.EqualTo(EnvironmentLayoutVariant.Reverse));
            Assert.That(definition.EventType, Is.EqualTo(RaceEventType.TimeTrial));

            yield return LaunchAndAssert(definition, RaceLaunchMode.Solo, false);
            var runtime = Object.FindAnyObjectByType<RaceEventRuntime>();
            Assert.That(runtime, Is.Not.Null);
            Assert.That(runtime.OpponentCount, Is.EqualTo(0));
            Assert.That(runtime.Grid, Is.Null);

            var race = runtime.Race;
            var playerBody = runtime.Player.GetComponent<Rigidbody>();
            yield return WaitForRunning(race);

            for (var index = 0; index < EnvironmentRaceRules.CheckpointCount; index++)
            {
                race.ReportCheckpoint(index, playerBody);
                Assert.That(race.AcceptedCheckpointCount, Is.EqualTo(index + 1));
            }

            race.ReportFinish(playerBody);
            Assert.That(race.State, Is.EqualTo(RaceState.Finished));
            Assert.That(race.Result.HasValue, Is.True);
        }

        [UnityTest]
        public IEnumerator ReverseEventScene_TenCycleLoadUnload_LeavesNoRetainedObjects()
        {
            var definition = _eventCatalog.FindById("c2-apex-challenger-round-1");
            Assert.That(definition, Is.Not.Null);
            var scenePath = GetScenePath(definition);

            for (var cycle = 1; cycle <= 10; cycle++)
            {
                yield return LoadEventScene(scenePath);
                var scene = SceneManager.GetSceneByPath(scenePath);
                var race = FindSingle<RaceController>(scene);
                var player = FindSingle<VehicleController>(scene);
                var runtime = CreateRuntime(definition, race, player);

                Assert.That(runtime.IsLaunched, Is.True, "Cycle " + cycle + ": launch failed.");
                Assert.That(runtime.OpponentCount, Is.EqualTo(
                    ChampionshipContentPolicy.CircuitRaceAiCount),
                    "Cycle " + cycle + ": expected four AI opponents.");

                Object.Destroy(runtime.gameObject);
                yield return null;
                yield return UnloadEventScene(scenePath);
                yield return null;

                Assert.That(Object.FindAnyObjectByType<RaceEventRuntime>(), Is.Null,
                    "Cycle " + cycle + ": retained a RaceEventRuntime.");
                Assert.That(Object.FindAnyObjectByType<RaceGridController>(), Is.Null,
                    "Cycle " + cycle + ": retained a RaceGridController.");
                Assert.That(Object.FindAnyObjectByType<AIDriver>(), Is.Null,
                    "Cycle " + cycle + ": retained an AIDriver.");
                Assert.That(Object.FindAnyObjectByType<RaceController>(), Is.Null,
                    "Cycle " + cycle + ": retained a RaceController.");
                Assert.That(Object.FindAnyObjectByType<EnvironmentSceneContext>(), Is.Null,
                    "Cycle " + cycle + ": retained an EnvironmentSceneContext.");
            }
        }

        private IEnumerator LaunchAndAssert(
            RaceEventDefinition definition,
            RaceLaunchMode mode,
            bool cleanup = true)
        {
            var scenePath = GetScenePath(definition);
            yield return LoadEventScene(scenePath);

            var scene = SceneManager.GetSceneByPath(scenePath);
            var race = FindSingle<RaceController>(scene);
            var player = FindSingle<VehicleController>(scene);
            Assert.That(race, Is.Not.Null, definition.EventId + ": missing RaceController.");
            Assert.That(player, Is.Not.Null, definition.EventId + ": missing player VehicleController.");

            var runtime = CreateRuntime(definition, race, player);
            Assert.That(runtime.IsLaunched, Is.True, definition.EventId + ": launch failed.");
            Assert.That(runtime.Variant, Is.EqualTo(definition.LayoutVariant));
            Assert.That(runtime.LaunchMode, Is.EqualTo(mode));
            Assert.That(
                runtime.TryVerifyGateOrder(out var gateError),
                Is.True,
                definition.EventId + ": " + gateError);

            if (mode == RaceLaunchMode.Grid)
            {
                Assert.That(runtime.OpponentCount, Is.EqualTo(
                    ChampionshipContentPolicy.CircuitRaceAiCount));
                Assert.That(runtime.Grid, Is.Not.Null);
                Assert.That(runtime.Grid.IsInitialized, Is.True);
                Assert.That(runtime.Grid.ParticipantCount, Is.EqualTo(
                    1 + ChampionshipContentPolicy.CircuitRaceAiCount));
                Assert.That(FindAll<AIDriver>(scene).Count, Is.EqualTo(
                    ChampionshipContentPolicy.CircuitRaceAiCount));
            }
            else
            {
                Assert.That(runtime.OpponentCount, Is.EqualTo(0));
                Assert.That(runtime.Grid, Is.Null);
                Assert.That(FindAll<AIDriver>(scene).Count, Is.EqualTo(0));
            }

            if (!cleanup)
            {
                yield break;
            }

            Object.Destroy(runtime.gameObject);
            yield return null;
            yield return UnloadEventScene(scenePath);
            yield return null;
        }

        private RaceEventRuntime CreateRuntime(
            RaceEventDefinition definition,
            RaceController race,
            VehicleController player)
        {
            Assert.That(
                EventLaunchPlanBuilder.TryBuild(
                    definition.EventId,
                    _eventCatalog,
                    _championshipCatalog,
                    _environmentCatalog,
                    out var plan,
                    out var error),
                Is.True,
                definition.EventId + ": " + error);

            var scene = race.gameObject.scene;
            var runtimeObject = new GameObject("Race Event Runtime");
            SceneManager.MoveGameObjectToScene(runtimeObject, scene);
            var runtime = runtimeObject.AddComponent<RaceEventRuntime>();

            var mode = definition.EventType == RaceEventType.CircuitRace
                ? RaceLaunchMode.Grid
                : RaceLaunchMode.Solo;
            Assert.That(
                runtime.TryInitialize(plan, race, player, _aiPrefab, mode),
                Is.True,
                definition.EventId + ": runtime initialization failed.");
            return runtime;
        }

        private static IEnumerator WaitForRunning(RaceController race)
        {
            var deadline = Time.realtimeSinceStartup + CountdownTimeoutSeconds;
            while (race.State == RaceState.Countdown && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(race.State, Is.EqualTo(RaceState.Running),
                "The race did not leave the countdown within " + CountdownTimeoutSeconds + " s.");
        }

        private IEnumerator LoadCatalogScene()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                CatalogScenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var scene = SceneManager.GetSceneByPath(CatalogScenePath);
                if (scene.isLoaded)
                {
                    _championshipCatalog = FindSingle<ChampionshipCatalog>(scene);
                    _environmentCatalog = FindSingle<EnvironmentCatalog>(scene);
                    _eventCatalog = FindSingle<EventCatalog>(scene);
                    if (_championshipCatalog != null &&
                        _environmentCatalog != null &&
                        _eventCatalog != null)
                    {
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail("ChampionshipCatalog scene did not expose its three catalog components.");
        }

        private static IEnumerator LoadEventScene(string scenePath)
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                scenePath,
                new LoadSceneParameters(LoadSceneMode.Additive));

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                var scene = SceneManager.GetSceneByPath(scenePath);
                if (scene.isLoaded && FindSingle<RaceController>(scene) != null)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(scenePath + " did not load within " + LoadTimeoutSeconds + " s.");
        }

        private static IEnumerator UnloadEventScene(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
                yield return null;
            }
        }

        private static IEnumerator UnloadEventScenes()
        {
            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded &&
                    scene.path.StartsWith(EnvironmentSceneFolder, StringComparison.Ordinal))
                {
                    yield return SceneManager.UnloadSceneAsync(scene);
                }
            }

            yield return null;
        }

        private static IEnumerator UnloadCatalogScene()
        {
            var scene = SceneManager.GetSceneByName(CatalogSceneName);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
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

                if (scene.name.StartsWith("InitTestScene", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }

        private static string GetScenePath(RaceEventDefinition definition)
        {
            var variant = definition.LayoutVariant == EnvironmentLayoutVariant.Reverse
                ? "reverse"
                : "signature";
            return EnvironmentSceneFolder +
                   EnvironmentThemeRules.GetStableFragment(definition.EnvironmentTheme) + "-" +
                   variant + ".unity";
        }

        private static T FindSingle<T>(Scene scene) where T : Component
        {
            var all = FindAll<T>(scene);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<T> FindAll<T>(Scene scene) where T : Component
        {
            var results = new List<T>();
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results;
        }
    }
}
