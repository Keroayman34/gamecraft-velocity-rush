namespace VelocityRush.Core
{
    /// <summary>
    /// Session-scoped vehicle selection shared between the Garage and the debug race spawn
    /// (TDD §34.3; T-8.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Phase-8 Garage is the temporary writer while the full GameFlow does not exist. Phase 11
    /// replaces that write with the single GameFlow transition writer pinned by §34.3; consumers
    /// (the debug spawner) only ever read the selected id.
    /// </para>
    /// <para>
    /// The context stores the stable <c>VehicleDefinition.VehicleId</c> only. It never stores a
    /// scene object, prefab reference, or physics profile, so it stays valid across additive scene
    /// unload/reload and cannot become a second source of truth.
    /// </para>
    /// </remarks>
    public static class SelectionContext
    {
        private static string _selectedVehicleId;

        /// <summary>True when a non-empty vehicle id has been selected for this session.</summary>
        public static bool HasSelection => !string.IsNullOrEmpty(_selectedVehicleId);

        /// <summary>The selected stable vehicle id, or null when nothing is selected.</summary>
        public static string SelectedVehicleId => _selectedVehicleId;

        /// <summary>
        /// Records the selected vehicle id. Empty or whitespace ids are rejected so a consumer can
        /// never resolve a partial selection.
        /// </summary>
        /// <param name="vehicleId">Stable id from a validated <c>VehicleDefinition</c>.</param>
        /// <returns>True when the id was stored.</returns>
        public static bool SetSelection(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                return false;
            }

            _selectedVehicleId = vehicleId.Trim();
            return true;
        }

        /// <summary>Clears the session selection (test isolation, debug return, or scene reset).</summary>
        public static void Clear()
        {
            _selectedVehicleId = null;
        }
    }
}
