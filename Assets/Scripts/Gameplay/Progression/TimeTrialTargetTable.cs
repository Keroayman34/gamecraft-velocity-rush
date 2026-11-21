using System;
using System.Collections.Generic;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Deterministic registry of target times (Gold, Silver, Bronze) for M8 Time Trial events (D-068).
    /// </summary>
    public static class TimeTrialTargetTable
    {
        private readonly struct Targets
        {
            public readonly double Gold;
            public readonly double Silver;
            public readonly double Bronze;

            public Targets(double gold, double silver, double bronze)
            {
                Gold = gold;
                Silver = silver;
                Bronze = bronze;
            }
        }

        private static readonly Dictionary<string, Targets> EventTargets =
            new Dictionary<string, Targets>(StringComparer.OrdinalIgnoreCase)
            {
                // C1 - Ignition Series (pace 0.90)
                { "c1-ignition-series-round-1", new Targets(50.0, 58.0, 70.0) },
                { "c1-ignition-series-round-3", new Targets(42.0, 49.0, 60.0) },

                // C2 - Apex Challenger (pace 0.97)
                { "c2-apex-challenger-round-2", new Targets(48.0, 56.0, 68.0) },
                { "c2-apex-challenger-round-4", new Targets(38.0, 44.0, 55.0) },

                // C3 - Velocity Masters (pace 1.03)
                { "c3-velocity-masters-round-3", new Targets(38.0, 44.0, 54.0) },
                { "c3-velocity-masters-round-5", new Targets(40.0, 46.0, 58.0) },
            };

        /// <summary>
        /// Retrieves Gold, Silver, and Bronze target times in seconds for the given event id.
        /// Falls back to conservative duration estimates if not pre-registered.
        /// </summary>
        public static void GetTargets(string eventId, out double goldSeconds, out double silverSeconds, out double bronzeSeconds)
        {
            if (EventTargets.TryGetValue(eventId, out var targets))
            {
                goldSeconds = targets.Gold;
                silverSeconds = targets.Silver;
                bronzeSeconds = targets.Bronze;
            }
            else
            {
                // Fallback: 45s gold, 52s silver, 65s bronze
                goldSeconds = 45.0;
                silverSeconds = 52.0;
                bronzeSeconds = 65.0;
            }
        }

        /// <summary>
        /// Evaluates elapsed seconds against event thresholds and returns stars earned [0..3].
        /// </summary>
        public static int EvaluateStars(string eventId, double elapsedSeconds, bool completed)
        {
            if (!completed || double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0d)
            {
                return 0;
            }

            GetTargets(eventId, out double gold, out double silver, out double bronze);

            if (elapsedSeconds <= gold) return 3;
            if (elapsedSeconds <= silver) return 2;
            if (elapsedSeconds <= bronze) return 1;

            return 1; // Completed run always receives at least 1 star per D-068
        }
    }
}
