#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Contained Phase-8 debug handoff from the Garage to the selected-vehicle handling course
    /// (T-8.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is deliberately not the GameFlow. It subscribes to the Garage's confirmed-selection
    /// event and performs a single synchronous scene load so a development build can exercise the
    /// full Garage → track → Garage loop. Phase 11 replaces this component with the GameFlow
    /// transition and loading-veil contract (TDD §30.6).
    /// </para>
    /// <para>
    /// Compiled only in the editor and development builds, so no release player can reach it.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GarageDebugLauncher : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.GarageDebug] ";

        [SerializeField] private GarageScreen screen;
        [SerializeField] private string courseSceneName = "HandlingCourse";

        private bool _subscribed;

        private void Start()
        {
            if (screen == null)
            {
                Debug.LogWarning(LogPrefix + "No GarageScreen assigned; debug handoff disabled.", this);
                return;
            }

            screen.SelectionConfirmed += HandleSelectionConfirmed;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (_subscribed && screen != null)
            {
                screen.SelectionConfirmed -= HandleSelectionConfirmed;
            }

            _subscribed = false;
        }

        private void HandleSelectionConfirmed(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(courseSceneName))
            {
                return;
            }

            Debug.Log(
                LogPrefix + "Confirmed '" + vehicleId + "'; loading debug course '" +
                courseSceneName + "'.", this);
            SceneManager.LoadScene(courseSceneName, LoadSceneMode.Single);
        }
    }
}
#endif
