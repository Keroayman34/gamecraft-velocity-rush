namespace VelocityRush.Core
{
    /// <summary>
    /// Session-scoped selection shared between the Garage, championship/event UI,
    /// and the debug race spawn (TDD §34.3; T-8.11, T-10.6.x).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Phase-8 Garage is the temporary writer while the full GameFlow does not exist. Phase 11
    /// replaces that write with the single GameFlow transition writer pinned by §34.3; consumers
    /// (the debug spawner) only ever read the selected ids.
    /// </para>
    /// <para>
    /// The context stores only stable ids. It never stores scene objects, prefab references,
    /// or physics profiles, so it stays valid across additive scene unload/reload and cannot
    /// become a second source of truth.
    /// </para>
    /// <para>
    /// Additive M8 extension (T-10.6.x): adds ChampionshipId and EventId to the existing
    /// VehicleId, enabling the race bootstrap to resolve the complete EventLaunchPlan.
    /// </para>
    /// </remarks>
    public static class SelectionContext
    {
        private static string _selectedVehicleId;
        private static string _selectedChampionshipId;
        private static string _selectedEventId;

        /// <summary>True when a non-empty vehicle id has been selected for this session.</summary>
        public static bool HasVehicleSelection => !string.IsNullOrEmpty(_selectedVehicleId);

        /// <summary>True when a non-empty championship id has been selected for this session.</summary>
        public static bool HasChampionshipSelection => !string.IsNullOrEmpty(_selectedChampionshipId);

        /// <summary>True when a non-empty event id has been selected for this session.</summary>
        public static bool HasEventSelection => !string.IsNullOrEmpty(_selectedEventId);

        /// <summary>True when all three selections are present (ready for EventLaunchPlan resolution).</summary>
        public static bool HasCompleteSelection =>
            HasVehicleSelection && HasChampionshipSelection && HasEventSelection;

        /// <summary>The selected stable vehicle id, or null when nothing is selected.</summary>
        public static string SelectedVehicleId => _selectedVehicleId;

        /// <summary>The selected stable championship id, or null when nothing is selected.</summary>
        public static string SelectedChampionshipId => _selectedChampionshipId;

        /// <summary>The selected stable event id, or null when nothing is selected.</summary>
        public static string SelectedEventId => _selectedEventId;

        /// <summary>
        /// Records the selected vehicle id. Empty or whitespace ids are rejected so a consumer can
        /// never resolve a partial selection.
        /// </summary>
        /// <param name="vehicleId">Stable id from a validated <c>VehicleDefinition</c>.</param>
        /// <returns>True when the id was stored.</returns>
        public static bool SetVehicleSelection(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                return false;
            }

            _selectedVehicleId = vehicleId.Trim();
            return true;
        }

        /// <summary>
        /// Records the selected championship id. Empty or whitespace ids are rejected.
        /// </summary>
        /// <param name="championshipId">Stable id from a validated <c>ChampionshipDefinition</c>.</param>
        /// <returns>True when the id was stored.</returns>
        public static bool SetChampionshipSelection(string championshipId)
        {
            if (string.IsNullOrWhiteSpace(championshipId))
            {
                return false;
            }

            _selectedChampionshipId = championshipId.Trim();
            return true;
        }

        /// <summary>
        /// Records the selected event id. Empty or whitespace ids are rejected.
        /// </summary>
        /// <param name="eventId">Stable id from a validated <c>RaceEventDefinition</c>.</param>
        /// <returns>True when the id was stored.</returns>
        public static bool SetEventSelection(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            _selectedEventId = eventId.Trim();
            return true;
        }

        /// <summary>
        /// Sets all three selection ids atomically for race launch.
        /// </summary>
        /// <param name="vehicleId">Stable id from a validated <c>VehicleDefinition</c>.</param>
        /// <param name="championshipId">Stable id from a validated <c>ChampionshipDefinition</c>.</param>
        /// <param name="eventId">Stable id from a validated <c>RaceEventDefinition</c>.</param>
        /// <returns>True when all ids were stored.</returns>
        public static bool SetCompleteSelection(
            string vehicleId,
            string championshipId,
            string eventId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId) ||
                string.IsNullOrWhiteSpace(championshipId) ||
                string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            _selectedVehicleId = vehicleId.Trim();
            _selectedChampionshipId = championshipId.Trim();
            _selectedEventId = eventId.Trim();
            return true;
        }

        /// <summary>Clears the session selections (test isolation, debug return, or scene reset).</summary>
        public static void Clear()
        {
            _selectedVehicleId = null;
            _selectedChampionshipId = null;
            _selectedEventId = null;
        }
    }
}
