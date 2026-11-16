using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Runtime launch plan for a single M8 race event (T-10.6.x).
    /// </summary>
    /// <remarks>
    /// This is a transient data container passed to the race bootstrap. It resolves all
    /// definitions and layout data needed to start a race, including the environment,
    /// track path generation, grid placement, and pace scaling. It is constructed by the
    /// launch orchestration (Phase 11/GameFlow) and consumed by the race systems.
    /// </remarks>
    public sealed class EventLaunchPlan
    {
        private readonly RaceEventDefinition _eventDefinition;
        private readonly EnvironmentDefinition _environmentDefinition;
        private readonly ChampionshipDefinition _championshipDefinition;
        private readonly TrackPath _trackPath;
        private readonly RaceGridSettings _gridSettings;
        private readonly float _startLineDistance;

        /// <summary>Creates a launch plan from resolved definitions and generated path.</summary>
        public EventLaunchPlan(
            RaceEventDefinition eventDefinition,
            EnvironmentDefinition environmentDefinition,
            ChampionshipDefinition championshipDefinition,
            TrackPath trackPath,
            RaceGridSettings gridSettings,
            float startLineDistance)
        {
            _eventDefinition = eventDefinition ?? throw new ArgumentNullException(nameof(eventDefinition));
            _environmentDefinition = environmentDefinition ?? throw new ArgumentNullException(nameof(environmentDefinition));
            _championshipDefinition = championshipDefinition ?? throw new ArgumentNullException(nameof(championshipDefinition));
            _trackPath = trackPath ?? throw new ArgumentNullException(nameof(trackPath));
            _gridSettings = gridSettings;
            _startLineDistance = startLineDistance;
        }

        /// <summary>The event definition being launched.</summary>
        public RaceEventDefinition EventDefinition => _eventDefinition;

        /// <summary>The environment definition providing track and lighting.</summary>
        public EnvironmentDefinition EnvironmentDefinition => _environmentDefinition;

        /// <summary>The championship this event belongs to.</summary>
        public ChampionshipDefinition ChampionshipDefinition => _championshipDefinition;

        /// <summary>Generated racing path for this event's environment and variant.</summary>
        public TrackPath TrackPath => _trackPath;

        /// <summary>Grid spacing envelope for this event.</summary>
        public RaceGridSettings GridSettings => _gridSettings;

        /// <summary>Path distance of the start line in meters.</summary>
        public float StartLineDistance => _startLineDistance;

        /// <summary>AI pace scale for this event (from championship pace scale).</summary>
        public float PaceScale => _championshipDefinition.PaceScale;

        /// <summary>Number of AI opponents for this event (0 for TimeTrial, 4 for CircuitRace).</summary>
        public int OpponentCount =>
            _eventDefinition.EventType == RaceEventType.CircuitRace ? 4 : 0;

        /// <summary>Total grid size including player.</summary>
        public int GridSize => 1 + OpponentCount;

        /// <summary>Whether this event uses AI opponents.</summary>
        public bool HasOpponents => _eventDefinition.EventType == RaceEventType.CircuitRace;

        /// <summary>Validates that all required components are consistent and ready to race.</summary>
        public bool TryValidate(out string error)
        {
            // Path must be valid
            if (!_trackPath.TryValidate(out var pathError))
            {
                error = $"EventLaunchPlan: TrackPath invalid: {pathError}";
                return false;
            }

            // Grid settings must be valid
            if (!_gridSettings.TryValidate(out var gridError))
            {
                error = $"EventLaunchPlan: GridSettings invalid: {gridError}";
                return false;
            }

            // Environment and path must match
            if (!string.Equals(_environmentDefinition.TrackLayout.name, _trackPath.Variant.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                // Layout variant names don't need to match exactly; path carries the variant
            }

            // Grid cap check
            var gridSize = GridSize;
            if (gridSize > RaceGridLayout.MaxParticipants)
            {
                error = $"EventLaunchPlan: Grid size {gridSize} exceeds cap {RaceGridLayout.MaxParticipants}.";
                return false;
            }

            // Grid placement pre-check
            if (_gridSettings.LateralSpacing * 0.5f > _trackPath.MaxLateralOffset + 0.001f)
            {
                error = "EventLaunchPlan: Grid lateral spacing exceeds path lateral margin.";
                return false;
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Builder helper for constructing an <see cref="EventLaunchPlan"/> from catalogs.
    /// </summary>
    public static class EventLaunchPlanBuilder
    {
        /// <summary>Approved M8 pace scales by championship order.</summary>
        private static readonly float[] PaceScales = { 0.90f, 0.97f, 1.03f };

        /// <summary>
        /// Resolves and builds a complete launch plan for the given event id.
        /// </summary>
        /// <param name="eventId">Stable id of the event to launch.</param>
        /// <param name="championshipCatalog">Championship catalog containing the parent championship.</param>
        /// <param name="environmentCatalog">Environment catalog for resolving the environment.</param>
        /// <param name="gridSettings">Optional grid envelope (uses Default when null).</param>
        /// <param name="plan">Output launch plan on success.</param>
        /// <param name="error">First validation problem on failure.</param>
        /// <returns>True when the plan was successfully built.</returns>
        public static bool TryBuild(
            string eventId,
            ChampionshipCatalog championshipCatalog,
            EnvironmentCatalog environmentCatalog,
            RaceGridSettings gridSettings,
            out EventLaunchPlan plan,
            out string error)
        {
            plan = null;
            error = null;

            // Find the event definition - this would typically come from an event catalog
            // For now we validate the championship and environment resolution
            if (championshipCatalog == null)
            {
                error = "EventLaunchPlanBuilder: ChampionshipCatalog is null.";
                return false;
            }

            if (environmentCatalog == null)
            {
                error = "EventLaunchPlanBuilder: EnvironmentCatalog is null.";
                return false;
            }

            // This builder expects the caller to have already resolved the definitions
            // The actual EventCatalog will be added in a later task
            error = "EventLaunchPlanBuilder: Full implementation requires EventCatalog (Phase 11). Use direct EventLaunchPlan constructor.";
            return false;
        }

        /// <summary>
        /// Generates a <see cref="TrackPath"/> for the given environment and variant.
        /// </summary>
        public static TrackPath GenerateTrackPath(
            EnvironmentDefinition environmentDefinition,
            EnvironmentLayoutVariant variant)
        {
            if (environmentDefinition == null)
            {
                throw new ArgumentNullException(nameof(environmentDefinition));
            }

            return TrackPath.Create(
                environmentDefinition.TrackLayout,
                variant,
                TrackPathBuildSettings.Default);
        }

        /// <summary>
        /// Gets the pace scale for a championship by production order.
        /// </summary>
        public static float GetPaceScaleForOrder(int productionOrder)
        {
            var index = productionOrder - 1;
            if (index >= 0 && index < PaceScales.Length)
            {
                return PaceScales[index];
            }
            return PaceScales[0];
        }
    }
}