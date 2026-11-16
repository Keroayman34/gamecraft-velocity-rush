using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Stage-3 PlayMode proof for the synchronized grid hold, multi-participant finish, unchanged
    /// solo result, and allocation-free standings polling (T-10.3.2–T-10.3.6).
    /// </summary>
    public sealed class RaceGridPlayModeTests
    {
        private const string TestTrackSceneName = "TestTrack";
        private const float Step = 0.02f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<TrackLayout> _layouts = new List<TrackLayout>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    UnityEngine.Object.Destroy(_objects[i]);
                }
            }

            for (var i = 0; i < _layouts.Count; i++)
            {
                if (_layouts[i] != null)
                {
                    UnityEngine.Object.Destroy(_layouts[i]);
                }
            }

            _objects.Clear();
            _layouts.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator AIDriver_StartHold_KeepsTheCarStationaryUntilReleased()
        {
            var path = CreateClosedLoopPath();
            var car = CreateAiCar("AI Grid Hold");
            var driver = car.AddComponent<AIDriver>();
            driver.Initialize(path, 40f);
            driver.SetStartHold(true);

            Assert.That(driver.IsStartHeld, Is.True);
            var heldDistance = driver.PathDistance;

            for (var i = 0; i < 250; i++)
            {
                driver.Tick(Step);
            }

            Assert.That(driver.PathDistance, Is.EqualTo(heldDistance).Within(0.001f),
                "A held car must not advance along the path.");
            Assert.That(driver.Speed, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(driver.Throttle, Is.EqualTo(0f));
            Assert.That(driver.Brake, Is.EqualTo(0f));
            Assert.That(driver.Steering, Is.EqualTo(0f));

            driver.SetStartHold(false);
            Assert.That(driver.IsStartHeld, Is.False);

            for (var i = 0; i < 150; i++)
            {
                driver.Tick(Step);
            }

            Assert.That(driver.PathDistance, Is.GreaterThan(heldDistance + 1f),
                "Releasing the hold must let the normal path step resume.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator MultiParticipantFinish_IsExactlyOnce_AndLeavesSoloResultUnchanged()
        {
            yield return LoadTestTrack();
            yield return WaitForRunning(FindSingle<RaceController>("RaceController"));

            var controller = FindSingle<RaceController>("RaceController");
            var playerBody = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();
            var opponent = CreateRigidbody("AI Finish Opponent");
            var opponentIndex = controller.RegisterParticipant(opponent);
            Assert.That(opponentIndex, Is.GreaterThanOrEqualTo(0));

            var participantFinished = new List<Rigidbody>();
            var playerResultCount = 0;
            RaceResult? playerResult = null;
            Action<Rigidbody> onParticipantFinished = body => participantFinished.Add(body);
            Action<RaceResult> onPlayerFinished = result =>
            {
                playerResultCount++;
                playerResult = result;
            };

            controller.ParticipantFinished += onParticipantFinished;
            controller.RaceFinished += onPlayerFinished;

            try
            {
                for (var i = 0; i < controller.RequiredCheckpointCount; i++)
                {
                    controller.ReportCheckpoint(i, opponent);
                }

                controller.ReportFinish(opponent);
                Assert.That(participantFinished.Count, Is.EqualTo(1));
                Assert.That(participantFinished[0], Is.EqualTo(opponent));

                for (var i = 0; i < controller.RequiredCheckpointCount; i++)
                {
                    controller.ReportCheckpoint(i, playerBody);
                }

                controller.ReportFinish(playerBody);
                Assert.That(participantFinished.Count, Is.EqualTo(2),
                    "The player must contribute exactly one participant-finish notification.");
                Assert.That(playerResultCount, Is.EqualTo(1));
                Assert.That(playerResult.HasValue, Is.True);
                Assert.That(playerResult.Value.Completed, Is.True);
                Assert.That(playerResult.Value.AcceptedCheckpointCount, Is.EqualTo(controller.RequiredCheckpointCount));

                // Duplicate finish reports must not produce a second notification or result.
                controller.ReportFinish(opponent);
                controller.ReportFinish(playerBody);
                Assert.That(participantFinished.Count, Is.EqualTo(2));
                Assert.That(playerResultCount, Is.EqualTo(1));
            }
            finally
            {
                controller.ParticipantFinished -= onParticipantFinished;
                controller.RaceFinished -= onPlayerFinished;
            }

            yield return null;
        }

        [UnityTest]
        public IEnumerator RaceGridController_ReleasesOnGo_AndPollsStandingsWithoutAllocating()
        {
            yield return LoadTestTrack();

            var controller = FindSingle<RaceController>("RaceController");
            var player = FindSingle<VehicleController>("VehicleController");
            controller.Retry();

            var path = CreateStraightPath(250f);
            var opponents = new AIDriver[2];
            for (var i = 0; i < opponents.Length; i++)
            {
                var car = CreateAiCar("Grid AI " + i);
                opponents[i] = car.AddComponent<AIDriver>();
            }

            var gridObject = new GameObject("Race Grid Controller");
            _objects.Add(gridObject);
            var grid = gridObject.AddComponent<RaceGridController>();
            var gates = new[] { 75f, 150f, 225f, 249f };

            Assert.That(
                grid.Initialize(path, controller, player, opponents, 60f, gates, RaceGridSettings.Default),
                Is.True,
                "The grid must initialize with a valid path and gate set.");

            Assert.That(grid.IsHolding, Is.True);
            Assert.That(grid.ParticipantCount, Is.EqualTo(3));
            Assert.That(controller.State, Is.EqualTo(RaceState.Countdown));
            for (var i = 0; i < opponents.Length; i++)
            {
                Assert.That(opponents[i].IsStartHeld, Is.True);
            }

            var buffer = new RaceStandingEntry[3];
            Assert.That(grid.CopyStandingsTo(buffer), Is.EqualTo(3));
            Assert.That(buffer[0].Rank, Is.EqualTo(1));
            Assert.That(buffer[1].Rank, Is.EqualTo(2));
            Assert.That(buffer[2].Rank, Is.EqualTo(3));

            // Warm up the standings order and the HUD buffer before measuring.
            for (var i = 0; i < 100; i++)
            {
                grid.CopyStandingsTo(buffer);
                grid.TryGetStanding(0, out _);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                grid.CopyStandingsTo(buffer);
                grid.TryGetStanding(0, out _);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "HUD standings polling must allocate 0 B; measured " + allocated + " B over 5000 polls.");

            yield return WaitForRunning(controller);
            yield return null;

            Assert.That(grid.IsHolding, Is.False,
                "The grid must release automatically when the race rules reach GO.");
            for (var i = 0; i < opponents.Length; i++)
            {
                Assert.That(opponents[i].IsStartHeld, Is.False);
            }

            yield return null;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.That(opponents[0].DistanceTravelled, Is.GreaterThan(0f));
            Assert.That(opponents[1].DistanceTravelled, Is.GreaterThan(0f));

            yield return null;
        }

        private IEnumerator LoadTestTrack()
        {
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!SceneManager.GetSceneByName(TestTrackSceneName).isLoaded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName(TestTrackSceneName).isLoaded, Is.True, "TestTrack did not load.");
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private static IEnumerator WaitForRunning(RaceController controller)
        {
            var deadline = Time.realtimeSinceStartup + 10f;
            while (controller.State != RaceState.Running && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(controller.State, Is.EqualTo(RaceState.Running), "Race did not reach Running.");
        }

        private GameObject CreateAiCar(string name)
        {
            var car = new GameObject(name);
            car.AddComponent<Rigidbody>();
            _objects.Add(car);
            return car;
        }

        private Rigidbody CreateRigidbody(string name)
        {
            var body = new GameObject(name);
            var rigidbody = body.AddComponent<Rigidbody>();
            _objects.Add(body);
            return rigidbody;
        }

        private TrackPath CreateClosedLoopPath()
        {
            var layout = ScriptableObject.CreateInstance<TrackLayout>();
            layout.ConfigureForAuthoring(
                12f,
                0.8f,
                0.3f,
                1f,
                true,
                new[]
                {
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1)
                },
                Array.Empty<TrackElevationKey>());
            _layouts.Add(layout);
            return TrackPath.Create(layout);
        }

        private TrackPath CreateStraightPath(float length)
        {
            var layout = ScriptableObject.CreateInstance<TrackLayout>();
            layout.ConfigureForAuthoring(
                10f,
                0.8f,
                0.3f,
                1f,
                false,
                new[]
                {
                    TrackSegment.Straight(length * 0.5f),
                    TrackSegment.Straight(length * 0.5f)
                },
                Array.Empty<TrackElevationKey>());
            _layouts.Add(layout);
            return TrackPath.Create(layout);
        }

        private static T FindSingle<T>(string label) where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            Assert.That(found.Length, Is.EqualTo(1), "Expected exactly one " + label + " in TestTrack.");
            return found[0];
        }
    }
}
