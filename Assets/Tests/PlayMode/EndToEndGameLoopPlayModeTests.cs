using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    [TestFixture]
    public sealed class EndToEndGameLoopPlayModeTests
    {
        private GameObject _bootstrapHolder;
        private GameCompositionRoot _compositionRoot;
        private InMemorySaveStore _store;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _bootstrapHolder = new GameObject("Bootstrap_TestHost");
            _bootstrapHolder.AddComponent<UIDocument>();
            _bootstrapHolder.AddComponent<GameFlowManager>();
            _bootstrapHolder.AddComponent<SceneLifecycleController>();
            _bootstrapHolder.AddComponent<LoadingVeil>();
            _bootstrapHolder.AddComponent<CommercialUiController>();

            _compositionRoot = _bootstrapHolder.AddComponent<GameCompositionRoot>();

            // Hermetic in-memory store
            _store = new InMemorySaveStore();
            var initialSave = SaveModel.CreateDefault();
            initialSave.credits = 1000;
            initialSave.settings.language = "en";
            initialSave.settings.masterVolume = 0.8f;
            _store.TrySave(JsonUtility.ToJson(initialSave), out _);

            _compositionRoot.Initialize(_store);
            // Use decoupled adapter-free GameFlow for hermetic game-loop testing
            _compositionRoot.GameFlow.Initialize(null, AppState.MainMenu);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_bootstrapHolder != null)
            {
                Object.Destroy(_bootstrapHolder);
            }
            Time.timeScale = 1f;
            AudioListener.pause = false;
            LocalizationService.Reset();
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootToMainMenu_HydratesSaveAndReachesMainMenu()
        {
            Assert.That(_compositionRoot.IsInitialized, Is.True);
            Assert.That(_compositionRoot.GameFlow.CurrentState, Is.EqualTo(AppState.MainMenu));
            Assert.That(_compositionRoot.PlayerProgress.Credits, Is.EqualTo(1000));
            Assert.That(LocalizationService.CurrentLanguage, Is.EqualTo("en"));
            Assert.That(AudioListener.volume, Is.EqualTo(0.8f).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RaceFinish_EvaluatesProgression_AndPersistsToSaveStore()
        {
            var flow = _compositionRoot.GameFlow;

            // Select C1 Round 1
            flow.SetChampionshipSelection("c1-ignition-series");
            flow.SetEventSelection("c1-ignition-series-round-1");

            // Transition through legal FSM path to Race
            flow.TryTransitionTo(AppState.ChampionshipSelection);
            flow.TryTransitionTo(AppState.EventSelection);
            flow.TryTransitionTo(AppState.PreEvent);
            flow.TryTransitionTo(AppState.Race);

            var eventDef = ScriptableObject.CreateInstance<RaceEventDefinition>();
#if UNITY_EDITOR
            eventDef.ConfigureForAuthoring(
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
                "Test event");
#endif

            // Simulate completed 1st place race result
            var raceResult = new RaceResult(42.50d, 3, 3, completed: true);
            _compositionRoot.HandleRaceFinished(raceResult, eventDef);
            yield return null;

            Assert.That(flow.CurrentState, Is.EqualTo(AppState.Results));
            Assert.That(_compositionRoot.PlayerProgress.GetEventStars("c1-ignition-series-round-1"), Is.EqualTo(3));
            Assert.That(_compositionRoot.PlayerProgress.Credits, Is.GreaterThan(1000));

            // Verify persistence to store
            bool loadSuccess = _store.TryLoad(out string json, out _);
            Assert.That(loadSuccess, Is.True);
            var savedModel = JsonUtility.FromJson<SaveModel>(json);
            Assert.That(savedModel.credits, Is.EqualTo(_compositionRoot.PlayerProgress.Credits));

            Object.DestroyImmediate(eventDef);
        }

        [UnityTest]
        public IEnumerator GarageUpgradePurchase_DeductsCredits_AndAppliesModifiers()
        {
            string vehicleId = "starter-apex-v";
            _compositionRoot.PlayerProgress.AddCredits(2000);
            int creditsBefore = _compositionRoot.PlayerProgress.Credits;

            var vClass = ProgressionService.GetVehicleClassFromId(vehicleId);
            int cost = UpgradeCalculator.GetUpgradeCost(vClass, 1);

            bool purchased = _compositionRoot.ProgressionService.TryPurchaseUpgrade(vehicleId, UpgradeCategory.Engine, vClass, out string error);
            Assert.That(purchased, Is.True, error);
            Assert.That(_compositionRoot.PlayerProgress.Credits, Is.EqualTo(creditsBefore - cost));
            Assert.That(_compositionRoot.PlayerProgress.GetUpgradeLevel(vehicleId, UpgradeCategory.Engine), Is.EqualTo(1));

            // Verify modifier calculation
            var state = _compositionRoot.PlayerProgress.GetUpgradeState(vehicleId);
            var modifiers = UpgradeCalculator.ComputeModifiers(state);
            Assert.That(modifiers.TorqueMultiplier, Is.GreaterThan(1.0f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseResume_FastRetry_AndQuitToMenu_Lifecycle()
        {
            var flow = _compositionRoot.GameFlow;

            // Transition to Race (via ChampionshipSelection -> EventSelection -> PreEvent -> Race)
            flow.TryTransitionTo(AppState.ChampionshipSelection);
            flow.TryTransitionTo(AppState.EventSelection);
            flow.TryTransitionTo(AppState.PreEvent);
            flow.TryTransitionTo(AppState.Race);
            Assert.That(flow.CurrentState, Is.EqualTo(AppState.Race));

            // Pause
            flow.PushOverlay(AppOverlay.Pause);
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(AudioListener.pause, Is.True);

            // Resume
            flow.PopOverlay();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(AudioListener.pause, Is.False);

            // Fast Retry
            flow.PushOverlay(AppOverlay.Pause);
            bool retried = flow.RequestFastRetry();
            Assert.That(retried, Is.True);
            Assert.That(flow.CurrentOverlay, Is.EqualTo(AppOverlay.None));
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            // Transition to Results then Quit to MainMenu
            flow.TryTransitionTo(AppState.Results);
            Assert.That(flow.CurrentState, Is.EqualTo(AppState.Results));

            flow.TryTransitionTo(AppState.MainMenu);
            yield return null;
            Assert.That(flow.CurrentState, Is.EqualTo(AppState.MainMenu));
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(AudioListener.pause, Is.False);
        }
    }
}
