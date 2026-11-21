using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// Decoupled seam for scene lifecycle coordination during app state transitions (TDD §35; D-070).
    /// </summary>
    public interface ISceneLifecycleAdapter
    {
        /// <summary>True while an asynchronous load or unload transition is currently in progress.</summary>
        bool IsTransitionInFlight { get; }

        /// <summary>Name of the currently active additive race scene, or null if none is loaded.</summary>
        string ActiveRaceSceneName { get; }

        /// <summary>
        /// Requests an asynchronous additive load of the specified race scene.
        /// </summary>
        /// <param name="sceneName">Target scene name or path.</param>
        /// <param name="onComplete">Callback invoked once the scene is loaded and activated.</param>
        /// <returns>True if the load transition was initiated; false if rejected.</returns>
        bool TryLoadRaceScene(string sceneName, Action onComplete = null);

        /// <summary>
        /// Requests an asynchronous unload of the currently active race scene.
        /// </summary>
        /// <param name="onComplete">Callback invoked once the scene is unloaded and root restored.</param>
        /// <returns>True if the unload transition was initiated; false if rejected.</returns>
        bool TryUnloadRaceScene(Action onComplete = null);

        /// <summary>
        /// Resets the active race in place without unloading or reloading the additive scene (TDD §35.5).
        /// </summary>
        /// <returns>True if the fast retry reset was executed; false if rejected.</returns>
        bool RequestFastRetry();
    }
}
