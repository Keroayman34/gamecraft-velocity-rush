using System;
using NUnit.Framework;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-3 coverage for checkpoint+distance ranking, deterministic ties, multi-car finish
    /// ordering, finish freezing, retry reset, and the 0 B HUD polling contract (T-10.3.3–T-10.3.6).
    /// </summary>
    public sealed class RaceStandingsTests
    {
        private const int CheckpointCount = 3;
        private const float TotalLength = 400f;

        [Test]
        public void Register_AssignsStableIndices_AndRejectsTheEighthParticipant()
        {
            var standings = CreateStandings();

            for (var i = 0; i < RaceStandings.MaxParticipants; i++)
            {
                Assert.That(standings.RegisterParticipant(i == 0), Is.EqualTo(i));
            }

            Assert.That(standings.RegisterParticipant(false), Is.EqualTo(-1));
            Assert.That(standings.ParticipantCount, Is.EqualTo(RaceStandings.MaxParticipants));
        }

        [Test]
        public void Ranking_UsesCheckpointCountBeforeDistance()
        {
            var standings = CreateStandings();
            var first = standings.RegisterParticipant(false);
            var second = standings.RegisterParticipant(false);
            var third = standings.RegisterParticipant(false);

            // The one-checkpoint car is farther from its next gate than both zero-checkpoint
            // cars, yet checkpoint count still ranks it first (D-061).
            standings.UpdatePathDistance(first, 99f);
            standings.UpdatePathDistance(second, 50f);
            standings.UpdatePathDistance(third, 10f);
            Assert.That(standings.TryAcceptCheckpoint(second, 0), Is.True);

            var ordered = CopyOrdered(standings);

            Assert.That(ordered[0].ParticipantIndex, Is.EqualTo(second));
            Assert.That(ordered[1].ParticipantIndex, Is.EqualTo(first));
            Assert.That(ordered[2].ParticipantIndex, Is.EqualTo(third));
        }

        [Test]
        public void Ranking_UsesDistanceToNextGateWithinTheSameCheckpointCount()
        {
            var standings = CreateStandings();
            var first = standings.RegisterParticipant(false);
            var second = standings.RegisterParticipant(false);
            var third = standings.RegisterParticipant(false);

            // All at zero checkpoints; the next gate is checkpoint 0 at 100 m.
            standings.UpdatePathDistance(first, 90f);
            standings.UpdatePathDistance(second, 20f);
            standings.UpdatePathDistance(third, 95f);

            var ordered = CopyOrdered(standings);

            Assert.That(ordered[0].ParticipantIndex, Is.EqualTo(third));
            Assert.That(ordered[1].ParticipantIndex, Is.EqualTo(first));
            Assert.That(ordered[2].ParticipantIndex, Is.EqualTo(second));
            Assert.That(ordered[0].DistanceToNextGate, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Ranking_TiesBreakByRegistrationIndex()
        {
            var standings = CreateStandings();
            var first = standings.RegisterParticipant(false);
            var second = standings.RegisterParticipant(false);
            var third = standings.RegisterParticipant(false);

            standings.UpdatePathDistance(first, 40f);
            standings.UpdatePathDistance(second, 40f);
            standings.UpdatePathDistance(third, 40f);

            var ordered = CopyOrdered(standings);

            Assert.That(ordered[0].ParticipantIndex, Is.EqualTo(first));
            Assert.That(ordered[1].ParticipantIndex, Is.EqualTo(second));
            Assert.That(ordered[2].ParticipantIndex, Is.EqualTo(third));
        }

        [Test]
        public void FinishedParticipants_RankAheadInFinishOrder()
        {
            var standings = CreateStandings();
            var player = standings.RegisterParticipant(true);
            var winner = standings.RegisterParticipant(false);
            var trailing = standings.RegisterParticipant(false);

            CompleteAllCheckpoints(standings, winner);
            Assert.That(standings.TryFinish(winner, 20d), Is.True);

            CompleteAllCheckpoints(standings, player);
            Assert.That(standings.TryFinish(player, 22d), Is.True);

            standings.UpdatePathDistance(trailing, 390f);
            CompleteAllCheckpoints(standings, trailing);

            var ordered = CopyOrdered(standings);

            Assert.That(ordered[0].ParticipantIndex, Is.EqualTo(winner));
            Assert.That(ordered[0].FinishPosition, Is.EqualTo(1));
            Assert.That(ordered[1].ParticipantIndex, Is.EqualTo(player));
            Assert.That(ordered[1].FinishPosition, Is.EqualTo(2));
            Assert.That(ordered[2].ParticipantIndex, Is.EqualTo(trailing));
            Assert.That(ordered[2].Finished, Is.False);
        }

        [Test]
        public void TryFinish_RequiresEveryCheckpoint()
        {
            var standings = CreateStandings();
            var participant = standings.RegisterParticipant(false);

            Assert.That(standings.TryFinish(participant, 1d), Is.False);
            Assert.That(standings.TryAcceptCheckpoint(participant, 0), Is.True);
            Assert.That(standings.TryFinish(participant, 1d), Is.False);

            Assert.That(standings.TryAcceptCheckpoint(participant, 1), Is.True);
            Assert.That(standings.TryAcceptCheckpoint(participant, 2), Is.True);
            Assert.That(standings.TryFinish(participant, 1d), Is.True);
        }

        [Test]
        public void TryFinish_IsExactlyOncePerParticipant()
        {
            var standings = CreateStandings();
            var participant = standings.RegisterParticipant(false);
            CompleteAllCheckpoints(standings, participant);

            Assert.That(standings.TryFinish(participant, 10d), Is.True);
            Assert.That(standings.TryFinish(participant, 11d), Is.False);
            Assert.That(standings.FinishCount, Is.EqualTo(1));
            Assert.That(standings.GetFinishPosition(participant), Is.EqualTo(1));
        }

        [Test]
        public void FinishedEntry_FreezesDistanceAndRejectsFurtherCheckpoints()
        {
            var standings = CreateStandings();
            var participant = standings.RegisterParticipant(false);
            standings.UpdatePathDistance(participant, 350f);
            CompleteAllCheckpoints(standings, participant);
            Assert.That(standings.TryFinish(participant, 30d), Is.True);

            var finishedDistance = standings.GetDistanceToNextGate(participant);
            standings.UpdatePathDistance(participant, 500f);

            Assert.That(standings.GetDistanceToNextGate(participant), Is.EqualTo(finishedDistance));
            Assert.That(standings.TryAcceptCheckpoint(participant, 0), Is.False);
        }

        [Test]
        public void Freeze_ClosesTheFinalOrderAndIgnoresLaterProgress()
        {
            var standings = CreateStandings();
            var player = standings.RegisterParticipant(true);
            var opponent = standings.RegisterParticipant(false);

            CompleteAllCheckpoints(standings, player);
            Assert.That(standings.TryFinish(player, 40d), Is.True);
            standings.Freeze();

            Assert.That(standings.IsFrozen, Is.True);
            Assert.That(standings.TryAcceptCheckpoint(opponent, 0), Is.False);
            var frozenDistance = standings.GetDistanceToNextGate(opponent);
            standings.UpdatePathDistance(opponent, 390f);
            Assert.That(standings.GetDistanceToNextGate(opponent), Is.EqualTo(frozenDistance));

            var before = CopyOrdered(standings);
            Assert.That(before[0].ParticipantIndex, Is.EqualTo(player));
            Assert.That(before[1].ParticipantIndex, Is.EqualTo(opponent));
        }

        [Test]
        public void ClosedLoop_DistanceToNextGateWrapsAcrossTheSeam()
        {
            var standings = CreateClosedLoopStandings();
            var participant = standings.RegisterParticipant(false);
            standings.UpdatePathDistance(participant, 90f);

            // Checkpoint 0 is at 25 m; from 90 m the remaining distance wraps to 35 m.
            Assert.That(standings.GetDistanceToNextGate(participant), Is.EqualTo(35f).Within(0.001f));
        }

        [Test]
        public void ResetProgress_ClearsFinishOrderAndReopensTheAggregate()
        {
            var standings = CreateStandings();
            var participant = standings.RegisterParticipant(false);
            CompleteAllCheckpoints(standings, participant);
            standings.TryFinish(participant, 12d);
            standings.Freeze();

            standings.ResetProgress();

            Assert.That(standings.IsFrozen, Is.False);
            Assert.That(standings.FinishCount, Is.EqualTo(0));
            Assert.That(standings.GetCheckpointCount(participant), Is.EqualTo(0));
            Assert.That(standings.IsFinished(participant), Is.False);
            Assert.That(standings.TryAcceptCheckpoint(participant, 0), Is.True);
        }

        [Test]
        public void CopyStandingsTo_FillsTheBufferInRankOrderAndHandlesSmallBuffers()
        {
            var standings = CreateStandings();
            var player = standings.RegisterParticipant(true);
            var opponent = standings.RegisterParticipant(false);
            standings.UpdatePathDistance(player, 30f);
            standings.UpdatePathDistance(opponent, 90f);

            var buffer = new RaceStandingEntry[2];
            Assert.That(standings.CopyStandingsTo(buffer), Is.EqualTo(2));
            Assert.That(buffer[0].ParticipantIndex, Is.EqualTo(opponent));
            Assert.That(buffer[0].Rank, Is.EqualTo(1));
            Assert.That(buffer[1].ParticipantIndex, Is.EqualTo(player));
            Assert.That(buffer[1].Rank, Is.EqualTo(2));
            Assert.That(buffer[1].IsPlayer, Is.True);

            Assert.That(standings.CopyStandingsTo(new RaceStandingEntry[1]), Is.EqualTo(0));
            Assert.That(standings.CopyStandingsTo(null), Is.EqualTo(0));
        }

        [Test]
        public void PlayerRankAndFinishPosition_FollowThePlayerEntry()
        {
            var standings = CreateStandings();
            var player = standings.RegisterParticipant(true);
            var opponent = standings.RegisterParticipant(false);
            standings.UpdatePathDistance(player, 20f);
            standings.UpdatePathDistance(opponent, 90f);

            Assert.That(standings.PlayerRank, Is.EqualTo(2));
            Assert.That(standings.PlayerFinishPosition, Is.EqualTo(0));

            CompleteAllCheckpoints(standings, opponent);
            standings.TryFinish(opponent, 8d);
            CompleteAllCheckpoints(standings, player);
            standings.TryFinish(player, 9d);

            Assert.That(standings.PlayerRank, Is.EqualTo(2));
            Assert.That(standings.PlayerFinishPosition, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_RejectsInvalidGateLayouts()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RaceStandings(0, new[] { 10f }, 100f, false));
            Assert.Throws<ArgumentException>(
                () => new RaceStandings(2, new[] { 10f, 20f }, 100f, false));
            Assert.Throws<ArgumentException>(
                () => new RaceStandings(1, new[] { 30f, 10f }, 100f, false));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RaceStandings(1, new[] { 10f, 110f }, 100f, false));
        }

        [Test]
        public void PollingAndUpdates_AfterWarmup_AllocateZeroBytes()
        {
            var standings = CreateStandings();
            for (var i = 0; i < 5; i++)
            {
                standings.RegisterParticipant(i == 0);
            }

            var buffer = new RaceStandingEntry[5];
            for (var warmup = 0; warmup < 100; warmup++)
            {
                standings.UpdatePathDistance(0, 80f);
                standings.UpdatePathDistance(1, 120f);
                standings.UpdatePathDistance(2, 160f);
                standings.UpdatePathDistance(3, 200f);
                standings.UpdatePathDistance(4, 240f);
                standings.CopyStandingsTo(buffer);
                standings.TryGetStanding(0, out _);
                standings.GetRankOf(2);
                _ = standings.PlayerRank;
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                standings.UpdatePathDistance(0, 80f + (i % 10));
                standings.UpdatePathDistance(1, 120f + (i % 10));
                standings.UpdatePathDistance(2, 160f + (i % 10));
                standings.UpdatePathDistance(3, 200f + (i % 10));
                standings.UpdatePathDistance(4, 240f + (i % 10));
                standings.CopyStandingsTo(buffer);
                standings.TryGetStanding(0, out _);
                standings.GetRankOf(2);
                _ = standings.PlayerRank;
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "Standing updates and polling must allocate 0 B; measured " + allocated + " B over 5000 iterations.");
        }

        private static RaceStandings CreateStandings()
        {
            return new RaceStandings(
                CheckpointCount,
                new[] { 100f, 200f, 300f, TotalLength },
                TotalLength,
                closedLoop: false);
        }

        private static RaceStandings CreateClosedLoopStandings()
        {
            return new RaceStandings(
                CheckpointCount,
                new[] { 25f, 50f, 75f, 99f },
                100f,
                closedLoop: true);
        }

        private static void CompleteAllCheckpoints(RaceStandings standings, int participant)
        {
            for (var i = 0; i < CheckpointCount; i++)
            {
                Assert.That(standings.TryAcceptCheckpoint(participant, i), Is.True);
            }
        }

        private static RaceStandingEntry[] CopyOrdered(RaceStandings standings)
        {
            var buffer = new RaceStandingEntry[standings.ParticipantCount];
            Assert.That(standings.CopyStandingsTo(buffer), Is.EqualTo(standings.ParticipantCount));
            return buffer;
        }
    }
}
