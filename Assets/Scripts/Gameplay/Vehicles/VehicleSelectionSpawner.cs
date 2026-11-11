using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Contained Phase-8 debug spawn: reads <see cref="SelectionContext"/> when the handling course
    /// starts and replaces the prototyped template vehicle with the selected data-driven rig
    /// (T-8.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The selected rig is instantiated under an inactive holder so input and profile can be bound
    /// before <see cref="VehicleController.Awake"/> validates the rig; the holder is then activated
    /// and the template is destroyed. Presentation that is authored scene-locally for the slice
    /// (engine/tyre audio, the three budgeted particle systems, impact one-shots) is transferred
    /// from the template to the selected root and rebound to its telemetry.
    /// </para>
    /// <para>
    /// When no selection exists (editor playtests of the slice, existing slice PlayMode suites, or
    /// a direct scene open) the template is left untouched, so this debug seam cannot change the
    /// approved vertical-slice behavior. Full GameFlow integration remains Phase 11.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleSelectionSpawner : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.SelectionSpawn] ";
        private const string EngineAudioName = "Engine Audio";
        private const string TyreAudioName = "Tyre Audio";
        private const string SmokeName = "Vfx Tyre Smoke";
        private const string DustName = "Vfx Ground Dust";
        private const string SparksName = "Vfx Impact Sparks";

        [Header("Data contract")]
        [SerializeField] private VehicleRigCatalog rigCatalog;

        [Header("Scene wiring")]
        [SerializeField] private GameObject templateVehicle;
        [SerializeField] private MonoBehaviour appInputSource;
        [SerializeField] private RaceController race;
        [SerializeField] private VehicleCameraRig cameraRig;
        [SerializeField] private SliceHudPresenter hudPresenter;

#if UNITY_EDITOR || DEBUG
        [SerializeField] private VehicleTelemetryOverlay telemetryOverlay;
#endif

        [Header("Debug loop (development builds only)")]
        [SerializeField] private string debugGarageScene = "Garage";
        [SerializeField] private KeyCode debugReturnKey = KeyCode.Escape;

        private bool _spawned;
        private string _selectedVehicleId;

        /// <summary>True when a selected rig replaced the template this session.</summary>
        public bool SpawnedSelection => _spawned;

        /// <summary>Stable id of the spawned rig, or null when the template remains.</summary>
        public string SelectedVehicleId => _selectedVehicleId;

        private void Start()
        {
            TrySpawnSelectedVehicle();
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (_spawned && !string.IsNullOrEmpty(debugGarageScene) && Input.GetKeyDown(debugReturnKey))
            {
                Debug.Log(LogPrefix + "Returning to debug Garage scene '" + debugGarageScene + "'.", this);
                SceneManager.LoadScene(debugGarageScene, LoadSceneMode.Single);
            }
#endif
        }

        /// <summary>
        /// Applies the current <see cref="SelectionContext"/> selection, if any. Public so the
        /// PlayMode spawn tests can drive the exact production path without a frame delay.
        /// </summary>
        /// <returns>True when the selected rig replaced the template.</returns>
        public bool TrySpawnSelectedVehicle()
        {
            if (_spawned)
            {
                return true;
            }

            if (!SelectionContext.HasSelection)
            {
                return false;
            }

            if (rigCatalog == null || templateVehicle == null)
            {
                Debug.LogWarning(LogPrefix + "Selection exists but the rig catalog or template is not wired; keeping the template.", this);
                return false;
            }

            var vehicleId = SelectionContext.SelectedVehicleId;
            if (!rigCatalog.TryGetRig(vehicleId, out var definition, out var rigPrefab))
            {
                Debug.LogError(LogPrefix + "No rig entry for selected vehicle '" + vehicleId + "'; keeping the template.", this);
                return false;
            }

            var templateController = templateVehicle.GetComponent<VehicleController>();
            if (templateController == null)
            {
                Debug.LogError(LogPrefix + "Template vehicle has no VehicleController; keeping the template.", this);
                return false;
            }

            var spawnPosition = templateVehicle.transform.position;
            var spawnRotation = templateVehicle.transform.rotation;

            var holder = new GameObject("SelectedVehicle_" + definition.VehicleId);
            holder.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            // Keep the holder inactive until input/profile are bound: VehicleController.Awake must
            // observe a complete rig or it disables itself for the rest of the session.
            holder.SetActive(false);

            var selectedGo = Instantiate(rigPrefab, holder.transform, false);
            selectedGo.name = definition.VehicleId + "_Rig";
            var selected = selectedGo.GetComponent<VehicleController>();
            if (selected == null)
            {
                Debug.LogError(LogPrefix + "Selected prefab '" + definition.VehicleId + "' has no VehicleController; keeping the template.", this);
                Destroy(holder);
                return false;
            }

            if (appInputSource != null && !selected.TrySetInputSource(appInputSource, out var inputError))
            {
                Debug.LogError(LogPrefix + inputError + " Keeping the template.", this);
                Destroy(holder);
                return false;
            }

            if (!selected.TryApplyProfile(definition.PhysicsProfile, out var profileError))
            {
                Debug.LogError(LogPrefix + profileError + " Keeping the template.", this);
                Destroy(holder);
                return false;
            }

            // The template is disabled first so its Rigidbody cannot keep participating while
            // presentation is moved, then it is destroyed once the replacement is live.
            templateVehicle.SetActive(false);
            TransferPresentation(templateVehicle, selectedGo, selected);

            holder.SetActive(true);
            if (!selected.isActiveAndEnabled)
            {
                Debug.LogError(LogPrefix + "Selected rig failed to activate; restoring the template.", this);
                holder.SetActive(false);
                Destroy(holder);
                templateVehicle.SetActive(true);
                return false;
            }

            if (race != null)
            {
                race.Configure(selected, spawnPosition, spawnRotation);
            }

            if (cameraRig != null)
            {
                cameraRig.Configure(selectedGo.transform, selected);
            }

            if (hudPresenter != null)
            {
                hudPresenter.Configure(selected);
            }

#if UNITY_EDITOR || DEBUG
            if (telemetryOverlay != null)
            {
                telemetryOverlay.Configure(selected);
            }
#endif

            Destroy(templateVehicle);
            _spawned = true;
            _selectedVehicleId = definition.VehicleId;

            Debug.Log(
                LogPrefix + "Spawned selected rig '" + definition.VehicleId + "' (" + definition.Class +
                ") from the data contract; template replaced.", this);
            return true;
        }

        private static void TransferPresentation(
            GameObject template,
            GameObject selectedGo,
            VehicleController selected)
        {
            MoveIfPresent(template.transform, selectedGo.transform, EngineAudioName, out var engine);
            if (engine != null)
            {
                var controller = engine.GetComponent<EngineAudioController>();
                if (controller != null)
                {
                    controller.Configure(selected);
                }
            }

            MoveIfPresent(template.transform, selectedGo.transform, TyreAudioName, out var tyre);
            if (tyre != null)
            {
                var controller = tyre.GetComponent<TyreAudioController>();
                if (controller != null)
                {
                    controller.Configure(selected);
                }
            }

            MoveIfPresent(template.transform, selectedGo.transform, SmokeName, out var smoke);
            MoveIfPresent(template.transform, selectedGo.transform, DustName, out var dust);
            MoveIfPresent(template.transform, selectedGo.transform, SparksName, out var sparks);

            var vfx = selectedGo.AddComponent<VehicleVfxController>();
            var anchors = new[]
            {
                selectedGo.transform.Find("Wheel RL Visual"),
                selectedGo.transform.Find("Wheel RR Visual")
            };
            vfx.Configure(
                selected,
                smoke != null ? smoke.GetComponent<ParticleSystem>() : null,
                dust != null ? dust.GetComponent<ParticleSystem>() : null,
                sparks != null ? sparks.GetComponent<ParticleSystem>() : null,
                anchors,
                null);

            AddImpactAudio(template, selectedGo, selected, vfx);
            return;
        }

        private static void AddImpactAudio(
            GameObject template,
            GameObject selectedGo,
            VehicleController selected,
            VehicleVfxController vfx)
        {
            var templateImpact = template.GetComponent<ImpactAudioController>();
            var clip = templateImpact != null ? templateImpact.ImpactClip : null;
            var impact = selectedGo.GetComponent<ImpactAudioController>();
            if (impact == null)
            {
                impact = selectedGo.AddComponent<ImpactAudioController>();
            }

            impact.Configure(clip, vfx);
        }

        private static void MoveIfPresent(
            Transform templateRoot,
            Transform selectedRoot,
            string childName,
            out Transform moved)
        {
            moved = templateRoot.Find(childName);
            if (moved == null)
            {
                return;
            }

            moved.SetParent(selectedRoot, false);
        }
    }
}
