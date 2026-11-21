namespace VelocityRush.Data
{
    /// <summary>
    /// Value-type event broadcast when a road coin is collected by the player (Phase 12 Stage 2; TDD §59).
    /// Zero heap allocation on dispatch.
    /// </summary>
    public readonly struct CoinCollectedEvent
    {
        public int CoinValue { get; }
        public int SessionCoinsCollected { get; }
        public int SessionCoinCredits { get; }
        public int LaneIndex { get; }

        public CoinCollectedEvent(int coinValue, int sessionCoinsCollected, int sessionCoinCredits, int laneIndex)
        {
            CoinValue = coinValue;
            SessionCoinsCollected = sessionCoinsCollected;
            SessionCoinCredits = sessionCoinCredits;
            LaneIndex = laneIndex;
        }
    }
}
