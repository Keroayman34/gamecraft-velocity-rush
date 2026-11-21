namespace VelocityRush.Core
{
    /// <summary>
    /// Pure, deterministic transition authority for the flat 9-state <see cref="AppState"/> FSM (TDD §30.2; D-070).
    /// Enforces fail-closed validation with zero allocations.
    /// </summary>
    public static class AppStateTransitionMatrix
    {
        /// <summary>
        /// Evaluates whether a transition from <paramref name="from"/> to <paramref name="to"/>
        /// is explicitly permitted by the TDD §30.2 state map.
        /// </summary>
        /// <param name="from">Current application state.</param>
        /// <param name="to">Requested target application state.</param>
        /// <returns>True if the transition is legal; false if rejected.</returns>
        public static bool IsTransitionAllowed(AppState from, AppState to)
        {
            switch (from)
            {
                case AppState.Boot:
                    return to == AppState.MainMenu;

                case AppState.MainMenu:
                    return to == AppState.Garage ||
                           to == AppState.ChampionshipSelection;

                case AppState.Garage:
                    return to == AppState.MainMenu ||
                           to == AppState.Upgrades ||
                           to == AppState.ChampionshipSelection ||
                           to == AppState.EventSelection ||
                           to == AppState.PreEvent;

                case AppState.Upgrades:
                    return to == AppState.Garage;

                case AppState.ChampionshipSelection:
                    return to == AppState.EventSelection ||
                           to == AppState.MainMenu ||
                           to == AppState.Garage;

                case AppState.EventSelection:
                    return to == AppState.PreEvent ||
                           to == AppState.ChampionshipSelection ||
                           to == AppState.Garage;

                case AppState.PreEvent:
                    return to == AppState.Race ||
                           to == AppState.EventSelection ||
                           to == AppState.Garage;

                case AppState.Race:
                    return to == AppState.Results;

                case AppState.Results:
                    return to == AppState.EventSelection ||
                           to == AppState.Race ||
                           to == AppState.MainMenu;

                default:
                    return false;
            }
        }
    }
}
