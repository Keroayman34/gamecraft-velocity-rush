using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VelocityRush.Data;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode integration tests for 4-lane pooled traffic system and Near-Miss detection (Phase 12 Stage 1; TDD §58).
    /// </summary>
    [TestFixture]
    public sealed class TrafficPlayModeTests
    {
        private GameObject _hostRoot;
        private TrackLayout _testLayout;
        private TrackPath _testTrackPath;
        private TrafficLaneManager _trafficManager;
        private GameObject _playerMock;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _hostRoot = new GameObject("TrafficPlayModeHost");

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

            var managerGo = new GameObject("TrafficManager");
            managerGo.transform.SetParent(_hostRoot.transform, false);
            _trafficManager = managerGo.AddComponent<TrafficLaneManager>();

            var config = TrafficLaneConfig.CreateDefault();
            config.poolSize = 16;
            _trafficManager.InitializeForTesting(_testTrackPath, _playerMock.transform, config);

            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_trafficManager != null)
            {
                _trafficManager.Teardown();
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
        public IEnumerator TrafficPool_PrewarmsExactCount_AndRecyclesBehindPlayerWithoutAllocating()
        {
            Assert.That(_trafficManager.Pool, Is.Not.Null);
            Assert.That(_trafficManager.Pool.Capacity, Is.EqualTo(16));
            Assert.That(_trafficManager.Pool.AvailableCount + _trafficManager.Pool.ActiveCount, Is.EqualTo(16));

            int initialActive = _trafficManager.Pool.ActiveCount;
            Assert.That(initialActive, Is.GreaterThan(0), "Traffic manager should pre-seed active vehicles ahead");

            // Advance player forward by 300 meters over multiple frames
            for (int frame = 0; frame < 60; frame++)
            {
                _playerMock.transform.position += new Vector3(0f, 0f, 5f);
                _trafficManager.TickTraffic(0.016f);
                yield return null;
            }

            // Total pool count must remain exactly 16
            Assert.That(_trafficManager.Pool.AvailableCount + _trafficManager.Pool.ActiveCount, Is.EqualTo(16));

            // Verify total TrafficVehicle components in scene never exceeded 16
            var sceneVehicles = Object.FindObjectsByType<TrafficVehicle>(FindObjectsInactive.Include);
            Assert.That(sceneVehicles.Length, Is.EqualTo(16), "No new TrafficVehicle objects should be instantiated during runtime");
        }

        [UnityTest]
        public IEnumerator TrafficLaneManager_FastRetry_ResetsTrafficCleanly_WithZeroLeakedObjects()
        {
            // Simulate driving and spawning
            for (int i = 0; i < 30; i++)
            {
                _playerMock.transform.position += new Vector3(0f, 0f, 4f);
                _trafficManager.TickTraffic(0.02f);
                yield return null;
            }

            // Fast Retry seam: return player to origin and reset traffic
            _playerMock.transform.position = Vector3.zero;
            _trafficManager.ResetTraffic();
            yield return null;

            // Assert clean reset: pool still contains exact capacity
            Assert.That(_trafficManager.Pool.AvailableCount + _trafficManager.Pool.ActiveCount, Is.EqualTo(16));

            var sceneVehicles = Object.FindObjectsByType<TrafficVehicle>(FindObjectsInactive.Include);
            Assert.That(sceneVehicles.Length, Is.EqualTo(16), "ResetTraffic must not leak or duplicate traffic objects");
        }

        [UnityTest]
        public IEnumerator TrafficLaneManager_Teardown_LeavesZeroRetainedTrafficVehicles()
        {
            // Drive forward to have active vehicles
            _playerMock.transform.position = new Vector3(0f, 0f, 100f);
            _trafficManager.TickTraffic(0.02f);
            yield return null;

            // Call Teardown
            _trafficManager.Teardown();
            yield return null;

            var sceneVehicles = Object.FindObjectsByType<TrafficVehicle>(FindObjectsInactive.Include);
            Assert.That(sceneVehicles.Length, Is.EqualTo(0), "Teardown must destroy all pooled traffic vehicles leaving zero retained objects");
        }
    }
}
