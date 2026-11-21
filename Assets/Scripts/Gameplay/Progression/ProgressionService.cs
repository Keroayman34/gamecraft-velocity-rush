using System;
using System.Collections.Generic;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure domain service executing race outcome evaluation, star scoring, rewards, and unlock rules (D-065, D-068).
    /// </summary>
    public sealed class ProgressionService
    {
        public const int BaseCompletionReward = 100;
        public const int RewardPerNewStar = 150;
        public const int FirstPlaceGoldBonus = 200;

        private readonly PlayerProgress _progress;

        public PlayerProgress Progress => _progress;

        public ProgressionService(PlayerProgress progress)
        {
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        #region Race Outcome & Stars

        /// <summary>
        /// Evaluates a completed race result, applies credits, updates stars, advances unlock chains,
        /// and returns an immutable <see cref="ProgressionOutcome"/> report.
        /// </summary>
        /// <param name="result">The race result produced by RaceRules.</param>
        /// <param name="eventDef">The event definition raced.</param>
        /// <param name="playerFinishPosition">1-based finish position (relevant for CircuitRace with AI).</param>
        public ProgressionOutcome ProcessRaceOutcome(
            RaceResult result,
            RaceEventDefinition eventDef,
            int playerFinishPosition = 0)
        {
            if (eventDef == null)
            {
                throw new ArgumentNullException(nameof(eventDef));
            }

            string eventId = eventDef.EventId;
            int starsBefore = _progress.GetEventStars(eventId);

            if (!result.Completed)
            {
                // DNF / Incomplete run grants 0 stars and 0 rewards
                return new ProgressionOutcome(
                    eventId,
                    starsBefore,
                    starsEarnedOnRun: 0,
                    newStarsEarned: 0,
                    totalStarsForEvent: starsBefore,
                    isNewBestTime: false,
                    bestTimeSeconds: _progress.GetEventBestTime(eventId),
                    baseCredits: 0,
                    starBonusCredits: 0,
                    firstPlaceBonusCredits: 0,
                    totalCreditsAwarded: 0,
                    newlyUnlockedEventIds: Array.Empty<string>(),
                    newlyUnlockedChampionshipIds: Array.Empty<string>());
            }

            // Calculate stars earned on this specific run
            int runStars = 0;
            bool isFirstPlaceOrGold = false;

            if (eventDef.EventType == RaceEventType.CircuitRace)
            {
                // Circuit Race with 4 AI (5 participants total):
                // 1st place -> 3 stars; Podium (top 3) -> 2 stars; Finished -> 1 star
                if (playerFinishPosition == 1)
                {
                    runStars = 3;
                    isFirstPlaceOrGold = true;
                }
                else if (playerFinishPosition >= 2 && playerFinishPosition <= 3)
                {
                    runStars = 2;
                }
                else
                {
                    runStars = 1;
                }
            }
            else
            {
                // Time Trial (Solo):
                runStars = TimeTrialTargetTable.EvaluateStars(eventId, result.ElapsedSeconds, true);
                if (runStars == 3)
                {
                    isFirstPlaceOrGold = true;
                }
            }

            // Mutate PlayerProgress record
            _progress.RecordEventResult(
                eventId,
                runStars,
                result.ElapsedSeconds,
                completed: true,
                out bool isNewBestTime,
                out int newStarsEarned);

            int totalStars = _progress.GetEventStars(eventId);

            // Compute reward credits
            int baseCredits = BaseCompletionReward;
            int starBonus = newStarsEarned * RewardPerNewStar;
            int firstBonus = isFirstPlaceOrGold ? FirstPlaceGoldBonus : 0;
            int totalCredits = baseCredits + starBonus + firstBonus;

            _progress.AddCredits(totalCredits);

            // Check unlock chains
            var newlyUnlockedEvents = new List<string>();
            var newlyUnlockedChampionships = new List<string>();

            EvaluateChainUnlocks(eventDef, newlyUnlockedEvents, newlyUnlockedChampionships);

            return new ProgressionOutcome(
                eventId,
                starsBefore,
                runStars,
                newStarsEarned,
                totalStars,
                isNewBestTime,
                _progress.GetEventBestTime(eventId),
                baseCredits,
                starBonus,
                firstBonus,
                totalCredits,
                newlyUnlockedEvents,
                newlyUnlockedChampionships);
        }

        private void EvaluateChainUnlocks(
            RaceEventDefinition completedEvent,
            List<string> newlyUnlockedEvents,
            List<string> newlyUnlockedChampionships)
        {
            // Linear event round chain (D-065):
            // Rounds 1..5 in each championship
            string champId = completedEvent.ChampionshipId;
            int round = completedEvent.RoundIndex;

            if (round >= 1 && round < 5)
            {
                // Round N unlocks Round N+1 in the same championship
                string nextEventId = $"{champId}-round-{round + 1}";
                newlyUnlockedEvents.Add(nextEventId);
            }
            else if (round == 5)
            {
                // Round 5 completes championship -> unlocks next championship
                if (string.Equals(champId, "c1-ignition-series", StringComparison.OrdinalIgnoreCase))
                {
                    if (_progress.UnlockChampionship("c2-apex-challenger"))
                    {
                        newlyUnlockedChampionships.Add("c2-apex-challenger");
                        newlyUnlockedEvents.Add("c2-apex-challenger-round-1");
                    }
                }
                else if (string.Equals(champId, "c2-apex-challenger", StringComparison.OrdinalIgnoreCase))
                {
                    if (_progress.UnlockChampionship("c3-velocity-masters"))
                    {
                        newlyUnlockedChampionships.Add("c3-velocity-masters");
                        newlyUnlockedEvents.Add("c3-velocity-masters-round-1");
                    }
                }
            }
        }

        /// <summary>
        /// Checks whether an event is unlocked according to D-065 linear progression rules.
        /// </summary>
        public bool IsEventUnlocked(RaceEventDefinition eventDef)
        {
            if (eventDef == null) return false;

            // Must have its championship unlocked
            if (!_progress.IsChampionshipUnlocked(eventDef.ChampionshipId))
            {
                return false;
            }

            // Round 1 is available if championship is unlocked
            if (eventDef.RoundIndex == 1)
            {
                return true;
            }

            // Rounds 2..5 require prior round completed
            string priorEventId = $"{eventDef.ChampionshipId}-round-{eventDef.RoundIndex - 1}";
            return _progress.IsEventCompleted(priorEventId);
        }

        #endregion

        #region Vehicle Unlocks & Purchasing

        /// <summary>
        /// Returns whether a vehicle can be purchased, and if not, the failure reason.
        /// </summary>
        public bool CanUnlockVehicle(string vehicleId, out int cost, out string failureReason)
        {
            cost = GetVehicleUnlockCost(vehicleId);

            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                failureReason = "Invalid vehicle id.";
                return false;
            }

            if (_progress.IsVehicleUnlocked(vehicleId))
            {
                failureReason = "Vehicle is already unlocked.";
                return false;
            }

            // Class prerequisites per D-068
            var vehicleClass = GetVehicleClassFromId(vehicleId);
            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    // Nova Hatch is purchasable immediately
                    break;
                case VehicleClass.Sport:
                    // Requires Championship 1 completed (c1-ignition-series-round-5)
                    if (!_progress.IsEventCompleted("c1-ignition-series-round-5"))
                    {
                        failureReason = "Sport class requires completing Championship 1 (Ignition Series).";
                        return false;
                    }
                    break;
                case VehicleClass.Drift:
                    // Requires Championship 2 completed (c2-apex-challenger-round-5)
                    if (!_progress.IsEventCompleted("c2-apex-challenger-round-5"))
                    {
                        failureReason = "Drift class requires completing Championship 2 (Apex Challenger).";
                        return false;
                    }
                    break;
                case VehicleClass.Supercar:
                    // Requires Championship 3 unlocked
                    if (!_progress.IsChampionshipUnlocked("c3-velocity-masters"))
                    {
                        failureReason = "Supercar class requires unlocking Championship 3 (Velocity Masters).";
                        return false;
                    }
                    break;
            }

            if (_progress.Credits < cost)
            {
                failureReason = $"Insufficient credits ({_progress.Credits} CR available; {cost} CR required).";
                return false;
            }

            failureReason = null;
            return true;
        }

        /// <summary>
        /// Attempts to purchase and unlock a vehicle atomically (fail-closed).
        /// </summary>
        public bool TryPurchaseVehicle(string vehicleId, out string failureReason)
        {
            if (!CanUnlockVehicle(vehicleId, out int cost, out failureReason))
            {
                return false;
            }

            if (!_progress.TryDeductCredits(cost))
            {
                failureReason = "Failed to deduct credits.";
                return false;
            }

            _progress.UnlockVehicle(vehicleId);
            failureReason = null;
            return true;
        }

        public static int GetVehicleUnlockCost(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId)) return 0;

            switch (vehicleId.ToLowerInvariant())
            {
                // Starter
                case "starter-apex-v": return 0; // Default
                case "starter-nova-hatch": return 1000;

                // Sport
                case "sport-velocity-gt": return 2500;
                case "sport-aero-coupe": return 3000;
                case "sport-pulse-r": return 3500;

                // Drift
                case "drift-sidewinder": return 4000;
                case "drift-kuma-dx": return 5000;

                // Supercar
                case "supercar-vortex-v10": return 7500;
                case "supercar-eclipse": return 8500;
                case "supercar-nebula-x": return 10000;

                default: return 2000;
            }
        }

        public static VehicleClass GetVehicleClassFromId(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId)) return VehicleClass.Starter;
            if (vehicleId.StartsWith("starter-", StringComparison.OrdinalIgnoreCase)) return VehicleClass.Starter;
            if (vehicleId.StartsWith("sport-", StringComparison.OrdinalIgnoreCase)) return VehicleClass.Sport;
            if (vehicleId.StartsWith("drift-", StringComparison.OrdinalIgnoreCase)) return VehicleClass.Drift;
            if (vehicleId.StartsWith("supercar-", StringComparison.OrdinalIgnoreCase)) return VehicleClass.Supercar;
            return VehicleClass.Starter;
        }

        #endregion

        #region Vehicle Upgrades

        /// <summary>
        /// Checks whether an upgrade tier can be purchased for the given vehicle.
        /// </summary>
        public bool CanPurchaseUpgrade(
            string vehicleId,
            UpgradeCategory category,
            VehicleClass vehicleClass,
            out int cost,
            out string failureReason)
        {
            cost = 0;
            if (!_progress.IsVehicleUnlocked(vehicleId))
            {
                failureReason = "Vehicle is not unlocked.";
                return false;
            }

            int currentTier = _progress.GetUpgradeLevel(vehicleId, category);
            if (currentTier >= VehicleUpgradeState.MaxTier)
            {
                failureReason = $"Category {category} is already at maximum tier ({VehicleUpgradeState.MaxTier}).";
                return false;
            }

            int targetTier = currentTier + 1;
            cost = UpgradeCalculator.GetUpgradeCost(vehicleClass, targetTier);

            if (_progress.Credits < cost)
            {
                failureReason = $"Insufficient credits ({_progress.Credits} CR available; {cost} CR required).";
                return false;
            }

            failureReason = null;
            return true;
        }

        /// <summary>
        /// Atomically purchases and applies an upgrade tier (fail-closed).
        /// </summary>
        public bool TryPurchaseUpgrade(
            string vehicleId,
            UpgradeCategory category,
            VehicleClass vehicleClass,
            out string failureReason)
        {
            if (!CanPurchaseUpgrade(vehicleId, category, vehicleClass, out int cost, out failureReason))
            {
                return false;
            }

            int currentTier = _progress.GetUpgradeLevel(vehicleId, category);
            int targetTier = currentTier + 1;

            if (!_progress.TryDeductCredits(cost))
            {
                failureReason = "Failed to deduct credits.";
                return false;
            }

            if (!_progress.TryApplyUpgrade(vehicleId, category, targetTier))
            {
                // Refund on failure
                _progress.AddCredits(cost);
                failureReason = "Failed to apply upgrade tier.";
                return false;
            }

            failureReason = null;
            return true;
        }

        #endregion
    }
}
