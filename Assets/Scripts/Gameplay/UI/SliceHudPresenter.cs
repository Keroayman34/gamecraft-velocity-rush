using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Connects a <see cref="RaceController"/> and the vehicle telemetry stream to the UI Toolkit
    /// slice HUD (T-7.5b).
    /// </summary>
    /// <remarks>
    /// <para>
    /// State is polled at a low, inspector-configurable refresh rate (10 Hz by default) so the HUD
    /// is decoupled from the render rate; results are event-driven and shown exactly once.
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

        private void Awake()
        {
            _telemetry = telemetrySource as IVehicleTelemetry;
            _refreshInterval = 1f / Mathf.Max(1f, refreshHz);
        }

        private void Start()
        {
            EnsureView();
        }

        private void OnEnable()
        {
            if (race == null)
            {
                return;
            }

            race.RaceFinished += HandleRaceFinished;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_subscribed && race != null)
            {
                race.RaceFinished -= HandleRaceFinished;
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

            _elapsedSinceRefresh += Time.unscaledDeltaTime;
            if (_elapsedSinceRefresh < _refreshInterval)
            {
                return;
            }

            _elapsedSinceRefresh = 0f;
            var snapshot = SliceHudModel.Evaluate(
                _telemetry.Telemetry,
                race.State,
                race.ElapsedSeconds,
                race.AcceptedCheckpointCount,
                race.RequiredCheckpointCount);
            _view.Apply(snapshot);
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
