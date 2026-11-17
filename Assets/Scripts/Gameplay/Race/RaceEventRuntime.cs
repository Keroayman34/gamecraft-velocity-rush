using System;
using System.Collections.Generic;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Gameplay
{
    /// <summary>How an event was launched: alone or with the approved AI field.</summary>
    public enum RaceLaunchMode
    {
        /// <summary>Solo run with no opponents (Time Trial, or a diagnostics launch).</summary>
        Solo = 0,

        /// <summary>Player plus the four approved AI opponents (Circuit Race).</summary>
        Grid = 1
    }

    /// <summary>
    /// Runtime seam that turns a resolved <see cref="EventLaunchPlan"/> into a launchable race
    /// (T-10.8.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The ten event configurations reuse the committed environment scenes read-only. This
    /// component is created by the event launcher in the loaded event scene: it resolves the
    /// plan, verifies that the scene's authored gates run in the event's direction of travel,
    /// instantiates the preauthored AI opponent prefab when the launch mode needs a field, and
    /// wires a <see cref="RaceGridController"/> with the plan's path, gate distances, and grid
    /// envelope.
    /// </para>
    /// <para>
    /// It never edits a scene asset and never changes player physics, AI behavior, or the solo
    /// <see cref="RaceResult"/> contract. Solo launches create no opponent objects and no grid.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class RaceEventRuntime : MonoBehaviour
    {
        /// <summary>Name of the runtime root that owns instantiated AI opponents.</summary>
        public const string OpponentRootName = "AI Opponents";

        private const string LogPrefix = "[VelocityRush.RaceEvent] ";

        [Header("Catalogs (dev launcher seam)")]
        [SerializeField] private ChampionshipCatalog championshipCatalog;
        [SerializeField] private EnvironmentCatalog environmentCatalog;
        [SerializeField] private EventCatalog eventCatalog;

        [Header("Event resolution")]
        [Tooltip("Stable event id. Leave empty to read SelectionContext.SelectedEventId.")]
        [SerializeField] private string eventId = string.Empty;

        [Tooltip("Resolve and launch automatically on Start (dev launcher flow).")]
        [SerializeField] private bool resolveOnStart = true;

        [Tooltip("Approved launch mode for this event.")]
        [SerializeField] private RaceLaunchMode launchMode = RaceLaunchMode.Grid;

        [Tooltip("Preauthored AI opponent prefab (player-visible rig with AIDriver/AIVisualRig).")]
        [SerializeField] private GameObject opponentPrefab;

        [Header("Grid envelope")]
        [SerializeField, Min(1f)] private float startLineDistance =
            EventLaunchPlanBuilder.DefaultStartLineDistance;

        private readonly List<AIDriver> _opponents =
            new List<AIDriver>(ChampionshipContentPolicy.CircuitRaceAiCount);

        private EventLaunchPlan _plan;
        private RaceGridController _grid;
        private RaceController _race;
        private VehicleController _player;
        private GameObject _opponentRoot;
        private bool _launched;

        /// <summary>Resolved plan, or null before a successful launch.</summary>
        public EventLaunchPlan Plan => _plan;

        /// <summary>Event definition resolved by the plan, or null.</summary>
        public RaceEventDefinition EventDefinition => _plan != null ? _plan.EventDefinition : null;

        /// <summary>Layout variant this launch uses.</summary>
        public EnvironmentLayoutVariant Variant =>
            _plan != null ? _plan.EventDefinition.LayoutVariant : EnvironmentLayoutVariant.Signature;

        /// <summary>True after a successful launch.</summary>
        public bool IsLaunched => _launched;

        /// <summary>Mode used for the current launch.</summary>
        public RaceLaunchMode LaunchMode { get; private set; }

        /// <summary>Number of instantiated AI opponents (zero for a solo launch).</summary>
        public int OpponentCount => _opponents.Count;

        /// <summary>Read-only view of the instantiated AI drivers.</summary>
        public IReadOnlyList<AIDriver> Opponents => _opponents;

        /// <summary>Grid controller created by a grid launch, or null.</summary>
        public RaceGridController Grid => _grid;

        /// <summary>Race controller this runtime drives.</summary>
        public RaceController Race => _race;

        /// <summary>Player vehicle controller this runtime drives.</summary>
        public VehicleController Player => _player;

        private void Start()
        {
            if (_launched || _plan != null || !resolveOnStart)
            {
                return;
            }

            var resolvedId = string.IsNullOrEmpty(eventId)
                ? SelectionContext.SelectedEventId
                : eventId;
            if (string.IsNullOrEmpty(resolvedId))
            {
                Debug.LogWarning(
                    LogPrefix + "No event id is assigned and none is selected; nothing to launch.",
                    this);
                return;
            }

            if (championshipCatalog == null || environmentCatalog == null || eventCatalog == null)
            {
                Debug.LogError(
                    LogPrefix + "Championship, environment, and event catalogs must all be assigned " +
                    "for automatic launch.", this);
                return;
            }

            if (!EventLaunchPlanBuilder.TryBuild(
                    resolvedId,
                    eventCatalog,
                    championshipCatalog,
                    environmentCatalog,
                    out var plan,
                    out var error))
            {
                Debug.LogError(LogPrefix + error, this);
                return;
            }

            var race = FindSingle<RaceController>();
            var player = FindSingle<VehicleController>();
            if (race == null || player == null)
            {
                Debug.LogError(
                    LogPrefix + "The event scene must contain one RaceController and one " +
                    "VehicleController.", this);
                return;
            }

            TryInitialize(plan, race, player, opponentPrefab, launchMode);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the development launcher to prepare a transient
        /// runtime object. It never saves the event scene.
        /// </summary>
        public void ConfigureForAuthoring(
            ChampionshipCatalog championships,
            EnvironmentCatalog environments,
            EventCatalog events,
            string id,
            RaceLaunchMode mode,
            GameObject aiPrefab)
        {
            championshipCatalog = championships;
            environmentCatalog = environments;
            eventCatalog = events;
            eventId = id;
            launchMode = mode;
            opponentPrefab = aiPrefab;
            resolveOnStart = true;
        }
#endif

        /// <summary>
        /// Configures the runtime from a resolved plan and scene objects, then launches in the
        /// requested mode. Returns false and logs the first problem without creating objects.
        /// </summary>
        public bool TryInitialize(
            EventLaunchPlan plan,
            RaceController race,
            VehicleController player,
            GameObject aiOpponentPrefab,
            RaceLaunchMode mode)
        {
            if (plan == null)
            {
                Debug.LogError(LogPrefix + "A resolved EventLaunchPlan is required.", this);
                return false;
            }

            if (race == null)
            {
                Debug.LogError(LogPrefix + "A RaceController is required.", this);
                return false;
            }

            if (player == null)
            {
                Debug.LogError(LogPrefix + "A player VehicleController is required.", this);
                return false;
            }

            if (_launched)
            {
                Debug.LogError(LogPrefix + "The runtime has already launched.", this);
                return false;
            }

            _plan = plan;
            _race = race;
            _player = player;
            eventId = plan.EventDefinition.EventId;
            launchMode = mode;
            startLineDistance = plan.StartLineDistance;
            opponentPrefab = aiOpponentPrefab;

            return Launch(mode);
        }

        /// <summary>
        /// Launches the configured event. Solo creates no opponents or grid; Grid creates exactly
        /// four opponents and a wired <see cref="RaceGridController"/>.
        /// </summary>
        public bool Launch(RaceLaunchMode mode)
        {
            if (_launched)
            {
                return false;
            }

            if (_plan == null)
            {
                Debug.LogError(LogPrefix + "No EventLaunchPlan has been resolved.", this);
                return false;
            }

            if (!TryVerifyGateOrder(_plan, out var gateError))
            {
                Debug.LogError(LogPrefix + gateError, this);
                return false;
            }

            LaunchMode = mode;
            if (mode == RaceLaunchMode.Grid)
            {
                if (!_plan.HasOpponents)
                {
                    Debug.LogError(
                        LogPrefix + "Event '" + _plan.EventDefinition.EventId +
                        "' is a " + _plan.EventDefinition.EventType +
                        " and must be launched solo.", this);
                    return false;
                }

                if (!TrySpawnField())
                {
                    return false;
                }
            }
            else if (!_plan.HasOpponents)
            {
                LaunchMode = RaceLaunchMode.Solo;
            }

            _launched = true;
            Debug.Log(
                LogPrefix + "Launched '" + _plan.EventDefinition.EventId +
                "' | mode=" + LaunchMode +
                " | variant=" + _plan.EventDefinition.LayoutVariant +
                " | opponents=" + _opponents.Count +
                " | pathLength=" + _plan.TrackPath.TotalLength.ToString("0.0") + " m");
            return true;
        }

        /// <summary>
        /// Verifies that the scene's ordered gates are reached in checkpoint-index order along the
        /// event path. This is the reverse-direction proof: a reverse scene must place checkpoint
        /// 0 first, then 1, then 2, then the finish.
        /// </summary>
        public bool TryVerifyGateOrder(out string error)
        {
            return TryVerifyGateOrder(_plan, out error);
        }

        private bool TryVerifyGateOrder(EventLaunchPlan plan, out string error)
        {
            error = null;
            if (plan == null)
            {
                error = "Cannot verify gate order without a plan.";
                return false;
            }

            var path = plan.TrackPath;
            var checkpoints = FindComponents<CheckpointTrigger>();
            if (checkpoints.Count != EnvironmentRaceRules.CheckpointCount)
            {
                error = "Event scene must contain exactly " + EnvironmentRaceRules.CheckpointCount +
                        " checkpoints (found " + checkpoints.Count + ").";
                return false;
            }

            var previous = -1f;
            for (var index = 0; index < EnvironmentRaceRules.CheckpointCount; index++)
            {
                var gate = FindCheckpoint(checkpoints, index);
                if (gate == null)
                {
                    error = "Event scene has no checkpoint with index " + index + ".";
                    return false;
                }

                var distance = path.ProjectDistance(gate.transform.position);
                if (distance <= previous)
                {
                    error = "Checkpoint " + index + " projects to " + distance.ToString("0.0") +
                            " m, which is not after the previous gate (" +
                            previous.ToString("0.0") + " m) for the " +
                            plan.EventDefinition.LayoutVariant + " path.";
                    return false;
                }

                previous = distance;
            }

            var finishes = FindComponents<FinishTrigger>();
            if (finishes.Count != 1)
            {
                error = "Event scene must contain exactly one finish trigger (found " +
                        finishes.Count + ").";
                return false;
            }

            var finishDistance = path.ProjectDistance(finishes[0].transform.position);
            if (finishDistance <= previous)
            {
                error = "Finish projects to " + finishDistance.ToString("0.0") +
                        " m, which is not after the final checkpoint (" +
                        previous.ToString("0.0") + " m) for the " +
                        plan.EventDefinition.LayoutVariant + " path.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes the resolved championship and event ids to the session context, plus the
        /// supplied vehicle id. Ownership stays with the launcher; this method never invents an id.
        /// </summary>
        public bool WriteSelectionContext(string vehicleId)
        {
            if (_plan == null)
            {
                return false;
            }

            return SelectionContext.SetCompleteSelection(
                vehicleId,
                _plan.ChampionshipDefinition.ChampionshipId,
                _plan.EventDefinition.EventId);
        }

        private bool TrySpawnField()
        {
            if (opponentPrefab == null)
            {
                Debug.LogError(
                    LogPrefix + "A preauthored AI opponent prefab is required for a grid launch.",
                    this);
                return false;
            }

            _opponentRoot = new GameObject(OpponentRootName);
            _opponentRoot.transform.SetParent(transform, false);

            for (var i = 0; i < ChampionshipContentPolicy.CircuitRaceAiCount; i++)
            {
                var instance = Instantiate(opponentPrefab, _opponentRoot.transform, false);
                instance.name = "AI Opponent " + i;
                instance.SetActive(true);

                var driver = instance.GetComponent<AIDriver>();
                if (driver == null)
                {
                    driver = instance.AddComponent<AIDriver>();
                }

                if (instance.GetComponent<AIVisualRig>() == null)
                {
                    instance.AddComponent<AIVisualRig>();
                }

                if (instance.GetComponent<AIVehicleInput>() == null)
                {
                    instance.AddComponent<AIVehicleInput>();
                }

                _opponents.Add(driver);
            }

            var gridObject = new GameObject("Race Grid");
            gridObject.transform.SetParent(transform, false);
            _grid = gridObject.AddComponent<RaceGridController>();

            var gateDistances = EventLaunchPlanBuilder.BuildGateDistances(
                _plan.EnvironmentDefinition.TrackLayout);
            if (!_grid.Initialize(
                    _plan.TrackPath,
                    _race,
                    _player,
                    _opponents.ToArray(),
                    startLineDistance,
                    gateDistances,
                    _plan.GridSettings))
            {
                Debug.LogError(LogPrefix + "The race grid failed to initialize.", this);
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            // The grid and opponent root are children of this object and are destroyed with it;
            // RaceGridController.OnDestroy unsubscribes every race event.
            _opponents.Clear();
            _grid = null;
            _opponentRoot = null;
            _launched = false;
        }

        private List<T> FindComponents<T>() where T : Component
        {
            var results = new List<T>();
            var scene = gameObject.scene;
            if (!scene.IsValid())
            {
                return results;
            }

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }

            return results;
        }

        private T FindSingle<T>() where T : Component
        {
            var matches = FindComponents<T>();
            if (matches.Count == 1)
            {
                return matches[0];
            }

            Debug.LogError(
                LogPrefix + "Expected exactly one " + typeof(T).Name + " in the event scene " +
                "(found " + matches.Count + ").", this);
            return null;
        }

        private static CheckpointTrigger FindCheckpoint(
            List<CheckpointTrigger> checkpoints,
            int index)
        {
            for (var i = 0; i < checkpoints.Count; i++)
            {
                if (checkpoints[i].Index == index)
                {
                    return checkpoints[i];
                }
            }

            return null;
        }
    }
}
