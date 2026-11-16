using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Approved event types for M8 championship production (G3/D-061).
    /// </summary>
    /// <remarks>
    /// Only Circuit Race and Time Trial are approved for M8. Drift, Precision, Jump,
    /// Brake, Obstacle, and Mixed challenges remain in the long-term planning list
    /// (D-031) and are not part of the M8 scope.
    /// </remarks>
    public enum RaceEventType
    {
        /// <summary>Multi-car circuit race with AI opponents and position-based results.</summary>
        CircuitRace = 0,

        /// <summary>Solo time trial against the clock; no opponents.</summary>
        TimeTrial = 1
    }

    /// <summary>
    /// Pure metadata and validation rules for <see cref="RaceEventType"/>.
    /// </summary>
    public static class RaceEventTypeRules
    {
        private static readonly RaceEventType[] ApprovedTypes =
        {
            RaceEventType.CircuitRace,
            RaceEventType.TimeTrial
        };

        /// <summary>All approved event types for M8.</summary>
        public static IReadOnlyList<RaceEventType> Approved => ApprovedTypes;

        /// <summary>True when the value is an approved M8 event type.</summary>
        public static bool IsApproved(RaceEventType type)
        {
            return type == RaceEventType.CircuitRace || type == RaceEventType.TimeTrial;
        }

        /// <summary>English display name for an approved type; "Unknown" otherwise.</summary>
        public static string GetDisplayName(RaceEventType type)
        {
            switch (type)
            {
                case RaceEventType.CircuitRace:
                    return "Circuit Race";
                case RaceEventType.TimeTrial:
                    return "Time Trial";
                default:
                    return "Unknown";
            }
        }

        /// <summary>Arabic display name for an approved type; "غير معروف" otherwise.</summary>
        public static string GetDisplayNameAr(RaceEventType type)
        {
            switch (type)
            {
                case RaceEventType.CircuitRace:
                    return "سباق الحلبة";
                case RaceEventType.TimeTrial:
                    return "التجربة الزمنية";
                default:
                    return "غير معروف";
            }
        }

        /// <summary>Validates a set of event types for M8 compliance.</summary>
        public static IReadOnlyList<string> ValidateTypeSet(IReadOnlyList<RaceEventType> types)
        {
            var issues = new List<string>();
            if (types == null)
            {
                issues.Add("Event type set is null.");
                return issues;
            }

            for (var i = 0; i < types.Count; i++)
            {
                if (!IsApproved(types[i]))
                {
                    issues.Add($"Event type entry {i} has unapproved value {(int)types[i]}; M8 permits only CircuitRace and TimeTrial.");
                }
            }

            return issues;
        }
    }
}