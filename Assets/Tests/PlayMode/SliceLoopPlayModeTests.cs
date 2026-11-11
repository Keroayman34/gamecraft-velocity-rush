using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode coverage for the Phase-7 vertical slice loop (T-7.13): additive HandlingCourse
    /// load, countdown-to-running transition, pause/resume through <see cref="SlicePauseDriver"/>,
    /// results binding, in-scene retry, and unload/reload stability.
    /// </summary>
    /// <remarks>
    /// The slice scene is intentionally absent from the prototype build settings, so it is loaded
    /// through the same editor play-mode path the slice loader uses for development review. Each
    /// test owns an isolated scene instance and restores <see cref="Time.timeScale"/> and
    /// <see cref="AudioListener.pause"/> so a failed assertion can never strand the runner frozen.
    /// </remarks>
    public sealed class SliceLoopPlayModeTests
    {
        private const float LoadTimeoutSeconds = 30f;
        private const float StateTimeoutSeconds = 15f;
        private const string HostSceneName = "SliceTestHost";

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            RestoreEngineGlobals();

            // Other PlayMode suites leave Bootstrap + TestTrack resident in the shared play
            // session. Isolate the slice tests in an empty host scene so exactly one slice
            // object graph is present regardless of execution order.
            yield return ResetToCleanHost();

            if (SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(PrototypeSceneLoader.HandlingCourseSceneName);
                yield return null;
            }

            var requested = PrototypeSceneLoader.TryLoadHandlingCourse(out var error);
            Assert.That(requested, Is.True, "HandlingCourse load request failed: " + error);

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (!SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded,
                Is.True,
                "HandlingCourse did not load additively within " + LoadTimeoutSeconds + " s.");

            // Let Awake/Start run and the UIDocument create its runtime panel.
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            var race = FindOptional<RaceController>();
            if (race != null && race.IsPaused)
            {
                race.SetPaused(false);
            }

            RestoreEngineGlobals();

            if (SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(PrototypeSceneLoader.HandlingCourseSceneName);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator SliceScene_LoadsAdditively_AndCompletesCountdownIntoTheRun()
        {
            var race = FindSingle<RaceController>("RaceController");
            var vehicle = FindSingle<VehicleController>("VehicleController");
            var checkpoints = UnityEngine.Object.FindObjectsByType<CheckpointTrigger>(FindObjectsInactive.Exclude);
            var finish = FindSingle<FinishTrigger>("FinishTrigger");

            Assert.That(vehicle.GetComponent<Rigidbody>(), Is.Not.Null, "Hero vehicle lost its physics body.");
            Assert.That(checkpoints.Length, Is.EqualTo(HandlingCourseLayout.CheckpointCount));
            Assert.That(finish, Is.Not.Null);

            yield return WaitForState(race, RaceState.Running);

            Assert.That(race.ElapsedSeconds, Is.GreaterThanOrEqualTo(0d));
            Assert.That(race.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Hud_BuildsTheCountdownPauseAndResultsSurfaces()
        {
            var document = FindSingle<UIDocument>("SliceHud UIDocument");
            var root = document.rootVisualElement;

            Assert.That(root, Is.Not.Null, "UIDocument did not create a runtime panel.");

            var countdown = root.Q("CountdownGroup");
            var pause = root.Q("PauseOverlay");
            var results = root.Q("ResultsPanel");
            var resultsTitle = root.Q<Label>("ResultsTitle");
            var resultsTime = root.Q<Label>("ResultsTime");
            var resultsCheckpoints = root.Q<Label>("ResultsCheckpoints");

            Assert.That(countdown, Is.Not.Null, "Countdown group missing from the HUD tree.");
            Assert.That(pause, Is.Not.Null, "Pause overlay missing from the HUD tree.");
            Assert.That(results, Is.Not.Null, "Results panel missing from the HUD tree.");
            Assert.That(resultsTitle, Is.Not.Null);
            Assert.That(resultsTime, Is.Not.Null);
            Assert.That(resultsCheckpoints, Is.Not.Null);
            Assert.That(pause.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
            Assert.That(results.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            yield return null;
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTheSimulation_ShowsTheOverlay_AndResumeRestores()
        {
            var race = FindSingle<RaceController>("RaceController");
            yield return WaitForState(race, RaceState.Running);

            var root = FindSingle<UIDocument>("SliceHud UIDocument").rootVisualElement;
            var pause = root.Q("PauseOverlay");
            var countdown = root.Q("CountdownGroup");

            race.SetPaused(true);
            yield return null;
            yield return null;

            Assert.That(race.IsPaused, Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(0f), "SlicePauseDriver must freeze the simulation.");
            Assert.That(AudioListener.pause, Is.True, "SlicePauseDriver must pause the audio listener.");
            Assert.That(pause.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(countdown.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));

            var frozenElapsed = race.ElapsedSeconds;
            yield return WaitRealtime(0.15f);
            Assert.That(race.ElapsedSeconds, Is.EqualTo(frozenElapsed).Within(1e-9),
                "Elapsed race time must not advance while paused.");

            race.SetPaused(false);
            yield return null;
            yield return null;

            Assert.That(race.IsPaused, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(AudioListener.pause, Is.False);
            Assert.That(pause.resolvedStyle.display, Is.EqualTo(DisplayStyle.None));
        }

        [UnityTest]
        public IEnumerator Pause_IsRefusedAfterFinish_AndResultsStayVisible()
        {
            var race = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();
            yield return WaitForState(race, RaceState.Running);
            yield return CompleteRace(race, body);

            var root = FindSingle<UIDocument>("SliceHud UIDocument").rootVisualElement;
            var results = root.Q("ResultsPanel");
            var resultsTitle = root.Q<Label>("ResultsTitle");
            var resultsTime = root.Q<Label>("ResultsTime");

            yield return null;
            Assert.That(results.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(resultsTitle.text, Is.EqualTo("FINISHED"));
            Assert.That(resultsTime.text, Does.EndWith(" S"));

            race.SetPaused(true);
            yield return null;

            Assert.That(race.IsPaused, Is.False, "A finished attempt must not accept a pause.");
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(results.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));
        }

        [UnityTest]
        public IEnumerator Retry_AfterFinish_RestartsTheCountdownAndHidesResults()
        {
            var race = FindSingle<RaceController>("RaceController");
            var body = FindSingle<VehicleController>("VehicleController").GetComponent<Rigidbody>();
            yield return WaitForState(race, RaceState.Running);
            yield return CompleteRace(race, body);

            var root = FindSingle<UIDocument>("SliceHud UIDocument").rootVisualElement;
            var results = root.Q("ResultsPanel");
            yield return null;
            Assert.That(results.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex));

            race.Retry();
            yield return null;
            yield return null;

            Assert.That(race.State, Is.EqualTo(RaceState.Countdown));
            Assert.That(race.AcceptedCheckpointCount, Is.EqualTo(0));
            Assert.That(race.Result.HasValue, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Retry must never leave the simulation frozen.");
            Assert.That(results.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                "The results overlay must not survive into the new countdown.");

            yield return WaitForState(race, RaceState.Running);
        }

        [UnityTest]
        public IEnumerator UnloadAndReload_LeavesTheSliceLoopStable()
        {
            var firstRace = FindSingle<RaceController>("RaceController");
            var firstHandle = SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).handle;

            yield return SceneManager.UnloadSceneAsync(PrototypeSceneLoader.HandlingCourseSceneName);
            yield return null;

            Assert.That(
                SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded,
                Is.False);
            Assert.That(FindOptional<RaceController>(), Is.Null, "Unload must remove the race controller.");

            var requested = PrototypeSceneLoader.TryLoadHandlingCourse(out var error);
            Assert.That(requested, Is.True, "Reload request failed: " + error);

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (!SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).isLoaded,
                Is.True);
            yield return null;
            yield return null;

            var secondRace = FindSingle<RaceController>("RaceController");
            Assert.That(
                SceneManager.GetSceneByName(PrototypeSceneLoader.HandlingCourseSceneName).handle,
                Is.Not.EqualTo(firstHandle),
                "A reload must create a fresh scene instance.");
            Assert.That(secondRace, Is.Not.SameAs(firstRace));
            Assert.That(FindSingle<UIDocument>("SliceHud UIDocument").rootVisualElement, Is.Not.Null);

            yield return WaitForState(secondRace, RaceState.Running);
        }

        private static IEnumerator ResetToCleanHost()
        {
            var host = SceneManager.GetSceneByName(HostSceneName);
            if (!host.isLoaded)
            {
                host = SceneManager.CreateScene(HostSceneName);
            }

            SceneManager.SetActiveScene(host);

            for (var i = SceneManager.sceneCount - 1; i >= 0; i--)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene == host || !scene.isLoaded)
                {
                    continue;
                }

                // The test runner owns its generated InitTestScene; everything else is a
                // leftover gameplay scene from another suite.
                if (scene.name.StartsWith("InitTestScene", StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }

        private static IEnumerator CompleteRace(RaceController race, Rigidbody body)
        {
            for (var i = 0; i < race.RequiredCheckpointCount; i++)
            {
                race.ReportCheckpoint(i, body);
            }

            race.ReportFinish(body);
            yield return null;

            Assert.That(race.State, Is.EqualTo(RaceState.Finished), "The scripted sequence must finish the run.");
            Assert.That(race.Result.HasValue, Is.True);
        }

        private static IEnumerator WaitForState(RaceController race, RaceState expected)
        {
            var deadline = Time.realtimeSinceStartup + StateTimeoutSeconds;
            while (race.State != expected && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(race.State, Is.EqualTo(expected), "Race did not reach " + expected + " in time.");
        }

        private static IEnumerator WaitRealtime(float seconds)
        {
            var deadline = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

        private static T FindSingle<T>(string label) where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            Assert.That(found.Length, Is.EqualTo(1), "Expected exactly one " + label + " in HandlingCourse.");
            return found[0];
        }

        private static T FindOptional<T>() where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Exclude);
            return found.Length > 0 ? found[0] : null;
        }

        private static void RestoreEngineGlobals()
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
