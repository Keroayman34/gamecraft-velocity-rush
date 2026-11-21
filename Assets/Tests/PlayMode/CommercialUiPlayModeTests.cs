using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    [TestFixture]
    public sealed class CommercialUiPlayModeTests
    {
        private GameObject _holder;
        private GameFlowManager _flow;
        private CommercialUiController _ui;
        private UIDocument _doc;
        private LocalizationTable _table;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _table = new LocalizationTable();
            LocalizationService.SetTable(_table);
            LocalizationService.SetLanguage("en");

            _holder = new GameObject("CommercialUiTest_Holder");
            _doc = _holder.AddComponent<UIDocument>();
            _flow = _holder.AddComponent<GameFlowManager>();
            _ui = _holder.AddComponent<CommercialUiController>();

            _flow.Initialize(null, AppState.Boot);
            _ui.Initialize(_flow, _doc);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_holder != null)
            {
                Object.Destroy(_holder);
            }
            LocalizationService.Reset();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FlowStateChange_TogglesCorrectScreenVisibility()
        {
            // Transition to MainMenu
            bool res = _flow.TryTransitionTo(AppState.MainMenu);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.MainMenuRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_ui.ChampSelectRoot.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(_ui.RaceHudRoot.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Transition to ChampionshipSelection
            res = _flow.TryTransitionTo(AppState.ChampionshipSelection);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.MainMenuRoot.style.display.value, Is.EqualTo(DisplayStyle.None));
            Assert.That(_ui.ChampSelectRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            // Transition to EventSelection
            res = _flow.TryTransitionTo(AppState.EventSelection);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.EventSelectRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_ui.ChampSelectRoot.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Transition to PreEvent then Race
            res = _flow.TryTransitionTo(AppState.PreEvent);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.RaceHudRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_ui.EventSelectRoot.style.display.value, Is.EqualTo(DisplayStyle.None));

            res = _flow.TryTransitionTo(AppState.Race);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.RaceHudRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));

            // Push Pause Overlay
            _flow.PushOverlay(AppOverlay.Pause);
            yield return null;

            Assert.That(_ui.PauseOverlayRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_ui.RaceHudRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex)); // HUD stays underneath

            // Pop Pause Overlay
            _flow.PopOverlay();
            yield return null;

            Assert.That(_ui.PauseOverlayRoot.style.display.value, Is.EqualTo(DisplayStyle.None));

            // Transition to Results
            res = _flow.TryTransitionTo(AppState.Results);
            Assert.That(res, Is.True);
            yield return null;

            Assert.That(_ui.ResultsRoot.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(_ui.RaceHudRoot.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator RaceHudPresenter_UpdateRaceState_ZeroSteadyStateAllocations()
        {
            var hudRoot = new VisualElement();
            _doc.rootVisualElement.Add(hudRoot);
            var presenter = new RaceHudPresenter(hudRoot);

            // Prime the presenter to warm up any one-time structures and strings
            for (int i = 0; i < 20; i++)
            {
                presenter.UpdateRaceState(RaceState.Running, 10.0, 0, 1, 150);
            }
            yield return null;

            // Measure steady-state allocation over 100 consecutive ticks with unchanged speed/state
            long before = System.GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 100; i++)
            {
                presenter.UpdateRaceState(RaceState.Running, 10.0, 0, 1, 150);
            }

            long allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;

            // Zero heap bytes allocated in steady state
            Assert.That(allocated, Is.EqualTo(0L), "HUD steady-state ticks must allocate 0 B GC.");

            presenter.Dispose();
            yield return null;
        }

        [UnityTest]
        public IEnumerator DynamicLanguageSwitch_UpdatesPresentersInstantly()
        {
            var menuRoot = new VisualElement();
            _doc.rootVisualElement.Add(menuRoot);
            var progress = new PlayerProgress();
            var menuPresenter = new MainMenuPresenter(menuRoot, progress);

            var titleLabel = menuRoot.Q<Label>("main-menu-title");
            Assert.That(titleLabel.text, Is.EqualTo("Velocity Rush"));

            // Switch to Arabic
            LocalizationService.SetLanguage("ar");
            yield return null;

            Assert.That(titleLabel.text, Is.EqualTo("فيلوسيتي رش"));

            // Switch back to English
            LocalizationService.SetLanguage("en");
            yield return null;

            Assert.That(titleLabel.text, Is.EqualTo("Velocity Rush"));

            menuPresenter.Dispose();
        }
    }
}
