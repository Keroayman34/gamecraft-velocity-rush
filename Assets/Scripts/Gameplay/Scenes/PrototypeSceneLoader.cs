using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Minimal, temporary loader for the prototype TestTrack scene (T5.2.1).
    /// Loads additively so the persistent Bootstrap root survives (TDD §35.1/§35.3).
    /// This is not a general scene framework and not the commercial GameFlow.
    /// </summary>
    /// <remarks>
    /// The loader is deliberately stateless from the caller's perspective: repeated requests
    /// while loading or after the scene is already loaded are ignored instead of queued.
    /// Failures are reported through the return value, <see cref="LastError"/> and a prefixed
    /// error log so a missing build-settings entry is diagnosable without a debugger.
    /// </remarks>
    public static class PrototypeSceneLoader
    {
        /// <summary>Scene name used by <see cref="SceneManager"/> lookups.</summary>
        public const string TestTrackSceneName = "TestTrack";

        /// <summary>Project-relative path used for deterministic build-index lookup and loading.</summary>
        public const string TestTrackScenePath = "Assets/Scenes/TestTrack.unity";

        private const string LogPrefix = "[VelocityRush.Prototype] ";

        private static AsyncOperation _operation;
        private static string _lastError;

        /// <summary>True while an additive TestTrack load is in flight.</summary>
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
            error = null;

            if (SceneManager.GetSceneByName(TestTrackSceneName).isLoaded)
            {
                return true;
            }

            if (IsLoading)
            {
                return true;
            }

            if (SceneUtility.GetBuildIndexByScenePath(TestTrackScenePath) < 0)
            {
                error = "TestTrack scene is not in Build Settings. Add '" + TestTrackScenePath +
                        "' at build index 1; Bootstrap must stay at index 0.";
                _lastError = error;
                Debug.LogError(LogPrefix + error);
                return false;
            }

            try
            {
                _lastError = null;
                _operation = SceneManager.LoadSceneAsync(TestTrackScenePath, LoadSceneMode.Additive);

                if (_operation == null)
                {
                    error = "Unity returned no AsyncOperation for '" + TestTrackScenePath + "'.";
                    _lastError = error;
                    Debug.LogError(LogPrefix + error);
                    return false;
                }

                _operation.completed += OnLoadCompleted;
                return true;
            }
            catch (Exception exception)
            {
                error = "Failed to start additive load of '" + TestTrackScenePath + "': " + exception.Message;
                _lastError = error;
                _operation = null;
                Debug.LogError(LogPrefix + error);
                return false;
            }
        }

        private static void OnLoadCompleted(AsyncOperation operation)
        {
            operation.completed -= OnLoadCompleted;
            _operation = null;

            if (!SceneManager.GetSceneByName(TestTrackSceneName).isLoaded)
            {
                _lastError = "Additive load finished but the TestTrack scene is not loaded.";
                Debug.LogError(LogPrefix + _lastError);
            }
        }
    }
}
