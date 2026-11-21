using System;
using System.Collections.Generic;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Structured result of a completed race outcome evaluation (D-068).
    /// </summary>
    public readonly struct ProgressionOutcome
    {
        public string EventId { get; }
        public int StarsBefore { get; }
        public int StarsEarnedOnRun { get; }
        public int NewStarsEarned { get; }
        public int TotalStarsForEvent { get; }
        public bool IsNewBestTime { get; }
        public double BestTimeSeconds { get; }
        public int BaseCredits { get; }
        public int StarBonusCredits { get; }
        public int FirstPlaceBonusCredits { get; }
        public int TotalCreditsAwarded { get; }
        public IReadOnlyList<string> NewlyUnlockedEventIds { get; }
        public IReadOnlyList<string> NewlyUnlockedChampionshipIds { get; }

        public ProgressionOutcome(
            string eventId,
            int starsBefore,
            int starsEarnedOnRun,
            int newStarsEarned,
            int totalStarsForEvent,
            bool isNewBestTime,
            double bestTimeSeconds,
            int baseCredits,
            int starBonusCredits,
            int firstPlaceBonusCredits,
            int totalCreditsAwarded,
            IReadOnlyList<string> newlyUnlockedEventIds,
            IReadOnlyList<string> newlyUnlockedChampionshipIds)
        {
            EventId = eventId;
            StarsBefore = starsBefore;
            StarsEarnedOnRun = starsEarnedOnRun;
            NewStarsEarned = newStarsEarned;
            TotalStarsForEvent = totalStarsForEvent;
            IsNewBestTime = isNewBestTime;
            BestTimeSeconds = bestTimeSeconds;
            BaseCredits = baseCredits;
            StarBonusCredits = starBonusCredits;
            FirstPlaceBonusCredits = firstPlaceBonusCredits;
            TotalCreditsAwarded = totalCreditsAwarded;
            NewlyUnlockedEventIds = newlyUnlockedEventIds ?? Array.Empty<string>();
            NewlyUnlockedChampionshipIds = newlyUnlockedChampionshipIds ?? Array.Empty<string>();
        }
    }
}
