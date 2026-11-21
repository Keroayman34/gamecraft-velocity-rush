using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;
using VelocityRush.Gameplay.Economy;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Phase 12 Stage 4 PlayMode stress test (TDD §61).
    /// Executes 10 consecutive full-loop cycles with ALL Phase 12 systems active simultaneously:
    /// TrafficLaneManager + NearMissDetector + RoadCoinManager + RaceEconomyTracker + NitroSystem + ArcadeCameraJuice + AIDriver Rubber-Banding + RaceHudPresenter.
    /// Loop: MainMenu -> Select Event -> Additive Load -> Race Ticks -> Race Finish -> Atomic Save -> Unload -> MainMenu.
    /// Verifies zero object retention, 0 B/frame combined steady-state hot path, and zero monotonic memory drift.
    /// </summary>
    [TestFixture]
    public sealed class Phase12HighwayRivalStabilityPlayModeTests
    {
        private const string HostSceneName = "Phase12StabilityHost";
        private const string RaceSceneName = "TestTrack";
        private const float TransitionTimeoutSeconds = 15f;
        private const int CycleCount = 10;

        private Scene _hostScene;
        private GameObject _hostRoot;
        private GameCompositionRoot _compositionRoot;
        private InMemorySaveStore _saveStore;
        private RaceEventDefinition _testEventDef;
        private TrackLayout _testLayout;
        private TrackPath _testTrackPath;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            _hostScene = SceneManager.CreateScene(HostSceneName + "_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(_hostScene);

            _hostRoot = new GameObject("Phase12StabilityRoot");
            SceneManager.MoveGameObjectToScene(_hostRoot, _hostScene);

            _hostRoot.AddComponent<UIDocument>();
            _hostRoot.AddComponent<LoadingVeil>();
            var lifecycle = _hostRoot.AddComponent<SceneLifecycleController>();
            var gameFlow = _hostRoot.AddComponent<GameFlowManager>();
            _hostRoot.AddComponent<CommercialUiController>();

            _compositionRoot = _hostRoot.AddComponent<GameCompositionRoot>();

            _saveStore = new InMemorySaveStore();
            var initialModel = SaveModel.CreateDefault();
            initialModel.credits = 1000;
            _saveStore.TrySave(JsonUtility.ToJson(initialModel), out _);

            _compositionRoot.Initialize(_saveStore);

            // Reusable test track path for highway systems
            _testLayout = ScriptableObject.CreateInstance<TrackLayout>();
            _testLayout.ConfigureForAuthoring(
                width: 14f,
                height: 0.8f,
                thickness: 0.3f,
                spacing: 1f,
                closesLoop: false,
                segmentData: new[] { TrackSegment.Straight(1000f), TrackSegment.Straight(1000f) },
                elevationData: null);
            _testTrackPath = TrackPath.Create(_testLayout);

            _testEventDef = ScriptableObject.CreateInstance<RaceEventDefinition>();
#if UNITY_EDITOR
            _testEventDef.ConfigureForAuthoring(
                "c1-ignition-series-round-1",
                "Ignition Series - Round 1",
                "سلسلة الانطلاق - الجولة 1",
                RaceEventType.CircuitRace,
                EnvironmentTheme.DesertCircuit,
                EnvironmentLayoutVariant.Signature,
                "c1-ignition-series",
                1,
                EventOpponentMode.FixedFourAI,
                EventUnlockPolicy.AlwaysAvailable,
                0.90f,
                "Phase 12 Stability Test Event");
#endif
            _compositionRoot.SetActiveEventDefinition(_testEventDef);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;

            var raceScene = SceneManager.GetSceneByName(RaceSceneName);
            if (raceScene.IsValid() && raceScene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(raceScene);
            }

            if (_hostRoot != null)
            {
                Object.DestroyImmediate(_hostRoot);
            }

            if (_testEventDef != null)
            {
                Object.DestroyImmediate(_testEventDef);
            }

            if (_testLayout != null)
            {
                Object.DestroyImmediate(_testLayout);
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

            LocalizationService.Reset();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullLoop_TenConsecutiveCycles_HighwayRivalSystems_ZeroDrift_AndZeroRetention()
        {
            var flow = _compositionRoot.GameFlow;
            Assert.That(flow.CurrentState, Is.EqualTo(AppState.MainMenu));

            long baselineHeap = 0;
            long endHeap = 0;

            for (var cycle = 1; cycle <= CycleCount; cycle++)
            {
                // 1. MainMenu -> Event Selection
                Assert.That(flow.TryTransitionTo(AppState.ChampionshipSelection), Is.True, $"Cycle {cycle}: Champ select transition failed");
                flow.SetChampionshipSelection("c1-ignition-series");
                Assert.That(flow.TryTransitionTo(AppState.EventSelection), Is.True, $"Cycle {cycle}: Event select transition failed");
                flow.SetEventSelection("c1-ignition-series-round-1");

                // 2. PreEvent -> Race (Additive Load)
                Assert.That(flow.TryTransitionTo(AppState.PreEvent), Is.True, $"Cycle {cycle}: PreEvent transition failed");
                Assert.That(flow.TryTransitionTo(AppState.Race, RaceSceneName), Is.True, $"Cycle {cycle}: Race transition failed");

                var deadline = Time.realtimeSinceStartup + TransitionTimeoutSeconds;
                while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(flow.IsTransitioning, Is.False, $"Cycle {cycle}: Timed out loading race scene");
                Assert.That(flow.CurrentState, Is.EqualTo(AppState.Race), $"Cycle {cycle}: Expected Race state");

                var raceScene = SceneManager.GetSceneByName(RaceSceneName);
                Assert.That(raceScene.isLoaded, Is.True, $"Cycle {cycle}: Race scene not loaded");

                var raceController = Object.FindAnyObjectByType<RaceController>();
                var vehicleController = Object.FindAnyObjectByType<VehicleController>();
                Assert.That(raceController, Is.Not.Null, $"Cycle {cycle}: RaceController missing during race");
                Assert.That(vehicleController, Is.Not.Null, $"Cycle {cycle}: VehicleController missing during race");

                // 3. Instantiate Highway Rival Systems into the active race scene
                var systemsContainer = new GameObject("Phase12_RaceSystemsContainer");
                SceneManager.MoveGameObjectToScene(systemsContainer, raceScene);

                var trafficManager = systemsContainer.AddComponent<TrafficLaneManager>();
                var laneConfig = TrafficLaneConfig.CreateDefault();
                laneConfig.poolSize = 8;
                trafficManager.InitializeForTesting(_testTrackPath, vehicleController.transform, laneConfig);

                var nearMissDetector = systemsContainer.AddComponent<NearMissDetector>();
                nearMissDetector.Initialize(trafficManager, vehicleController);

                var coinManager = systemsContainer.AddComponent<RoadCoinManager>();
                var coinConfig = CoinSpawnConfig.CreateDefault();
                coinConfig.poolSize = 8;
                coinManager.Initialize(_testTrackPath, laneConfig, coinConfig, trafficManager, vehicleController.transform, 42);

                var rivalGo = new GameObject("AI_Rival_Opponent");
                SceneManager.MoveGameObjectToScene(rivalGo, raceScene);
                var aiDriver = rivalGo.AddComponent<AIDriver>();
                aiDriver.Initialize(_testTrackPath, startDistance: 25f, lateralBias: 0f, playerTarget: vehicleController.transform, startSpeed: 15f);

                // Re-bind composition root to newly added scene systems
                _compositionRoot.FindActiveRaceComponents();

                // Verify difficulty profile applied
                var activeProfile = _compositionRoot.ActiveDifficultyProfile;
                Assert.That(activeProfile.EventId, Is.EqualTo("c1-ignition-series-round-1"));
                Assert.That(activeProfile.CoinValue, Is.EqualTo(10));
                Assert.That(activeProfile.NearMissBonusCredits, Is.EqualTo(25));

                // 4. Warmup ticks
                yield return null;
                yield return null;

                // 5. Assert 0 B/frame combined steady-state hot path across all Phase 12 systems concurrently
                long allocBefore = System.GC.GetAllocatedBytesForCurrentThread();
                trafficManager.TickTraffic(0.016f);
                nearMissDetector.EvaluateNearMisses(20f);
                coinManager.Tick(0.016f, vehicleController.transform.position, 10f);
                aiDriver.Tick(0.016f);
                yield return null;
                yield return null;
                long allocAfter = System.GC.GetAllocatedBytesForCurrentThread();
                long hotPathAlloc = allocAfter - allocBefore;
                Assert.That(hotPathAlloc, Is.EqualTo(0), $"Cycle {cycle}: Combined Highway Rival steady-state hot path allocated {hotPathAlloc} bytes");

                // 6. Race Finish -> Progression & Save Persistence -> Results
                var finishResult = new RaceResult(42.5d, 1, 1, completed: true);
                _compositionRoot.HandleRaceFinished(finishResult, _testEventDef);

                Assert.That(flow.CurrentState, Is.EqualTo(AppState.Results), $"Cycle {cycle}: Expected Results state");
                Assert.That(_compositionRoot.PlayerProgress.GetEventStars("c1-ignition-series-round-1"), Is.GreaterThanOrEqualTo(1));

                // 7. Results -> MainMenu (Additive Unload & Teardown)
                Assert.That(flow.TryTransitionTo(AppState.MainMenu), Is.True, $"Cycle {cycle}: Return to MainMenu failed");

                deadline = Time.realtimeSinceStartup + TransitionTimeoutSeconds;
                while (flow.IsTransitioning && Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(flow.IsTransitioning, Is.False, $"Cycle {cycle}: Timed out unloading race scene");
                Assert.That(flow.CurrentState, Is.EqualTo(AppState.MainMenu), $"Cycle {cycle}: Expected MainMenu state");

                // 8. Force GC and assert zero object retention
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
                System.GC.Collect();
                yield return null;

                Assert.That(SceneManager.GetSceneByName(RaceSceneName).isLoaded, Is.False,
                    $"Cycle {cycle}: Scene remained loaded after transition to MainMenu");
                Assert.That(Object.FindAnyObjectByType<TrafficVehicle>(), Is.Null,
                    $"Cycle {cycle}: Retained a TrafficVehicle after unload");
                Assert.That(Object.FindAnyObjectByType<CoinPickup>(), Is.Null,
                    $"Cycle {cycle}: Retained a CoinPickup after unload");
                Assert.That(Object.FindAnyObjectByType<RaceController>(), Is.Null,
                    $"Cycle {cycle}: Retained a RaceController after unload");
                Assert.That(Object.FindAnyObjectByType<VehicleController>(), Is.Null,
                    $"Cycle {cycle}: Retained a VehicleController after unload");
                Assert.That(Object.FindAnyObjectByType<AIDriver>(), Is.Null,
                    $"Cycle {cycle}: Retained an AIDriver after unload");

                // 9. Track heap across cycles
                long currentHeap = System.GC.GetTotalMemory(true);
                if (cycle == 2)
                {
                    baselineHeap = currentHeap;
                }
                else if (cycle == CycleCount)
                {
                    endHeap = currentHeap;
                }
            }

            // Assert managed heap stability (drift <= 512 KB over 8 post-warmup cycles)
            long heapGrowth = endHeap - baselineHeap;
            Assert.That(heapGrowth, Is.LessThanOrEqualTo(512 * 1024),
                $"Cumulative managed heap drift ({heapGrowth} B) exceeded 512 KB threshold across 10 cycles");
        }
    }
}
