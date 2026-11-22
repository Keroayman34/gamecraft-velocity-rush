using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Persistent UI Toolkit shell controller that reacts to <see cref="GameFlowManager.StateChanged"/>
    /// and <see cref="GameFlowManager.OverlayChanged"/> to toggle screen root visibility.
    /// All screen elements are built once; visibility is toggled via DisplayStyle.Flex/None (0 B/frame).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CommercialUiController : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _shellRoot;
        private VisualElement _mainMenuRoot;
        private VisualElement _champSelectRoot;
        private VisualElement _eventSelectRoot;
        private VisualElement _garageRoot;
        private VisualElement _upgradesRoot;
        private VisualElement _raceHudRoot;
        private VisualElement _resultsRoot;
        private VisualElement _pauseOverlayRoot;
        private VisualElement _settingsOverlayRoot;

        private GameFlowManager _flow;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        /// <summary>Returns the shell root for test inspection.</summary>
        public VisualElement ShellRoot => _shellRoot;

        public void Initialize(GameFlowManager flow, UIDocument doc = null)
        {
            _flow = flow;
            if (doc != null)
            {
                uiDocument = doc;
            }

            BuildShell();

            if (_flow != null)
            {
                _flow.StateChanged += OnStateChanged;
                _flow.OverlayChanged += OnOverlayChanged;
                OnStateChanged(_flow.CurrentState);
            }

            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_flow != null)
            {
                _flow.StateChanged -= OnStateChanged;
                _flow.OverlayChanged -= OnOverlayChanged;
            }
        }

        private void BuildShell()
        {
            VisualElement parent = uiDocument != null ? uiDocument.rootVisualElement : null;

            _shellRoot = new VisualElement { name = "commercial-ui-shell" };
            _shellRoot.style.position = Position.Absolute;
            _shellRoot.style.left = 0f;
            _shellRoot.style.top = 0f;
            _shellRoot.style.right = 0f;
            _shellRoot.style.bottom = 0f;
            _shellRoot.style.width = Length.Percent(100f);
            _shellRoot.style.height = Length.Percent(100f);

            _mainMenuRoot = CreateScreen("screen-main-menu");
            _champSelectRoot = CreateScreen("screen-champ-select");
            _eventSelectRoot = CreateScreen("screen-event-select");
            _garageRoot = CreateScreen("screen-garage");
            _upgradesRoot = CreateScreen("screen-upgrades");
            _raceHudRoot = CreateScreen("screen-race-hud");
            _resultsRoot = CreateScreen("screen-results");
            _pauseOverlayRoot = CreateScreen("overlay-pause");
            _settingsOverlayRoot = CreateScreen("overlay-settings");

            // Dark arcade canvas for primary menu screens (#0B1020)
            var menuBgColor = new Color(0.043f, 0.063f, 0.125f, 0.98f);
            _mainMenuRoot.style.backgroundColor = menuBgColor;
            _champSelectRoot.style.backgroundColor = menuBgColor;
            _eventSelectRoot.style.backgroundColor = menuBgColor;
            _garageRoot.style.backgroundColor = menuBgColor;
            _upgradesRoot.style.backgroundColor = menuBgColor;

            _shellRoot.Add(_mainMenuRoot);
            _shellRoot.Add(_champSelectRoot);
            _shellRoot.Add(_eventSelectRoot);
            _shellRoot.Add(_garageRoot);
            _shellRoot.Add(_upgradesRoot);
            _shellRoot.Add(_raceHudRoot);
            _shellRoot.Add(_resultsRoot);
            _shellRoot.Add(_pauseOverlayRoot);
            _shellRoot.Add(_settingsOverlayRoot);

            if (parent != null)
            {
                parent.Add(_shellRoot);
            }
        }

        /// <summary>Binds the shell to an external root (testing seam).</summary>
        public void BindRoot(VisualElement root)
        {
            if (_shellRoot != null && root != null && _shellRoot.parent != root)
            {
                _shellRoot.RemoveFromHierarchy();
                root.Add(_shellRoot);
            }
        }

        private VisualElement CreateScreen(string screenName)
        {
            var screen = new VisualElement { name = screenName };
            screen.style.position = Position.Absolute;
            screen.style.left = 0f;
            screen.style.top = 0f;
            screen.style.right = 0f;
            screen.style.bottom = 0f;
            screen.style.width = Length.Percent(100f);
            screen.style.height = Length.Percent(100f);
            screen.style.display = DisplayStyle.None;
            return screen;
        }

        private void OnStateChanged(AppState state)
        {
            // Hide all primary screens
            SetVisible(_mainMenuRoot, false);
            SetVisible(_champSelectRoot, false);
            SetVisible(_eventSelectRoot, false);
            SetVisible(_garageRoot, false);
            SetVisible(_upgradesRoot, false);
            SetVisible(_raceHudRoot, false);
            SetVisible(_resultsRoot, false);

            // Hide overlays on state change
            SetVisible(_pauseOverlayRoot, false);
            SetVisible(_settingsOverlayRoot, false);

            switch (state)
            {
                case AppState.MainMenu:
                    SetVisible(_mainMenuRoot, true);
                    break;
                case AppState.ChampionshipSelection:
                    SetVisible(_champSelectRoot, true);
                    break;
                case AppState.EventSelection:
                    SetVisible(_eventSelectRoot, true);
                    break;
                case AppState.Garage:
                    SetVisible(_garageRoot, true);
                    break;
                case AppState.Upgrades:
                    SetVisible(_upgradesRoot, true);
                    break;
                case AppState.Race:
                case AppState.PreEvent:
                    SetVisible(_raceHudRoot, true);
                    break;
                case AppState.Results:
                    SetVisible(_resultsRoot, true);
                    break;
                case AppState.Boot:
                    // No screen shown during boot
                    break;
            }
        }

        private void OnOverlayChanged(AppOverlay overlay)
        {
            SetVisible(_pauseOverlayRoot, false);
            SetVisible(_settingsOverlayRoot, false);

            switch (overlay)
            {
                case AppOverlay.Pause:
                    SetVisible(_pauseOverlayRoot, true);
                    break;
                case AppOverlay.Settings:
                    SetVisible(_settingsOverlayRoot, true);
                    break;
                case AppOverlay.Rewards:
                    // Rewards overlay uses the results screen root
                    SetVisible(_resultsRoot, true);
                    break;
            }
        }

        private static void SetVisible(VisualElement element, bool visible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // Public accessors for screen root elements (test inspection & presenters)
        public VisualElement MainMenuRoot => _mainMenuRoot;
        public VisualElement ChampSelectRoot => _champSelectRoot;
        public VisualElement EventSelectRoot => _eventSelectRoot;
        public VisualElement GarageRoot => _garageRoot;
        public VisualElement UpgradesRoot => _upgradesRoot;
        public VisualElement RaceHudRoot => _raceHudRoot;
        public VisualElement ResultsRoot => _resultsRoot;
        public VisualElement PauseOverlayRoot => _pauseOverlayRoot;
        public VisualElement SettingsOverlayRoot => _settingsOverlayRoot;

        /// <summary>Returns the VisualElement for the given state (test helper).</summary>
        public VisualElement GetScreenForState(AppState state)
        {
            switch (state)
            {
                case AppState.MainMenu: return _mainMenuRoot;
                case AppState.ChampionshipSelection: return _champSelectRoot;
                case AppState.EventSelection: return _eventSelectRoot;
                case AppState.Garage: return _garageRoot;
                case AppState.Upgrades: return _upgradesRoot;
                case AppState.Race: return _raceHudRoot;
                case AppState.PreEvent: return _raceHudRoot;
                case AppState.Results: return _resultsRoot;
                default: return null;
            }
        }
    }
}
