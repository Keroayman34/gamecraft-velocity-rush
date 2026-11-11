using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Connects a <see cref="RaceController"/> and the vehicle telemetry stream to the UI Toolkit
    /// slice HUD (T-7.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// State is polled at a low, inspector-configurable refresh rate (10 Hz by default) so the HUD
    /// is decoupled from the render rate. Race results and pause transitions are event-driven and
    /// force an immediate refresh, so an overlay never lags a state change by a refresh interval.
    /// </para>
    /// <para>
    /// The presenter is deliberately independent of the concrete vehicle type: it consumes
    /// <see cref="IVehicleTelemetry"/> and <see cref="ISliceHudView"/>, so both the simulation and
    /// the UI stack can be replaced without changing this class.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SliceHudPresenter : MonoBehaviour
    {
        [SerializeField] private RaceController race;
        [SerializeField] private MonoBehaviour telemetrySource;
        [SerializeField] private UIDocument document;
        [SerializeField, Min(1f)] private float refreshHz = 10f;

        private ISliceHudView _view;
        private IVehicleTelemetry _telemetry;
        private float _refreshInterval;
        private float _elapsedSinceRefresh;
        private bool _subscribed;
        private bool _refreshRequested;
        private RaceState _lastState;

        private void Awake()
        {
            _telemetry = telemetrySource as IVehicleTelemetry;
            _refreshInterval = 1f / Mathf.Max(1f, refreshHz);
            _lastState = RaceState.Countdown;
        }

        /// <summary>
        /// Rebinds the HUD telemetry stream to a runtime-spawned vehicle (T-8.11). The view and the
        /// race subscription stay unchanged.
        /// </summary>
        /// <param name="newTelemetrySource">MonoBehaviour implementing <see cref="IVehicleTelemetry"/>.</param>
        /// <returns>True when the source implements the telemetry contract.</returns>
        public bool Configure(MonoBehaviour newTelemetrySource)
        {
            var telemetry = newTelemetrySource as IVehicleTelemetry;
            if (telemetry == null)
            {
                return false;
            }

            telemetrySource = newTelemetrySource;
            _telemetry = telemetry;
            _refreshRequested = true;
            return true;
        }

        private void Start()
        {
            EnsureView();
            RefreshNow();
        }

        private void OnEnable()
        {
            if (race == null)
            {
                return;
            }

            race.RaceFinished += HandleRaceFinished;
            race.PausedChanged += HandlePausedChanged;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed && race != null)
            {
                race.RaceFinished -= HandleRaceFinished;
                race.PausedChanged -= HandlePausedChanged;
            }

            _subscribed = false;
        }

        private void Update()
        {
            if (_view == null)
            {
                EnsureView();
            }

            if (_view == null || race == null || _telemetry == null)
            {
                return;
            }

            // A retry rewinds Finished -> Countdown; treat any lifecycle change as immediate so the
            // results overlay cannot survive into the new countdown.
            if (race.State != _lastState)
            {
                _lastState = race.State;
                RefreshNow();
                return;
            }

            _elapsedSinceRefresh += Time.unscaledDeltaTime;
            if (!_refreshRequested && _elapsedSinceRefresh < _refreshInterval)
            {
                return;
            }

            RefreshNow();
        }

        private void HandleRaceFinished(RaceResult result)
        {
            if (_view == null)
            {
                EnsureView();
            }

            if (_view != null)
            {
                _view.ShowResults(result);
            }
        }

        private void HandlePausedChanged(bool paused)
        {
            _refreshRequested = true;
        }

        private void RefreshNow()
        {
            _refreshRequested = false;
            _elapsedSinceRefresh = 0f;

            if (_view == null || race == null || _telemetry == null)
            {
                return;
            }

            var snapshot = SliceHudModel.Evaluate(
                _telemetry.Telemetry,
                race.State,
                race.ElapsedSeconds,
                race.AcceptedCheckpointCount,
                race.RequiredCheckpointCount,
                race.CountdownRemainingSeconds,
                race.IsPaused);
            _view.Apply(snapshot);
        }

        private void EnsureView()
        {
            if (_view != null || document == null)
            {
                return;
            }

            // UIDocument creates its runtime panel during its own OnEnable. The presenter is a
            // separate component, so the root may legitimately be null during Awake; resolving it
            // lazily keeps the binding independent of component execution order.
            var root = document.rootVisualElement;
            if (root != null)
            {
                _view = new SliceHudView(root);
            }
        }
    }
}
