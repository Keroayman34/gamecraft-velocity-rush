using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.Economy;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for road coin pooling, proximity collection, fast retry resets,
    /// atomic progression persistence, and zero object leaks (Phase 12 Stage 2; TDD §59).
    /// </summary>
    [TestFixture]
    public sealed class RoadCoinPlayModeTests
    {
        private GameObject _hostRoot;
        private TrackLayout _testLayout;
        private TrackPath _testTrackPath;
        private RoadCoinManager _coinManager;
        private GameObject _playerMock;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hostRoot = new GameObject("RoadCoinPlayModeHost");

            _testLayout = ScriptableObject.CreateInstance<TrackLayout>();
            _testLayout.ConfigureForAuthoring(
                width: 14f,
                height: 0.8f,
                thickness: 0.3f,
                spacing: 1f,
                closesLoop: false,
                segmentData: new[] { TrackSegment.Straight(500f), TrackSegment.Straight(500f) },
                elevationData: null);

            _testTrackPath = TrackPath.Create(_testLayout);

            _playerMock = new GameObject("PlayerMock");
            _playerMock.transform.SetParent(_hostRoot.transform, false);
            _playerMock.transform.position = Vector3.zero;

            var managerGo = new GameObject("RoadCoinManager");
            managerGo.transform.SetParent(_hostRoot.transform, false);
            _coinManager = managerGo.AddComponent<RoadCoinManager>();

            var coinConfig = CoinSpawnConfig.CreateDefault();
            coinConfig.poolSize = 16;
            var laneConfig = TrafficLaneConfig.CreateDefault();

            _coinManager.Initialize(_testTrackPath, laneConfig, coinConfig, null, _playerMock.transform, 42);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_coinManager != null)
            {
                _coinManager.Teardown();
            }

            if (_hostRoot != null)
            {
                Object.DestroyImmediate(_hostRoot);
            }

            if (_testLayout != null)
            {
                Object.DestroyImmediate(_testLayout);
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RoadCoinPool_PreWarmsExactCount_AndRecyclesWithoutRuntimeInstantiation()
        {
            Assert.That(_coinManager.Pool, Is.Not.Null);
            Assert.That(_coinManager.Pool.Capacity, Is.EqualTo(16));
            Assert.That(_coinManager.Pool.AvailableCount + _coinManager.Pool.ActiveCount, Is.EqualTo(16));

            // Verify initial pre-warmed count in scene
            var allCoins = Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            Assert.That(allCoins.Length, Is.EqualTo(16), "Exactly 16 CoinPickup instances must be pre-warmed.");

            // Drive forward past multiple coin arcs
            for (int frame = 0; frame < 60; frame++)
            {
                _playerMock.transform.position += new Vector3(0f, 0f, 4f);
                _coinManager.Tick(0.016f, _playerMock.transform.position, _playerMock.transform.position.z);
                yield return null;
            }

            // Pool capacity must remain exactly 16
            Assert.That(_coinManager.Pool.AvailableCount + _coinManager.Pool.ActiveCount, Is.EqualTo(16));
            var sceneCoins = Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            Assert.That(sceneCoins.Length, Is.EqualTo(16), "No new CoinPickup objects should be instantiated during gameplay.");
        }

        [UnityTest]
        public IEnumerator RoadCoinManager_PlayerProximity_CollectsCoinsAndEmitsEvent()
        {
            int eventFiredCount = 0;
            int lastCoinValue = 0;
            int lastSessionCredits = 0;

            _coinManager.OnCoinCollected += evt =>
            {
                eventFiredCount++;
                lastCoinValue = evt.CoinValue;
                lastSessionCredits = evt.SessionCoinCredits;
            };

            // Find first active coin in pool
            CoinPickup targetCoin = null;
            for (int i = 0; i < _coinManager.Pool.ActiveCount; i++)
            {
                if (_coinManager.Pool.ActiveCoins[i] != null && _coinManager.Pool.ActiveCoins[i].IsActive)
                {
                    targetCoin = _coinManager.Pool.ActiveCoins[i];
                    break;
                }
            }

            Assert.That(targetCoin, Is.Not.Null, "There should be active coins placed ahead of the player.");
            Vector3 coinPos = targetCoin.WorldPosition;
            float coinDist = targetCoin.TrackDistance;

            // Move player directly onto coin position
            _playerMock.transform.position = coinPos;
            _coinManager.Tick(0.016f, coinPos, coinDist);
            yield return null;

            Assert.That(eventFiredCount, Is.GreaterThanOrEqualTo(1), "Coin pickup event should have fired on proximity.");
            Assert.That(lastCoinValue, Is.EqualTo(10));
            Assert.That(lastSessionCredits, Is.GreaterThanOrEqualTo(10));
            Assert.That(_coinManager.SessionCoinsCollected, Is.GreaterThanOrEqualTo(1));
            Assert.That(_coinManager.SessionCoinCredits, Is.GreaterThanOrEqualTo(10));

            // Collected coin must have been recycled back to pool and either deactivated or re-spawned ahead at the horizon
            bool recycled = !targetCoin.IsActive || Vector3.Distance(targetCoin.WorldPosition, coinPos) > 10f;
            Assert.That(recycled, Is.True, "Collected coin should be returned to pool or recycled forward.");
        }

        [UnityTest]
        public IEnumerator RoadCoinManager_FastRetry_ResetsTallyAndReSeedsWithZeroLeaks()
        {
            // Collect coins by driving
            for (int i = 0; i < 30; i++)
            {
                _playerMock.transform.position += new Vector3(0f, 0f, 3f);
                _coinManager.Tick(0.02f, _playerMock.transform.position, _playerMock.transform.position.z);
                yield return null;
            }

            // Execute Fast Retry seam: return player to origin and reset coin manager
            _playerMock.transform.position = Vector3.zero;
            _coinManager.ResetForRetry();
            yield return null;

            Assert.That(_coinManager.SessionCoinsCollected, Is.EqualTo(0));
            Assert.That(_coinManager.SessionCoinCredits, Is.EqualTo(0));
            Assert.That(_coinManager.Pool.AvailableCount + _coinManager.Pool.ActiveCount, Is.EqualTo(16));

            var sceneCoins = Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            Assert.That(sceneCoins.Length, Is.EqualTo(16), "ResetForRetry must maintain exact capacity with 0 leaked objects.");
        }

        [UnityTest]
        public IEnumerator RaceFinish_CommitsRoadCoinsAndNearMisses_ToPlayerProgressAndSaveStore()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store);

            var progress = new PlayerProgress();
            int startingCredits = progress.Credits;

            var tracker = new RaceEconomyTracker();
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 1, 10, 0));
            tracker.RecordCoinCollected(new CoinCollectedEvent(10, 2, 20, 1));
            tracker.RecordNearMiss(new NearMissEvent(1, 16f, 1.4f, 1), 25);
            tracker.RecordNearMiss(new NearMissEvent(2, 17f, 1.1f, 2), 25); // combo 2 -> 50

            int expectedBonus = 20 + 25 + 50; // 95
            Assert.That(tracker.TotalBonusCredits, Is.EqualTo(expectedBonus));

            // Commit to progress on race finish
            int committed = tracker.CommitToProgress(progress);
            Assert.That(committed, Is.EqualTo(expectedBonus));
            Assert.That(progress.Credits, Is.EqualTo(startingCredits + expectedBonus));

            // Persist to save store
            service.SetModel(progress.ExportSaveModel());
            service.SaveNow();

            // Re-hydrate into new progress instance from store
            var reloadedModel = service.CurrentModel;
            var reloadedProgress = new PlayerProgress(reloadedModel);

            Assert.That(reloadedProgress.Credits, Is.EqualTo(startingCredits + expectedBonus),
                "Bonus credits from road coins and near-misses must persist cleanly in save store.");

            // Verify spendable in garage upgrades
            var progService = new ProgressionService(reloadedProgress);
            bool canAfford = progService.TryPurchaseUpgrade(
                reloadedProgress.SelectedVehicleId,
                UpgradeCategory.Tires,
                VehicleClass.Starter,
                out string error);

            Assert.That(canAfford, Is.True, "Reloaded credits must be immediately spendable in Garage: " + error);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RoadCoinManager_Teardown_LeavesZeroRetainedCoinPickups()
        {
            // Advance player forward
            _playerMock.transform.position = new Vector3(0f, 0f, 100f);
            _coinManager.Tick(0.02f, _playerMock.transform.position, 100f);
            yield return null;

            // Call Teardown
            _coinManager.Teardown();
            yield return null;

            var sceneCoins = Object.FindObjectsByType<CoinPickup>(FindObjectsInactive.Include);
            Assert.That(sceneCoins.Length, Is.EqualTo(0), "Teardown must destroy all pooled CoinPickup instances leaving zero retained objects.");
        }
    }
}
