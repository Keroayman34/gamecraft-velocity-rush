namespace VelocityRush.Core
{
    /// <summary>
    /// Application-level navigation state owned by GameFlow (TDD §30.1/§30.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the shared vocabulary for the flat application FSM defined in TDD §30.2. It is
    /// intentionally limited to states already listed there; no screen or state is invented.
    /// GameFlow is the single owner of transitions and of the transition rule table — this type
    /// carries no transition logic.
    /// </para>
    /// <para>
    /// Consumers (UIS/Navigation, input adapters, audio) are observers only. The input adapter
    /// uses <see cref="IAppStateSource.StateChanged"/> to reset driving input on every transition
    /// (TDD §37.3), which prevents stale held keys from leaking across states.
    /// </para>
    /// </remarks>
    public enum AppState
    {
        /// <summary>Launch/bootstrap; no screen. Presentation may show a splash/loading veil.</summary>
        Boot = 0,

        /// <summary>Main Menu.</summary>
        MainMenu = 1,

        /// <summary>Garage.</summary>
        Garage = 2,

        /// <summary>Upgrades.</summary>
        Upgrades = 3,

        /// <summary>Championship selection.</summary>
        ChampionshipSelection = 4,

        /// <summary>Event selection.</summary>
        EventSelection = 5,

        /// <summary>Pre-event.</summary>
        PreEvent = 6,

        /// <summary>In-race HUD (pause/settings are overlays, not separate states).</summary>
        Race = 7,

        /// <summary>Results.</summary>
        Results = 8
    }
}
