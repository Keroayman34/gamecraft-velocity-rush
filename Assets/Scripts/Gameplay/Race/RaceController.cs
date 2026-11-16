using System;
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

        private readonly Rigidbody[] _participantBodies = new Rigidbody[RaceStandings.MaxParticipants];
        private readonly int[] _participantCheckpointCounts = new int[RaceStandings.MaxParticipants];
        private readonly bool[] _participantFinished = new bool[RaceStandings.MaxParticipants];
        private int _participantCount;
        private int _playerParticipantIndex = -1;

        /// <summary>Current prototype race state; Countdown before initialization.</summary>
        public RaceState State => _rules != null ? _rules.State : RaceState.Countdown;

        /// <summary>Live/frozen elapsed race seconds, excluding countdown and paused time.</summary>
        public double ElapsedSeconds => _rules != null ? _rules.ElapsedSeconds : 0d;

        /// <summary>Remaining countdown seconds (zero once running or finished).</summary>
        public double CountdownRemainingSeconds => _rules != null ? _rules.CountdownRemainingSeconds : 0d;

        /// <summary>True while the attempt is paused; the HUD and pause driver read this.</summary>
        public bool IsPaused => _rules != null && _rules.IsPaused;

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

        /// <summary>
        /// Raised exactly once per accepted pause or resume transition, after the rules accepted
        /// it. The HUD and the pause driver subscribe; no per-frame polling of the intent is needed.
        /// </summary>
        public event Action<bool> PausedChanged;

        /// <summary>
        /// Raised once for each accepted, in-order checkpoint report from any registered
        /// participant (T-10.3.3). Solo races have only the player registered, so behavior is
        /// unchanged; the event is additive and has no subscribers in the solo stack.
        /// </summary>
        public event Action<int, Rigidbody> CheckpointAccepted;

        /// <summary>
        /// Raised exactly once per participant when that participant completes every checkpoint and
        /// crosses the finish while running (T-10.3.4). The player's solo
        /// <see cref="RaceFinished"/> result is unaffected.
        /// </summary>
        public event Action<Rigidbody> ParticipantFinished;

        /// <summary>
        /// Raised after <see cref="Retry"/> resets the rules and participant progress so a grid
        /// controller can return every car to its slot (T-10.3.2).
        /// </summary>
        public event Action Retried;

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
            _playerParticipantIndex = RegisterParticipant(_vehicleBody);
            _initialized = true;
        }

        /// <summary>
        /// Rebinds the race participant and spawn pose to a runtime-spawned vehicle (T-8.11 debug
        /// selection flow). The race rules and checkpoint wiring are unchanged; only the body used
        /// for participant checks and the retry pose move to the selected rig.
        /// </summary>
        /// <param name="newVehicle">Selected vehicle controller.</param>
        /// <param name="spawnPosition">Spawn pose captured before the template was replaced.</param>
        /// <param name="spawnRotation">Spawn rotation captured before the template was replaced.</param>
        /// <returns>True when the participant was rebound.</returns>
        public bool Configure(VehicleController newVehicle, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            if (!_initialized || newVehicle == null)
            {
                return false;
            }

            var newBody = newVehicle.GetComponent<Rigidbody>();
            if (newBody == null)
            {
                return false;
            }

            vehicle = newVehicle;
            _vehicleBody = newBody;
            _spawnPosition = spawnPosition;
            _spawnRotation = spawnRotation;
            if (_playerParticipantIndex >= 0)
            {
                _participantBodies[_playerParticipantIndex] = newBody;
            }

            return true;
        }

        /// <summary>
        /// Registers an additional race participant (T-10.3.3). The player is registered
        /// automatically. Returns the stable participant index, or -1 when the seven-car cap is
        /// reached. Solo races never register another body, so their behavior is unchanged.
        /// </summary>
        /// <param name="body">Participant chassis rigidbody; null is rejected.</param>
        public int RegisterParticipant(Rigidbody body)
        {
            if (body == null)
            {
                return -1;
            }

            for (var i = 0; i < _participantCount; i++)
            {
                if (_participantBodies[i] == body)
                {
                    return i;
                }
            }

            if (_participantCount >= RaceStandings.MaxParticipants)
            {
                return -1;
            }

            var index = _participantCount;
            _participantCount++;
            _participantBodies[index] = body;
            _participantCheckpointCounts[index] = 0;
            _participantFinished[index] = false;
            return index;
        }

        /// <summary>
        /// The participant index assigned to a registered body, or -1 when the body is not part of
        /// this race. Never allocates.
        /// </summary>
        public int GetParticipantIndex(Rigidbody body)
        {
            return FindParticipant(body);
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

            // Single logical reader of the adapter's one-frame pause/retry intents (TDD §37.1).
            if (_appInput.PausePressed)
            {
                TogglePause();
            }

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
            if (!_initialized)
            {
                return;
            }

            var slot = FindParticipant(participant);
            if (slot < 0)
            {
                return;
            }

            var accepted = false;
            if (slot == _playerParticipantIndex)
            {
                accepted = _rules.TryAcceptCheckpoint(checkpointIndex);
                if (accepted)
                {
                    _participantCheckpointCounts[slot] = _rules.AcceptedCheckpointCount;
                }
            }
            else
            {
                // Opponents use the same ordered-checkpoint contract as the player, but their
                // progress is independent of the player's solo RaceRules state. They may only
                // advance while the shared race is live.
                if (_rules.State == RaceState.Running && !_rules.IsPaused &&
                    !_participantFinished[slot] &&
                    checkpointIndex >= 0 && checkpointIndex < checkpoints.Length &&
                    checkpointIndex == _participantCheckpointCounts[slot])
                {
                    _participantCheckpointCounts[slot] = checkpointIndex + 1;
                    accepted = true;
                }
            }

            if (accepted)
            {
                CheckpointAccepted?.Invoke(checkpointIndex, participant);
            }
        }

        /// <summary>
        /// Reports a finish-line contact. The player's solo rule core accepts at most one result
        /// per attempt and only after every required checkpoint. Every registered participant also
        /// emits <see cref="ParticipantFinished"/> exactly once after its own ordered checkpoint
        /// sequence (T-10.3.4).
        /// </summary>
        public void ReportFinish(Rigidbody participant)
        {
            if (!_initialized)
            {
                return;
            }

            var slot = FindParticipant(participant);
            if (slot < 0)
            {
                return;
            }

            if (slot == _playerParticipantIndex)
            {
                if (_rules.TryFinish())
                {
                    _participantFinished[slot] = true;
                    ParticipantFinished?.Invoke(participant);
                }

                return;
            }

            if (_rules.State != RaceState.Running || _rules.IsPaused ||
                _participantFinished[slot] ||
                _participantCheckpointCounts[slot] < checkpoints.Length)
            {
                return;
            }

            _participantFinished[slot] = true;
            ParticipantFinished?.Invoke(participant);
        }

        /// <summary>
        /// Freezes or resumes the attempt. Pausing is refused after a valid finish, so a completed
        /// result can never be altered by the pause key.
        /// </summary>
        /// <param name="paused">True to pause, false to resume.</param>
        public void SetPaused(bool paused)
        {
            if (_rules == null)
            {
                return;
            }

            var changed = paused ? _rules.Pause() : _rules.Resume();
            if (changed)
            {
                PausedChanged?.Invoke(_rules.IsPaused);
            }
        }

        /// <summary>Toggles the pause state and reports it through <see cref="PausedChanged"/>.</summary>
        public void TogglePause()
        {
            SetPaused(!IsPaused);
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

            // A retry always leaves pause first; otherwise Time.timeScale could stay frozen and
            // the new countdown could run invisibly.
            if (_rules.IsPaused)
            {
                _rules.Resume();
                PausedChanged?.Invoke(false);
            }

            _rules.Reset();
            ResetParticipantProgress();
            vehicle.ResetVehicleState(_spawnPosition, _spawnRotation);
            Retried?.Invoke();

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

        private int FindParticipant(Rigidbody body)
        {
            if (body == null)
            {
                return -1;
            }

            for (var i = 0; i < _participantCount; i++)
            {
                if (_participantBodies[i] == body)
                {
                    return i;
                }
            }

            return -1;
        }

        private void ResetParticipantProgress()
        {
            for (var i = 0; i < _participantCount; i++)
            {
                _participantCheckpointCounts[i] = 0;
                _participantFinished[i] = false;
            }
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
