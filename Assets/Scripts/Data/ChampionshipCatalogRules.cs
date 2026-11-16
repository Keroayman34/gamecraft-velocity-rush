using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure catalog rules for the three M8 championships (T-10.6.x, G1/G2/G6).
    /// </summary>
    /// <remarks>
    /// The catalog must contain exactly three definitions in production order C1, C2, C3
    /// with approved pace scales 0.90, 0.97, 1.03 and sequential unlock chain.
    /// </remarks>
    public static class ChampionshipCatalogRules
    {
        /// <summary>Required number of championships in M8.</summary>
        public const int RequiredChampionshipCount = 3;

        /// <summary>
        /// Validates a complete catalog: exact count, non-null entries, per-definition validity,
        /// unique ids, exactly one definition per production order, pace scale consistency,
        /// and approved sequential unlock chain.
        /// </summary>
        public static IReadOnlyList<string> ValidateCatalog(
            IReadOnlyList<ChampionshipDefinition> definitions)
        {
            var issues = new List<string>();
            if (definitions == null)
            {
                issues.Add("Championship catalog is null.");
                return issues;
            }

            if (definitions.Count != RequiredChampionshipCount)
            {
                issues.Add($"Championship catalog must contain exactly {RequiredChampionshipCount} definitions (found {definitions.Count}).");
                // Continue validation on what we have
            }

            var ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var orderCounts = new int[RequiredChampionshipCount];

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null)
                {
                    issues.Add($"Championship catalog entry {index} is null.");
                    continue;
                }

                if (!definition.TryValidate(out var entryError))
                {
                    issues.Add($"Championship catalog entry {index} ('{Describe(definition)}'): {entryError}");
                }

                var id = definition.ChampionshipId;
                if (!string.IsNullOrEmpty(id))
                {
                    if (ids.TryGetValue(id, out var firstIndex))
                    {
                        issues.Add($"Duplicate ChampionshipId '{id}' at catalog entries {firstIndex} and {index}.");
                    }
                    else
                    {
                        ids.Add(id, index);
                    }
                }

                var order = definition.ProductionOrder;
                if (order >= 1 && order <= RequiredChampionshipCount)
                {
                    orderCounts[order - 1]++;
                }
            }

            // Exactly one per production order
            for (var order = 1; order <= RequiredChampionshipCount; order++)
            {
                if (orderCounts[order - 1] != 1)
                {
                    issues.Add($"ProductionOrder {order} must appear exactly once (found {orderCounts[order - 1]}).");
                }
            }

            // Validate pace scale sequence matches production order
            var paceScaleByOrder = new float[RequiredChampionshipCount];
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition != null)
                {
                    var order = definition.ProductionOrder;
                    if (order >= 1 && order <= RequiredChampionshipCount)
                    {
                        paceScaleByOrder[order - 1] = definition.PaceScale;
                    }
                }
            }

            var expectedScales = new[] { 0.90f, 0.97f, 1.03f };
            for (var order = 1; order <= RequiredChampionshipCount; order++)
            {
                var actualScale = paceScaleByOrder[order - 1];
                var expectedScale = expectedScales[order - 1];
                if (actualScale > 0f && Mathf.Abs(actualScale - expectedScale) > 0.001f)
                {
                    issues.Add($"ProductionOrder {order} pace scale must be {expectedScale} (found {actualScale}).");
                }
            }

            // Validate unlock chain: C1=AlwaysAvailable, C2=RequiresPreviousChampionship, C3=RequiresPreviousChampionship
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition != null)
                {
                    var order = definition.ProductionOrder;
                    var expectedPolicy = order switch
                    {
                        1 => EventUnlockPolicy.AlwaysAvailable,
                        2 => EventUnlockPolicy.RequiresPreviousChampionship,
                        3 => EventUnlockPolicy.RequiresPreviousChampionship,
                        _ => EventUnlockPolicy.AlwaysAvailable
                    };

                    if (definition.UnlockPolicy != expectedPolicy)
                    {
                        issues.Add($"Championship {definition.ChampionshipId} (ProductionOrder {order}) must have UnlockPolicy {expectedPolicy} (found {definition.UnlockPolicy}).");
                    }
                }
            }

            return issues;
        }

        /// <summary>Validates a catalog and returns the first issue.</summary>
        public static bool TryValidateCatalog(
            IReadOnlyList<ChampionshipDefinition> definitions,
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

        private static string Describe(ChampionshipDefinition definition)
        {
            return string.IsNullOrEmpty(definition.ChampionshipId)
                ? definition.name
                : definition.ChampionshipId;
        }
    }
}