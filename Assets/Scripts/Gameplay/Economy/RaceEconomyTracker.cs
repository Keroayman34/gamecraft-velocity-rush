using System;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Gameplay.Economy
{
    /// <summary>
    /// Pure C# live race economy ledger tracking collected coins and near-miss bonus credits (Phase 12 Stage 2; TDD §59).
    /// Ensures atomic, idempotent commitment to <see cref="PlayerProgress"/> upon race finish,
    /// protecting against early-exit or retry farming exploits.
    /// </summary>
    public sealed class RaceEconomyTracker
    {
        private int _coinsCollected;
        private int _coinCredits;
        private int _nearMissCount;
        private int _nearMissCredits;
        private bool _isCommitted;

        public int CoinsCollected => _coinsCollected;
        public int CoinCredits => _coinCredits;
        public int NearMissCount => _nearMissCount;
        public int NearMissCredits => _nearMissCredits;
        public int TotalBonusCredits => _coinCredits + _nearMissCredits;
        public bool IsCommitted => _isCommitted;

        public void RecordCoinCollected(in CoinCollectedEvent evt)
        {
            _coinsCollected++;
            _coinCredits += evt.CoinValue;
        }

        public void RecordNearMiss(in NearMissEvent evt, int creditsPerNearMiss)
        {
            _nearMissCount++;
            int bonus = creditsPerNearMiss * Math.Max(1, evt.ComboCount);
            _nearMissCredits += bonus;
        }

        /// <summary>
        /// Clears all session counters and uncommits the ledger.
        /// Must be called on Fast Retry or Quit to Menu to eliminate farming exploits.
        /// </summary>
        public void Reset()
        {
            _coinsCollected = 0;
            _coinCredits = 0;
            _nearMissCount = 0;
            _nearMissCredits = 0;
            _isCommitted = false;
        }

        /// <summary>
        /// Atomically and idempotently commits accumulated bonus credits to player progress.
        /// Returns the number of credits awarded. Returns 0 if already committed, if progress is null,
        /// or if total bonus credits are non-positive.
        /// </summary>
        public int CommitToProgress(PlayerProgress progress)
        {
            if (_isCommitted || progress == null || TotalBonusCredits <= 0)
            {
                return 0;
            }

            int creditsToCommit = TotalBonusCredits;
            progress.AddCredits(creditsToCommit);
            _isCommitted = true;
            return creditsToCommit;
        }
    }
}
