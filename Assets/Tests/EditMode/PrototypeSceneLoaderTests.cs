using NUnit.Framework;
using UnityEditor;
using UnityEngine.SceneManagement;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode guards for the T5.2.1 scene hosting contract: Bootstrap stays build index 0,
    /// TestTrack is the deterministic additive content scene, and the loader exposes a stable
    /// identity. Actual asynchronous loading is validated in PlayMode against the copy.
    /// </summary>
    public sealed class PrototypeSceneLoaderTests
    {
        [Test]
        public void BuildSettings_PreserveBootstrapFirst_AndAddTestTrack()
        {
            var scenes = EditorBuildSettings.scenes;

            Assert.That(scenes.Length, Is.GreaterThanOrEqualTo(2), "Bootstrap and TestTrack must both be build scenes.");
            Assert.That(scenes[0].path, Is.EqualTo("Assets/Scenes/Bootstrap.unity"));
            Assert.That(scenes[0].enabled, Is.True, "Bootstrap must remain enabled at build index 0.");
            Assert.That(scenes[1].path, Is.EqualTo(PrototypeSceneLoader.TestTrackScenePath));
            Assert.That(scenes[1].enabled, Is.True, "TestTrack must be enabled for deterministic additive loading.");
        }

        [Test]
        public void TestTrackScene_IsResolvableFromBuildSettings()
        {
            Assert.That(SceneUtility.GetBuildIndexByScenePath(PrototypeSceneLoader.TestTrackScenePath), Is.EqualTo(1));
            Assert.That(SceneUtility.GetScenePathByBuildIndex(1), Is.EqualTo(PrototypeSceneLoader.TestTrackScenePath));
        }

        [Test]
        public void Loader_ExposesDeterministicSceneIdentity()
        {
            Assert.That(PrototypeSceneLoader.TestTrackSceneName, Is.EqualTo("TestTrack"));
            Assert.That(PrototypeSceneLoader.TestTrackScenePath, Is.EqualTo("Assets/Scenes/TestTrack.unity"));
        }

        [Test]
        public void Loader_StartsIdle_WithNoError()
        {
            Assert.That(PrototypeSceneLoader.IsLoading, Is.False);
            Assert.That(PrototypeSceneLoader.LastError, Is.Null.Or.Empty);
        }

        [Test]
        public void SelectContentScene_PrefersTestTrack_WhenBothProfilesAreListed()
        {
            var selected = PrototypeSceneLoader.SelectContentScene(1, 2, out var name, out var path);

            Assert.That(selected, Is.True);
            Assert.That(name, Is.EqualTo(PrototypeSceneLoader.TestTrackSceneName));
            Assert.That(path, Is.EqualTo(PrototypeSceneLoader.TestTrackScenePath));
        }

        [Test]
        public void SelectContentScene_FallsBackToHandlingCourse_ForTheSliceProfile()
        {
            var selected = PrototypeSceneLoader.SelectContentScene(-1, 1, out var name, out var path);

            Assert.That(selected, Is.True);
            Assert.That(name, Is.EqualTo(PrototypeSceneLoader.HandlingCourseSceneName));
            Assert.That(path, Is.EqualTo(PrototypeSceneLoader.HandlingCourseScenePath));
        }

        [Test]
        public void SelectContentScene_ReturnsFalse_WhenNeitherProfileIsListed()
        {
            var selected = PrototypeSceneLoader.SelectContentScene(-1, -1, out var name, out var path);

            Assert.That(selected, Is.False);
            Assert.That(name, Is.Null);
            Assert.That(path, Is.Null);
        }

        [Test]
        public void HandlingCourseScene_IsImportable()
        {
            Assert.That(
                AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeSceneLoader.HandlingCourseScenePath),
                Is.Not.Null,
                "Rebuild the handling course via Tools > GameCraft Studio > Velocity Rush > Build Handling Course Scene.");
        }
    }
}
