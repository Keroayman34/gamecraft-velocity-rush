using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class TrafficSystemTests
    {
        private GameObject _testRoot;
        private TrackLayout _testLayout;
        private TrackPath _testTrackPath;

        [SetUp]
        public void SetUp()
        {
            _testRoot = new GameObject("TrafficTestRoot");

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
        public void TrafficLaneConfig_DefaultIsValid_AndClampsInvalidInputs()
        {
            var config = TrafficLaneConfig.CreateDefault();
            Assert.That(config.TryValidate(out var error), Is.True, "Default config should be valid: " + error);
            Assert.That(config.laneCount, Is.EqualTo(4));
            Assert.That(config.poolSize, Is.EqualTo(20));

            // Test invalid lane count
            var invalidLanes = config;
            invalidLanes.laneCount = 1;
            Assert.That(invalidLanes.TryValidate(out _), Is.False);

            // Test invalid lane width
            var invalidWidth = config;
            invalidWidth.laneWidth = 0f;
            Assert.That(invalidWidth.TryValidate(out _), Is.False);

            // Test min speed > max speed
            var invalidSpeed = config;
            invalidSpeed.trafficMinSpeed = 30f;
            invalidSpeed.trafficMaxSpeed = 20f;
            Assert.That(invalidSpeed.TryValidate(out _), Is.False);

            // Test invalid density
            var invalidDensity = config;
            invalidDensity.density = 1.5f;
            Assert.That(invalidDensity.TryValidate(out _), Is.False);
        }

        [Test]
        public void TrafficLaneConfig_ComputesSymmetricLaneOffsets()
        {
            var config = TrafficLaneConfig.CreateDefault();
            config.laneCount = 4;
            config.laneWidth = 3.25f;

            // Lane 0: -1.5 * 3.25 = -4.875
            // Lane 1: -0.5 * 3.25 = -1.625
            // Lane 2: +0.5 * 3.25 = +1.625
            // Lane 3: +1.5 * 3.25 = +4.875
            Assert.That(config.GetLaneLateralOffset(0), Is.EqualTo(-4.875f).Within(0.001f));
            Assert.That(config.GetLaneLateralOffset(1), Is.EqualTo(-1.625f).Within(0.001f));
            Assert.That(config.GetLaneLateralOffset(2), Is.EqualTo(1.625f).Within(0.001f));
            Assert.That(config.GetLaneLateralOffset(3), Is.EqualTo(4.875f).Within(0.001f));

            // Test nearest lane index resolution
            Assert.That(config.GetNearestLaneIndex(-5.0f), Is.EqualTo(0));
            Assert.That(config.GetNearestLaneIndex(-1.5f), Is.EqualTo(1));
            Assert.That(config.GetNearestLaneIndex(1.5f), Is.EqualTo(2));
            Assert.That(config.GetNearestLaneIndex(5.0f), Is.EqualTo(3));
        }

        [Test]
        public void AntiWallGuarantee_NeverBlocksAllFourLanes_Over500SpawnTicks()
        {
            var config = TrafficLaneConfig.CreateDefault();
            config.laneCount = 4;
            config.density = 1.0f; // maximum pressure

            var managerGo = new GameObject("TrafficManager_AntiWallTest");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<TrafficLaneManager>();

            var playerGo = new GameObject("PlayerMock");
            playerGo.transform.SetParent(_testRoot.transform, false);
            playerGo.transform.position = Vector3.zero;

            manager.InitializeForTesting(_testTrackPath, playerGo.transform, config);

            // Run 500 simulated ticks advancing player forward
            float playerDist = 0f;
            for (int tick = 0; tick < 500; tick++)
            {
                playerDist += 0.5f;
                playerGo.transform.position = new Vector3(0f, 0f, playerDist);
                manager.TickTraffic(0.02f);

                // Anti-wall verification: check all longitudinal slices of active vehicles
                var activeVehicles = manager.Pool.ActiveVehicles;
                int activeCount = activeVehicles.Length;

                for (int i = 0; i < activeCount; i++)
                {
                    float centerDist = activeVehicles[i].TrackDistance;
                    float sliceWindow = config.minSameLaneSpacing * 0.7f;

                    int occupiedMask = 0;
                    for (int j = 0; j < activeCount; j++)
                    {
                        if (Mathf.Abs(activeVehicles[j].TrackDistance - centerDist) < sliceWindow)
                        {
                            occupiedMask |= (1 << activeVehicles[j].LaneIndex);
                        }
                    }

                    // Assert: all 4 lanes (mask == 0b1111 == 15) must NEVER be blocked simultaneously!
                    Assert.That(occupiedMask, Is.Not.EqualTo(15),
                        $"Anti-Wall Violation at tick {tick}, distance {centerDist:F1}m! All 4 lanes were blocked simultaneously.");
                }
            }
        }

        [Test]
        public void NearMissDetector_TriggersOnHighSpeedClosePass_AndRejectsSlowPass()
        {
            var config = TrafficLaneConfig.CreateDefault();
            var managerGo = new GameObject("TrafficManager_NearMissTest");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<TrafficLaneManager>();

            var playerGo = new GameObject("PlayerMock");
            playerGo.transform.SetParent(_testRoot.transform, false);
            playerGo.transform.position = new Vector3(0f, 0f, 50f);

            manager.InitializeForTesting(_testTrackPath, playerGo.transform, config);

            var detectorGo = new GameObject("NearMissDetectorTest");
            detectorGo.transform.SetParent(_testRoot.transform, false);
            var detector = detectorGo.AddComponent<NearMissDetector>();
            detector.InitializeForTesting(manager, playerGo.transform, testMinSpeed: 15f);

            int nearMissCount = 0;
            NearMissEvent lastEvent = default;
            detector.OnNearMiss += (in NearMissEvent evt) =>
            {
                nearMissCount++;
                lastEvent = evt;
            };

            // Rent a vehicle and position it 2.0m to the right at distance 50m
            var trafficVehicle = manager.Pool.Rent();
            Assert.That(trafficVehicle, Is.Not.Null);
            trafficVehicle.Spawn(2, 50f, 15f, new Vector3(2.0f, 0f, 50f), Quaternion.identity);

            // 1. Slow pass (10 m/s < 15 m/s threshold) -> Rejected
            detector.EvaluateNearMisses(10f);
            Assert.That(nearMissCount, Is.EqualTo(0), "Slow pass below minNearMissSpeed must not trigger near miss");

            // 2. High-speed pass (25 m/s >= 15 m/s threshold) -> Accepted!
            detector.EvaluateNearMisses(25f);
            Assert.That(nearMissCount, Is.EqualTo(1), "High-speed pass within lateral clearance must trigger near miss");
            Assert.That(lastEvent.VehicleId, Is.EqualTo(trafficVehicle.VehicleId));
            Assert.That(lastEvent.ComboCount, Is.EqualTo(1));
        }

        [Test]
        public void NearMissDetector_CollisionCancelsNearMiss()
        {
            var config = TrafficLaneConfig.CreateDefault();
            var managerGo = new GameObject("TrafficManager_CollisionCancelTest");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<TrafficLaneManager>();

            var playerGo = new GameObject("PlayerMock");
            playerGo.transform.SetParent(_testRoot.transform, false);
            playerGo.transform.position = new Vector3(0f, 0f, 50f);

            manager.InitializeForTesting(_testTrackPath, playerGo.transform, config);

            var detectorGo = new GameObject("NearMissDetectorTest");
            detectorGo.transform.SetParent(_testRoot.transform, false);
            var detector = detectorGo.AddComponent<NearMissDetector>();
            detector.InitializeForTesting(manager, playerGo.transform, testMinSpeed: 15f);

            int nearMissCount = 0;
            detector.OnNearMiss += (in NearMissEvent evt) => nearMissCount++;

            var trafficVehicle = manager.Pool.Rent();
            trafficVehicle.Spawn(2, 50f, 15f, new Vector3(1.5f, 0f, 50f), Quaternion.identity);

            // Mark vehicle as collided
            trafficVehicle.HasCollidedWithPlayer = true;

            // Attempt high speed pass
            detector.EvaluateNearMisses(25f);
            Assert.That(nearMissCount, Is.EqualTo(0), "Collision with traffic vehicle must cancel near-miss qualification");
        }

        [Test]
        public void NearMissDetector_PerVehicleCooldown_PreventsDuplicateTriggers()
        {
            var config = TrafficLaneConfig.CreateDefault();
            var managerGo = new GameObject("TrafficManager_DuplicateTest");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<TrafficLaneManager>();

            var playerGo = new GameObject("PlayerMock");
            playerGo.transform.SetParent(_testRoot.transform, false);
            playerGo.transform.position = new Vector3(0f, 0f, 50f);

            manager.InitializeForTesting(_testTrackPath, playerGo.transform, config);

            var detectorGo = new GameObject("NearMissDetectorTest");
            detectorGo.transform.SetParent(_testRoot.transform, false);
            var detector = detectorGo.AddComponent<NearMissDetector>();
            detector.InitializeForTesting(manager, playerGo.transform, testMinSpeed: 15f);

            int nearMissCount = 0;
            detector.OnNearMiss += (in NearMissEvent evt) => nearMissCount++;

            var trafficVehicle = manager.Pool.Rent();
            trafficVehicle.Spawn(2, 50f, 15f, new Vector3(2.0f, 0f, 50f), Quaternion.identity);

            // First evaluation triggers
            detector.EvaluateNearMisses(25f);
            Assert.That(nearMissCount, Is.EqualTo(1));

            // Subsequent evaluations for the same vehicle must NOT duplicate
            detector.EvaluateNearMisses(25f);
            detector.EvaluateNearMisses(25f);
            detector.EvaluateNearMisses(25f);
            Assert.That(nearMissCount, Is.EqualTo(1), "Near miss must only trigger once per vehicle pass");
        }

        [Test]
        public void ZeroAllocationProof_TrafficUpdateAndNearMissEvaluation_AllocatesZeroBytes()
        {
            var config = TrafficLaneConfig.CreateDefault();
            var managerGo = new GameObject("TrafficManager_AllocProof");
            managerGo.transform.SetParent(_testRoot.transform, false);
            var manager = managerGo.AddComponent<TrafficLaneManager>();

            var playerGo = new GameObject("PlayerMock");
            playerGo.transform.SetParent(_testRoot.transform, false);
            playerGo.transform.position = new Vector3(0f, 0f, 100f);

            manager.InitializeForTesting(_testTrackPath, playerGo.transform, config);

            var detectorGo = new GameObject("NearMissDetector_AllocProof");
            detectorGo.transform.SetParent(_testRoot.transform, false);
            var detector = detectorGo.AddComponent<NearMissDetector>();
            detector.InitializeForTesting(manager, playerGo.transform, testMinSpeed: 15f);

            // Warmup ticks (2 ticks to warm up JIT and initial cache)
            manager.TickTraffic(0.02f);
            detector.EvaluateNearMisses(20f);
            manager.TickTraffic(0.02f);
            detector.EvaluateNearMisses(20f);

            // Measure steady-state allocation across 500 consecutive ticks
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int i = 0; i < 500; i++)
            {
                playerGo.transform.position = new Vector3(0f, 0f, 100f + i * 0.4f);
                manager.TickTraffic(0.02f);
                detector.EvaluateNearMisses(22f);
            }

            long allocAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAllocated = allocAfter - allocBefore;

            Assert.That(totalAllocated, Is.EqualTo(0),
                $"Zero-allocation violation: steady-state traffic and near-miss evaluation allocated {totalAllocated} bytes over 500 ticks.");
        }
    }
}
