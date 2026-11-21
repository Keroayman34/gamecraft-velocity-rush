using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Core
{
    /// <summary>
    /// Authoritative application lifecycle coordinator and state machine owner (TDD §29 S2, §30, §34.3; D-070).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Single mutator for <see cref="AppState"/> transitions and <see cref="SelectionContext"/> facts per TDD §34.3.
    /// Manages overlay presentation (<see cref="AppOverlay"/>) without mutating the underlying state machine.
    /// Coordinates with <see cref="ISceneLifecycleAdapter"/> to handle additive scene loading and unloading.
    /// Emits zero-allocation change notifications.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GameFlowManager : MonoBehaviour, IAppStateSource
    {
        private const string LogPrefix = "[VelocityRush.GameFlow] ";
        private const int MaxOverlayDepth = 4;

        [SerializeField] private AppState initialState = AppState.Boot;

        private AppState _currentState = AppState.Boot;
        private readonly List<AppOverlay> _overlayStack = new List<AppOverlay>(MaxOverlayDepth);
        private ISceneLifecycleAdapter _sceneLifecycle;
        private bool _isTransitioning;

        /// <summary>Current primary application lifecycle state.</summary>
        public AppState CurrentState => _currentState;

        /// <summary>True while an asynchronous state transition or scene operation is in flight.</summary>
        public bool IsTransitioning => _isTransitioning;

        /// <summary>Topmost active overlay, or <see cref="AppOverlay.None"/> if no overlay is open.</summary>
        public AppOverlay CurrentOverlay => _overlayStack.Count > 0 ? _overlayStack[_overlayStack.Count - 1] : AppOverlay.None;

        /// <summary>Depth of the active overlay stack.</summary>
        public int OverlayDepth => _overlayStack.Count;

        // SelectionContext read-only accessors (TDD §34.3)
        public string SelectedVehicleId => SelectionContext.SelectedVehicleId;
        public string SelectedChampionshipId => SelectionContext.SelectedChampionshipId;
        public string SelectedEventId => SelectionContext.SelectedEventId;
        public bool HasCompleteSelection => SelectionContext.HasCompleteSelection;

        /// <summary>Raised once after any successful primary state transition.</summary>
        public event Action<AppState> StateChanged;

        /// <summary>Raised when a state transition is rejected with the source, target, and diagnostic reason.</summary>
        public event Action<AppState, AppState, string> TransitionRejected;

        /// <summary>Raised when an overlay is pushed, popped, or cleared.</summary>
        public event Action<AppOverlay> OverlayChanged;

        /// <summary>
        /// Initializes the coordinator with an injected scene lifecycle adapter and starting state.
        /// Useful for hermetic testing or custom composition roots.
        /// </summary>
        public void Initialize(ISceneLifecycleAdapter sceneLifecycle, AppState startState = AppState.Boot)
        {
            _sceneLifecycle = sceneLifecycle;
            _currentState = startState;
            _overlayStack.Clear();
            _isTransitioning = false;
        }

        private void Awake()
        {
            if (_sceneLifecycle == null)
            {
                _sceneLifecycle = GetComponent<ISceneLifecycleAdapter>();
            }
            _currentState = initialState;
        }

        // SelectionContext Single Writer Ownership (TDD §34.3)
        public bool SetVehicleSelection(string vehicleId) => SelectionContext.SetVehicleSelection(vehicleId);
        public bool SetChampionshipSelection(string championshipId) => SelectionContext.SetChampionshipSelection(championshipId);
        public bool SetEventSelection(string eventId) => SelectionContext.SetEventSelection(eventId);
        public bool SetCompleteSelection(string vehicleId, string championshipId, string eventId) =>
            SelectionContext.SetCompleteSelection(vehicleId, championshipId, eventId);
        public void ClearSelections() => SelectionContext.Clear();

        // Overlay Stack Management
        public bool PushOverlay(AppOverlay overlay)
        {
            if (overlay == AppOverlay.None)
            {
                return false;
            }

            if (_overlayStack.Count >= MaxOverlayDepth)
            {
                Debug.LogWarning(LogPrefix + "Cannot push overlay " + overlay + ": max overlay depth reached.");
                return false;
            }

            if (_overlayStack.Contains(overlay))
            {
                // Overlay already active on stack
                return false;
            }

            _overlayStack.Add(overlay);
            OverlayChanged?.Invoke(CurrentOverlay);
            return true;
        }

        public bool PopOverlay()
        {
            if (_overlayStack.Count == 0)
            {
                return false;
            }

            _overlayStack.RemoveAt(_overlayStack.Count - 1);
            OverlayChanged?.Invoke(CurrentOverlay);
            return true;
        }

        public void CloseAllOverlays()
        {
            if (_overlayStack.Count == 0)
            {
                return;
            }

            _overlayStack.Clear();
            OverlayChanged?.Invoke(AppOverlay.None);
        }

        public bool IsOverlayOpen(AppOverlay overlay) => _overlayStack.Contains(overlay);

        // State Machine Transition Execution
        public bool TryTransitionTo(AppState targetState, string targetSceneName = null)
        {
            // Re-entrancy guard: fail closed if a transition is currently in progress
            if (_isTransitioning)
            {
                var reason = "Transition already in flight.";
                TransitionRejected?.Invoke(_currentState, targetState, reason);
                Debug.LogWarning(LogPrefix + "Rejected transition to " + targetState + ": " + reason);
                return false;
            }

            // Transition matrix validation
            if (!AppStateTransitionMatrix.IsTransitionAllowed(_currentState, targetState))
            {
                var reason = "Illegal state transition from " + _currentState + " to " + targetState + ".";
                TransitionRejected?.Invoke(_currentState, targetState, reason);
                Debug.LogWarning(LogPrefix + "Rejected transition: " + reason);
                return false;
            }

            // Clear overlays on any primary state transition
            CloseAllOverlays();

            // Handle transition to Race (triggers additive scene loading if an adapter is wired)
            if (targetState == AppState.Race)
            {
                var sceneToLoad = !string.IsNullOrEmpty(targetSceneName)
                    ? targetSceneName
                    : (!string.IsNullOrEmpty(SelectionContext.SelectedEventId) ? SelectionContext.SelectedEventId : "TestTrack");

                if (_sceneLifecycle != null)
                {
                    _isTransitioning = true;
                    var loadStarted = _sceneLifecycle.TryLoadRaceScene(sceneToLoad, () =>
                    {
                        _currentState = AppState.Race;
                        _isTransitioning = false;
                        StateChanged?.Invoke(_currentState);
                    });

                    if (!loadStarted)
                    {
                        _isTransitioning = false;
                        var reason = "Failed to initiate race scene load: " + sceneToLoad;
                        TransitionRejected?.Invoke(_currentState, targetState, reason);
                        return false;
                    }

                    return true;
                }

                _currentState = AppState.Race;
                StateChanged?.Invoke(_currentState);
                return true;
            }

            // Handle transition away from Race/Results to non-race menu state (triggers additive scene unload)
            if ((_currentState == AppState.Race || _currentState == AppState.Results) &&
                targetState != AppState.Race && targetState != AppState.Results)
            {
                if (_sceneLifecycle != null && !string.IsNullOrEmpty(_sceneLifecycle.ActiveRaceSceneName))
                {
                    _isTransitioning = true;
                    var unloadStarted = _sceneLifecycle.TryUnloadRaceScene(() =>
                    {
                        _currentState = targetState;
                        _isTransitioning = false;
                        StateChanged?.Invoke(_currentState);
                    });

                    if (!unloadStarted)
                    {
                        _isTransitioning = false;
                        var reason = "Failed to initiate race scene unload.";
                        TransitionRejected?.Invoke(_currentState, targetState, reason);
                        return false;
                    }

                    return true;
                }

                _currentState = targetState;
                StateChanged?.Invoke(_currentState);
                return true;
            }

            // Standard menu-to-menu transition or Race -> Results
            _currentState = targetState;
            StateChanged?.Invoke(_currentState);
            return true;
        }

        // Fast In-Scene Retry Seam (TDD §35.5)
        public bool RequestFastRetry()
        {
            if (_isTransitioning)
            {
                Debug.LogWarning(LogPrefix + "Fast retry rejected: transition in flight.");
                return false;
            }

            if (_currentState != AppState.Race && _currentState != AppState.Results)
            {
                Debug.LogWarning(LogPrefix + "Fast retry rejected: must be in Race or Results state (current: " + _currentState + ").");
                return false;
            }

            CloseAllOverlays();

            if (_sceneLifecycle != null)
            {
                var success = _sceneLifecycle.RequestFastRetry();
                if (!success)
                {
                    return false;
                }
            }

            if (_currentState == AppState.Results)
            {
                _currentState = AppState.Race;
                StateChanged?.Invoke(_currentState);
            }

            return true;
        }
    }
}
