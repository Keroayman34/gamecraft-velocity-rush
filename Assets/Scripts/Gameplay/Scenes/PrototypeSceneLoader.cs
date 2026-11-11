using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Minimal, temporary loader for the prototype content scenes (T5.2.1, T-7.12).
    /// Loads additively so the persistent Bootstrap root survives (TDD §35.1/§35.3).
    /// This is not a general scene framework and not the commercial GameFlow.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The loader is deliberately stateless from the caller's perspective: repeated requests
    /// while loading or after the scene is already loaded are ignored instead of queued.
    /// Failures are reported through the return value, <see cref="LastError"/> and a prefixed
    /// error log so a missing build-settings entry is diagnosable without a debugger.
    /// </para>
    /// <para>
    /// Two build profiles share this loader. The prototype profile lists TestTrack, the vertical
    /// slice profile lists HandlingCourse. Selection is data-driven from the actual build-scene
    /// list rather than a compile flag, so no player setting or build-settings edit is needed:
    /// <see cref="SelectContentScene"/> prefers TestTrack when both are present, preserving the
    /// Phase-5/6 prototype contract, and falls back to HandlingCourse.
    /// </para>
    /// </remarks>
    public static class PrototypeSceneLoader
    {
        /// <summary>Scene name used by <see cref="SceneManager"/> lookups.</summary>
        public const string TestTrackSceneName = "TestTrack";

        /// <summary>Project-relative path used for deterministic build-index lookup and loading.</summary>
        public const string TestTrackScenePath = "Assets/Scenes/TestTrack.unity";

        /// <summary>Scene name of the Phase-7 vertical-slice content scene.</summary>
        public const string HandlingCourseSceneName = "HandlingCourse";

        /// <summary>Project-relative path of the Phase-7 vertical-slice content scene.</summary>
        public const string HandlingCourseScenePath = "Assets/Scenes/HandlingCourse.unity";

        private const string LogPrefix = "[VelocityRush.Prototype] ";

        private static AsyncOperation _operation;
        private static string _pendingSceneName;
        private static string _lastError;

        /// <summary>True while an additive content-scene load is in flight.</summary>
        public static bool IsLoading => _operation != null && !_operation.isDone;

        /// <summary>Diagnostic from the most recent failed load request, or null.</summary>
        public static string LastError => _lastError;

        /// <summary>
        /// Requests one additive TestTrack load. Returns true when the scene is already loaded or
        /// a load is already in flight (no duplicate request is queued). Returns false with a
        /// diagnostic when the scene cannot be loaded.
        /// </summary>
        public static bool TryLoadTestTrack(out string error)
        {
            return TryLoadAdditive(TestTrackSceneName, TestTrackScenePath, out error);
        }

        /// <summary>
        /// Requests one additive HandlingCourse (vertical slice) load. The scene intentionally
        /// stays out of the prototype build settings, so in the editor it is loaded through the
        /// play-mode scene API; a player build must list it via the WebGL slice profile.
        /// </summary>
        public static bool TryLoadHandlingCourse(out string error)
        {
            return TryLoadAdditive(HandlingCourseSceneName, HandlingCourseScenePath, out error);
        }

        /// <summary>
        /// Requests the content scene for whichever profile is present in Build Settings:
        /// TestTrack when listed (prototype), otherwise HandlingCourse (vertical slice).
        /// </summary>
        /// <param name="error">Diagnostic when no content scene can be loaded.</param>
        /// <returns>True when a scene is loaded, loading, or already resident.</returns>
        public static bool TryLoadContentScene(out string error)
        {
            error = null;

            var testTrackIndex = SceneUtility.GetBuildIndexByScenePath(TestTrackScenePath);
            var handlingCourseIndex = SceneUtility.GetBuildIndexByScenePath(HandlingCourseScenePath);

            if (!SelectContentScene(testTrackIndex, handlingCourseIndex, out var sceneName, out var scenePath))
            {
                error = "No content scene is in Build Settings. Add '" + TestTrackScenePath +
                        "' (prototype) or '" + HandlingCourseScenePath +
                        "' (vertical slice) after Bootstrap at index 0.";
                _lastError = error;
                Debug.LogError(LogPrefix + error);
                return false;
            }

            return TryLoadAdditive(sceneName, scenePath, out error);
        }

        /// <summary>
        /// Pure profile-selection rule shared by the loader and its tests. TestTrack wins when
        /// both scenes are listed so the prototype contract cannot silently change.
        /// </summary>
        /// <param name="testTrackBuildIndex">Build index of TestTrack, or a negative value.</param>
        /// <param name="handlingCourseBuildIndex">Build index of HandlingCourse, or a negative value.</param>
        /// <param name="sceneName">Selected scene name.</param>
        /// <param name="scenePath">Selected project-relative scene path.</param>
        /// <returns>True when a selectable scene exists.</returns>
        public static bool SelectContentScene(
            int testTrackBuildIndex,
            int handlingCourseBuildIndex,
            out string sceneName,
            out string scenePath)
        {
            if (testTrackBuildIndex >= 0)
            {
                sceneName = TestTrackSceneName;
                scenePath = TestTrackScenePath;
                return true;
            }

            if (handlingCourseBuildIndex >= 0)
            {
                sceneName = HandlingCourseSceneName;
                scenePath = HandlingCourseScenePath;
                return true;
            }

            sceneName = null;
            scenePath = null;
            return false;
        }

        private static bool TryLoadAdditive(string sceneName, string scenePath, out string error)
        {
            error = null;

            if (SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                return true;
            }

            if (IsLoading)
            {
                return true;
            }

            if (SceneUtility.GetBuildIndexByScenePath(scenePath) < 0 && !IsEditorPlayModeLoadAvailable())
            {
                error = sceneName + " scene is not in Build Settings. Add '" + scenePath +
                        "' after Bootstrap at build index 0.";
                _lastError = error;
                Debug.LogError(LogPrefix + error);
                return false;
            }

            try
            {
                _lastError = null;
                _pendingSceneName = sceneName;
                _operation = StartAdditiveLoad(scenePath);

                if (_operation == null)
                {
                    error = "Unity returned no AsyncOperation for '" + scenePath + "'.";
                    _lastError = error;
                    _pendingSceneName = null;
                    Debug.LogError(LogPrefix + error);
                    return false;
                }

                _operation.completed += OnLoadCompleted;
                return true;
            }
            catch (Exception exception)
            {
                error = "Failed to start additive load of '" + scenePath + "': " + exception.Message;
                _lastError = error;
                _operation = null;
                _pendingSceneName = null;
                Debug.LogError(LogPrefix + error);
                return false;
            }
        }

        private static AsyncOperation StartAdditiveLoad(string scenePath)
        {
#if UNITY_EDITOR
            // Development affordance: review/PlayMode-test the slice scene even though the
            // prototype build profile deliberately does not list it. Player builds still use the
            // build-settings path below and require the slice profile to include the scene.
            if (Application.isPlaying && SceneUtility.GetBuildIndexByScenePath(scenePath) < 0)
            {
                return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                    scenePath,
                    new LoadSceneParameters(LoadSceneMode.Additive));
            }
#endif
            return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
        }

        private static bool IsEditorPlayModeLoadAvailable()
        {
#if UNITY_EDITOR
            return Application.isPlaying;
#else
            return false;
#endif
        }

        private static void OnLoadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnLoadCompleted;
            _operation = null;

            var sceneName = _pendingSceneName;
            _pendingSceneName = null;

            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                _lastError = "Additive load finished but the " + sceneName + " scene is not loaded.";
                Debug.LogError(LogPrefix + _lastError);
            }
        }
    }
}
