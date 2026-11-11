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
    /// PlayMode proof for the Stage-4 Garage foundation (T-8.9/T-8.10): the generated scene boots,
    /// exposes the real view/presenter pair, and repeated roster navigation allocates 0 B.
    /// </summary>
    /// <remarks>
    /// The scene is loaded additively through the same editor play-mode path used by the slice
    /// loader, so it works without adding <c>Garage.unity</c> to <c>EditorBuildSettings</c> (the
    /// approved build profile stays Bootstrap → TestTrack). The measured block is the exact
    /// synchronous navigation path the screen uses for a key press or button click; UI Toolkit's
    /// own panel layout work happens outside that block and is not attributed to navigation.
    /// </remarks>
    public sealed class GaragePlayModeTests
    {
        private const string SceneName = "Garage";
        private const string ScenePath = "Assets/Scenes/Garage.unity";
        private const string HostSceneName = "GarageTestHost";
        private const float LoadTimeoutSeconds = 30f;
        private const int WarmupIterations = 240;
        private const int MeasuredIterations = 600;

        private GarageScreen _screen;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            VelocityRush.Core.SelectionContext.Clear();
            yield return ResetToCleanHost();

            if (SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(SceneName);
                yield return null;
            }

#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Additive));
#else
            yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
#endif

            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (Time.realtimeSinceStartup < deadline)
            {
                _screen = Object.FindAnyObjectByType<GarageScreen>();
                if (_screen != null && _screen.Presenter != null)
                {
                    break;
                }

                yield return null;
            }

            Assert.That(_screen, Is.Not.Null, "Garage scene has no GarageScreen.");
            Assert.That(_screen.Presenter, Is.Not.Null,
                "GarageScreen did not build its presenter; check catalog/document wiring.");

            // Let one full frame finish so style resolution and layout are warm before measuring.
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            VelocityRush.Core.SelectionContext.Clear();
            if (SceneManager.GetSceneByName(SceneName).isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(SceneName);
            }

            _screen = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_BootsWithTenVehiclesAndNamedControls()
        {
            var presenter = _screen.Presenter;
            var document = _screen.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null);
            Assert.That(document.panelSettings, Is.Not.Null, "Garage UIDocument must use the slice panel settings.");

            var root = document.rootVisualElement;
            Assert.That(root, Is.Not.Null);
            Assert.That(presenter.Count, Is.EqualTo(10));
            Assert.That(presenter.SelectedIndex, Is.EqualTo(0));

            Assert.That(root.Q<Button>("GaragePrevious"), Is.Not.Null);
            Assert.That(root.Q<Button>("GarageNext"), Is.Not.Null);
            Assert.That(root.Q<Button>("GarageConfirm"), Is.Not.Null);
            Assert.That(root.Q<Label>("GarageConfirmation"), Is.Not.Null);
            Assert.That(root.Q("GarageDetail_" + presenter.Current.VehicleId), Is.Not.Null);

            presenter.Next();
            presenter.Confirm();
            Assert.That(presenter.ConfirmedIndex, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Confirm_WritesTheStableVehicleIdToSelectionContext()
        {
            // Remove the contained debug handoff first: this test exercises the selection write,
            // not the Garage -> HandlingCourse scene load.
            var launcher = Object.FindAnyObjectByType<GarageDebugLauncher>();
            if (launcher != null)
            {
                Object.Destroy(launcher.gameObject);
                yield return null;
            }

            _screen.Presenter.Next();
            var expected = _screen.Presenter.Current.VehicleId;
            Assert.That(_screen.ConfirmSelection(), Is.True);
            Assert.That(VelocityRush.Core.SelectionContext.SelectedVehicleId, Is.EqualTo(expected));
        }

        [UnityTest]
        public IEnumerator RepeatedSelection_InPlayMode_AllocatesZeroBytes()
        {
            var presenter = _screen.Presenter;

            for (var i = 0; i < WarmupIterations; i++)
            {
                presenter.Next();
                if ((i & 1) == 0)
                {
                    presenter.Confirm();
                }
            }

            yield return null;

            var before = System.GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < MeasuredIterations; i++)
            {
                if ((i & 1) == 0)
                {
                    presenter.Next();
                }
                else
                {
                    presenter.Previous();
                }
            }

            var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "Garage navigation must allocate 0 B in play mode; measured " + allocated +
                " B over " + MeasuredIterations + " selections.");
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

                if (scene.name.StartsWith("InitTestScene", System.StringComparison.Ordinal))
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return null;
        }
    }
}
