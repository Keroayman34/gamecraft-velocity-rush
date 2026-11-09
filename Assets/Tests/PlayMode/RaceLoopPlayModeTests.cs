using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Real Unity PlayMode coverage for the T5.4 one-way timed run: countdown transition, ordered
    /// checkpoint gates, finish blocking, exactly-once result delivery, in-scene retry, and
    /// vehicle pose/velocity reset. Uses the real Bootstrap → additive TestTrack path and the real
    /// trigger colliders; no scene reload is used by the retry path.
    /// </summary>
    public sealed class RaceLoopPlayModeTests
    {
        private const string TestTrackSceneName = "TestTrack";

        private static readonly FieldInfo AppInputField =
            typeof(RaceController).GetField("_appInput", BindingFlags.Instance | BindingFlags.NonPublic);

        [UnityTest]
        public IEnumerator Countdown_TransitionsToRunning_AndExcludesCountdownTimeFromElapsed()
        {
            yield return LoadTestTrack();
            var controller = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();

            // Force a deterministic attempt start in the resident scene (also exercises retry).
            controller.Retry();
            Assert.That(controller.State, Is.EqualTo(RaceState.Countdown));

            var resetAt = Time.realtimeSinceStartup;
            yield return WaitForRunning(controller);
            var runningAt = Time.realtimeSinceStartup;

            Assert.That(runningAt - resetAt, Is.GreaterThanOrEqualTo(0.5f),
                "Running must not be reached before a meaningful countdown has elapsed.");
            Assert.That(controller.ElapsedSeconds, Is.GreaterThanOrEqualTo(0d));
            Assert.That(controller.ElapsedSeconds, Is.LessThan(runningAt - resetAt),
                "Race elapsed time must exclude the countdown.");

            yield return WaitRealtime(0.2f);
            Assert.That(controller.ElapsedSeconds, Is.GreaterThan(0d));
            Assert.That(body, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator OrderedCheckpoints_GateFinish_AndEmitExactlyOneResult()
        {
            yield return LoadTestTrack();
            yield return WaitForRunning(FindSingle<RaceController>("RaceController"));

            var controller = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();
            var checkpoints = FindCheckpoints();
            var finish = FindSingle<FinishTrigger>("FinishTrigger");

            Assert.That(checkpoints.Length, Is.EqualTo(3), "The prototype run must have three ordered checkpoints.");

            var finishCount = 0;
            RaceResult? observed = null;
            Action<RaceResult> onFinished = result =>
            {
                finishCount++;
                observed = result;
            };
            controller.RaceFinished += onFinished;

            try
            {
                // Later checkpoint before the expected one is rejected.
                yield return Teleport(body, checkpoints[1].transform.position.z);
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(0),
                    "An out-of-order checkpoint must not advance progress.");

                // Finish is blocked until every checkpoint is accepted.
                yield return Teleport(body, finish.transform.position.z);
                Assert.That(controller.State, Is.EqualTo(RaceState.Running));
                Assert.That(controller.Result.HasValue, Is.False);
                Assert.That(finishCount, Is.EqualTo(0));

                // Accept checkpoint 0, then re-enter it; the duplicate must not increment.
                yield return Teleport(body, checkpoints[0].transform.position.z);
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(1));
                yield return Teleport(body, -50f);
                yield return Teleport(body, checkpoints[0].transform.position.z);
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(1),
                    "Re-entering an accepted checkpoint must not increment progress.");

                // Complete the ordered sequence.
                yield return Teleport(body, checkpoints[1].transform.position.z);
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(2));
                yield return Teleport(body, checkpoints[2].transform.position.z);
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(3));

                // Finish now succeeds exactly once.
                yield return Teleport(body, finish.transform.position.z);
                Assert.That(controller.State, Is.EqualTo(RaceState.Finished));
                Assert.That(controller.Result.HasValue, Is.True);
                Assert.That(finishCount, Is.EqualTo(1));

                var result = controller.Result.Value;
                Assert.That(result.AcceptedCheckpointCount, Is.EqualTo(3));
                Assert.That(result.RequiredCheckpointCount, Is.EqualTo(3));
                Assert.That(result.Completed, Is.True);
                Assert.That(result.ElapsedSeconds, Is.GreaterThan(0d));
                Assert.That(observed.HasValue, Is.True);
                Assert.That(observed.Value.ElapsedSeconds, Is.EqualTo(result.ElapsedSeconds).Within(1e-9));

                var frozenElapsed = controller.ElapsedSeconds;

                // Leave and re-enter the finish trigger; no second result may be emitted.
                yield return Teleport(body, -50f);
                yield return Teleport(body, finish.transform.position.z);

                Assert.That(finishCount, Is.EqualTo(1), "A duplicate finish trigger must not emit a second result.");
                Assert.That(controller.ElapsedSeconds, Is.EqualTo(frozenElapsed).Within(1e-9),
                    "Elapsed time must remain frozen after finish.");
            }
            finally
            {
                controller.RaceFinished -= onFinished;
            }
        }

        [UnityTest]
        public IEnumerator RetryIntent_ResetsRaceState_WithoutReloadingTheScene()
        {
            yield return LoadTestTrack();
            yield return WaitForRunning(FindSingle<RaceController>("RaceController"));

            var controller = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();
            yield return CompleteRace(controller, body);

            var trackScene = SceneManager.GetSceneByName(TestTrackSceneName);
            var sceneCountBefore = SceneManager.sceneCount;
            var trackHandleBefore = trackScene.handle;
            var vehicleInstanceId = body.GetEntityId();
            var finishCount = 0;
            Action<RaceResult> onFinished = _ => finishCount++;
            controller.RaceFinished += onFinished;

            try
            {
                Assert.That(controller.State, Is.EqualTo(RaceState.Finished));

                Assert.That(AppInputField, Is.Not.Null, "RaceController app-input seam changed; cannot inject retry intent.");
                var fakeInput = new FakeAppInput { RetryPressed = true };
                AppInputField.SetValue(controller, fakeInput);

                yield return null;
                fakeInput.RetryPressed = false;

                Assert.That(controller.State, Is.EqualTo(RaceState.Countdown));
                Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(0));
                Assert.That(controller.ElapsedSeconds, Is.EqualTo(0d));
                Assert.That(controller.Result.HasValue, Is.False);
                Assert.That(finishCount, Is.EqualTo(0), "Retry must clear the finish guard without emitting a result.");

                Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore),
                    "Retry must not unload or load scenes.");
                Assert.That(SceneManager.GetSceneByName(TestTrackSceneName).isLoaded, Is.True);
                Assert.That(SceneManager.GetSceneByName(TestTrackSceneName).handle, Is.EqualTo(trackHandleBefore),
                    "Retry must reuse the resident TestTrack scene instance.");
                Assert.That(body.GetEntityId(), Is.EqualTo(vehicleInstanceId),
                    "Retry must reuse the existing vehicle instance.");
            }
            finally
            {
                controller.RaceFinished -= onFinished;
            }
        }

        [UnityTest]
        public IEnumerator Retry_ResetsVehiclePoseAndLinearAngularVelocity()
        {
            yield return LoadTestTrack();
            yield return WaitForRunning(FindSingle<RaceController>("RaceController"));

            var controller = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();

            var spawnPosition = body.position;
            var spawnRotation = body.rotation;

            body.linearVelocity = new Vector3(4f, 1f, -3f);
            body.angularVelocity = new Vector3(2f, -5f, 1f);
            body.position = new Vector3(1f, 0.5f, 40f);

            controller.Retry();

            Assert.That(controller.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(controller.AcceptedCheckpointCount, Is.EqualTo(0));
            Assert.That(controller.ElapsedSeconds, Is.EqualTo(0d));
            Assert.That(controller.Result.HasValue, Is.False);
            Assert.That(Vector3.Distance(body.position, spawnPosition), Is.LessThan(0.05f),
                "Retry must return the vehicle to its captured spawn position.");
            Assert.That(Quaternion.Angle(body.rotation, spawnRotation), Is.LessThan(0.5f),
                "Retry must restore the captured spawn rotation.");
            Assert.That(body.linearVelocity.magnitude, Is.LessThan(0.001f),
                "Retry must clear linear velocity.");
            Assert.That(body.angularVelocity.magnitude, Is.LessThan(0.001f),
                "Retry must clear angular velocity.");
        }

        private static IEnumerator CompleteRace(RaceController controller, Rigidbody body)
        {
            var checkpoints = FindCheckpoints();
            var finish = FindSingle<FinishTrigger>("FinishTrigger");

            for (var i = 0; i < checkpoints.Length; i++)
            {
                yield return Teleport(body, checkpoints[i].transform.position.z);
            }

            yield return Teleport(body, finish.transform.position.z);

            Assert.That(controller.State, Is.EqualTo(RaceState.Finished), "The scripted sequence must complete the run.");
            Assert.That(controller.Result.HasValue, Is.True);
        }

        private static IEnumerator LoadTestTrack()
        {
            yield return SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);

            var deadline = Time.realtimeSinceStartup + 15f;
            while (!SceneManager.GetSceneByName(TestTrackSceneName).isLoaded && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(SceneManager.GetSceneByName(TestTrackSceneName).isLoaded, Is.True, "TestTrack did not load.");

            // Let physics settle one step and the vehicle/camera awakes finish.
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

        private static IEnumerator WaitRealtime(float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static IEnumerator Teleport(Rigidbody body, float z)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            var position = body.position;
            body.position = new Vector3(position.x, 0.5f, z);
            Physics.SyncTransforms();

            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
        }

        private static T FindSingle<T>(string label) where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            Assert.That(found.Length, Is.EqualTo(1), "Expected exactly one " + label + " in TestTrack.");
            return found[0];
        }

        private static CheckpointTrigger[] FindCheckpoints()
        {
            var found = UnityEngine.Object.FindObjectsByType<CheckpointTrigger>(FindObjectsInactive.Exclude);
            Array.Sort(found, (left, right) => left.Index.CompareTo(right.Index));
            return found;
        }

        private sealed class FakeAppInput : IAppInput
        {
            public bool PausePressed { get; set; }

            public bool RetryPressed { get; set; }
        }
    }
}
