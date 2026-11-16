using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Phase-10 Stage-4 lifecycle proof (T-10.4.3/T-10.4.4): the AI grid scene loads and unloads
    /// ten times without retaining a single scene object, and destroying a grid unsubscribes every
    /// race event so no listener leak can reach a destroyed controller.
    /// </summary>
    public sealed class Phase10AiGridLifecyclePlayModeTests
    {
        private const string HostSceneName = "Phase10AiGridTestHost";
        private const string SceneName = "Phase10AIGrid";
        private const string ScenePath = "Assets/Scenes/" + SceneName + ".unity";
        private const float LoadTimeoutSeconds = 30f;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            yield return ResetToCleanHost();
            yield return UnloadScene();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return UnloadScene();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AiGridScene_TenCycleLoadUnload_LeavesNoRetainedObjects()
        {
            for (var cycle = 1; cycle <= 10; cycle++)
            {
                yield return LoadScene();

                var scene = SceneManager.GetSceneByName(SceneName);
                Assert.That(scene.isLoaded, Is.True, "Cycle " + cycle + ": scene did not load.");

                var roots = scene.GetRootGameObjects();
                var rootNames = new List<string>(roots.Length);
                for (var i = 0; i < roots.Length; i++)
                {
                    rootNames.Add(roots[i].name);
                }

                var grid = FindSingleInScene<RaceGridController>(scene);
                Assert.That(grid, Is.Not.Null, "Cycle " + cycle + ": no RaceGridController.");
                Assert.That(grid.IsInitialized, Is.True,
                    "Cycle " + cycle + ": grid did not initialize (check grid/hold wiring).");
                Assert.That(grid.ParticipantCount, Is.EqualTo(5),
                    "Cycle " + cycle + ": expected player + four AI participants.");

                var drivers = FindAllInScene<AIDriver>(scene);
                Assert.That(drivers.Count, Is.EqualTo(4),
                    "Cycle " + cycle + ": expected exactly four kinematic AI drivers.");

                var standings = new RaceStandingEntry[5];
                Assert.That(grid.CopyStandingsTo(standings), Is.EqualTo(5),
                    "Cycle " + cycle + ": standings must expose all five participants.");
                Assert.That(standings[0].Rank, Is.EqualTo(1));
                Assert.That(standings[4].Rank, Is.EqualTo(5));

                yield return UnloadScene();
                yield return null;

                Assert.That(SceneManager.GetSceneByName(SceneName).isLoaded, Is.False,
                    "Cycle " + cycle + ": scene did not unload.");
                Assert.That(Object.FindAnyObjectByType<RaceGridController>(), Is.Null,
                    "Cycle " + cycle + ": retained a RaceGridController.");
                Assert.That(Object.FindAnyObjectByType<AIDriver>(), Is.Null,
                    "Cycle " + cycle + ": retained an AIDriver.");
                Assert.That(Object.FindAnyObjectByType<RaceController>(), Is.Null,
                    "Cycle " + cycle + ": retained a RaceController.");
                Assert.That(Object.FindAnyObjectByType<EnvironmentSceneContext>(), Is.Null,
                    "Cycle " + cycle + ": retained an EnvironmentSceneContext.");

                for (var i = 0; i < rootNames.Count; i++)
                {
                    Assert.That(GameObject.Find(rootNames[i]), Is.Null,
                        "Cycle " + cycle + ": retained root '" + rootNames[i] + "'.");
                }
            }
        }

        [UnityTest]
        public IEnumerator DestroyingTheGrid_UnsubscribesEveryRaceEvent()
        {
            yield return LoadScene();

            var scene = SceneManager.GetSceneByName(SceneName);
            var grid = FindSingleInScene<RaceGridController>(scene);
            var race = FindSingleInScene<RaceController>(scene);
            Assert.That(grid, Is.Not.Null);
            Assert.That(race, Is.Not.Null);

            Object.Destroy(grid.gameObject);
            yield return null;
            Assert.That(grid == null, Is.True, "The grid object must be destroyed.");

            // If OnDestroy had left any handler subscribed, Retried would call into the destroyed
            // grid and its destroyed opponents, producing a MissingReferenceException.
            race.Retry();
            race.Retry();
            Assert.That(race.State, Is.EqualTo(RaceState.Countdown),
                "The race must remain usable after the grid is torn down.");

            yield return null;
        }

        private static IEnumerator LoadScene()
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
                var scene = SceneManager.GetSceneByName(SceneName);
                var grid = scene.isLoaded ? FindSingleInScene<RaceGridController>(scene) : null;
                if (scene.isLoaded && grid != null && grid.IsInitialized)
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail(SceneName + " did not load or initialize within " + LoadTimeoutSeconds + " s.");
        }

        private static IEnumerator UnloadScene()
        {
            var scene = SceneManager.GetSceneByName(SceneName);
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

                if (scene.name.StartsWith("InitTestScene", System.StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }

        private static T FindSingleInScene<T>(Scene scene) where T : Component
        {
            var all = FindAllInScene<T>(scene);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<T> FindAllInScene<T>(Scene scene) where T : Component
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
