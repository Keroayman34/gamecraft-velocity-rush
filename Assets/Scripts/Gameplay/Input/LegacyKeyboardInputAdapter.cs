using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Legacy Input Manager keyboard adapter (TDD §37.1/§37.2). Reads the keyboard and exposes
    /// normalized <see cref="IVehicleInput"/> samples plus frame-scoped <see cref="IAppInput"/>
    /// intents, so gameplay code never references a device API.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Provisional fixed bindings (see <see cref="KeyboardBindings"/>): W/Up = throttle,
    /// S/Down = brake, A/Left = steer left, D/Right = steer right, Space = handbrake,
    /// P = pause intent, R = retry intent. Escape is intentionally unbound and reserved for
    /// future overlay-close behavior (TDD §37.2/§37.6); it must never become a pause dependency.
    /// Bindings are prototype-only and not final (OQ-A03; TDD §37.7).
    /// </para>
    /// <para>
    /// Requires the legacy Input Manager (the project's current active input handling). This
    /// adapter performs no allocation per frame and no response shaping — shaping belongs to the
    /// vehicle domain. Intents are edge-triggered and true only for the frame they are detected;
    /// the intended consumer contract is one logical reader sampling in <c>Update</c>.
    /// </para>
    /// <para>
    /// Focus or application-pause loss clears both the adapter's own state and Unity's internal
    /// key/axis state (<see cref="Input.ResetInputAxes"/>) once at the transition, so stale held
    /// keys cannot resurface when focus returns (stale-input guard, TDD §37.3). The composition
    /// root additionally binds an <see cref="IAppStateSource"/> via
    /// <see cref="BindAppStateSource"/>; every accepted application-state transition then performs
    /// a full driving-input reset (TDD §37.3) without the adapter referencing GameFlow's concrete
    /// type.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class LegacyKeyboardInputAdapter : MonoBehaviour, IVehicleInput, IAppInput
    {
        private readonly KeyboardInputState _state = new KeyboardInputState();
        private readonly AppInputState _appState = new AppInputState();

        private IAppStateSource _appStateSource;
        private bool _hasFocus = true;

        /// <summary>Current normalized driving snapshot.</summary>
        public VehicleInputSnapshot Snapshot => _state.Snapshot;

        /// <inheritdoc />
        public float Throttle => _state.Snapshot.Throttle;

        /// <inheritdoc />
        public float Brake => _state.Snapshot.Brake;

        /// <inheritdoc />
        public float Steering => _state.Snapshot.Steering;

        /// <inheritdoc />
        public bool Handbrake => _state.Snapshot.Handbrake;

        /// <inheritdoc />
        public bool PausePressed => _appState.PausePressed;

        /// <inheritdoc />
        public bool RetryPressed => _appState.RetryPressed;

        private void OnEnable()
        {
            // Re-sync on every enable: a disable/enable cycle across a focus change must not
            // leave the adapter permanently gated off.
            _hasFocus = Application.isFocused;
            ResetInput();
        }

        private void OnDisable()
        {
            ResetInput();
        }

        private void Update()
        {
            if (!_hasFocus)
            {
                ResetInput();
                return;
            }

            _state.Sample(
                throttleDown: Input.GetKey(KeyboardBindings.ThrottlePrimary) || Input.GetKey(KeyboardBindings.ThrottleAlternate),
                brakeDown: Input.GetKey(KeyboardBindings.BrakePrimary) || Input.GetKey(KeyboardBindings.BrakeAlternate),
                steerLeftDown: Input.GetKey(KeyboardBindings.SteerLeftPrimary) || Input.GetKey(KeyboardBindings.SteerLeftAlternate),
                steerRightDown: Input.GetKey(KeyboardBindings.SteerRightPrimary) || Input.GetKey(KeyboardBindings.SteerRightAlternate),
                handbrakeDown: Input.GetKey(KeyboardBindings.Handbrake));

            _appState.Sample(
                pauseDown: Input.GetKeyDown(KeyboardBindings.Pause),
                retryDown: Input.GetKeyDown(KeyboardBindings.Retry));
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _hasFocus = hasFocus;

            if (!hasFocus)
            {
                ClearPlatformInput();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                ClearPlatformInput();
            }
        }

        /// <summary>Returns every driving channel and intent to neutral.</summary>
        public void ResetInput()
        {
            _state.Reset();
            _appState.Reset();
        }

        /// <summary>
        /// Binds the application-state source. The adapter resets all driving input on every
        /// accepted transition (TDD §37.3) and never reaches into GameFlow directly.
        /// </summary>
        /// <remarks>
        /// Safe to call repeatedly: re-binding the same instance is a no-op, and binding a new
        /// source unsubscribes from the previous one. Pass <c>null</c> to detach.
        /// </remarks>
        /// <param name="appStateSource">State-transition source, or <c>null</c> to detach.</param>
        public void BindAppStateSource(IAppStateSource appStateSource)
        {
            if (ReferenceEquals(_appStateSource, appStateSource))
            {
                return;
            }

            UnbindAppStateSource();
            _appStateSource = appStateSource;

            if (_appStateSource != null)
            {
                _appStateSource.StateChanged += HandleAppStateChanged;
            }
        }

        /// <summary>Detaches from the bound state source, if any.</summary>
        public void UnbindAppStateSource()
        {
            if (_appStateSource == null)
            {
                return;
            }

            _appStateSource.StateChanged -= HandleAppStateChanged;
            _appStateSource = null;
        }

        private void HandleAppStateChanged(AppState state)
        {
            // Any accepted transition invalidates held samples and pending intents, so a key held
            // across a state boundary cannot leak into the new state (TDD §37.3).
            ResetInput();
        }

        private void OnDestroy()
        {
            UnbindAppStateSource();
        }

        private void ClearPlatformInput()
        {
            // Clear Unity's own key/axis state once at the transition (never per frame) so a key
            // released while unfocused cannot stay virtually held after focus returns.
            Input.ResetInputAxes();
            ResetInput();
        }
    }
}
