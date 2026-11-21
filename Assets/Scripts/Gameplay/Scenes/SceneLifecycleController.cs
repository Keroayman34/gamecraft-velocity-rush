using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Core;
using Object = UnityEngine.Object;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Persistent-root coordinator for additive scene loading, activation, and unloading (TDD §35; D-070).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Keeps <c>Bootstrap.unity</c> resident for the entire session. Loads environment/event scenes
    /// additively via <see cref="LoadSceneMode.Additive"/>, sets the loaded scene active for correct
    /// lighting/skybox rendering, and cleans up with <see cref="Resources.UnloadUnusedAssets"/> on exit.
    /// </para>
    /// <para>
    /// Provides an in-scene fast retry seam (<see cref="RequestFastRetry"/>) that resets the active
    /// race without touching the scene manager.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SceneLifecycleController : MonoBehaviour, ISceneLifecycleAdapter
    {
        private const string LogPrefix = "[VelocityRush.SceneLifecycle] ";

        [SerializeField] private MonoBehaviour loadingVeilSource;

        private ILoadingVeil _loadingVeil;
        private bool _isTransitionInFlight;
        private string _activeRaceSceneName;
        private Coroutine _activeTransitionCoroutine;

        public bool IsTransitionInFlight => _isTransitionInFlight;
        public string ActiveRaceSceneName => _activeRaceSceneName;

        public void Initialize(ILoadingVeil loadingVeil)
        {
            _loadingVeil = loadingVeil;
        }

        private void Awake()
        {
            if (_loadingVeil == null && loadingVeilSource is ILoadingVeil veil)
            {
                _loadingVeil = veil;
            }

            if (_loadingVeil == null)
            {
                _loadingVeil = GetComponent<ILoadingVeil>();
            }
        }

        public bool TryLoadRaceScene(string sceneName, Action onComplete = null)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                Debug.LogWarning(LogPrefix + "Load rejected: sceneName is null or empty.");
                return false;
            }

            if (_isTransitionInFlight)
            {
                Debug.LogWarning(LogPrefix + "Load rejected: transition already in flight.");
                return false;
            }

            // Check if scene is already loaded
            var existing = SceneManager.GetSceneByName(sceneName);
            if (existing.IsValid() && existing.isLoaded)
            {
                _activeRaceSceneName = sceneName;
                SceneManager.SetActiveScene(existing);
                onComplete?.Invoke();
                return true;
            }

            _activeTransitionCoroutine = StartCoroutine(LoadRaceSceneCoroutine(sceneName.Trim(), onComplete));
            return true;
        }

        private IEnumerator LoadRaceSceneCoroutine(string sceneName, Action onComplete)
        {
            _isTransitionInFlight = true;
            _loadingVeil?.Show("Loading " + sceneName);

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (asyncOp == null)
            {
                Debug.LogError(LogPrefix + "SceneManager.LoadSceneAsync returned null for scene '" + sceneName + "'.");
                _isTransitionInFlight = false;
                _loadingVeil?.Hide();
                yield break;
            }

            while (!asyncOp.isDone)
            {
                _loadingVeil?.SetProgress(asyncOp.progress);
                yield return null;
            }

            var loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                loadedScene = SceneManager.GetSceneByPath(sceneName);
            }

            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                SceneManager.SetActiveScene(loadedScene);
                _activeRaceSceneName = loadedScene.name;
                Debug.Log(LogPrefix + "Activated additive race scene: " + _activeRaceSceneName);
            }
            else
            {
                Debug.LogWarning(LogPrefix + "Loaded scene could not be retrieved: " + sceneName);
            }

            _loadingVeil?.Hide();
            _isTransitionInFlight = false;
            _activeTransitionCoroutine = null;
            onComplete?.Invoke();
        }

        public bool TryUnloadRaceScene(Action onComplete = null)
        {
            if (_isTransitionInFlight)
            {
                Debug.LogWarning(LogPrefix + "Unload rejected: transition already in flight.");
                return false;
            }

            if (string.IsNullOrEmpty(_activeRaceSceneName))
            {
                onComplete?.Invoke();
                return true;
            }

            var sceneToUnload = SceneManager.GetSceneByName(_activeRaceSceneName);
            if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
            {
                _activeRaceSceneName = null;
                onComplete?.Invoke();
                return true;
            }

            _activeTransitionCoroutine = StartCoroutine(UnloadRaceSceneCoroutine(sceneToUnload, onComplete));
            return true;
        }

        private IEnumerator UnloadRaceSceneCoroutine(Scene scene, Action onComplete)
        {
            _isTransitionInFlight = true;
            _loadingVeil?.Show("Unloading race scene...");

            // Restore the persistent root host scene (or build index 0) as ActiveScene before unloading
            var hostScene = gameObject.scene;
            if (hostScene.IsValid() && hostScene.isLoaded)
            {
                SceneManager.SetActiveScene(hostScene);
            }
            else
            {
                var bootstrapScene = SceneManager.GetSceneByBuildIndex(0);
                if (bootstrapScene.IsValid() && bootstrapScene.isLoaded)
                {
                    SceneManager.SetActiveScene(bootstrapScene);
                }
            }

            var asyncOp = SceneManager.UnloadSceneAsync(scene);
            while (asyncOp != null && !asyncOp.isDone)
            {
                yield return null;
            }

            _activeRaceSceneName = null;

            // Explicit cleanup per TDD §35.6
            yield return Resources.UnloadUnusedAssets();

            _loadingVeil?.Hide();
            _isTransitionInFlight = false;
            _activeTransitionCoroutine = null;
            onComplete?.Invoke();
        }

        public bool RequestFastRetry()
        {
            if (_isTransitionInFlight)
            {
                Debug.LogWarning(LogPrefix + "Fast retry rejected: transition in flight.");
                return false;
            }

            var raceController = Object.FindAnyObjectByType<RaceController>();
            if (raceController != null)
            {
                raceController.Retry();
                Debug.Log(LogPrefix + "Fast retry executed via RaceController.Retry().");
                return true;
            }

            Debug.LogWarning(LogPrefix + "Fast retry failed: no active RaceController found in scene.");
            return false;
        }
    }
}
