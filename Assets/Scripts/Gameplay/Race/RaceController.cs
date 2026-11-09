using System;
using System.Globalization;
using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Scene adapter for the prototype one-way timed run: owns the pure <see cref="RaceRules"/>
    /// core, converts track trigger signals into checkpoint/finish requests, reads the existing
    /// retry intent, and resets the resident race without reloading the scene (TDD §29 S4/S5,
    /// §35.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Temporary development-prototype component. It is not the commercial GameFlow, owns no UI,
    /// rewards, progression, save, or multiplayer state, and adds no singleton, event bus, or
    /// service locator. All wiring is explicit scene references.
    /// </para>
    /// <para>
    /// The vehicle is the authoritative participant: only trigger contacts whose attached
    /// Rigidbody is the assigned vehicle chassis are reported. The race timer uses a monotonic
    /// <see cref="StopwatchRaceClock"/>; countdown time is excluded from the result.
    /// </para>
    /// <para>
    /// Retry is a race-state reset: Countdown state, cleared progress/result guards, vehicle
    /// returned to its captured spawn pose, and linear/angular velocity cleared. It never calls
    /// SceneManager and never destroys/recreates the scene or race objects.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class RaceController : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Race] ";

        [Header("Prototype race configuration (placeholder — not final design)")]
        [SerializeField, Min(0f)] private float countdownSeconds = 2f;

        [Header("Scene wiring")]
        [SerializeField] private VehicleController vehicle;
        [SerializeField] private MonoBehaviour appInputSource;
        [SerializeField] private CheckpointTrigger[] checkpoints;
        [SerializeField] private FinishTrigger finishTrigger;

        private RaceRules _rules;
        private IAppInput _appInput;
        private Rigidbody _vehicleBody;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private bool _initialized;

        /// <summary>Current prototype race state; Countdown before initialization.</summary>
        public RaceState State => _rules != null ? _rules.State : RaceState.Countdown;

        /// <summary>Live/frozen elapsed race seconds, excluding countdown.</summary>
        public double ElapsedSeconds => _rules != null ? _rules.ElapsedSeconds : 0d;

        /// <summary>Accepted checkpoint count (also the next expected index).</summary>
        public int AcceptedCheckpointCount => _rules != null ? _rules.AcceptedCheckpointCount : 0;

        /// <summary>Required checkpoint count for this run.</summary>
        public int RequiredCheckpointCount => _rules != null ? _rules.RequiredCheckpointCount : 0;

        /// <summary>Completed result, or null before a valid finish (and after retry).</summary>
        public RaceResult? Result => _rules != null ? _rules.Result : null;

        /// <summary>
        /// Forwards the exactly-once <see cref="RaceRules.RaceFinished"/> notification to an
        /// explicit test/logger observer. No UI or progression system is attached in T5.4.
        /// </summary>
        public event Action<RaceResult> RaceFinished;

        private void Awake()
        {
            if (vehicle == null)
            {
                Fail("No VehicleController assigned.");
                return;
            }

            _vehicleBody = vehicle.GetComponent<Rigidbody>();
            if (_vehicleBody == null)
            {
                Fail("Assigned vehicle has no Rigidbody.");
                return;
            }

            _appInput = appInputSource as IAppInput;
            if (_appInput == null)
            {
                Fail("No app input source implementing IAppInput assigned.");
                return;
            }

            if (!ValidateCheckpoints(out var checkpointError))
            {
                Fail(checkpointError);
                return;
            }

            if (finishTrigger == null)
            {
                Fail("No FinishTrigger assigned.");
                return;
            }

            if (float.IsNaN(countdownSeconds) || float.IsInfinity(countdownSeconds) || countdownSeconds < 0f)
            {
                Fail("Countdown seconds must be finite and non-negative.");
                return;
            }

            _rules = new RaceRules(new StopwatchRaceClock(), countdownSeconds, checkpoints.Length);
            _rules.RaceFinished += HandleRaceFinished;

            _spawnPosition = _vehicleBody.position;
            _spawnRotation = _vehicleBody.rotation;
            _initialized = true;
        }

        private void OnDestroy()
        {
            if (_rules != null)
            {
                _rules.RaceFinished -= HandleRaceFinished;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            _rules.Tick();

            // Single logical reader of the adapter's one-frame retry intent (TDD §37.1).
            if (_appInput.RetryPressed)
            {
                Retry();
            }
        }

        /// <summary>
        /// Reports a checkpoint contact. Non-participant colliders and stale/duplicate indices are
        /// rejected by the participant check and <see cref="RaceRules.TryAcceptCheckpoint"/>.
        /// </summary>
        public void ReportCheckpoint(int checkpointIndex, Rigidbody participant)
        {
            if (!_initialized || !IsParticipant(participant))
            {
                return;
            }

            _rules.TryAcceptCheckpoint(checkpointIndex);
        }

        /// <summary>
        /// Reports a finish-line contact. The rule core accepts at most one result per attempt and
        /// only after every required checkpoint was accepted while running.
        /// </summary>
        public void ReportFinish(Rigidbody participant)
        {
            if (!_initialized || !IsParticipant(participant))
            {
                return;
            }

            _rules.TryFinish();
        }

        /// <summary>
        /// Resets the race in the resident scene: Countdown, zero progress, zero elapsed time,
        /// cleared result/finish guards, vehicle returned to its captured spawn pose with zero
        /// linear/angular velocity. No scene load, additive reload, or object recreation.
        /// </summary>
        public void Retry()
        {
            if (!_initialized)
            {
                return;
            }

            _rules.Reset();
            vehicle.ResetVehicleState(_spawnPosition, _spawnRotation);

            Debug.Log(LogPrefix + "Retry: race reset to Countdown in the resident scene (no scene reload).", this);
        }

        private bool ValidateCheckpoints(out string error)
        {
            if (checkpoints == null || checkpoints.Length == 0)
            {
                error = "No checkpoint triggers assigned.";
                return false;
            }

            for (var i = 0; i < checkpoints.Length; i++)
            {
                var checkpoint = checkpoints[i];
                if (checkpoint == null)
                {
                    error = "Checkpoint trigger at array slot " + i + " is missing.";
                    return false;
                }

                if (checkpoint.Index != i)
                {
                    error = "Checkpoint trigger at array slot " + i + " reports index " + checkpoint.Index +
                            "; indices must be stable sequential values matching array order.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private bool IsParticipant(Rigidbody body)
        {
            return body != null && body == _vehicleBody;
        }

        private void HandleRaceFinished(RaceResult result)
        {
            // Forward before logging so an attached observer sees the same exactly-once event.
            RaceFinished?.Invoke(result);

            Debug.Log(
                LogPrefix + "Finished: " + result.ToString() + ".",
                this);
        }

        private void Fail(string message)
        {
            Debug.LogError(LogPrefix + message + " Race controller disabled.", this);
            enabled = false;
        }
    }
}
