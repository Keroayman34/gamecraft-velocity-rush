namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Prototype race lifecycle states (TDD §29 S5 / §35.5).
    /// </summary>
    /// <remarks>
    /// Temporary development-prototype vocabulary only; the commercial race sub-state model may
    /// grow additional states later. The minimal loop is Countdown → Running → Finished.
    /// </remarks>
    public enum RaceState
    {
        /// <summary>Race is staged; the elapsed timer has not started.</summary>
        Countdown = 0,

        /// <summary>Race is live; checkpoints and finish are accepted and the timer runs.</summary>
        Running = 1,

        /// <summary>Race is over; the timer is frozen and no further progress is accepted.</summary>
        Finished = 2
    }
}
