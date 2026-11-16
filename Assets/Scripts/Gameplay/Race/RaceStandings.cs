using System;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Immutable HUD-facing snapshot of one participant's race position (T-10.3.6).
    /// </summary>
    /// <remarks>
    /// Returned by value from <see cref="RaceStandings"/>; creating or reading one never allocates.
    /// <see cref="Rank"/> is 1-based and follows the current order: finished participants first by
    /// finish position, then unfinished participants by checkpoint count and distance to the next
    /// gate. Ties fall back to participant registration order for determinism.
    /// </remarks>
    public readonly struct RaceStandingEntry
    {
        /// <summary>Stable zero-based participant registration index.</summary>
        public int ParticipantIndex { get; }

        /// <summary>True when this participant is the player.</summary>
        public bool IsPlayer { get; }

        /// <summary>Current 1-based race position.</summary>
        public int Rank { get; }

        /// <summary>Checkpoint count accepted for this participant.</summary>
        public int CheckpointCount { get; }

        /// <summary>Checkpoint count required to finish.</summary>
        public int RequiredCheckpointCount { get; }

        /// <summary>Last projected path distance in meters.</summary>
        public float PathDistance { get; }

        /// <summary>
        /// Remaining path distance to the next expected gate in meters. Frozen at the participant's
        /// finish and reported as zero for a finished participant.
        /// </summary>
        public float DistanceToNextGate { get; }

        /// <summary>True when this participant has crossed the finish line.</summary>
        public bool Finished { get; }

        /// <summary>1-based finish position, or zero while unfinished.</summary>
        public int FinishPosition { get; }

        /// <summary>Elapsed race seconds at this participant's finish, or zero while unfinished.</summary>
        public double FinishTimeSeconds { get; }

        /// <summary>Creates a snapshot entry.</summary>
        public RaceStandingEntry(
            int participantIndex,
            bool isPlayer,
            int rank,
            int checkpointCount,
            int requiredCheckpointCount,
            float pathDistance,
            float distanceToNextGate,
            bool finished,
            int finishPosition,
            double finishTimeSeconds)
        {
            ParticipantIndex = participantIndex;
            IsPlayer = isPlayer;
            Rank = rank;
            CheckpointCount = checkpointCount;
            RequiredCheckpointCount = requiredCheckpointCount;
            PathDistance = pathDistance;
            DistanceToNextGate = distanceToNextGate;
            Finished = finished;
            FinishPosition = finishPosition;
            FinishTimeSeconds = finishTimeSeconds;
        }
    }

    /// <summary>
    /// Allocation-free multi-participant position tracker and results aggregate (T-10.3.3–T-10.3.5,
    /// D-061).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ranking uses exactly two race facts: accepted checkpoint count, then remaining distance to
    /// the next gate along the path. Finished participants rank ahead of unfinished participants
    /// by their recorded finish order. Every tie falls back to the participant's registration
    /// index, so identical inputs always produce the same order.
    /// </para>
    /// <para>
    /// All storage is fixed-size and allocated once in the constructor. Ordering is a bounded
    /// insertion sort over at most <see cref="MaxParticipants"/> entries into a preallocated index
    /// array; there is no LINQ, no delegate allocation, and no per-frame collection. Polling
    /// <see cref="CopyStandingsTo"/> or <see cref="TryGetStanding"/> therefore allocates 0 B.
    /// </para>
    /// <para>
    /// <see cref="Freeze"/> closes the aggregate when the player finishes: later checkpoint,
    /// distance, or finish reports are ignored and the current order becomes the final result. This
    /// is the D-061 "ranking frozen at finish" rule and prevents post-finish position changes.
    /// </para>
    /// </remarks>
    public sealed class RaceStandings
    {
        /// <summary>Hard participant cap from D-060.</summary>
        public const int MaxParticipants = 7;

        private readonly int _checkpointCount;
        private readonly int _gateCount;
        private readonly float[] _gateDistances;
        private readonly float _totalLength;
        private readonly bool _closedLoop;

        private readonly bool[] _active = new bool[MaxParticipants];
        private readonly bool[] _isPlayer = new bool[MaxParticipants];
        private readonly int[] _checkpointCounts = new int[MaxParticipants];
        private readonly float[] _pathDistances = new float[MaxParticipants];
        private readonly bool[] _finished = new bool[MaxParticipants];
        private readonly int[] _finishPositions = new int[MaxParticipants];
        private readonly double[] _finishTimes = new double[MaxParticipants];
        private readonly int[] _order = new int[MaxParticipants];

        private int _participantCount;
        private int _finishCount;
        private bool _orderDirty = true;
        private bool _frozen;

        /// <summary>Checkpoints required before a participant may finish.</summary>
        public int RequiredCheckpointCount => _checkpointCount;

        /// <summary>Number of registered participants.</summary>
        public int ParticipantCount => _participantCount;

        /// <summary>True once the final order has been frozen.</summary>
        public bool IsFrozen => _frozen;

        /// <summary>Number of participants that have crossed the finish line.</summary>
        public int FinishCount => _finishCount;

        /// <summary>1-based rank of the player, or zero when no player is registered.</summary>
        public int PlayerRank
        {
            get
            {
                for (var i = 0; i < _participantCount; i++)
                {
                    if (_isPlayer[i])
                    {
                        return GetRankOf(i);
                    }
                }

                return 0;
            }
        }

        /// <summary>1-based finish position of the player, or zero while unfinished.</summary>
        public int PlayerFinishPosition
        {
            get
            {
                for (var i = 0; i < _participantCount; i++)
                {
                    if (_isPlayer[i])
                    {
                        return _finishPositions[i];
                    }
                }

                return 0;
            }
        }

        /// <param name="checkpointCount">Positive required checkpoint count.</param>
        /// <param name="gateDistances">
        /// Ordered path distances for every checkpoint followed by the finish gate. Length must be
        /// <paramref name="checkpointCount"/> + 1.
        /// </param>
        /// <param name="totalLength">Path total length in meters, used for closed-loop wrap.</param>
        /// <param name="closedLoop">Whether the path wraps behind the start line.</param>
        public RaceStandings(
            int checkpointCount,
            float[] gateDistances,
            float totalLength,
            bool closedLoop)
        {
            if (checkpointCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(checkpointCount), "At least one checkpoint is required.");
            }

            if (gateDistances == null || gateDistances.Length != checkpointCount + 1)
            {
                throw new ArgumentException(
                    "Gate distances must contain one entry per checkpoint plus the finish gate.",
                    nameof(gateDistances));
            }

            if (float.IsNaN(totalLength) || float.IsInfinity(totalLength) || totalLength <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(totalLength), "Total length must be finite and positive.");
            }

            var previous = -1f;
            for (var i = 0; i < gateDistances.Length; i++)
            {
                var gate = gateDistances[i];
                if (float.IsNaN(gate) || float.IsInfinity(gate) || gate < 0f || gate > totalLength + 0.001f)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(gateDistances),
                        "Gate distance at index " + i + " must be finite and within the path.");
                }

                if (gate < previous - 0.001f)
                {
                    throw new ArgumentException(
                        "Gate distances must be non-decreasing at index " + i + ".",
                        nameof(gateDistances));
                }

                previous = gate;
            }

            _checkpointCount = checkpointCount;
            _gateCount = gateDistances.Length;
            _gateDistances = new float[_gateCount];
            Array.Copy(gateDistances, _gateDistances, _gateCount);
            _totalLength = totalLength;
            _closedLoop = closedLoop;
        }

        /// <summary>
        /// Registers a participant. Returns the stable index, or -1 when the field is full.
        /// </summary>
        public int RegisterParticipant(bool isPlayer)
        {
            if (_participantCount >= MaxParticipants)
            {
                return -1;
            }

            var index = _participantCount;
            _participantCount++;
            _active[index] = true;
            _isPlayer[index] = isPlayer;
            _checkpointCounts[index] = 0;
            _pathDistances[index] = 0f;
            _finished[index] = false;
            _finishPositions[index] = 0;
            _finishTimes[index] = 0d;
            _orderDirty = true;
            return index;
        }

        /// <summary>True when <paramref name="participantIndex"/> is a registered participant.</summary>
        public bool IsRegistered(int participantIndex)
        {
            return participantIndex >= 0 && participantIndex < _participantCount && _active[participantIndex];
        }

        /// <summary>
        /// Updates the participant's projected path distance. Ignored after that participant
        /// finishes or after the aggregate is frozen.
        /// </summary>
        public void UpdatePathDistance(int participantIndex, float pathDistance)
        {
            if (!IsRegistered(participantIndex) || _finished[participantIndex] || _frozen)
            {
                return;
            }

            _pathDistances[participantIndex] = Normalize(pathDistance);
            _orderDirty = true;
        }

        /// <summary>
        /// Accepts only the next expected checkpoint for the participant. Returns false for stale,
        /// duplicate, out-of-range, finished, or frozen reports.
        /// </summary>
        public bool TryAcceptCheckpoint(int participantIndex, int checkpointIndex)
        {
            if (!IsRegistered(participantIndex) || _finished[participantIndex] || _frozen)
            {
                return false;
            }

            if (checkpointIndex < 0 || checkpointIndex >= _checkpointCount)
            {
                return false;
            }

            if (checkpointIndex != _checkpointCounts[participantIndex])
            {
                return false;
            }

            _checkpointCounts[participantIndex] = checkpointIndex + 1;
            _orderDirty = true;
            return true;
        }

        /// <summary>
        /// Records a participant's finish exactly once, but only after every checkpoint and before
        /// the aggregate is frozen.
        /// </summary>
        public bool TryFinish(int participantIndex, double elapsedSeconds)
        {
            if (!IsRegistered(participantIndex) || _finished[participantIndex] || _frozen)
            {
                return false;
            }

            if (_checkpointCounts[participantIndex] < _checkpointCount)
            {
                return false;
            }

            if (double.IsNaN(elapsedSeconds) || double.IsInfinity(elapsedSeconds) || elapsedSeconds < 0d)
            {
                return false;
            }

            _finished[participantIndex] = true;
            _finishCount++;
            _finishPositions[participantIndex] = _finishCount;
            _finishTimes[participantIndex] = elapsedSeconds;
            _orderDirty = true;
            return true;
        }

        /// <summary>
        /// Freezes the current order as the final result. Idempotent; later progress reports are
        /// ignored.
        /// </summary>
        public void Freeze()
        {
            _frozen = true;
            _orderDirty = true;
        }

        /// <summary>
        /// Clears all progress and finish order while keeping participant registrations. Used by the
        /// in-scene retry path.
        /// </summary>
        public void ResetProgress()
        {
            for (var i = 0; i < _participantCount; i++)
            {
                _checkpointCounts[i] = 0;
                _pathDistances[i] = 0f;
                _finished[i] = false;
                _finishPositions[i] = 0;
                _finishTimes[i] = 0d;
            }

            _finishCount = 0;
            _frozen = false;
            _orderDirty = true;
        }

        /// <summary>True when the participant has crossed the finish line.</summary>
        public bool IsFinished(int participantIndex)
        {
            return IsRegistered(participantIndex) && _finished[participantIndex];
        }

        /// <summary>1-based finish position, or zero while unfinished.</summary>
        public int GetFinishPosition(int participantIndex)
        {
            return IsRegistered(participantIndex) ? _finishPositions[participantIndex] : 0;
        }

        /// <summary>Current checkpoint count for a participant.</summary>
        public int GetCheckpointCount(int participantIndex)
        {
            return IsRegistered(participantIndex) ? _checkpointCounts[participantIndex] : 0;
        }

        /// <summary>Remaining path distance to the next gate for a participant.</summary>
        public float GetDistanceToNextGate(int participantIndex)
        {
            return IsRegistered(participantIndex) ? ComputeDistanceToNextGate(participantIndex) : 0f;
        }

        /// <summary>
        /// Copies the current ordered standings into a caller-owned buffer and returns the entry
        /// count. Returns zero without writing when the buffer is null or too small. Never allocates.
        /// </summary>
        public int CopyStandingsTo(RaceStandingEntry[] destination)
        {
            if (destination == null || destination.Length < _participantCount)
            {
                return 0;
            }

            RefreshOrder();
            for (var rank = 0; rank < _participantCount; rank++)
            {
                destination[rank] = BuildEntry(_order[rank], rank + 1);
            }

            return _participantCount;
        }

        /// <summary>
        /// Reads one rank (zero-based) into an entry. Returns false for an invalid rank. Never
        /// allocates.
        /// </summary>
        public bool TryGetStanding(int rank, out RaceStandingEntry entry)
        {
            RefreshOrder();
            if (rank < 0 || rank >= _participantCount)
            {
                entry = default;
                return false;
            }

            entry = BuildEntry(_order[rank], rank + 1);
            return true;
        }

        /// <summary>1-based rank of a participant, or zero when unknown. Never allocates.</summary>
        public int GetRankOf(int participantIndex)
        {
            if (!IsRegistered(participantIndex))
            {
                return 0;
            }

            RefreshOrder();
            for (var rank = 0; rank < _participantCount; rank++)
            {
                if (_order[rank] == participantIndex)
                {
                    return rank + 1;
                }
            }

            return 0;
        }

        private RaceStandingEntry BuildEntry(int participantIndex, int rank)
        {
            return new RaceStandingEntry(
                participantIndex,
                _isPlayer[participantIndex],
                rank,
                _checkpointCounts[participantIndex],
                _checkpointCount,
                _pathDistances[participantIndex],
                ComputeDistanceToNextGate(participantIndex),
                _finished[participantIndex],
                _finishPositions[participantIndex],
                _finishTimes[participantIndex]);
        }

        private float ComputeDistanceToNextGate(int participantIndex)
        {
            if (_finished[participantIndex])
            {
                return 0f;
            }

            var gateIndex = _checkpointCounts[participantIndex] < _checkpointCount
                ? _checkpointCounts[participantIndex]
                : _checkpointCount;

            var delta = _gateDistances[gateIndex] - _pathDistances[participantIndex];
            if (_closedLoop && _totalLength > 0.0001f)
            {
                delta %= _totalLength;
                if (delta < 0f)
                {
                    delta += _totalLength;
                }

                return delta;
            }

            return delta < 0f ? 0f : delta;
        }

        private void RefreshOrder()
        {
            if (!_orderDirty)
            {
                return;
            }

            for (var i = 0; i < _participantCount; i++)
            {
                _order[i] = i;
            }

            // Bounded insertion sort over a fixed field of at most seven participants. Stable for
            // equal keys, even though the explicit registration-index tie-break makes ties total.
            for (var i = 1; i < _participantCount; i++)
            {
                var key = _order[i];
                var j = i - 1;
                while (j >= 0 && CompareParticipants(key, _order[j]) < 0)
                {
                    _order[j + 1] = _order[j];
                    j--;
                }

                _order[j + 1] = key;
            }

            _orderDirty = false;
        }

        private int CompareParticipants(int a, int b)
        {
            if (_finished[a] || _finished[b])
            {
                if (_finished[a] != _finished[b])
                {
                    return _finished[a] ? -1 : 1;
                }

                var finish = _finishPositions[a].CompareTo(_finishPositions[b]);
                return finish != 0 ? finish : a.CompareTo(b);
            }

            var checkpoint = _checkpointCounts[b].CompareTo(_checkpointCounts[a]);
            if (checkpoint != 0)
            {
                return checkpoint;
            }

            var distance = ComputeDistanceToNextGate(a).CompareTo(ComputeDistanceToNextGate(b));
            return distance != 0 ? distance : a.CompareTo(b);
        }

        private float Normalize(float distance)
        {
            if (float.IsNaN(distance))
            {
                return 0f;
            }

            if (_closedLoop)
            {
                var wrapped = distance % _totalLength;
                if (wrapped < 0f)
                {
                    wrapped += _totalLength;
                }

                return wrapped;
            }

            if (distance < 0f)
            {
                return 0f;
            }

            return distance > _totalLength ? _totalLength : distance;
        }
    }
}
