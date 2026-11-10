namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Immutable per-refresh HUD values for the vertical-slice time trial.
    /// </summary>
    /// <remarks>
    /// A value type so the presenter can build one per refresh without allocating. All numeric
    /// values are already clamped and split for direct digit rendering.
    /// </remarks>
    public readonly struct SliceHudSnapshot
    {
        /// <summary>Display speed in km/h, 0–999.</summary>
        public int SpeedKph { get; }

        /// <summary>Whole elapsed seconds, 0–999.</summary>
        public int TimeWholeSeconds { get; }

        /// <summary>Hundredths of a second, 0–99.</summary>
        public int TimeHundredths { get; }

        /// <summary>Accepted checkpoints.</summary>
        public int AcceptedCheckpoints { get; }

        /// <summary>Required checkpoints.</summary>
        public int RequiredCheckpoints { get; }

        /// <summary>Current race lifecycle state.</summary>
        public RaceState State { get; }

        public SliceHudSnapshot(
            int speedKph,
            int timeWholeSeconds,
            int timeHundredths,
            int acceptedCheckpoints,
            int requiredCheckpoints,
            RaceState state)
        {
            SpeedKph = speedKph;
            TimeWholeSeconds = timeWholeSeconds;
            TimeHundredths = timeHundredths;
            AcceptedCheckpoints = acceptedCheckpoints;
            RequiredCheckpoints = requiredCheckpoints;
            State = state;
        }
    }
}
