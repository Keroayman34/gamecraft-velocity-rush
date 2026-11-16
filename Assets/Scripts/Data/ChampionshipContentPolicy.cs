using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Immutable content policy for the approved M8 championship/event matrix
    /// (D-062, D-063, D-064, D-065).
    /// </summary>
    /// <remarks>
    /// <para>
    /// These constants are the authoritative content rules. Every championship, event, and
    /// validation test must satisfy them; changing a value requires a new owner decision.
    /// </para>
    /// <para>
    /// Approved shape: <b>3 championships × 5 events = 15 events</b>, exactly <b>3 events per
    /// environment</b>, all <b>10 configurations</b> (five themes × signature/reverse), exactly
    /// <b>9 Circuit Races</b> and <b>6 Time Trials</b>, and exactly <b>4 AI opponents</b> for
    /// every Circuit Race grid. Circuit Race and Time Trial are the only approved M8 types.
    /// </para>
    /// </remarks>
    public static class ChampionshipContentPolicy
    {
        /// <summary>Exactly three championships for M8 (G1/D-029).</summary>
        public const int RequiredChampionshipCount = 3;

        /// <summary>Exactly five events per championship (G2).</summary>
        public const int EventsPerChampionship = 5;

        /// <summary>Exactly fifteen events in the complete M8 matrix (G2).</summary>
        public const int RequiredTotalEvents = RequiredChampionshipCount * EventsPerChampionship;

        /// <summary>Exactly three events per environment theme (D-063).</summary>
        public const int RequiredEventsPerEnvironment = 3;

        /// <summary>Number of approved environment themes (D-054).</summary>
        public const int RequiredEnvironmentCount = EnvironmentThemeRules.RequiredThemeCount;

        /// <summary>Two configurations per environment: signature and reverse (D-064).</summary>
        public const int RequiredConfigurationsPerEnvironment = 2;

        /// <summary>All ten environment/configuration pairs must be used (D-064).</summary>
        public const int RequiredConfigurationCount =
            RequiredEnvironmentCount * RequiredConfigurationsPerEnvironment;

        /// <summary>Approved Circuit Race count in the M8 matrix (D-062).</summary>
        public const int RequiredCircuitRaceEvents = 9;

        /// <summary>Approved Time Trial count in the M8 matrix (D-062).</summary>
        public const int RequiredTimeTrialEvents = 6;

        /// <summary>Exactly four AI opponents for every Circuit Race event (G5/D-060).</summary>
        public const int CircuitRaceAiCount = 4;

        /// <summary>Approved event types for M8 (G3/D-062): Circuit Race and Time Trial only.</summary>
        public static readonly RaceEventType[] ApprovedEventTypes =
        {
            RaceEventType.CircuitRace,
            RaceEventType.TimeTrial
        };

        /// <summary>Approved opponent modes for M8 (G3/D-062).</summary>
        public static readonly EventOpponentMode[] ApprovedOpponentModes =
        {
            EventOpponentMode.None,
            EventOpponentMode.FixedFourAI
        };

        /// <summary>Approved pace scales per championship order (G5/D-062).</summary>
        public static readonly float[] ApprovedPaceScales = { 0.90f, 0.97f, 1.03f };

        /// <summary>Championship unlock policy per production order (G6/D-062).</summary>
        public static readonly EventUnlockPolicy[] ApprovedChampionshipUnlockPolicies =
        {
            EventUnlockPolicy.AlwaysAvailable,
            EventUnlockPolicy.RequiresPreviousChampionship,
            EventUnlockPolicy.RequiresPreviousChampionship
        };

        /// <summary>Event unlock policy per round (G6/D-062).</summary>
        public static readonly EventUnlockPolicy[] ApprovedEventUnlockPolicies =
        {
            EventUnlockPolicy.AlwaysAvailable,
            EventUnlockPolicy.RequiresPreviousEvent,
            EventUnlockPolicy.RequiresPreviousEvent,
            EventUnlockPolicy.RequiresPreviousEvent,
            EventUnlockPolicy.RequiresPreviousEvent
        };

        /// <summary>The two required layout variants: signature and reverse (D-064).</summary>
        public static readonly EnvironmentLayoutVariant[] RequiredLayoutVariants =
        {
            EnvironmentLayoutVariant.Signature,
            EnvironmentLayoutVariant.Reverse
        };

        /// <summary>True when the event type is approved for M8.</summary>
        public static bool IsApprovedEventType(RaceEventType type)
        {
            for (var i = 0; i < ApprovedEventTypes.Length; i++)
            {
                if (type == ApprovedEventTypes[i])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the opponent mode is approved for M8.</summary>
        public static bool IsApprovedOpponentMode(EventOpponentMode mode)
        {
            for (var i = 0; i < ApprovedOpponentModes.Length; i++)
            {
                if (mode == ApprovedOpponentModes[i])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the pace scale is one of the three approved values.</summary>
        public static bool IsApprovedPaceScale(float paceScale)
        {
            for (var i = 0; i < ApprovedPaceScales.Length; i++)
            {
                if (Mathf.Abs(paceScale - ApprovedPaceScales[i]) < 0.001f)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the layout variant is one of the two required variants.</summary>
        public static bool IsRequiredLayoutVariant(EnvironmentLayoutVariant variant)
        {
            for (var i = 0; i < RequiredLayoutVariants.Length; i++)
            {
                if (variant == RequiredLayoutVariants[i])
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Expected pace scale for a one-based championship production order.</summary>
        public static float GetExpectedPaceScale(int productionOrder)
        {
            var index = productionOrder - 1;
            return index >= 0 && index < ApprovedPaceScales.Length
                ? ApprovedPaceScales[index]
                : float.NaN;
        }

        /// <summary>Expected championship unlock policy for a one-based production order.</summary>
        public static EventUnlockPolicy GetExpectedChampionshipUnlockPolicy(int productionOrder)
        {
            var index = productionOrder - 1;
            return index >= 0 && index < ApprovedChampionshipUnlockPolicies.Length
                ? ApprovedChampionshipUnlockPolicies[index]
                : EventUnlockPolicy.RequiresPreviousChampionship;
        }

        /// <summary>Expected event unlock policy for a one-based round index.</summary>
        public static EventUnlockPolicy GetExpectedEventUnlockPolicy(int roundIndex)
        {
            var index = roundIndex - 1;
            return index >= 0 && index < ApprovedEventUnlockPolicies.Length
                ? ApprovedEventUnlockPolicies[index]
                : EventUnlockPolicy.RequiresPreviousEvent;
        }

        /// <summary>
        /// Expected opponent mode for an event type. The policy is deliberately explicit rather
        /// than inferred from the enum value.
        /// </summary>
        public static EventOpponentMode GetExpectedOpponentMode(RaceEventType eventType)
        {
            return eventType == RaceEventType.CircuitRace
                ? EventOpponentMode.FixedFourAI
                : EventOpponentMode.None;
        }
    }
}
