using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for GameFlow state machine, additive scene lifecycle,
    /// LoadingVeil visibility, fast retry, and clean unload without object retention (TDD §30, §35; D-070).
    /// </summary>
    public sealed class GameFlowPlayModeTests
    {
        private const string HostSceneName = "GameFlowTestHost";
        private const string RaceSceneName = "TestTrack";
        private const float TimeoutSeconds = 15f;

        private GameObject _hostRoot;
        private GameFlowManager _gameFlow;
        private SceneLifecycleController _lifecycleController;
        private LoadingVeil _loadingVeil;
        private Scene _hostScene;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SelectionContext.Clear();
            Time.timeScale = 1f;

            // Create a clean host scene to emulate persistent Bootstrap root
            _hostScene = SceneManager.CreateScene(HostSceneName + "_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_hostScene);

            _hostRoot = new GameObject("GameFlowHostRoot");
            SceneManager.MoveGameObjectToScene(_hostRoot, _hostScene);

            _loadingVeil = _hostRoot.AddComponent<LoadingVeil>();
            _lifecycleController = _hostRoot.AddComponent<SceneLifecycleController>();
            _lifecycleController.Initialize(_loadingVeil);

            _gameFlow = _hostRoot.AddComponent<GameFlowManager>();
            _gameFlow.Initialize(_lifecycleController, AppState.Boot);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            SelectionContext.Clear();
            Time.timeScale = 1f;

            // Ensure any loaded additive race scene is unloaded
            var raceScene = SceneManager.GetSceneByName(RaceSceneName);
            if (raceScene.IsValid() && raceScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(raceScene);
            }

            if (_hostRoot != null)
            {
                Object.DestroyImmediate(_hostRoot);
            }

            if (_hostScene.IsValid() && _hostScene.isLoaded)
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    var sc = SceneManager.GetSceneAt(i);
                    if (sc.handle != _hostScene.handle && sc.isLoaded)
                    {
                        SceneManager.SetActiveScene(sc);
                        break;
                    }
                }
                yield return SceneManager.UnloadSceneAsync(_hostScene);
            }
        }

        [UnityTest]
        public IEnumerator GameFlow_AdditiveSceneLifecycle_LoadActivateUnloadCycle()
        {
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Boot));

            // Navigate: Boot -> MainMenu -> Garage -> PreEvent
            Assert.That(_gameFlow.TryTransitionTo(AppState.MainMenu), Is.True);
            Assert.That(_gameFlow.TryTransitionTo(AppState.Garage), Is.True);
            Assert.That(_gameFlow.TryTransitionTo(AppState.PreEvent), Is.True);

            // Transition to Race: loads TestTrack additively
            Assert.That(_gameFlow.TryTransitionTo(AppState.Race, RaceSceneName), Is.True);
            Assert.That(_gameFlow.IsTransitioning, Is.True);

            // Wait for transition to complete
            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_gameFlow.IsTransitioning, Is.False, "Transition timed out.");
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Verify scene is loaded, activated, and tracked
            var loadedScene = SceneManager.GetSceneByName(RaceSceneName);
            Assert.That(loadedScene.IsValid() && loadedScene.isLoaded, Is.True, "Race scene must be loaded.");
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(RaceSceneName), "Loaded race scene must be active.");
            Assert.That(_lifecycleController.ActiveRaceSceneName, Is.EqualTo(RaceSceneName));

            // Transition: Race -> Results
            Assert.That(_gameFlow.TryTransitionTo(AppState.Results), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Results));
            Assert.That(loadedScene.isLoaded, Is.True, "Scene stays loaded in Results.");

            // Transition: Results -> MainMenu (triggers unload)
            Assert.That(_gameFlow.TryTransitionTo(AppState.MainMenu), Is.True);
            Assert.That(_gameFlow.IsTransitioning, Is.True);

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_gameFlow.IsTransitioning, Is.False, "Unload transition timed out.");
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.MainMenu));

            // Verify scene is unloaded and host scene is restored active
            Assert.That(!SceneManager.GetSceneByName(RaceSceneName).isLoaded, Is.True, "Race scene must be unloaded.");
            Assert.That(_lifecycleController.ActiveRaceSceneName, Is.Null);
            Assert.That(SceneManager.GetActiveScene().handle, Is.EqualTo(_hostScene.handle), "Host scene must be restored active.");
        }

        [UnityTest]
        public IEnumerator GameFlow_LoadingVeil_ShowsDuringTransition_AndHidesWhenComplete()
        {
            Assert.That(_loadingVeil.IsVisible, Is.False, "Loading veil must start hidden.");

            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);

            // Start load
            _gameFlow.TryTransitionTo(AppState.Race, RaceSceneName);
            Assert.That(_loadingVeil.IsVisible, Is.True, "Loading veil must be visible during scene load.");

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_loadingVeil.IsVisible, Is.False, "Loading veil must hide once load finishes.");
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Transition: Results -> MainMenu (unload)
            _gameFlow.TryTransitionTo(AppState.Results);
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            Assert.That(_loadingVeil.IsVisible, Is.True, "Loading veil must be visible during scene unload.");

            deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_loadingVeil.IsVisible, Is.False, "Loading veil must hide once unload finishes.");
        }

        [UnityTest]
        public IEnumerator GameFlow_FastRetry_KeepsAdditiveSceneLoaded_WhileResettingRace()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);
            _gameFlow.TryTransitionTo(AppState.Race, RaceSceneName);

            var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
            while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            var raceController = Object.FindAnyObjectByType<RaceController>();
            Assert.That(raceController, Is.Not.Null, "RaceController must be present in TestTrack.");

            // Transition to Results
            _gameFlow.TryTransitionTo(AppState.Results);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Results));

            // Request Fast Retry
            var retrySuccess = _gameFlow.RequestFastRetry();
            Assert.That(retrySuccess, Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race), "Fast retry from Results must return to Race.");

            // Verify scene remains resident without reloading
            var scene = SceneManager.GetSceneByName(RaceSceneName);
            Assert.That(scene.isLoaded, Is.True, "Fast retry must not unload the scene.");
            Assert.That(raceController.State, Is.EqualTo(RaceState.Countdown), "RaceController must be reset to Countdown.");
        }

        [UnityTest]
        public IEnumerator GameFlow_RepeatedSceneCycles_ZeroRetainedObjects()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);

            for (var cycle = 0; cycle < 3; cycle++)
            {
                // Load race scene
                _gameFlow.TryTransitionTo(AppState.Race, RaceSceneName);
                var deadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(SceneManager.GetSceneByName(RaceSceneName).isLoaded, Is.True);

                // Exit back to MainMenu (unloads scene)
                _gameFlow.TryTransitionTo(AppState.Results);
                _gameFlow.TryTransitionTo(AppState.MainMenu);
                deadline = Time.realtimeSinceStartup + TimeoutSeconds;
                while (_gameFlow.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }
                Assert.That(!SceneManager.GetSceneByName(RaceSceneName).isLoaded, Is.True);

                // Prepare next cycle
                if (cycle < 2)
                {
                    _gameFlow.TryTransitionTo(AppState.Garage);
                    _gameFlow.TryTransitionTo(AppState.PreEvent);
                }
            }

            // Verify zero retained race objects after all cycles
            var leftoverControllers = Object.FindObjectsByType<RaceController>(FindObjectsInactive.Include);
            var leftoverVehicles = Object.FindObjectsByType<VehicleController>(FindObjectsInactive.Include);

            Assert.That(leftoverControllers.Length, Is.EqualTo(0), "No RaceController must be retained in memory.");
            Assert.That(leftoverVehicles.Length, Is.EqualTo(0), "No VehicleController must be retained in memory.");
        }
    }
}
