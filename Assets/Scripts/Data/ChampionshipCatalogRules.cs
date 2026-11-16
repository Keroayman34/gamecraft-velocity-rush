using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure catalog rules for the three M8 championships (T-10.6.x, G1/G2/G6).
    /// </summary>
    /// <remarks>
    /// The catalog must contain exactly three definitions in production order C1, C2, C3
    /// with approved pace scales 0.90, 0.97, 1.03, exactly one root, an acyclic prior-championship
    /// chain, and every championship reachable from that root.
    /// </remarks>
    public static class ChampionshipCatalogRules
    {
        /// <summary>Required number of championships in M8.</summary>
        public const int RequiredChampionshipCount = ChampionshipContentPolicy.RequiredChampionshipCount;

        /// <summary>
        /// Validates a complete catalog: exact count, non-null entries, per-definition validity,
        /// unique ids, one definition per production order, pace scale consistency, approved
        /// unlock policy, exactly one root, acyclic chain, and full reachability.
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
            }

            var ids = new Dictionary<string, int>(StringComparer.Ordinal);
            var byId = new Dictionary<string, ChampionshipDefinition>(StringComparer.Ordinal);
            var orderCounts = new int[RequiredChampionshipCount];

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null)
                {
                    issues.Add($"Championship catalog entry {index} is null.");
                    continue;
                }

                var entryIssues = definition.Validate();
                for (var issueIndex = 0; issueIndex < entryIssues.Count; issueIndex++)
                {
                    issues.Add($"Championship catalog entry {index} ('{Describe(definition)}'): {entryIssues[issueIndex]}");
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
                        byId.Add(id, definition);
                    }
                }

                var order = definition.ProductionOrder;
                if (order >= 1 && order <= RequiredChampionshipCount)
                {
                    orderCounts[order - 1]++;
                }
            }

            for (var order = 1; order <= RequiredChampionshipCount; order++)
            {
                if (orderCounts[order - 1] != 1)
                {
                    issues.Add($"ProductionOrder {order} must appear exactly once (found {orderCounts[order - 1]}).");
                }
            }

            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null)
                {
                    continue;
                }

                var order = definition.ProductionOrder;
                if (order < 1 || order > RequiredChampionshipCount)
                {
                    continue;
                }

                var expectedScale = ChampionshipContentPolicy.GetExpectedPaceScale(order);
                if (Mathf.Abs(definition.PaceScale - expectedScale) > 0.001f)
                {
                    issues.Add($"ProductionOrder {order} pace scale must be {expectedScale} (found {definition.PaceScale}).");
                }

                var expectedPolicy = ChampionshipContentPolicy.GetExpectedChampionshipUnlockPolicy(order);
                if (definition.UnlockPolicy != expectedPolicy)
                {
                    issues.Add($"Championship {definition.ChampionshipId} (ProductionOrder {order}) must have UnlockPolicy {expectedPolicy} (found {definition.UnlockPolicy}).");
                }

                ValidatePriorLink(definition, order, byId, issues);
            }

            ValidateRootAndReachability(definitions, byId, issues);
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

        private static void ValidatePriorLink(
            ChampionshipDefinition definition,
            int order,
            Dictionary<string, ChampionshipDefinition> byId,
            List<string> issues)
        {
            var prior = definition.PriorChampionship;

            if (definition.UnlockPolicy == EventUnlockPolicy.AlwaysAvailable)
            {
                if (prior != null)
                {
                    issues.Add($"Championship {definition.ChampionshipId} is a root (AlwaysAvailable) but references a prior championship '{Describe(prior)}'.");
                }

                return;
            }

            if (prior == null)
            {
                issues.Add($"Championship {definition.ChampionshipId} requires a previous championship but PriorChampionship is missing.");
                return;
            }

            if (string.Equals(prior.ChampionshipId, definition.ChampionshipId, StringComparison.Ordinal))
            {
                issues.Add($"Championship {definition.ChampionshipId} cannot reference itself as its prior championship.");
                return;
            }

            if (!byId.TryGetValue(prior.ChampionshipId, out var catalogPrior))
            {
                issues.Add($"Championship {definition.ChampionshipId} references prior championship '{Describe(prior)}' that is not in the catalog.");
                return;
            }

            if (catalogPrior.ProductionOrder != order - 1)
            {
                issues.Add($"Championship {definition.ChampionshipId} (ProductionOrder {order}) must reference ProductionOrder {order - 1} as its prior championship (found {catalogPrior.ProductionOrder}).");
            }
        }

        private static void ValidateRootAndReachability(
            IReadOnlyList<ChampionshipDefinition> definitions,
            Dictionary<string, ChampionshipDefinition> byId,
            List<string> issues)
        {
            ChampionshipDefinition root = null;
            var rootCount = 0;

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (definition.UnlockPolicy == EventUnlockPolicy.AlwaysAvailable)
                {
                    rootCount++;
                    root = definition;
                }
            }

            if (rootCount != 1)
            {
                issues.Add($"UnlockChain: must have exactly one root championship (AlwaysAvailable), found {rootCount}.");
            }

            // Cycle detection over explicit prior references.
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition != null && DetectCycle(definition, byId, visited, visiting))
                {
                    issues.Add($"UnlockChain: cyclic dependency detected at championship '{definition.ChampionshipId}'.");
                }
            }

            if (rootCount != 1 || root == null)
            {
                return;
            }

            // Every championship must be reachable by following prior references forward from root.
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<ChampionshipDefinition>();
            pending.Enqueue(root);
            reachable.Add(root.ChampionshipId);

            while (pending.Count > 0)
            {
                var current = pending.Dequeue();
                for (var i = 0; i < definitions.Count; i++)
                {
                    var candidate = definitions[i];
                    if (candidate == null || !reachable.Add(candidate.ChampionshipId))
                    {
                        continue;
                    }

                    if (IsPriorOf(candidate, current))
                    {
                        pending.Enqueue(candidate);
                    }
                    else
                    {
                        reachable.Remove(candidate.ChampionshipId);
                    }
                }
            }

            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition != null && !reachable.Contains(definition.ChampionshipId))
                {
                    issues.Add($"UnlockChain: championship '{definition.ChampionshipId}' is not reachable from the root.");
                }
            }
        }

        private static bool DetectCycle(
            ChampionshipDefinition definition,
            Dictionary<string, ChampionshipDefinition> byId,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (definition == null || string.IsNullOrEmpty(definition.ChampionshipId))
            {
                return false;
            }

            if (visiting.Contains(definition.ChampionshipId))
            {
                return true;
            }

            if (!visited.Add(definition.ChampionshipId))
            {
                return false;
            }

            visiting.Add(definition.ChampionshipId);
            var prior = definition.PriorChampionship;
            var hasCycle = false;
            if (prior != null &&
                byId.TryGetValue(prior.ChampionshipId, out var catalogPrior) &&
                DetectCycle(catalogPrior, byId, visited, visiting))
            {
                hasCycle = true;
            }

            visiting.Remove(definition.ChampionshipId);
            return hasCycle;
        }

        private static bool IsPriorOf(ChampionshipDefinition candidate, ChampionshipDefinition current)
        {
            var prior = candidate == null ? null : candidate.PriorChampionship;
            return prior != null &&
                   string.Equals(prior.ChampionshipId, current.ChampionshipId, StringComparison.Ordinal);
        }

        private static string Describe(ChampionshipDefinition definition)
        {
            return definition == null
                ? "<null>"
                : string.IsNullOrEmpty(definition.ChampionshipId)
                    ? definition.name
                    : definition.ChampionshipId;
        }
    }
}
