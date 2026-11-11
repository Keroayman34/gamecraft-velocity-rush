namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Presentation seam for the Garage foundation (T-8.9). Implementations must treat
    /// <see cref="Refresh"/> as a per-navigation hot path: no string construction, no tree
    /// rebuilds, and no managed allocation.
    /// </summary>
    public interface IGarageView
    {
        /// <summary>Shows the selected vehicle and updates the highlighted row.</summary>
        void Refresh(in GarageVehicleSummary summary, int index, int count);

        /// <summary>Shows the confirmation state for the selected vehicle.</summary>
        void ShowConfirmation(in GarageVehicleSummary summary);
    }
}
