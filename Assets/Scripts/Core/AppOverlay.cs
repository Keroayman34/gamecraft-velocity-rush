namespace VelocityRush.Core
{
    /// <summary>
    /// Application overlay states that present over a primary <see cref="AppState"/>
    /// without mutating or corrupting the underlying state machine (TDD §30.1, §31; D-069, D-070).
    /// </summary>
    public enum AppOverlay
    {
        /// <summary>No overlay active.</summary>
        None = 0,

        /// <summary>In-race or menu pause overlay.</summary>
        Pause = 1,

        /// <summary>Audio, controls, and localization settings overlay.</summary>
        Settings = 2,

        /// <summary>Post-race or progression rewards and unlock presentation overlay.</summary>
        Rewards = 3
    }
}
