using System;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Multi-participant grid, synchronized start, and standings adapter (T-10.3.1–T-10.3.6,
    /// D-060/D-061).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This component is additive. A scene without it keeps the single-participant
    /// <see cref="RaceController"/> path byte-for-byte: no grid, no extra participant, no standings
    /// aggregate, and the unchanged solo <see cref="RaceResult"/>. When present, it builds a
    /// <see cref="TrackPath"/> from the authored <see cref="TrackLayout"/>, places the player and
    /// every assigned <see cref="AIDriver"/> on a validated <see cref="RaceGridLayout"/>, holds all
    /// cars until the race rules reach <see cref="RaceState.Running"/> (GO), and feeds the
    /// allocation-free <see cref="RaceStandings"/> tracker.
    /// </para>
    /// <para>
    /// The player remains the only physics vehicle (D-059). The grid controller merely freezes its
    /// chassis and disables the vehicle controller during the hold, then returns a simulated,
    /// enabled player on release. Release is fail-safe: it happens on any non-<see cref="RaceState.Countdown"/>
    /// state, on the explicit seam, or when the unpaused hold outlives the remaining countdown plus a
    /// fixed margin, and it never re-applies a cached kinematic flag. AI cars are held by
    /// <see cref="AIDriver.SetStartHold(bool)"/>.
    /// </para>
    /// <para>
    /// The HUD polls <see cref="CopyStandingsTo"/> or <see cref="TryGetStanding"/>; both are
    /// allocation-free after initialization.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-100)]
    public sealed class RaceGridController : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.RaceGrid] ";

        [Header("Scene wiring")]
        [SerializeField] private RaceController race;
        [SerializeField] private VehicleController player;
        [SerializeField] private AIDriver[] opponents;
        [SerializeField] private TrackLayout trackLayout;
        [SerializeField] private EnvironmentLayoutVariant variant = EnvironmentLayoutVariant.Signature;

        [Header("Starting grid")]
        [SerializeField, Min(0f)] private float startLineDistance = 20f;
        [SerializeField, Min(0f)] private float playerSpawnHeight = 0.5f;
        [SerializeField] private RaceGridSettings gridSettings = RaceGridSettings.Default;
        [SerializeField] private bool releaseWhenRaceRunning = true;

        private readonly RaceGridSlot[] _slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];
        private readonly int[] _participantIndices = new int[RaceGridLayout.MaxParticipants];
        private readonly Rigidbody[] _participantBodies = new Rigidbody[RaceGridLayout.MaxParticipants];

        /// <summary>
        /// Extra unpaused seconds the grid tolerates a Countdown that never completes before it
        /// releases the field. Normal runs release on GO well inside this margin.
        /// </summary>
        private const float HoldTimeoutMarginSeconds = 2f;

        private TrackPath _path;
        private RaceStandings _standings;
        private Rigidbody _playerBody;
        private int _participantCount;
        private bool _initialized;
        private bool _holding;
        private bool _playerWasKinematic;
        private bool _playerWasEnabled;
        private float _holdElapsedSeconds;
        private float _holdTimeoutSeconds;

        /// <summary>True after a successful grid initialization.</summary>
        public bool IsInitialized => _initialized;

        /// <summary>True while every car is held on the grid waiting for GO.</summary>
        public bool IsHolding => _holding;

        /// <summary>Number of grid participants, including the player.</summary>
        public int ParticipantCount => _participantCount;

        /// <summary>The standings tracker, or null before initialization.</summary>
        public RaceStandings Standings => _standings;

        /// <summary>1-based player position, or zero before initialization.</summary>
        public int PlayerRank => _standings != null ? _standings.PlayerRank : 0;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the deterministic Phase-10 AI-grid scene builder
        /// (T-10.4.1). Runtime behavior is unchanged.
        /// </summary>
        public void ConfigureForAuthoring(
            RaceController raceController,
            VehicleController playerController,
            AIDriver[] opponentDrivers,
            TrackLayout layout,
            EnvironmentLayoutVariant layoutVariant,
            float startLine,
            float spawnHeight)
        {
            race = raceController;
            player = playerController;
            opponents = opponentDrivers;
            trackLayout = layout;
            variant = layoutVariant;
            startLineDistance = startLine;
            playerSpawnHeight = spawnHeight;
        }
#endif

        private void Start()
        {
            if (_initialized || race == null || player == null || trackLayout == null)
            {
                return;
            }

            TrackPath path;
            try
            {
                path = TrackPath.Create(trackLayout, variant);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Could not generate the grid path: " + exception.Message, this);
                enabled = false;
                return;
            }

            var gates = BuildGateDistances(trackLayout);
            if (!Initialize(path, race, player, opponents, startLineDistance, gates, gridSettings))
            {
                enabled = false;
            }
        }

        /// <summary>
        /// Explicit initialization used by scene wiring and tests. Builds no scene objects; the
        /// caller supplies the player, opponents, path, and gate distances.
        /// </summary>
        /// <param name="path">Validated path shared by the grid.</param>
        /// <param name="raceController">Race adapter that receives checkpoint/finish reports.</param>
        /// <param name="playerController">Player vehicle controller.</param>
        /// <param name="opponentDrivers">AI opponents, at most six.</param>
        /// <param name="startLine">Path distance of the start line in meters.</param>
        /// <param name="gateDistances">Checkpoint distances followed by the finish distance.</param>
        /// <param name="settings">Grid spacing envelope.</param>
        /// <returns>True when every participant was placed and registered.</returns>
        public bool Initialize(
            TrackPath path,
            RaceController raceController,
            VehicleController playerController,
            AIDriver[] opponentDrivers,
            float startLine,
            float[] gateDistances,
            RaceGridSettings settings)
        {
            if (path == null || raceController == null || playerController == null)
            {
                Debug.LogError(LogPrefix + "Path, race controller, and player are required.", this);
                return false;
            }

            if (gateDistances == null || gateDistances.Length < 2)
            {
                Debug.LogError(LogPrefix + "At least one checkpoint and one finish distance are required.", this);
                return false;
            }

            Shutdown();

            race = raceController;
            player = playerController;
            opponents = opponentDrivers;
            var opponentCount = opponents != null ? opponents.Length : 0;
            var participantCount = opponentCount + 1;
            if (participantCount > RaceGridLayout.MaxParticipants)
            {
                Debug.LogError(LogPrefix + "Participant count " + participantCount +
                               " exceeds the " + RaceGridLayout.MaxParticipants + "-car cap.", this);
                return false;
            }

            if (!RaceGridLayout.TryCreate(
                    path,
                    startLine,
                    participantCount,
                    settings,
                    _slots,
                    out _participantCount,
                    out var gridError))
            {
                Debug.LogError(LogPrefix + gridError, this);
                return false;
            }

            _path = path;
            _playerBody = playerController.GetComponent<Rigidbody>();
            if (_playerBody == null)
            {
                Debug.LogError(LogPrefix + "Player has no Rigidbody.", this);
                return false;
            }

            _standings = new RaceStandings(
                gateDistances.Length - 1,
                gateDistances,
                path.TotalLength,
                path.IsClosedLoop);

            var playerIndex = raceController.RegisterParticipant(_playerBody);
            if (playerIndex < 0)
            {
                Debug.LogError(LogPrefix + "Could not register the player with the race.", this);
                return false;
            }

            var playerStanding = _standings.RegisterParticipant(true);
            if (playerStanding < 0)
            {
                Debug.LogError(LogPrefix + "Could not register the player with the standings.", this);
                return false;
            }

            _participantIndices[0] = playerStanding;
            _participantBodies[0] = _playerBody;

            for (var i = 0; i < opponentCount; i++)
            {
                var driver = opponentDrivers[i];
                if (driver == null)
                {
                    Debug.LogError(LogPrefix + "Opponent " + i + " is missing.", this);
                    return false;
                }

                var body = driver.GetComponent<Rigidbody>();
                if (body == null)
                {
                    Debug.LogError(LogPrefix + "Opponent " + i + " has no Rigidbody.", this);
                    return false;
                }

                var opponentIndex = raceController.RegisterParticipant(body);
                if (opponentIndex < 0)
                {
                    Debug.LogError(LogPrefix + "Could not register opponent " + i + " with the race.", this);
                    return false;
                }

                var opponentStanding = _standings.RegisterParticipant(false);
                if (opponentStanding < 0)
                {
                    Debug.LogError(LogPrefix + "Could not register opponent " + i + " with the standings.", this);
                    return false;
                }

                _participantIndices[i + 1] = opponentStanding;
                _participantBodies[i + 1] = body;
            }

            PlaceOnGrid(opponentDrivers);

            raceController.CheckpointAccepted += HandleCheckpointAccepted;
            raceController.ParticipantFinished += HandleParticipantFinished;
            raceController.Retried += HandleRaceRetried;

            _initialized = true;
            SetStartHold(true);
            return true;
        }

        /// <summary>Releases the grid before the race rules report GO (test/authoring seam).</summary>
        public void ReleaseStart()
        {
            SetStartHold(false);
        }

        private void Update()
        {
            if (!_initialized || !_holding || !releaseWhenRaceRunning)
            {
                return;
            }

            if (race == null)
            {
                Debug.LogWarning(
                    LogPrefix + "The grid lost its race controller while holding; releasing the field.",
                    this);
                SetStartHold(false);
                return;
            }

            // Any post-countdown state releases: comparing against Countdown (instead of Running)
            // also hands back a race that reached Finished before the grid observed GO.
            if (race.State != RaceState.Countdown)
            {
                SetStartHold(false);
                return;
            }

            // Fail-safe: a rules core that never initializes reports Countdown forever. Without a
            // bounded hold the player would stay disabled and kinematic for the whole session,
            // which looks exactly like a car that revs but cannot move. Only unpaused time counts
            // so a deliberate pause cannot trip the release.
            if (!race.IsPaused)
            {
                _holdElapsedSeconds += Time.unscaledDeltaTime;
            }

            if (_holdElapsedSeconds >= _holdTimeoutSeconds)
            {
                Debug.LogWarning(
                    LogPrefix + "The race stayed in Countdown for " +
                    _holdTimeoutSeconds.ToString("0.0") +
                    " s while the grid was holding; releasing the field so the player can drive.",
                    this);
                SetStartHold(false);
            }
        }

        private void FixedUpdate()
        {
            if (!_initialized)
            {
                return;
            }

            if (_holding)
            {
                HoldPlayer();
                return;
            }

            UpdateStandingsDistances();
        }

        /// <summary>
        /// Copies the current ordered standings into a caller-owned buffer. The method allocates
        /// nothing; it returns zero when the buffer is null, too small, or before initialization.
        /// </summary>
        public int CopyStandingsTo(RaceStandingEntry[] destination)
        {
            return _standings != null ? _standings.CopyStandingsTo(destination) : 0;
        }

        /// <summary>Reads one zero-based rank into an entry without allocating.</summary>
        public bool TryGetStanding(int rank, out RaceStandingEntry entry)
        {
            if (_standings == null)
            {
                entry = default;
                return false;
            }

            return _standings.TryGetStanding(rank, out entry);
        }

        /// <summary>Reads a computed grid slot by participant order, or returns false.</summary>
        public bool TryGetGridSlot(int index, out RaceGridSlot slot)
        {
            if (!_initialized || index < 0 || index >= _participantCount)
            {
                slot = default;
                return false;
            }

            slot = _slots[index];
            return true;
        }

        private void SetStartHold(bool held)
        {
            if (!_initialized && !held)
            {
                return;
            }

            if (_holding == held)
            {
                return;
            }

            _holding = held;
            if (held)
            {
                _playerWasKinematic = _playerBody != null && _playerBody.isKinematic;
                _playerWasEnabled = player != null && player.enabled;
                _holdElapsedSeconds = 0f;
                _holdTimeoutSeconds = HoldTimeoutMarginSeconds;
                if (race != null)
                {
                    _holdTimeoutSeconds += (float)race.CountdownRemainingSeconds;
                }

                if (_playerWasKinematic)
                {
                    Debug.LogWarning(
                        LogPrefix + "The player chassis was already kinematic when the grid took " +
                        "its hold; it will be returned to a simulated body when the hold releases.",
                        this);
                }

                if (player != null)
                {
                    player.enabled = false;
                }

                if (_playerBody != null)
                {
                    _playerBody.linearVelocity = Vector3.zero;
                    _playerBody.angularVelocity = Vector3.zero;
                    _playerBody.isKinematic = true;
                }

                HoldOpponents(true);
                return;
            }

            HoldOpponents(false);
            _holdElapsedSeconds = 0f;

            if (_playerBody != null)
            {
                // D-059: the player is the only physics vehicle, so releasing the hold must always
                // hand back a simulated chassis. Restoring the cached isKinematic could re-apply a
                // stale freeze (the body was kinematic before this hold, or a second owner froze it
                // during the hold); the controller would then be enabled and the drivetrain would
                // apply torque to a body that never integrates -- the car revs but cannot move.
                _playerBody.isKinematic = false;
                _playerBody.useGravity = true;
                _playerBody.WakeUp();
            }

            if (player != null)
            {
                player.enabled = _playerWasEnabled;
            }
        }

        private void HoldOpponents(bool held)
        {
            if (opponents == null)
            {
                return;
            }

            for (var i = 0; i < opponents.Length; i++)
            {
                if (opponents[i] != null)
                {
                    opponents[i].SetStartHold(held);
                }
            }
        }

        private void HoldPlayer()
        {
            if (_playerBody == null || _playerBody.isKinematic)
            {
                return;
            }

            _playerBody.linearVelocity = Vector3.zero;
            _playerBody.angularVelocity = Vector3.zero;
        }

        private void UpdateStandingsDistances()
        {
            if (_standings == null || _path == null || _playerBody == null)
            {
                return;
            }

            _standings.UpdatePathDistance(
                _participantIndices[0],
                _path.ProjectDistance(_playerBody.position));

            if (opponents == null)
            {
                return;
            }

            for (var i = 0; i < opponents.Length; i++)
            {
                var driver = opponents[i];
                if (driver != null)
                {
                    _standings.UpdatePathDistance(_participantIndices[i + 1], driver.PathDistance);
                }
            }
        }

        private void PlaceOnGrid(AIDriver[] opponentDrivers)
        {
            var playerSlot = _slots[0];
            player.ResetVehicleState(
                playerSlot.Position + (Vector3.up * playerSpawnHeight),
                playerSlot.Rotation);

            if (opponentDrivers == null)
            {
                return;
            }

            for (var i = 0; i < opponentDrivers.Length; i++)
            {
                var slot = _slots[i + 1];
                opponentDrivers[i].Initialize(
                    _path,
                    slot.Distance,
                    slot.LateralOffset,
                    player.transform,
                    0f);
                opponentDrivers[i].SetStartHold(true);
            }
        }

        private void HandleCheckpointAccepted(int checkpointIndex, Rigidbody body)
        {
            if (!_initialized || _standings == null)
            {
                return;
            }

            var slot = IndexOfParticipant(body);
            if (slot >= 0)
            {
                _standings.TryAcceptCheckpoint(_participantIndices[slot], checkpointIndex);
            }
        }

        private void HandleParticipantFinished(Rigidbody body)
        {
            if (!_initialized || _standings == null || race == null)
            {
                return;
            }

            var slot = IndexOfParticipant(body);
            if (slot < 0)
            {
                return;
            }

            _standings.TryFinish(_participantIndices[slot], race.ElapsedSeconds);

            // The player's finish closes the race. Freezing here implements the D-061 rule that the
            // checkpoint+distance ranking cannot change after the finish.
            if (slot == 0)
            {
                _standings.Freeze();
            }
        }

        private void HandleRaceRetried()
        {
            if (!_initialized || _standings == null)
            {
                return;
            }

            _standings.ResetProgress();
            PlaceOnGrid(opponents);
            SetStartHold(true);
        }

        private int IndexOfParticipant(Rigidbody body)
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

        private static float[] BuildGateDistances(TrackLayout layout)
        {
            var gates = new float[EnvironmentRaceRules.CheckpointCount + 1];
            for (var i = 0; i < EnvironmentRaceRules.CheckpointCount; i++)
            {
                gates[i] = EnvironmentRaceRules.GetCheckpointDistance(layout, i);
            }

            gates[gates.Length - 1] = EnvironmentRaceRules.GetFinishDistance(layout);
            return gates;
        }

        private void Shutdown()
        {
            if (_holding)
            {
                SetStartHold(false);
            }

            if (race != null)
            {
                race.CheckpointAccepted -= HandleCheckpointAccepted;
                race.ParticipantFinished -= HandleParticipantFinished;
                race.Retried -= HandleRaceRetried;
            }

            _standings = null;
            _participantCount = 0;
            _initialized = false;
            _holding = false;
        }

        private void OnDestroy()
        {
            Shutdown();
        }
    }
}
