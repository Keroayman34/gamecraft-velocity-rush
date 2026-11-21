using System;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure, deterministic next-event resolution authority across championship rounds
    /// and championship tier boundaries (D-065, D-068).
    /// </summary>
    public static class NextEventResolver
    {
        public const string Championship1Id = "c1-ignition-series";
        public const string Championship2Id = "c2-apex-challenger";
        public const string Championship3Id = "c3-velocity-masters";

        public const int RoundsPerChampionship = 5;

        /// <summary>
        /// Resolves the next event following <paramref name="currentEventId"/>.
        /// Returns true if a next event exists, or false if the game is complete or the id is invalid.
        /// </summary>
        public static bool TryResolveNextEvent(
            string currentEventId,
            out string nextChampionshipId,
            out string nextEventId)
        {
            nextChampionshipId = null;
            nextEventId = null;

            if (string.IsNullOrWhiteSpace(currentEventId))
            {
                return false;
            }

            var trimmed = currentEventId.Trim().ToLowerInvariant();

            // Extract championship ID and round number
            string champId;
            int round;

            if (trimmed.StartsWith(Championship1Id, StringComparison.Ordinal))
            {
                champId = Championship1Id;
            }
            else if (trimmed.StartsWith(Championship2Id, StringComparison.Ordinal))
            {
                champId = Championship2Id;
            }
            else if (trimmed.StartsWith(Championship3Id, StringComparison.Ordinal))
            {
                champId = Championship3Id;
            }
            else
            {
                return false;
            }

            // Find round number at the end
            int lastDash = trimmed.LastIndexOf('-');
            if (lastDash < 0 || lastDash >= trimmed.Length - 1)
            {
                return false;
            }

            if (!int.TryParse(trimmed.Substring(lastDash + 1), out round))
            {
                return false;
            }

            if (round < 1 || round > RoundsPerChampionship)
            {
                return false;
            }

            if (round < RoundsPerChampionship)
            {
                // Next round in same championship
                nextChampionshipId = champId;
                nextEventId = $"{champId}-round-{round + 1}";
                return true;
            }

            // Boundary: end of championship (Round 5)
            if (string.Equals(champId, Championship1Id, StringComparison.Ordinal))
            {
                nextChampionshipId = Championship2Id;
                nextEventId = $"{Championship2Id}-round-1";
                return true;
            }

            if (string.Equals(champId, Championship2Id, StringComparison.Ordinal))
            {
                nextChampionshipId = Championship3Id;
                nextEventId = $"{Championship3Id}-round-1";
                return true;
            }

            // End of C3: Game completed!
            return false;
        }

        /// <summary>
        /// Computes the environment scene name from theme and variant.
        /// Matches the naming convention: "desert-signature", "coastal-reverse", etc.
        /// </summary>
        public static string GetEnvironmentSceneName(EnvironmentTheme theme, EnvironmentLayoutVariant variant)
        {
            var fragment = EnvironmentThemeRules.GetStableFragment(theme);
            var varStr = variant == EnvironmentLayoutVariant.Reverse ? "reverse" : "signature";
            return $"{fragment}-{varStr}";
        }

        /// <summary>
        /// Returns the scene name for the given event definition.
        /// </summary>
        public static string GetSceneNameForEvent(RaceEventDefinition eventDef)
        {
            if (eventDef == null)
            {
                return null;
            }

            return GetEnvironmentSceneName(eventDef.EnvironmentTheme, eventDef.LayoutVariant);
        }
    }
}
