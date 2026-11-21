using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Persistent application composition root and coordinator (TDD §29 S1, §34.3; D-067, D-068, D-069, D-070).
    /// Orchestrates SaveService, PlayerProgress, ProgressionService, GameFlowManager,
    /// SceneLifecycleController, CommercialUiController, and all 8 commercial presenters.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class GameCompositionRoot : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.CompositionRoot] ";

        [Header("Scene Dependencies")]
        [SerializeField] private GameFlowManager gameFlow;
        [SerializeField] private SceneLifecycleController sceneLifecycle;
        [SerializeField] private CommercialUiController commercialUi;
        [SerializeField] private UIDocument uiDocument;

        [Header("Content Registries (Optional serialized; auto-resolved if empty)")]
        [SerializeField] private ChampionshipDefinition[] championships;
        [SerializeField] private RaceEventDefinition[] events;
        [SerializeField] private VehicleDefinition[] vehicles;

        private ISaveStore _saveStore;
        private SaveService _saveService;
        private PlayerProgress _progress;
        private ProgressionService _progressionService;
        private LocalizationTable _localizationTable;

        // Presenters
        private MainMenuPresenter _mainMenuPresenter;
        private ChampionshipSelectPresenter _champSelectPresenter;
        private EventSelectPresenter _eventSelectPresenter;
        private GarageUpgradePresenter _garagePresenter;
        private RaceHudPresenter _raceHudPresenter;
        private PauseOverlayPresenter _pausePresenter;
        private ResultsPresenter _resultsPresenter;
        private SettingsPresenter _settingsPresenter;

        // Active Race Scene Cache
        private RaceController _activeRaceController;
        private VehicleController _activePlayerVehicle;
        private RaceStandings _activeStandings;
        private RaceEventDefinition _activeEventDefinition;

        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        public SaveService SaveService => _saveService;
        public PlayerProgress PlayerProgress => _progress;
        public ProgressionService ProgressionService => _progressionService;
        public GameFlowManager GameFlow => gameFlow;

        private void Awake()
        {
            EnsureDependencies();
        }

        private void Start()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        /// <summary>
        /// Ensures all coordinator components exist on this persistent root GameObject.
        /// </summary>
        public void EnsureDependencies()
        {
            if (gameFlow == null)
            {
                gameFlow = GetComponent<GameFlowManager>();
                if (gameFlow == null) gameFlow = gameObject.AddComponent<GameFlowManager>();
            }

            if (sceneLifecycle == null)
            {
                sceneLifecycle = GetComponent<SceneLifecycleController>();
                if (sceneLifecycle == null) sceneLifecycle = gameObject.AddComponent<SceneLifecycleController>();
            }

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (commercialUi == null)
            {
                commercialUi = GetComponent<CommercialUiController>();
                if (commercialUi == null) commercialUi = gameObject.AddComponent<CommercialUiController>();
            }

            // Bind GameFlow scene lifecycle adapter
            gameFlow.Initialize(sceneLifecycle, AppState.Boot);
        }

        /// <summary>
        /// Explicit initialization entry point (supports runtime boot and hermetic test harnesses).
        /// </summary>
        public void Initialize(ISaveStore customStore = null)
        {
            if (_isInitialized)
            {
                return;
            }

            EnsureDependencies();

            // 1. Persistence & Progression
            _saveStore = customStore ?? new PlayerPrefsJsonSaveStore();
            _saveService = new SaveService(_saveStore);
            _saveService.Load();

            _progress = new PlayerProgress(_saveService.CurrentModel);
            _progressionService = new ProgressionService(_progress);

            // 2. Bilingual Localization
            _localizationTable = new LocalizationTable();
            LocalizationService.SetTable(_localizationTable);
            LocalizationService.SetLanguage(_progress.Settings.language);

            // 3. Audio Initial Settings
            ApplyAudioSettings(_progress.Settings);

            // 4. UI Shell & Presenters
            commercialUi.Initialize(gameFlow, uiDocument);
            BuildPresenters();

            // 5. Subscribe GameFlow Events
            gameFlow.StateChanged += HandleStateChanged;
            gameFlow.OverlayChanged += HandleOverlayChanged;

            _isInitialized = true;

            // Transition to MainMenu on initial boot completion
            gameFlow.TryTransitionTo(AppState.MainMenu);
            Debug.Log(LogPrefix + "Commercial GameCompositionRoot initialized successfully.");
        }

        private void BuildPresenters()
        {
            _mainMenuPresenter = new MainMenuPresenter(commercialUi.MainMenuRoot, _progress);
            _mainMenuPresenter.PlayRequested += () => gameFlow.TryTransitionTo(AppState.ChampionshipSelection);
            _mainMenuPresenter.GarageRequested += () => gameFlow.TryTransitionTo(AppState.Garage);
            _mainMenuPresenter.SettingsRequested += () => gameFlow.PushOverlay(AppOverlay.Settings);
            _mainMenuPresenter.LanguageToggleRequested += HandleLanguageToggle;

            _champSelectPresenter = new ChampionshipSelectPresenter(commercialUi.ChampSelectRoot);
            _champSelectPresenter.ChampionshipSelected += HandleChampionshipSelected;
            _champSelectPresenter.BackRequested += () => gameFlow.TryTransitionTo(AppState.MainMenu);

            _eventSelectPresenter = new EventSelectPresenter(commercialUi.EventSelectRoot);
            _eventSelectPresenter.EventLaunched += HandleEventLaunch;
            _eventSelectPresenter.BackRequested += () => gameFlow.TryTransitionTo(AppState.ChampionshipSelection);

            _garagePresenter = new GarageUpgradePresenter(commercialUi.GarageRoot, commercialUi.UpgradesRoot, _progress);
            _garagePresenter.VehicleSelectRequested += HandleVehicleSelected;
            _garagePresenter.VehicleBuyRequested += HandleVehiclePurchased;
            _garagePresenter.VehicleUpgradesRequested += id => gameFlow.TryTransitionTo(AppState.Upgrades);
            _garagePresenter.UpgradeBuyRequested += HandleUpgradePurchased;
            _garagePresenter.GarageBackRequested += () => gameFlow.TryTransitionTo(AppState.MainMenu);
            _garagePresenter.UpgradesBackRequested += () => gameFlow.TryTransitionTo(AppState.Garage);

            _raceHudPresenter = new RaceHudPresenter(commercialUi.RaceHudRoot);

            _pausePresenter = new PauseOverlayPresenter(commercialUi.PauseOverlayRoot);
            _pausePresenter.ResumeRequested += () => gameFlow.PopOverlay();
            _pausePresenter.RetryRequested += HandleFastRetry;
            _pausePresenter.SettingsRequested += () => gameFlow.PushOverlay(AppOverlay.Settings);
            _pausePresenter.QuitRequested += HandleQuitToMenu;

            _resultsPresenter = new ResultsPresenter(commercialUi.ResultsRoot);
            _resultsPresenter.NextEventRequested += HandleNextEvent;
            _resultsPresenter.RetryRequested += HandleFastRetry;
            _resultsPresenter.MenuRequested += () => gameFlow.TryTransitionTo(AppState.MainMenu);

            _settingsPresenter = new SettingsPresenter(commercialUi.SettingsOverlayRoot, _progress);
            _settingsPresenter.SettingsChanged += HandleSettingsChanged;
            _settingsPresenter.BackRequested += () => gameFlow.PopOverlay();
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            // Pause Input (Esc / P)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (gameFlow.CurrentState == AppState.Race)
                {
                    if (gameFlow.CurrentOverlay == AppOverlay.None)
                    {
                        gameFlow.PushOverlay(AppOverlay.Pause);
                    }
                    else if (gameFlow.CurrentOverlay == AppOverlay.Pause || gameFlow.CurrentOverlay == AppOverlay.Settings)
                    {
                        gameFlow.PopOverlay();
                    }
                }
            }

            // Zero-allocation live HUD updates
            if (gameFlow.CurrentState == AppState.Race || gameFlow.CurrentState == AppState.PreEvent)
            {
                UpdateRaceHud();
            }
        }

        private void UpdateRaceHud()
        {
            if (_activeRaceController == null)
            {
                FindActiveRaceComponents();
            }

            if (_activeRaceController != null)
            {
                int speedKph = 0;
                if (_activePlayerVehicle != null)
                {
                    speedKph = SliceHudFormat.SpeedToKph(_activePlayerVehicle.Telemetry.Speed);
                }

                int rank = _activeStandings != null ? _activeStandings.PlayerRank : 1;
                _raceHudPresenter.UpdateRaceState(
                    _activeRaceController.State,
                    _activeRaceController.ElapsedSeconds,
                    _activeRaceController.CountdownRemainingSeconds,
                    rank,
                    speedKph);
            }
        }

        private void FindActiveRaceComponents()
        {
            _activeRaceController = FindAnyObjectByType<RaceController>();
            if (_activeRaceController != null)
            {
                _activeRaceController.RaceFinished -= HandleRaceFinished;
                _activeRaceController.RaceFinished += HandleRaceFinished;
            }

            var vehicles = FindObjectsByType<VehicleController>();
            for (int i = 0; i < vehicles.Length; i++)
            {
                if (vehicles[i].CompareTag("Player") || vehicles[i].name.Contains("Player") || i == 0)
                {
                    _activePlayerVehicle = vehicles[i];
                    break;
                }
            }

            // Apply runtime upgrade modifiers to player vehicle
            if (_activePlayerVehicle != null && !string.IsNullOrEmpty(gameFlow.SelectedVehicleId))
            {
                var upgradeState = _progress.GetUpgradeState(gameFlow.SelectedVehicleId);
                var modifiers = UpgradeCalculator.ComputeModifiers(upgradeState);
                _activePlayerVehicle.ApplyUpgradeModifiers(modifiers);
            }
        }

        #region GameFlow & State Routing

        private void HandleStateChanged(AppState state)
        {
            switch (state)
            {
                case AppState.MainMenu:
                    _mainMenuPresenter.Refresh();
                    break;
                case AppState.ChampionshipSelection:
                    RefreshChampionshipSelect();
                    break;
                case AppState.EventSelection:
                    RefreshEventSelect();
                    break;
                case AppState.Garage:
                    RefreshGarage();
                    break;
                case AppState.Upgrades:
                    RefreshUpgrades();
                    break;
                case AppState.Race:
                    FindActiveRaceComponents();
                    break;
            }
        }

        private void HandleOverlayChanged(AppOverlay overlay)
        {
            if (overlay == AppOverlay.Pause)
            {
                Time.timeScale = 0f;
                AudioListener.pause = true;
            }
            else if (overlay == AppOverlay.None)
            {
                Time.timeScale = 1f;
                AudioListener.pause = false;
            }
        }

        #endregion

        #region Presenter Actions & Handlers

        private void HandleLanguageToggle()
        {
            string nextLang = LocalizationService.IsRTL ? LocalizationService.LanguageEnglish : LocalizationService.LanguageArabic;
            LocalizationService.SetLanguage(nextLang);
            _progress.Settings.language = nextLang;
            SyncAndSave();
        }

        private void HandleSettingsChanged(PlayerSettingsDto updated)
        {
            _progress.Settings.masterVolume = updated.masterVolume;
            _progress.Settings.sfxVolume = updated.sfxVolume;
            _progress.Settings.engineVolume = updated.engineVolume;
            _progress.Settings.language = updated.language;

            ApplyAudioSettings(updated);
            SyncAndSave();
        }

        private void ApplyAudioSettings(PlayerSettingsDto settings)
        {
            if (settings != null)
            {
                AudioListener.volume = settings.masterVolume;
            }
        }

        private void HandleChampionshipSelected(string championshipId)
        {
            gameFlow.SetChampionshipSelection(championshipId);
            gameFlow.TryTransitionTo(AppState.EventSelection);
        }

        private void HandleEventLaunch(string eventId)
        {
            gameFlow.SetEventSelection(eventId);

            // Determine target environment scene
            string sceneName = "desert-signature"; // Default fallback
            var eventDef = ResolveEventDefinition(eventId);
            if (eventDef != null)
            {
                _activeEventDefinition = eventDef;
                sceneName = NextEventResolver.GetSceneNameForEvent(eventDef);
            }

            // Transition: EventSelection -> PreEvent -> Race
            if (gameFlow.TryTransitionTo(AppState.PreEvent))
            {
                gameFlow.TryTransitionTo(AppState.Race, sceneName);
            }
        }

        private void HandleFastRetry()
        {
            gameFlow.CloseAllOverlays();
            gameFlow.RequestFastRetry();
            FindActiveRaceComponents();
        }

        private void HandleQuitToMenu()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
            gameFlow.CloseAllOverlays();
            gameFlow.TryTransitionTo(AppState.MainMenu);
        }

        private void HandleNextEvent()
        {
            string currentEvent = gameFlow.SelectedEventId;
            if (NextEventResolver.TryResolveNextEvent(currentEvent, out string nextChampId, out string nextEventId))
            {
                gameFlow.SetChampionshipSelection(nextChampId);
                HandleEventLaunch(nextEventId);
            }
            else
            {
                // Championship series completed
                gameFlow.TryTransitionTo(AppState.MainMenu);
            }
        }

        public void SetActiveEventDefinition(RaceEventDefinition eventDef)
        {
            _activeEventDefinition = eventDef;
        }

        public void HandleRaceFinished(RaceResult result)
        {
            HandleRaceFinished(result, null);
        }

        public void HandleRaceFinished(RaceResult result, RaceEventDefinition explicitEventDef)
        {
            var eventDef = explicitEventDef ?? _activeEventDefinition ?? ResolveEventDefinition(gameFlow.SelectedEventId);
            if (eventDef == null)
            {
                Debug.LogWarning(LogPrefix + "Cannot process race outcome: event definition not found.");
                gameFlow.TryTransitionTo(AppState.Results);
                return;
            }

            int finishPos = 1; // Default solo
            if (_activeStandings != null)
            {
                finishPos = _activeStandings.PlayerFinishPosition > 0 ? _activeStandings.PlayerFinishPosition : 1;
            }

            var outcome = _progressionService.ProcessRaceOutcome(result, eventDef, finishPos);
            SyncAndSave();

            gameFlow.TryTransitionTo(AppState.Results);
            _resultsPresenter.DisplayOutcome(outcome, finishPos);
        }

        private void HandleVehicleSelected(string vehicleId)
        {
            _progress.SetSelectedVehicle(vehicleId);
            gameFlow.SetVehicleSelection(vehicleId);
            SyncAndSave();
            RefreshGarage();
        }

        private void HandleVehiclePurchased(string vehicleId)
        {
            if (_progressionService.TryPurchaseVehicle(vehicleId, out var error))
            {
                _progress.SetSelectedVehicle(vehicleId);
                gameFlow.SetVehicleSelection(vehicleId);
                SyncAndSave();
                RefreshGarage();
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Failed to purchase vehicle: " + error);
            }
        }

        private void HandleUpgradePurchased(UpgradeCategory category)
        {
            string vehicleId = _progress.SelectedVehicleId;
            var vClass = ProgressionService.GetVehicleClassFromId(vehicleId);

            if (_progressionService.TryPurchaseUpgrade(vehicleId, category, vClass, out var error))
            {
                SyncAndSave();
                RefreshUpgrades();
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Failed to purchase upgrade: " + error);
            }
        }

        #endregion

        #region Refresh Data Helpers

        private void RefreshChampionshipSelect()
        {
            var champs = new[]
            {
                new ChampionshipSelectPresenter.ChampionshipDisplayData
                {
                    ChampionshipId = NextEventResolver.Championship1Id,
                    NameKey = "champ.c1_name",
                    DifficultyKey = "champ.c1_diff",
                    IsUnlocked = _progress.IsChampionshipUnlocked(NextEventResolver.Championship1Id),
                    TotalStars = ComputeChampionshipStars(NextEventResolver.Championship1Id),
                    MaxStars = 15
                },
                new ChampionshipSelectPresenter.ChampionshipDisplayData
                {
                    ChampionshipId = NextEventResolver.Championship2Id,
                    NameKey = "champ.c2_name",
                    DifficultyKey = "champ.c2_diff",
                    IsUnlocked = _progress.IsChampionshipUnlocked(NextEventResolver.Championship2Id),
                    TotalStars = ComputeChampionshipStars(NextEventResolver.Championship2Id),
                    MaxStars = 15
                },
                new ChampionshipSelectPresenter.ChampionshipDisplayData
                {
                    ChampionshipId = NextEventResolver.Championship3Id,
                    NameKey = "champ.c3_name",
                    DifficultyKey = "champ.c3_diff",
                    IsUnlocked = _progress.IsChampionshipUnlocked(NextEventResolver.Championship3Id),
                    TotalStars = ComputeChampionshipStars(NextEventResolver.Championship3Id),
                    MaxStars = 15
                }
            };

            _champSelectPresenter.SetData(champs);
        }

        private void RefreshEventSelect()
        {
            string champId = gameFlow.SelectedChampionshipId;
            if (string.IsNullOrEmpty(champId))
            {
                champId = NextEventResolver.Championship1Id;
            }

            var eventData = new EventSelectPresenter.EventDisplayData[NextEventResolver.RoundsPerChampionship];
            for (int i = 0; i < NextEventResolver.RoundsPerChampionship; i++)
            {
                int round = i + 1;
                string eventId = $"{champId}-round-{round}";
                bool isUnlocked = (round == 1 && _progress.IsChampionshipUnlocked(champId)) ||
                                  (round > 1 && _progress.IsEventCompleted($"{champId}-round-{round - 1}"));

                eventData[i] = new EventSelectPresenter.EventDisplayData
                {
                    EventId = eventId,
                    DisplayName = "Round " + round,
                    EventTypeKey = (round % 2 == 1) ? "event.circuit" : "event.timetrial",
                    IsUnlocked = isUnlocked,
                    StarsEarned = _progress.GetEventStars(eventId),
                    BestTimeSeconds = _progress.GetEventBestTime(eventId),
                    RoundIndex = round
                };
            }

            _eventSelectPresenter.SetData(eventData);
        }

        private void RefreshGarage()
        {
            var vehicleList = new[]
            {
                "starter-apex-v", "starter-nova-hatch",
                "sport-velocity-gt", "sport-aero-coupe", "sport-pulse-r",
                "drift-sidewinder", "drift-kuma-dx",
                "supercar-vortex-v10", "supercar-eclipse", "supercar-nebula-x"
            };

            var data = new GarageUpgradePresenter.VehicleDisplayData[vehicleList.Length];
            for (int i = 0; i < vehicleList.Length; i++)
            {
                string vId = vehicleList[i];
                var vClass = ProgressionService.GetVehicleClassFromId(vId);

                data[i] = new GarageUpgradePresenter.VehicleDisplayData
                {
                    VehicleId = vId,
                    NameKey = "vehicle." + vId,
                    ClassKey = "garage.class_" + vClass.ToString().ToLowerInvariant(),
                    VehicleClass = vClass,
                    IsUnlocked = _progress.IsVehicleUnlocked(vId),
                    IsSelected = string.Equals(_progress.SelectedVehicleId, vId, StringComparison.OrdinalIgnoreCase),
                    UnlockCost = ProgressionService.GetVehicleUnlockCost(vId)
                };
            }

            _garagePresenter.SetVehicleData(data);
        }

        private void RefreshUpgrades()
        {
            string vehicleId = _progress.SelectedVehicleId;
            var vClass = ProgressionService.GetVehicleClassFromId(vehicleId);

            var categories = new[]
            {
                UpgradeCategory.Engine,
                UpgradeCategory.Tires,
                UpgradeCategory.Brakes,
                UpgradeCategory.Suspension,
                UpgradeCategory.Transmission,
                UpgradeCategory.Aerodynamics
            };

            var data = new GarageUpgradePresenter.UpgradeDisplayData[categories.Length];
            for (int i = 0; i < categories.Length; i++)
            {
                var cat = categories[i];
                int tier = _progress.GetUpgradeLevel(vehicleId, cat);
                int cost = UpgradeCalculator.GetUpgradeCost(vClass, tier + 1);

                data[i] = new GarageUpgradePresenter.UpgradeDisplayData
                {
                    Category = cat,
                    NameKey = "upgrade." + cat.ToString().ToLowerInvariant(),
                    CurrentTier = tier,
                    MaxTier = VehicleUpgradeState.MaxTier,
                    NextTierCost = cost,
                    CanAfford = _progress.Credits >= cost,
                    IsMaxed = tier >= VehicleUpgradeState.MaxTier
                };
            }

            _garagePresenter.SetUpgradeData(data, "vehicle." + vehicleId);
        }

        private int ComputeChampionshipStars(string championshipId)
        {
            int total = 0;
            for (int round = 1; round <= NextEventResolver.RoundsPerChampionship; round++)
            {
                total += _progress.GetEventStars($"{championshipId}-round-{round}");
            }
            return total;
        }

        private RaceEventDefinition ResolveEventDefinition(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return null;

            if (events != null)
            {
                for (int i = 0; i < events.Length; i++)
                {
                    if (events[i] != null && string.Equals(events[i].EventId, eventId, StringComparison.OrdinalIgnoreCase))
                    {
                        return events[i];
                    }
                }
            }

            // Fallback: look for EventCatalog in active scenes
            var catalog = FindAnyObjectByType<EventCatalog>();
            if (catalog != null)
            {
                return catalog.FindById(eventId);
            }

#if UNITY_EDITOR
            var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<RaceEventDefinition>($"Assets/Data/Events/{eventId}.asset");
            if (editorAsset != null)
            {
                return editorAsset;
            }
#endif

            return null;
        }

        private void SyncAndSave()
        {
            var model = _progress.ExportSaveModel();
            _saveService.SetModel(model);
            _saveService.SaveNow();
        }

        private void Cleanup()
        {
            if (gameFlow != null)
            {
                gameFlow.StateChanged -= HandleStateChanged;
                gameFlow.OverlayChanged -= HandleOverlayChanged;
            }

            if (_activeRaceController != null)
            {
                _activeRaceController.RaceFinished -= HandleRaceFinished;
            }

            _mainMenuPresenter?.Dispose();
            _champSelectPresenter?.Dispose();
            _eventSelectPresenter?.Dispose();
            _garagePresenter?.Dispose();
            _raceHudPresenter?.Dispose();
            _pausePresenter?.Dispose();
            _resultsPresenter?.Dispose();
            _settingsPresenter?.Dispose();

            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        #endregion
    }
}
