using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Real PlayMode coverage for the T5.3 chase rig: telemetry-source resolution, track-only
    /// obstruction wiring, finite follow pose, distance clamping, and FOV ramp bounds.
    /// </summary>
    public sealed class CameraRigPlayModeTests
    {
        private const string TestTrackSceneName = "TestTrack";

        [UnityTest]
        public IEnumerator CameraRig_ResolvesVehicleTelemetryAndTrackOnlyMask()
        {
            yield return LoadTestTrack();

            var rig = FindSingle<VehicleCameraRig>("VehicleCameraRig");
            var controller = FindSingle<VehicleController>("VehicleController");

            Assert.That(rig.TryValidateRig(out var error), Is.True, error);
            Assert.That(rig.TelemetrySource, Is.SameAs(controller),
                "The rig must consume the vehicle through IVehicleTelemetry.");
            Assert.That(rig.TelemetrySource, Is.InstanceOf<IVehicleTelemetry>());

            var trackLayer = LayerMask.NameToLayer("Track");
            Assert.That(trackLayer, Is.GreaterThanOrEqualTo(0), "The Track layer must exist.");
            Assert.That(rig.ObstructionMask.value, Is.EqualTo(1 << trackLayer),
                "Camera obstruction must be limited to the Track layer.");
        }

        [UnityTest]
        public IEnumerator CameraRig_FollowsVehicleWithFinitePoseAndBoundedDistance()
        {
            yield return LoadTestTrack();

            var rig = FindSingle<VehicleCameraRig>("VehicleCameraRig");
            var controller = FindSingle<VehicleController>("VehicleController");
            var body = controller.GetComponent<Rigidbody>();

            body.linearVelocity = body.transform.forward * 12f;
            yield return new WaitForSeconds(1.5f);

            var cameraTransform = rig.transform;
            var camera = cameraTransform.GetComponent<Camera>();

            Assert.That(IsFinite(cameraTransform.position), Is.True, "Camera position must remain finite.");
            Assert.That(IsFinite(cameraTransform.rotation), Is.True, "Camera rotation must remain finite.");
            Assert.That(controller.Telemetry.IsGrounded, Is.True,
                "Track layer reassignment must not break WheelCollider ground contact.");
            Assert.That(IsFinite(controller.Telemetry.Speed), Is.True, "Telemetry speed must remain finite.");
            Assert.That(rig.CurrentDistance, Is.GreaterThanOrEqualTo(rig.Profile.MinimumDistance - 0.05f));
            Assert.That(rig.CurrentDistance, Is.LessThanOrEqualTo(rig.Profile.FollowDistance + 0.05f));
            Assert.That(camera.fieldOfView,
                Is.InRange(rig.Profile.FieldOfViewAtRest - 0.01f, rig.Profile.FieldOfViewAtTopSpeed + 0.01f));

            var offsetFromVehicle = cameraTransform.position - body.position;
            Assert.That(Vector3.Dot(offsetFromVehicle, body.transform.forward), Is.LessThan(0f),
                "Chase camera must remain behind the vehicle.");
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

            // Let the vehicle, camera, and physics settle for a few frames.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private static T FindSingle<T>(string label) where T : Component
        {
            var found = Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            Assert.That(found.Length, Is.EqualTo(1), "Expected exactly one " + label + " in TestTrack.");
            return found[0];
        }

        private static bool IsFinite(Vector3 value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
        }

        private static bool IsFinite(Quaternion value)
        {
            return IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z) && IsFinite(value.w);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
