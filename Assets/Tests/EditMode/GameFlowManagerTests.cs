using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Hermetic unit tests for AppState transition matrix, GameFlowManager FSM,
    /// overlay stack, SelectionContext ownership, and re-entrancy protection (TDD §30.2, §34.3; D-070).
    /// </summary>
    [TestFixture]
    public sealed class GameFlowManagerTests
    {
        private sealed class FakeSceneLifecycleAdapter : ISceneLifecycleAdapter
        {
            public bool IsTransitionInFlight { get; set; }
            public string ActiveRaceSceneName { get; set; }
            public int LoadRequests { get; private set; }
            public int UnloadRequests { get; private set; }
            public int RetryRequests { get; private set; }
            public Action PendingLoadCallback { get; private set; }
            public Action PendingUnloadCallback { get; private set; }
            public bool AutoComplete { get; set; } = true;
            public bool FailLoad { get; set; }
            public bool FailRetry { get; set; }

            public bool TryLoadRaceScene(string sceneName, Action onComplete = null)
            {
                if (IsTransitionInFlight || FailLoad)
                {
                    return false;
                }

                LoadRequests++;
                ActiveRaceSceneName = sceneName;

                if (AutoComplete)
                {
                    onComplete?.Invoke();
                }
                else
                {
                    PendingLoadCallback = onComplete;
                }

                return true;
            }

            public bool TryUnloadRaceScene(Action onComplete = null)
            {
                if (IsTransitionInFlight)
                {
                    return false;
                }

                UnloadRequests++;
                ActiveRaceSceneName = null;

                if (AutoComplete)
                {
                    onComplete?.Invoke();
                }
                else
                {
                    PendingUnloadCallback = onComplete;
                }

                return true;
            }

            public bool RequestFastRetry()
            {
                if (IsTransitionInFlight || FailRetry)
                {
                    return false;
                }

                RetryRequests++;
                return true;
            }
        }

        private GameObject _hostObject;
        private GameFlowManager _gameFlow;
        private FakeSceneLifecycleAdapter _fakeLifecycle;

        [SetUp]
        public void SetUp()
        {
            SelectionContext.Clear();
            _hostObject = new GameObject("Test_GameFlowManager");
            _gameFlow = _hostObject.AddComponent<GameFlowManager>();
            _fakeLifecycle = new FakeSceneLifecycleAdapter();
            _gameFlow.Initialize(_fakeLifecycle, AppState.Boot);
        }

        [TearDown]
        public void TearDown()
        {
            SelectionContext.Clear();
            if (_hostObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_hostObject);
            }
        }

        [Test]
        public void TransitionMatrix_ExhaustiveAllCells_OnlyPermittedTransitionsAllowed()
        {
            // All 9 states from TDD §30.2
            var allStates = (AppState[])Enum.GetValues(typeof(AppState));
            Assert.That(allStates.Length, Is.EqualTo(9));

            // Permitted transitions explicitly defined in TDD §30.2:
            var permitted = new HashSet<(AppState, AppState)>
            {
                (AppState.Boot, AppState.MainMenu),
                (AppState.MainMenu, AppState.Garage),
                (AppState.MainMenu, AppState.ChampionshipSelection),
                (AppState.Garage, AppState.MainMenu),
                (AppState.Garage, AppState.Upgrades),
                (AppState.Garage, AppState.ChampionshipSelection),
                (AppState.Garage, AppState.EventSelection),
                (AppState.Garage, AppState.PreEvent),
                (AppState.Upgrades, AppState.Garage),
                (AppState.ChampionshipSelection, AppState.EventSelection),
                (AppState.ChampionshipSelection, AppState.MainMenu),
                (AppState.ChampionshipSelection, AppState.Garage),
                (AppState.EventSelection, AppState.PreEvent),
                (AppState.EventSelection, AppState.ChampionshipSelection),
                (AppState.EventSelection, AppState.Garage),
                (AppState.PreEvent, AppState.Race),
                (AppState.PreEvent, AppState.EventSelection),
                (AppState.PreEvent, AppState.Garage),
                (AppState.Race, AppState.Results),
                (AppState.Results, AppState.EventSelection),
                (AppState.Results, AppState.Race),
                (AppState.Results, AppState.MainMenu)
            };

            var testedCount = 0;
            var permittedCount = 0;
            var rejectedCount = 0;

            for (var i = 0; i < allStates.Length; i++)
            {
                for (var j = 0; j < allStates.Length; j++)
                {
                    testedCount++;
                    var from = allStates[i];
                    var to = allStates[j];
                    var isPermitted = AppStateTransitionMatrix.IsTransitionAllowed(from, to);

                    if (permitted.Contains((from, to)))
                    {
                        Assert.That(isPermitted, Is.True, $"Expected legal transition from {from} to {to}.");
                        permittedCount++;
                    }
                    else
                    {
                        Assert.That(isPermitted, Is.False, $"Expected illegal transition from {from} to {to} to be rejected.");
                        rejectedCount++;
                    }
                }
            }

            Assert.That(testedCount, Is.EqualTo(81), "Total cells in 9x9 matrix must be 81.");
            Assert.That(permittedCount, Is.EqualTo(22), "Exact legal transitions count per TDD §30.2 must be 22.");
            Assert.That(rejectedCount, Is.EqualTo(59), "Exact illegal transitions count must be 59.");
        }

        [Test]
        public void GameFlowManager_PermittedTransitions_UpdateStateAndEmitEvent()
        {
            var statesObserved = new List<AppState>();
            _gameFlow.StateChanged += state => statesObserved.Add(state);

            // Flow: Boot -> MainMenu -> Garage -> Upgrades -> Garage -> PreEvent -> Race -> Results -> MainMenu
            Assert.That(_gameFlow.TryTransitionTo(AppState.MainMenu), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.MainMenu));

            Assert.That(_gameFlow.TryTransitionTo(AppState.Garage), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Garage));

            Assert.That(_gameFlow.TryTransitionTo(AppState.Upgrades), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Upgrades));

            Assert.That(_gameFlow.TryTransitionTo(AppState.Garage), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Garage));

            Assert.That(_gameFlow.TryTransitionTo(AppState.PreEvent), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.PreEvent));

            Assert.That(_gameFlow.TryTransitionTo(AppState.Race, "TestTrack"), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));
            Assert.That(_fakeLifecycle.LoadRequests, Is.EqualTo(1));

            Assert.That(_gameFlow.TryTransitionTo(AppState.Results), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Results));

            Assert.That(_gameFlow.TryTransitionTo(AppState.MainMenu), Is.True);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.MainMenu));
            Assert.That(_fakeLifecycle.UnloadRequests, Is.EqualTo(1));

            var expected = new[]
            {
                AppState.MainMenu,
                AppState.Garage,
                AppState.Upgrades,
                AppState.Garage,
                AppState.PreEvent,
                AppState.Race,
                AppState.Results,
                AppState.MainMenu
            };

            Assert.That(statesObserved.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void GameFlowManager_IllegalTransition_IsRejectedCleanlyWithoutStateCorruption()
        {
            AppState rejectedFrom = default;
            AppState rejectedTo = default;
            string rejectionReason = null;

            _gameFlow.TransitionRejected += (from, to, reason) =>
            {
                rejectedFrom = from;
                rejectedTo = to;
                rejectionReason = reason;
            };

            // Current is Boot. Direct jump to Race is illegal.
            var success = _gameFlow.TryTransitionTo(AppState.Race);

            Assert.That(success, Is.False);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Boot), "State must remain uncorrupted.");
            Assert.That(rejectedFrom, Is.EqualTo(AppState.Boot));
            Assert.That(rejectedTo, Is.EqualTo(AppState.Race));
            Assert.That(rejectionReason, Is.Not.Null.And.Not.Empty);

            // Transition to MainMenu, then try jumping directly to Upgrades (illegal from MainMenu)
            Assert.That(_gameFlow.TryTransitionTo(AppState.MainMenu), Is.True);
            success = _gameFlow.TryTransitionTo(AppState.Upgrades);

            Assert.That(success, Is.False);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.MainMenu));
            Assert.That(rejectedFrom, Is.EqualTo(AppState.MainMenu));
            Assert.That(rejectedTo, Is.EqualTo(AppState.Upgrades));
        }

        [Test]
        public void GameFlowManager_OverlayHandling_PushAndPopWithoutAlteringPrimaryState()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);
            _gameFlow.TryTransitionTo(AppState.Race, "TestTrack");

            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.None));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(0));

            var overlayChanges = new List<AppOverlay>();
            _gameFlow.OverlayChanged += o => overlayChanges.Add(o);

            // Open Pause overlay
            Assert.That(_gameFlow.PushOverlay(AppOverlay.Pause), Is.True);
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.Pause));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(1));
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race), "Primary AppState must not change.");

            // Open Settings on top of Pause
            Assert.That(_gameFlow.PushOverlay(AppOverlay.Settings), Is.True);
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.Settings));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(2));
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Pop Settings -> back to Pause
            Assert.That(_gameFlow.PopOverlay(), Is.True);
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.Pause));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(1));
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Pop Pause -> back to None
            Assert.That(_gameFlow.PopOverlay(), Is.True);
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.None));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(0));
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            Assert.That(overlayChanges, Is.EqualTo(new[]
            {
                AppOverlay.Pause,
                AppOverlay.Settings,
                AppOverlay.Pause,
                AppOverlay.None
            }));
        }

        [Test]
        public void GameFlowManager_OverlayConstraints_DuplicateAndNoneAreRejected()
        {
            Assert.That(_gameFlow.PushOverlay(AppOverlay.None), Is.False);
            Assert.That(_gameFlow.PushOverlay(AppOverlay.Settings), Is.True);
            // Duplicate push
            Assert.That(_gameFlow.PushOverlay(AppOverlay.Settings), Is.False);
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(1));

            // Close all
            _gameFlow.CloseAllOverlays();
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.None));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(0));
            Assert.That(_gameFlow.PopOverlay(), Is.False);
        }

        [Test]
        public void GameFlowManager_PrimaryStateTransition_ClearsActiveOverlays()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.PushOverlay(AppOverlay.Settings);
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.Settings));

            // Transitioning from MainMenu to Garage must close overlays
            _gameFlow.TryTransitionTo(AppState.Garage);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Garage));
            Assert.That(_gameFlow.CurrentOverlay, Is.EqualTo(AppOverlay.None));
            Assert.That(_gameFlow.OverlayDepth, Is.EqualTo(0));
        }

        [Test]
        public void GameFlowManager_SelectionContextOwnership_MutatesCorrectly()
        {
            Assert.That(_gameFlow.HasCompleteSelection, Is.False);

            Assert.That(_gameFlow.SetVehicleSelection("starter-apex-v"), Is.True);
            Assert.That(_gameFlow.SelectedVehicleId, Is.EqualTo("starter-apex-v"));

            Assert.That(_gameFlow.SetChampionshipSelection("c1-ignition-series"), Is.True);
            Assert.That(_gameFlow.SelectedChampionshipId, Is.EqualTo("c1-ignition-series"));

            Assert.That(_gameFlow.SetEventSelection("c1-ignition-series-round-1"), Is.True);
            Assert.That(_gameFlow.SelectedEventId, Is.EqualTo("c1-ignition-series-round-1"));

            Assert.That(_gameFlow.HasCompleteSelection, Is.True);

            _gameFlow.ClearSelections();
            Assert.That(_gameFlow.HasCompleteSelection, Is.False);
            Assert.That(_gameFlow.SelectedVehicleId, Is.Null);
            Assert.That(_gameFlow.SelectedChampionshipId, Is.Null);
            Assert.That(_gameFlow.SelectedEventId, Is.Null);
        }

        [Test]
        public void GameFlowManager_ReentrancyGuard_RejectsConcurrentTransitions()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);

            // Configure fake lifecycle to hold load async (not auto-completing immediately)
            _fakeLifecycle.AutoComplete = false;

            // Start transition to Race
            var firstCall = _gameFlow.TryTransitionTo(AppState.Race, "TestTrack");
            Assert.That(firstCall, Is.True);
            Assert.That(_gameFlow.IsTransitioning, Is.True);

            // Second transition while first is in flight must be rejected
            var rejected = false;
            _gameFlow.TransitionRejected += (from, to, reason) =>
            {
                rejected = true;
                Assert.That(reason, Does.Contain("Transition already in flight"));
            };

            var secondCall = _gameFlow.TryTransitionTo(AppState.Race);
            Assert.That(secondCall, Is.False);
            Assert.That(rejected, Is.True);

            // Complete the in-flight transition
            _fakeLifecycle.PendingLoadCallback?.Invoke();
            Assert.That(_gameFlow.IsTransitioning, Is.False);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));
        }

        [Test]
        public void GameFlowManager_FastRetry_FromRaceOrResults_ResetsRaceAndKeepsRaceState()
        {
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            _gameFlow.TryTransitionTo(AppState.Garage);
            _gameFlow.TryTransitionTo(AppState.PreEvent);
            _gameFlow.TryTransitionTo(AppState.Race, "TestTrack");

            // Fast retry while in Race
            Assert.That(_gameFlow.RequestFastRetry(), Is.True);
            Assert.That(_fakeLifecycle.RetryRequests, Is.EqualTo(1));
            Assert.That(_fakeLifecycle.UnloadRequests, Is.EqualTo(0), "Fast retry must not unload scenes.");
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Transition to Results
            _gameFlow.TryTransitionTo(AppState.Results);
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Results));

            // Fast retry while in Results -> transitions back to Race without scene reload
            Assert.That(_gameFlow.RequestFastRetry(), Is.True);
            Assert.That(_fakeLifecycle.RetryRequests, Is.EqualTo(2));
            Assert.That(_fakeLifecycle.UnloadRequests, Is.EqualTo(0));
            Assert.That(_gameFlow.CurrentState, Is.EqualTo(AppState.Race));

            // Fast retry from a menu state must be rejected
            _gameFlow.TryTransitionTo(AppState.Results);
            _gameFlow.TryTransitionTo(AppState.MainMenu);
            Assert.That(_gameFlow.RequestFastRetry(), Is.False);
        }
    }
}
