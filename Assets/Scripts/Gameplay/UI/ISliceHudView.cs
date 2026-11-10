namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Renderer-agnostic surface consumed by <see cref="SliceHudPresenter"/>.
    /// </summary>
    /// <remarks>
    /// Keeping the presenter behind this interface means the per-frame mapping can be exercised
    /// with a test double, and a future UI stack can replace UI Toolkit without touching gameplay.
    /// </remarks>
    public interface ISliceHudView
    {
        /// <summary>Applies a per-refresh snapshot to the live HUD.</summary>
        void Apply(in SliceHudSnapshot snapshot);

        /// <summary>Shows the results overlay once the race finishes.</summary>
        void ShowResults(in RaceResult result);

        /// <summary>Hides the results overlay, e.g. after retry returns to countdown.</summary>
        void HideResults();
    }
}
