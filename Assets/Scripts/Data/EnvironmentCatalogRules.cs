using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure catalog rules for the five Commercial V1.0 environment packages (T-9.1, D-054).
    /// </summary>
    /// <remarks>
    /// The catalog must contain exactly one definition per approved theme, in stable ids. The
    /// signature and reverse configurations share the definition and layout; they do not add
    /// catalog entries or scenes (D-057).
    /// </remarks>
    public static class EnvironmentCatalogRules
    {
        /// <summary>Required number of environment packages (one per theme).</summary>
        public const int RequiredEnvironmentCount = EnvironmentThemeRules.RequiredThemeCount;

        /// <summary>
        /// Validates a complete catalog: exact count, non-null entries, per-definition validity,
        /// unique ids, exactly one definition per theme, and a complete production order.
        /// </summary>
        public static IReadOnlyList<string> ValidateCatalog(
            IReadOnlyList<EnvironmentDefinition> definitions)
        {
            var issues = new List<string>();
            if (definitions == null)
            {
                issues.Add("Environment catalog is null.");
                return issues;
            }

            if (definitions.Count != RequiredEnvironmentCount)
            {
                issues.Add("Environment catalog must contain exactly " + RequiredEnvironmentCount +
                           " definitions (found " + definitions.Count + ").");
            }

            var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var themeCounts = new int[EnvironmentThemeRules.RequiredThemeCount];
            var orders = new Dictionary<int, int>();

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null)
                {
                    issues.Add("Environment catalog entry " + index + " is null.");
                    continue;
                }

                if (!definition.TryValidate(out var entryError))
                {
                    issues.Add("Environment catalog entry " + index + " ('" +
                               Describe(definition) + "'): " + entryError);
                }

                var id = definition.EnvironmentId;
                if (!string.IsNullOrEmpty(id))
                {
                    if (ids.TryGetValue(id, out var firstIndex))
                    {
                        issues.Add("Duplicate EnvironmentId '" + id + "' at catalog entries " +
                                   firstIndex + " and " + index + ".");
                    }
                    else
                    {
                        ids.Add(id, index);
                    }
                }

                if (EnvironmentThemeRules.IsDefined(definition.Theme))
                {
                    themeCounts[(int)definition.Theme]++;
                }

                if (orders.TryGetValue(definition.ProductionOrder, out var firstOrderIndex))
                {
                    issues.Add("Duplicate ProductionOrder " + definition.ProductionOrder +
                               " at catalog entries " + firstOrderIndex + " and " + index + ".");
                }
                else
                {
                    orders.Add(definition.ProductionOrder, index);
                }
            }

            var themes = EnvironmentThemeRules.Themes;
            for (var i = 0; i < themes.Count; i++)
            {
                if (themeCounts[i] != 1)
                {
                    issues.Add("Theme '" + EnvironmentThemeRules.GetDisplayName(themes[i]) +
                               "' must appear exactly once (found " + themeCounts[i] + ").");
                }
            }

            for (var order = 1; order <= RequiredEnvironmentCount; order++)
            {
                if (!orders.ContainsKey(order))
                {
                    issues.Add("Missing ProductionOrder " + order + ".");
                }
            }

            return issues;
        }

        /// <summary>Validates a catalog and returns the first issue.</summary>
        public static bool TryValidateCatalog(
            IReadOnlyList<EnvironmentDefinition> definitions,
            out string error)
        {
            var issues = ValidateCatalog(definitions);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

        private static string Describe(EnvironmentDefinition definition)
        {
            return string.IsNullOrEmpty(definition.EnvironmentId)
                ? definition.name
                : definition.EnvironmentId;
        }
    }
}
