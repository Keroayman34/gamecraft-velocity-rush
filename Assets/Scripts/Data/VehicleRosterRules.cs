using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure roster-level rules for the Commercial V1.0 vehicle set (D-010/D-014).
    /// </summary>
    /// <remarks>
    /// Editor tooling and EditMode tests share these rules; no Unity editor API is referenced,
    /// so the contract remains testable and usable from batch validation. The rules enforce the
    /// <b>provisional planning distribution</b> (2 Starter / 3 Sport / 2 Drift / 3 Supercar)
    /// until the owner freezes OQ-14/OQ-06 and records a decision change.
    /// </remarks>
    public static class VehicleRosterRules
    {
        /// <summary>Total Commercial V1.0 roster size (D-010).</summary>
        public const int RequiredVehicleCount = 10;

        /// <summary>Required Starter count (D-014 planning distribution).</summary>
        public const int RequiredStarterCount = 2;

        /// <summary>Required Sport count (D-014 planning distribution).</summary>
        public const int RequiredSportCount = 3;

        /// <summary>Required Drift count (D-014 planning distribution).</summary>
        public const int RequiredDriftCount = 2;

        /// <summary>Required Supercar count (D-014 planning distribution).</summary>
        public const int RequiredSupercarCount = 3;

        private static readonly VehicleClass[] AllClasses =
        {
            VehicleClass.Starter,
            VehicleClass.Sport,
            VehicleClass.Drift,
            VehicleClass.Supercar
        };

        /// <summary>All defined roster classes in distribution order.</summary>
        public static IReadOnlyList<VehicleClass> Classes => AllClasses;

        /// <summary>Required count for a class, or 0 for an undefined value.</summary>
        public static int GetRequiredCount(VehicleClass vehicleClass)
        {
            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    return RequiredStarterCount;
                case VehicleClass.Sport:
                    return RequiredSportCount;
                case VehicleClass.Drift:
                    return RequiredDriftCount;
                case VehicleClass.Supercar:
                    return RequiredSupercarCount;
                default:
                    return 0;
            }
        }

        /// <summary>Stable asset-folder name for a class ("Starter", "Sport", ...).</summary>
        public static string GetClassFolderName(VehicleClass vehicleClass)
        {
            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    return "Starter";
                case VehicleClass.Sport:
                    return "Sport";
                case VehicleClass.Drift:
                    return "Drift";
                case VehicleClass.Supercar:
                    return "Supercar";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Validates a complete roster: every entry, unique ids, exact total, and exact
        /// per-class distribution. Returns every issue found (cold authoring path only).
        /// </summary>
        public static IReadOnlyList<string> ValidateRoster(IReadOnlyList<VehicleDefinition> roster)
        {
            var issues = new List<string>();
            if (roster == null)
            {
                issues.Add("Roster is null.");
                return issues;
            }

            if (roster.Count != RequiredVehicleCount)
            {
                issues.Add("Roster must contain exactly " + RequiredVehicleCount +
                           " definitions (found " + roster.Count + ").");
            }

            var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var classCounts = new int[AllClasses.Length];

            for (var index = 0; index < roster.Count; index++)
            {
                var definition = roster[index];
                if (definition == null)
                {
                    issues.Add("Roster entry " + index + " is null.");
                    continue;
                }

                if (!definition.TryValidate(out var entryError))
                {
                    issues.Add("Roster entry " + index + " ('" + Describe(definition) + "'): " + entryError);
                }

                var id = definition.VehicleId;
                if (!string.IsNullOrEmpty(id))
                {
                    if (ids.TryGetValue(id, out var firstIndex))
                    {
                        issues.Add("Duplicate VehicleId '" + id + "' at roster entries " +
                                   firstIndex + " and " + index + ".");
                    }
                    else
                    {
                        ids.Add(id, index);
                    }
                }

                var classIndex = (int)definition.Class;
                if (classIndex >= 0 && classIndex < classCounts.Length)
                {
                    classCounts[classIndex]++;
                }
            }

            for (var i = 0; i < AllClasses.Length; i++)
            {
                var vehicleClass = AllClasses[i];
                var expected = GetRequiredCount(vehicleClass);
                var actual = classCounts[i];
                if (actual != expected)
                {
                    issues.Add(GetClassFolderName(vehicleClass) + " count must be " + expected +
                               " (found " + actual + ").");
                }
            }

            return issues;
        }

        /// <summary>
        /// Validates a roster and returns the first issue. Prefer <see cref="ValidateRoster"/>
        /// when authoring so every defect is visible at once.
        /// </summary>
        public static bool TryValidateRoster(IReadOnlyList<VehicleDefinition> roster, out string error)
        {
            var issues = ValidateRoster(roster);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

        private static string Describe(VehicleDefinition definition)
        {
            return string.IsNullOrEmpty(definition.VehicleId) ? definition.name : definition.VehicleId;
        }
    }
}
