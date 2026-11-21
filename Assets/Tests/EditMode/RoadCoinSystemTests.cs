using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.Economy;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Unit tests for road coin spawn configuration, economy ledger, HUD formatters,
    /// anti-exploit commitment, and zero-allocation runtime performance (Phase 12 Stage 2; TDD §59).
    /// </summary>
    [TestFixture]
    public sealed class RoadCoinSystemTests
    {
        private GameObject _testRoot;
        private TrackLayout _testLayout;
        private TrackPath _testTrackPath;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("RoadCoinTestRoot");

            _testLayout = ScriptableObject.CreateInstance<TrackLayout>();
            _testLayout.ConfigureForAuthoring(
                width: 14f,
                height: 0.8f,
                thickness: 0.3f,
                spacing: 1f,
                closesLoop: false,
                segmentData: new[] { TrackSegment.Straight(250f), TrackSegment.Straight(250f) },
                elevationData: null);

            _testTrackPath = TrackPath.Create(_testLayout);
        }

        [TearDown]
        public void TearDown()
        {
            if (_testRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(_testRoot);
            }

            if (_testLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(_testLayout);
            }
        }

        [Test]
        public void CoinSpawnConfig_Validation_RejectsInvalidAndAcceptsDefault()
        {
            var config = CoinSpawnConfig.CreateDefault();
            Assert.That(config.TryValidate(out var error), Is.True, "Default config should be valid: " + error);
            Assert.That(config.poolSize, Is.EqualTo(36));
            Assert.That(config.coinValue, Is.EqualTo(10));
            Assert.That(config.nearMissBonusCredits, Is.EqualTo(25));
            Assert.That(config.pickupRadius, Is.EqualTo(2.4f));

            // Test fail-closed boundaries
            var invalidPool = config;
            invalidPool.poolSize = 4; // < 8
            Assert.That(invalidPool.TryValidate(out _), Is.False);

            var invalidValue = config;
            invalidValue.coinValue = 0; // <= 0
            Assert.That(invalidValue.TryValidate(out _), Is.False);

            var invalidRadius = config;
            invalidRadius.pickupRadius = 0.2f; // < 0.5
            Assert.That(invalidRadius.TryValidate(out _), Is.False);

            var invalidArc = config;
            invalidArc.arcMinCoins = 6;
            invalidArc.arcMaxCoins = 3; // max < min
            Assert.That(invalidArc.TryValidate(out _), Is.False);
        }

        [Test]
        public void RaceEconomyTracker_AccumulatesCoinsAndNearMisses_Accurately()
        {
            var tracker = new RaceEconomyTracker();
            Assert.That(tracker.CoinsCollected, Is.EqualTo(0));
            Assert.That(tracker.CoinCredits, Is.EqualTo(0));
            Assert.That(tracker.NearMissCount, Is.EqualTo(0));
            Assert.That(tracker.NearMissCredits, Is.EqualTo(0));
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(0));
            Assert.That(tracker.IsCommitted, Is.False);

            // Record 3 coins of value 10
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 1, 10, 0));
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 2, 20, 1));
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 3, 30, 2));

            Assert.That(tracker.CoinsCollected, Is.EqualTo(3));
            Assert.That(tracker.CoinCredits, Is.EqualTo(30));

            // Record 2 near misses: streak 1 and streak 2
            tracker.RecordNearMiss(new NearMissEvent(1, 15f, 1.5f, 1), 25);
            tracker.RecordNearMiss(new NearMissEvent(2, 18f, 1.2f, 2), 25);

            Assert.That(tracker.NearMissCount, Is.EqualTo(2));
            Assert.That(tracker.NearMissCredits, Is.EqualTo(25 * 1 + 25 * 2)); // 25 + 50 = 75
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(30 + 75)); // 105
        }

        [Test]
        public void RaceEconomyTracker_ResetOnRetryOrQuit_PreventsFarmingExploit()
        {
            var tracker = new RaceEconomyTracker();
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 1, 10, 0));
            tracker.RecordNearMiss(new NearMissEvent(1, 15f, 1.5f, 1), 25);
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(35));

            // Player restarts or quits early: reset ledger without committing
            tracker.Reset();

            Assert.That(tracker.CoinsCollected, Is.EqualTo(0));
            Assert.That(tracker.CoinCredits, Is.EqualTo(0));
            Assert.That(tracker.NearMissCount, Is.EqualTo(0));
            Assert.That(tracker.NearMissCredits, Is.EqualTo(0));
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(0));
            Assert.That(tracker.IsCommitted, Is.False);

            var progress = new PlayerProgress();
            int initialCredits = progress.Credits;

            int committed = tracker.CommitToProgress(progress);
            Assert.That(committed, Is.EqualTo(0));
            Assert.That(progress.Credits, Is.EqualTo(initialCredits), "Uncommitted/reset ledger must award 0 credits.");
        }

        [Test]
        public void RaceEconomyTracker_CommitToProgress_IsIdempotentAndAtomic()
        {
            var tracker = new RaceEconomyTracker();
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 1, 10, 0));
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 2, 20, 1));
            tracker.RecordNearMiss(new NearMissEvent(1, 15f, 1.5f, 1), 25);

            int expectedBonus = 20 + 25; // 45
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(expectedBonus));

            var progress = new PlayerProgress();
            int startingCredits = progress.Credits;

            // First commit succeeds
            int awarded1 = tracker.CommitToProgress(progress);
            Assert.That(awarded1, Is.EqualTo(expectedBonus));
            Assert.That(progress.Credits, Is.EqualTo(startingCredits + expectedBonus));
            Assert.That(tracker.IsCommitted, Is.True);

            // Second commit is idempotent: awards 0, does not double-credit
            int awarded2 = tracker.CommitToProgress(progress);
            Assert.That(awarded2, Is.EqualTo(0));
            Assert.That(progress.Credits, Is.EqualTo(startingCredits + expectedBonus));
        }

        [Test]
        public void HudStringFormatter_BonusAndPopup_FormatsCorrectly()
        {
            var buffer = new char[32];

            int len1 = HudStringFormatter.WriteBonusCredits(buffer, 0, 120);
            Assert.That(new string(buffer, 0, len1), Is.EqualTo("+120 CR"));

            int len2 = HudStringFormatter.WritePopup(buffer, 0, "COIN", 10);
            Assert.That(new string(buffer, 0, len2), Is.EqualTo("COIN +10 CR"));

            int len3 = HudStringFormatter.WritePopup(buffer, 0, "NEAR MISS!", 50);
            Assert.That(new string(buffer, 0, len3), Is.EqualTo("NEAR MISS! +50 CR"));
        }

        [Test]
        public void RoadCoinManager_ZeroSteadyStateAllocation_Proof()
        {
            var coinConfig = CoinSpawnConfig.CreateDefault();
            coinConfig.poolSize = 16;
            var laneConfig = TrafficLaneConfig.CreateDefault();

            var managerGo = new GameObject("RoadCoinManager_ZeroAlloc");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<RoadCoinManager>();

            var playerMock = new GameObject("PlayerMock");
            playerMock.transform.SetParent(_testRoot.transform, false);
            playerMock.transform.position = Vector3.zero;

            manager.Initialize(_testTrackPath, laneConfig, coinConfig, null, playerMock.transform, 12345);

            // Warmup iterations
            for (int i = 0; i < 20; i++)
            {
                manager.Tick(0.016f, new Vector3(0f, 0f, 10f), 10f);
            }

            // Steady-state measurement over 200 consecutive ticks
            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 200; i++)
            {
                manager.Tick(0.016f, new Vector3(0f, 0f, 10f), 10f);
            }

            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L), "RoadCoinManager.Tick must allocate exactly 0 bytes in steady state.");
        }
    }
}
