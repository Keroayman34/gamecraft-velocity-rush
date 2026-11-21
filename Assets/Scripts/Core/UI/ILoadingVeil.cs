namespace VelocityRush.Core
{
    /// <summary>
    /// Interface for the non-allocating scene loading overlay (TDD §35.4; D-069, D-070).
    /// </summary>
    public interface ILoadingVeil
    {
        /// <summary>True if the veil overlay is currently visible.</summary>
        bool IsVisible { get; }

        /// <summary>Current display message.</summary>
        string CurrentMessage { get; }

        /// <summary>Current normalized progress [0..1].</summary>
        float CurrentProgress { get; }

        /// <summary>Shows the loading veil with an optional message.</summary>
        void Show(string message = null);

        /// <summary>Updates the normalized progress value [0..1].</summary>
        void SetProgress(float progress);

        /// <summary>Hides the loading veil overlay.</summary>
        void Hide();
    }
}
