using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Complete validation pipeline for the M8 championship/event catalog (T-10.7.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Validates schema integrity, the championship and event unlock chains (one root, acyclic,
    /// all reachable), and coverage across all five environments and both layout configurations.
    /// The validator is fail-closed: any error makes <see cref="ValidationResult.IsValid"/> false.
    /// Warnings are reported but do not fail validation unless the caller opts in.
    /// </para>
    /// <para>
    /// This is a cold authoring/CI path. It may allocate; runtime event resolution must not.
    /// </para>
    /// </remarks>
    public static class ChampionshipCatalogValidator
    {
        /// <summary>Result of a full catalog validation run.</summary>
        public readonly struct ValidationResult
        {
            /// <summary>True when no error was reported.</summary>
            public readonly bool IsValid;

            /// <summary>Every blocking problem, in validation order; never null.</summary>
            public readonly IReadOnlyList<string> Errors;

            /// <summary>Non-blocking problems; never null.</summary>
            public readonly IReadOnlyList<string> Warnings;

            /// <summary>Coverage statistics computed from the submitted content.</summary>
            public readonly CatalogCoverage Coverage;

            public ValidationResult(
                bool isValid,
                IReadOnlyList<string> errors,
                IReadOnlyList<string> warnings,
                CatalogCoverage coverage)
            {
                IsValid = isValid;
                Errors = errors ?? Array.Empty<string>();
                Warnings = warnings ?? Array.Empty<string>();
                Coverage = coverage;
            }

            /// <summary>Creates a valid result.</summary>
            public static ValidationResult Valid(CatalogCoverage coverage) =>
                new ValidationResult(true, Array.Empty<string>(), Array.Empty<string>(), coverage);

            /// <summary>Creates a valid result that carries warnings.</summary>
            public static ValidationResult ValidWithWarnings(
                IReadOnlyList<string> warnings,
                CatalogCoverage coverage) =>
                new ValidationResult(true, Array.Empty<string>(), warnings, coverage);

            /// <summary>Creates an invalid result.</summary>
            public static ValidationResult Invalid(
                IReadOnlyList<string> errors,
                CatalogCoverage coverage) =>
                new ValidationResult(false, errors, Array.Empty<string>(), coverage);

            /// <summary>Creates an invalid result with warnings.</summary>
            public static ValidationResult InvalidWithWarnings(
                IReadOnlyList<string> errors,
                IReadOnlyList<string> warnings,
                CatalogCoverage coverage) =>
                new ValidationResult(false, errors, warnings, coverage);
        }

        /// <summary>Coverage statistics for the validated catalog.</summary>
        public readonly struct CatalogCoverage
        {
            public readonly int ChampionshipCount;
            public readonly int TotalEventCount;
            public readonly int CircuitRaceCount;
            public readonly int TimeTrialCount;
            public readonly int[] EventsPerEnvironment;
            public readonly int[] EventsPerLayoutVariant;
            public readonly int[] EventsPerConfiguration;
            public readonly int[] EventsPerChampionship;
            public readonly bool UnlockChainValid;
            public readonly bool HasCyclicUnlock;
            public readonly bool AllEventsReachable;

            public CatalogCoverage(
                int championshipCount,
                int totalEventCount,
                int circuitRaceCount,
                int timeTrialCount,
                int[] eventsPerEnvironment,
                int[] eventsPerLayoutVariant,
                int[] eventsPerConfiguration,
                int[] eventsPerChampionship,
                bool unlockChainValid,
                bool hasCyclicUnlock,
                bool allEventsReachable)
            {
                ChampionshipCount = championshipCount;
                TotalEventCount = totalEventCount;
                CircuitRaceCount = circuitRaceCount;
                TimeTrialCount = timeTrialCount;
                EventsPerEnvironment = eventsPerEnvironment ?? new int[ChampionshipContentPolicy.RequiredEnvironmentCount];
                EventsPerLayoutVariant = eventsPerLayoutVariant ?? new int[ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment];
                EventsPerConfiguration = eventsPerConfiguration ?? new int[ChampionshipContentPolicy.RequiredConfigurationCount];
                EventsPerChampionship = eventsPerChampionship ?? new int[ChampionshipContentPolicy.RequiredChampionshipCount];
                UnlockChainValid = unlockChainValid;
                HasCyclicUnlock = hasCyclicUnlock;
                AllEventsReachable = allEventsReachable;
            }

            /// <summary>Number of distinct environment/configuration pairs covered.</summary>
            public int CoveredConfigurationCount
            {
                get
                {
                    var configurations = EventsPerConfiguration;
                    if (configurations == null)
                    {
                        return 0;
                    }

                    var count = 0;
                    for (var i = 0; i < configurations.Length; i++)
                    {
                        if (configurations[i] > 0)
                        {
                            count++;
                        }
                    }

                    return count;
                }
            }
        }

        /// <summary>
        /// Validates the complete catalog and all event definitions. This is the single entry
        /// point used by the editor menu, batch runner, and EditMode defect tests.
        /// </summary>
        /// <param name="championshipCatalog">The three-championship catalog.</param>
        /// <param name="eventDefinitions">All fifteen event definitions.</param>
        /// <param name="environmentCatalog">The five-environment catalog.</param>
        public static ValidationResult ValidateCompleteCatalog(
            ChampionshipCatalog championshipCatalog,
            IReadOnlyList<RaceEventDefinition> eventDefinitions,
            EnvironmentCatalog environmentCatalog)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            if (championshipCatalog == null)
            {
                errors.Add("ChampionshipCatalog is null.");
            }

            var championships = championshipCatalog == null
                ? Array.Empty<ChampionshipDefinition>()
                : championshipCatalog.Championships;

            ValidateChampionshipSchema(championships, errors);
            var environmentLookup = ValidateEnvironmentSchema(environmentCatalog, errors);
            ValidateEvents(championships, eventDefinitions, environmentLookup, errors, warnings);

            var coverageIssues = ValidateCoveragePolicy(championships, eventDefinitions);
            for (var i = 0; i < coverageIssues.Count; i++)
            {
                errors.Add(coverageIssues[i]);
            }

            var chain = ValidateUnlockChains(championships, eventDefinitions, errors);
            var coverage = CollectCoverage(
                championships,
                eventDefinitions,
                chain.IsValid,
                chain.HasCyclic,
                chain.AllReachable);

            if (errors.Count > 0)
            {
                return warnings.Count > 0
                    ? ValidationResult.InvalidWithWarnings(errors, warnings, coverage)
                    : ValidationResult.Invalid(errors, coverage);
            }

            return warnings.Count > 0
                ? ValidationResult.ValidWithWarnings(warnings, coverage)
                : ValidationResult.Valid(coverage);
        }

        private static void ValidateChampionshipSchema(
            IReadOnlyList<ChampionshipDefinition> championships,
            List<string> errors)
        {
            if (championships == null)
            {
                errors.Add("ChampionshipCatalog is null.");
                return;
            }

            if (championships.Count != ChampionshipContentPolicy.RequiredChampionshipCount)
            {
                errors.Add($"ChampionshipCatalog: must contain exactly {ChampionshipContentPolicy.RequiredChampionshipCount} championships (found {championships.Count}).");
            }

            var ids = new Dictionary<string, int>(StringComparer.Ordinal);
            var orderCounts = new int[ChampionshipContentPolicy.RequiredChampionshipCount];

            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship == null)
                {
                    errors.Add($"ChampionshipCatalog: entry {i} is null.");
                    continue;
                }

                AddIssues(
                    $"ChampionshipDefinition [{i}] ('{Describe(championship)}')",
                    championship.Validate(),
                    errors);

                if (!string.IsNullOrEmpty(championship.ChampionshipId))
                {
                    if (ids.TryGetValue(championship.ChampionshipId, out var firstIndex))
                    {
                        errors.Add($"Duplicate ChampionshipId '{championship.ChampionshipId}' at entries {firstIndex} and {i}.");
                    }
                    else
                    {
                        ids.Add(championship.ChampionshipId, i);
                    }
                }

                var order = championship.ProductionOrder;
                if (order >= 1 && order <= ChampionshipContentPolicy.RequiredChampionshipCount)
                {
                    orderCounts[order - 1]++;
                }
            }

            for (var order = 1; order <= ChampionshipContentPolicy.RequiredChampionshipCount; order++)
            {
                if (orderCounts[order - 1] != 1)
                {
                    errors.Add($"ChampionshipCatalog: ProductionOrder {order} must appear exactly once (found {orderCounts[order - 1]}).");
                }
            }

            ValidatePriorLinks(championships, ids, errors);
        }

        private static void ValidatePriorLinks(
            IReadOnlyList<ChampionshipDefinition> championships,
            Dictionary<string, int> ids,
            List<string> errors)
        {
            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship == null)
                {
                    continue;
                }

                var order = championship.ProductionOrder;
                if (order >= 1 && order <= ChampionshipContentPolicy.RequiredChampionshipCount)
                {
                    var expectedPolicy =
                        ChampionshipContentPolicy.GetExpectedChampionshipUnlockPolicy(order);
                    if (championship.UnlockPolicy != expectedPolicy)
                    {
                        errors.Add($"ChampionshipCatalog: championship '{championship.ChampionshipId}' (ProductionOrder {order}) must have UnlockPolicy {expectedPolicy} (found {championship.UnlockPolicy}).");
                    }
                }

                var prior = championship.PriorChampionship;
                if (championship.UnlockPolicy == EventUnlockPolicy.AlwaysAvailable)
                {
                    if (prior != null)
                    {
                        errors.Add($"ChampionshipCatalog: root championship '{championship.ChampionshipId}' must not reference a prior championship.");
                    }

                    continue;
                }

                if (prior == null)
                {
                    errors.Add($"ChampionshipCatalog: championship '{championship.ChampionshipId}' requires a previous championship but PriorChampionship is missing.");
                    continue;
                }

                if (string.Equals(prior.ChampionshipId, championship.ChampionshipId, StringComparison.Ordinal))
                {
                    errors.Add($"ChampionshipCatalog: championship '{championship.ChampionshipId}' cannot reference itself as its prior championship.");
                    continue;
                }

                if (!ids.ContainsKey(prior.ChampionshipId))
                {
                    errors.Add($"ChampionshipCatalog: championship '{championship.ChampionshipId}' references prior championship '{prior.ChampionshipId}' that is not in the catalog.");
                    continue;
                }

                if (prior.ProductionOrder != order - 1)
                {
                    errors.Add($"ChampionshipCatalog: championship '{championship.ChampionshipId}' (ProductionOrder {order}) must reference ProductionOrder {order - 1} as its prior championship (found {prior.ProductionOrder}).");
                }
            }
        }

        private static Dictionary<EnvironmentTheme, EnvironmentDefinition> ValidateEnvironmentSchema(
            EnvironmentCatalog environmentCatalog,
            List<string> errors)
        {
            var lookup = new Dictionary<EnvironmentTheme, EnvironmentDefinition>();

            if (environmentCatalog == null)
            {
                errors.Add("EnvironmentCatalog is null.");
                return lookup;
            }

            var environments = environmentCatalog.Environments;
            if (environments.Length != ChampionshipContentPolicy.RequiredEnvironmentCount)
            {
                errors.Add($"EnvironmentCatalog: must contain exactly {ChampionshipContentPolicy.RequiredEnvironmentCount} environments (found {environments.Length}).");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < environments.Length; i++)
            {
                var environment = environments[i];
                if (environment == null)
                {
                    errors.Add($"EnvironmentCatalog: entry {i} is null.");
                    continue;
                }

                AddIssues(
                    $"EnvironmentDefinition [{i}]",
                    environment.Validate(),
                    errors);

                if (!string.IsNullOrEmpty(environment.EnvironmentId) &&
                    !ids.Add(environment.EnvironmentId))
                {
                    errors.Add($"EnvironmentCatalog: duplicate EnvironmentId '{environment.EnvironmentId}'.");
                }

                var theme = environment.Theme;
                if (!EnvironmentThemeRules.IsDefined(theme))
                {
                    errors.Add($"EnvironmentCatalog: entry {i} has undefined Theme value {(int)theme}.");
                }
                else if (lookup.ContainsKey(theme))
                {
                    errors.Add($"EnvironmentCatalog: duplicate Theme '{EnvironmentThemeRules.GetDisplayName(theme)}'.");
                }
                else
                {
                    lookup.Add(theme, environment);
                }
            }

            for (var i = 0; i < EnvironmentThemeRules.Themes.Count; i++)
            {
                var theme = EnvironmentThemeRules.Themes[i];
                if (!lookup.ContainsKey(theme))
                {
                    errors.Add($"EnvironmentCatalog: missing required environment '{EnvironmentThemeRules.GetDisplayName(theme)}'.");
                }
            }

            return lookup;
        }

        private static void ValidateEvents(
            IReadOnlyList<ChampionshipDefinition> championships,
            IReadOnlyList<RaceEventDefinition> eventDefinitions,
            Dictionary<EnvironmentTheme, EnvironmentDefinition> environments,
            List<string> errors,
            List<string> warnings)
        {
            if (eventDefinitions == null)
            {
                errors.Add("EventDefinitions list is null.");
                return;
            }

            if (eventDefinitions.Count != ChampionshipContentPolicy.RequiredTotalEvents)
            {
                errors.Add($"EventDefinitions: must contain exactly {ChampionshipContentPolicy.RequiredTotalEvents} events (found {eventDefinitions.Count}).");
            }

            var championshipById = new Dictionary<string, ChampionshipDefinition>(StringComparer.Ordinal);
            if (championships != null)
            {
                for (var i = 0; i < championships.Count; i++)
                {
                    var championship = championships[i];
                    if (championship != null &&
                        !string.IsNullOrEmpty(championship.ChampionshipId))
                    {
                        championshipById[championship.ChampionshipId] = championship;
                    }
                }
            }

            var eventIds = new HashSet<string>(StringComparer.Ordinal);
            var eventsPerChampionship = new Dictionary<string, int>(StringComparer.Ordinal);
            var roundsPerChampionship = new Dictionary<string, HashSet<int>>(StringComparer.Ordinal);
            var stringsSeen = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < eventDefinitions.Count; i++)
            {
                var definition = eventDefinitions[i];
                if (definition == null)
                {
                    errors.Add($"EventDefinitions: entry {i} is null.");
                    continue;
                }

                AddIssues($"RaceEventDefinition [{i}] ('{Describe(definition)}')", definition.Validate(), errors);

                if (!string.IsNullOrEmpty(definition.EventId))
                {
                    if (!eventIds.Add(definition.EventId))
                    {
                        errors.Add($"Duplicate EventId '{definition.EventId}'.");
                    }

                    var nameKey = definition.DisplayNameEn;
                    if (!string.IsNullOrEmpty(nameKey) && !stringsSeen.Add(nameKey))
                    {
                        warnings.Add($"Event display-name duplicate: '{nameKey}'.");
                    }
                }

                if (!championshipById.TryGetValue(definition.ChampionshipId ?? string.Empty, out var championship))
                {
                    errors.Add($"UnlockChain: championship '{definition.ChampionshipId}' not found for event '{Describe(definition)}'.");
                    championship = null;
                }
                else
                {
                    Inc(eventsPerChampionship, championship.ChampionshipId);
                    if (!roundsPerChampionship.TryGetValue(championship.ChampionshipId, out var rounds))
                    {
                        rounds = new HashSet<int>();
                        roundsPerChampionship.Add(championship.ChampionshipId, rounds);
                    }

                    if (!rounds.Add(definition.RoundIndex))
                    {
                        errors.Add($"EventDefinitions: championship '{championship.ChampionshipId}' has duplicate RoundIndex {definition.RoundIndex} (event '{definition.EventId}').");
                    }
                }

                if (!EnvironmentThemeRules.IsDefined(definition.EnvironmentTheme))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' has undefined EnvironmentTheme value {(int)definition.EnvironmentTheme}.");
                }
                else if (!environments.ContainsKey(definition.EnvironmentTheme))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' references environment '{EnvironmentThemeRules.GetDisplayName(definition.EnvironmentTheme)}' that is missing from the EnvironmentCatalog.");
                }

                if (!EnvironmentLayoutVariantRules.IsDefined(definition.LayoutVariant))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' has undefined LayoutVariant value {(int)definition.LayoutVariant}.");
                }

                if (!ChampionshipContentPolicy.IsApprovedEventType(definition.EventType))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' has unapproved EventType {(int)definition.EventType}; M8 permits only CircuitRace and TimeTrial.");
                }
                else
                {
                    var expectedMode = ChampionshipContentPolicy.GetExpectedOpponentMode(definition.EventType);
                    if (definition.OpponentMode != expectedMode)
                    {
                        errors.Add($"Coverage: event '{definition.EventId}' ({definition.EventType}) does not match OpponentMode {expectedMode} (found {definition.OpponentMode}).");
                    }

                    if (definition.EventType == RaceEventType.CircuitRace &&
                        definition.OpponentMode != EventOpponentMode.FixedFourAI)
                    {
                        errors.Add($"Coverage: CircuitRace event '{definition.EventId}' must use OpponentMode FixedFourAI with exactly {ChampionshipContentPolicy.CircuitRaceAiCount} AI opponents.");
                    }
                }

                if (championship != null)
                {
                    var expectedPace = ChampionshipContentPolicy.GetExpectedPaceScale(championship.ProductionOrder);
                    if (Mathf.Abs(definition.PaceScale - expectedPace) > 0.001f)
                    {
                        errors.Add($"Coverage: event '{definition.EventId}' pace scale {definition.PaceScale} does not match championship '{championship.ChampionshipId}' expected {expectedPace}.");
                    }
                }
            }

        }

        private readonly struct ChainResult
        {
            public readonly bool IsValid;
            public readonly bool HasCyclic;
            public readonly bool AllReachable;

            public ChainResult(bool isValid, bool hasCyclic, bool allReachable)
            {
                IsValid = isValid;
                HasCyclic = hasCyclic;
                AllReachable = allReachable;
            }
        }

        private static ChainResult ValidateUnlockChains(
            IReadOnlyList<ChampionshipDefinition> championships,
            IReadOnlyList<RaceEventDefinition> eventDefinitions,
            List<string> errors)
        {
            var valid = true;
            var hasCyclic = false;
            var allReachable = true;

            if (championships == null)
            {
                return new ChainResult(false, false, false);
            }

            var byId = new Dictionary<string, ChampionshipDefinition>(StringComparer.Ordinal);
            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship != null && !string.IsNullOrEmpty(championship.ChampionshipId))
                {
                    byId[championship.ChampionshipId] = championship;
                }
            }

            var rootCount = 0;
            ChampionshipDefinition root = null;
            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship == null)
                {
                    continue;
                }

                if (championship.UnlockPolicy == EventUnlockPolicy.AlwaysAvailable)
                {
                    rootCount++;
                    root = championship;
                }

                if (championship.PriorChampionship != null &&
                    string.Equals(
                        championship.PriorChampionship.ChampionshipId,
                        championship.ChampionshipId,
                        StringComparison.Ordinal))
                {
                    errors.Add($"UnlockChain: cyclic dependency detected at championship '{championship.ChampionshipId}'.");
                    hasCyclic = true;
                    valid = false;
                }
            }

            if (rootCount != 1)
            {
                errors.Add($"UnlockChain: must have exactly one root championship (AlwaysAvailable), found {rootCount}.");
                valid = false;
            }

            var visited = new HashSet<string>(StringComparer.Ordinal);
            var visiting = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship == null)
                {
                    continue;
                }

                if (DetectChampionshipCycle(championship, byId, visited, visiting))
                {
                    errors.Add($"UnlockChain: cyclic dependency detected at championship '{championship.ChampionshipId}'.");
                    hasCyclic = true;
                    valid = false;
                }
            }

            var reachable = new HashSet<string>(StringComparer.Ordinal);
            if (root != null)
            {
                var queue = new Queue<ChampionshipDefinition>();
                queue.Enqueue(root);
                reachable.Add(root.ChampionshipId);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    for (var i = 0; i < championships.Count; i++)
                    {
                        var candidate = championships[i];
                        if (candidate == null ||
                            reachable.Contains(candidate.ChampionshipId) ||
                            candidate.PriorChampionship == null ||
                            !string.Equals(
                                candidate.PriorChampionship.ChampionshipId,
                                current.ChampionshipId,
                                StringComparison.Ordinal))
                        {
                            continue;
                        }

                        reachable.Add(candidate.ChampionshipId);
                        queue.Enqueue(candidate);
                    }
                }
            }

            for (var i = 0; i < championships.Count; i++)
            {
                var championship = championships[i];
                if (championship != null && !reachable.Contains(championship.ChampionshipId))
                {
                    errors.Add($"UnlockChain: championship '{championship.ChampionshipId}' is not reachable from the root.");
                    allReachable = false;
                    valid = false;
                }
            }

            // Event chains: every championship has exactly five rounds forming a linear chain.
            var eventsByChampionship = new Dictionary<string, List<RaceEventDefinition>>(StringComparer.Ordinal);
            if (eventDefinitions != null)
            {
                for (var i = 0; i < eventDefinitions.Count; i++)
                {
                    var definition = eventDefinitions[i];
                    if (definition == null || string.IsNullOrEmpty(definition.ChampionshipId))
                    {
                        continue;
                    }

                    if (!eventsByChampionship.TryGetValue(definition.ChampionshipId, out var list))
                    {
                        list = new List<RaceEventDefinition>();
                        eventsByChampionship.Add(definition.ChampionshipId, list);
                    }

                    list.Add(definition);
                }
            }

            foreach (var pair in eventsByChampionship)
            {
                if (!byId.TryGetValue(pair.Key, out var championship))
                {
                    continue;
                }

                var events = pair.Value;
                events.Sort((left, right) => left.RoundIndex.CompareTo(right.RoundIndex));

                var expectedRootPolicy = championship.ProductionOrder == 1
                    ? EventUnlockPolicy.AlwaysAvailable
                    : EventUnlockPolicy.RequiresPreviousChampionship;

                var rootEvents = 0;
                for (var i = 0; i < events.Count; i++)
                {
                    var definition = events[i];
                    var expectedPolicy = definition.RoundIndex == 1
                        ? expectedRootPolicy
                        : EventUnlockPolicy.RequiresPreviousEvent;

                    if (definition.RoundIndex == 1 &&
                        definition.UnlockPolicy == expectedRootPolicy)
                    {
                        rootEvents++;
                    }

                    if (definition.UnlockPolicy != expectedPolicy)
                    {
                        errors.Add($"UnlockChain: event '{definition.EventId}' (Round {definition.RoundIndex}) in championship '{championship.ChampionshipId}' must have unlock policy {expectedPolicy} (found {definition.UnlockPolicy}).");
                        valid = false;
                    }

                    if (definition.UnlockPolicy == EventUnlockPolicy.RequiresPreviousEvent)
                    {
                        var previousRound = definition.RoundIndex - 1;
                        if (previousRound < 1 || !ContainsRound(events, previousRound))
                        {
                            errors.Add($"UnlockChain: event '{definition.EventId}' requires a previous event but the chain is cyclic or broken at Round {definition.RoundIndex}.");
                            hasCyclic = true;
                            valid = false;
                        }
                    }
                }

                if (rootEvents != 1)
                {
                    errors.Add($"UnlockChain: championship '{championship.ChampionshipId}' must have exactly one root event (expected {expectedRootPolicy}), found {rootEvents}.");
                    valid = false;
                    allReachable = false;
                }

                if (!AllRoundsPresent(events))
                {
                    allReachable = false;
                    valid = false;
                }
            }

            return new ChainResult(valid, hasCyclic, allReachable);
        }

        private static bool DetectChampionshipCycle(
            ChampionshipDefinition championship,
            Dictionary<string, ChampionshipDefinition> byId,
            HashSet<string> visited,
            HashSet<string> visiting)
        {
            if (championship == null || string.IsNullOrEmpty(championship.ChampionshipId))
            {
                return false;
            }

            if (visiting.Contains(championship.ChampionshipId))
            {
                return true;
            }

            if (!visited.Add(championship.ChampionshipId))
            {
                return false;
            }

            visiting.Add(championship.ChampionshipId);
            var prior = championship.PriorChampionship;
            var cycle = prior != null &&
                        byId.TryGetValue(prior.ChampionshipId, out var catalogPrior) &&
                        DetectChampionshipCycle(catalogPrior, byId, visited, visiting);
            visiting.Remove(championship.ChampionshipId);
            return cycle;
        }

        private static bool ContainsRound(List<RaceEventDefinition> events, int round)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].RoundIndex == round)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AllRoundsPresent(List<RaceEventDefinition> events)
        {
            for (var round = 1; round <= ChampionshipContentPolicy.EventsPerChampionship; round++)
            {
                if (!ContainsRound(events, round))
                {
                    return false;
                }
            }

            return true;
        }

        private static CatalogCoverage CollectCoverage(
            IReadOnlyList<ChampionshipDefinition> championships,
            IReadOnlyList<RaceEventDefinition> eventDefinitions,
            bool unlockValid,
            bool hasCyclic,
            bool allReachable)
        {
            var perEnvironment = new int[ChampionshipContentPolicy.RequiredEnvironmentCount];
            var perVariant = new int[ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment];
            var perConfiguration = new int[ChampionshipContentPolicy.RequiredConfigurationCount];
            var perChampionship = new int[ChampionshipContentPolicy.RequiredChampionshipCount];
            var circuit = 0;
            var timeTrial = 0;

            var championshipOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            if (championships != null)
            {
                for (var i = 0; i < championships.Count; i++)
                {
                    var championship = championships[i];
                    if (championship != null && !string.IsNullOrEmpty(championship.ChampionshipId))
                    {
                        championshipOrder[championship.ChampionshipId] = championship.ProductionOrder;
                    }
                }
            }

            if (eventDefinitions != null)
            {
                for (var i = 0; i < eventDefinitions.Count; i++)
                {
                    var definition = eventDefinitions[i];
                    if (definition == null)
                    {
                        continue;
                    }

                    if (EnvironmentThemeRules.IsDefined(definition.EnvironmentTheme))
                    {
                        perEnvironment[(int)definition.EnvironmentTheme]++;
                    }

                    if (EnvironmentLayoutVariantRules.IsDefined(definition.LayoutVariant))
                    {
                        perVariant[(int)definition.LayoutVariant]++;

                        if (EnvironmentThemeRules.IsDefined(definition.EnvironmentTheme))
                        {
                            var configurationIndex =
                                ((int)definition.EnvironmentTheme *
                                 ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment) +
                                (int)definition.LayoutVariant;
                            if (configurationIndex >= 0 &&
                                configurationIndex < perConfiguration.Length)
                            {
                                perConfiguration[configurationIndex]++;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(definition.ChampionshipId) &&
                        championshipOrder.TryGetValue(definition.ChampionshipId, out var order) &&
                        order >= 1 &&
                        order <= ChampionshipContentPolicy.RequiredChampionshipCount)
                    {
                        perChampionship[order - 1]++;
                    }

                    if (definition.EventType == RaceEventType.CircuitRace)
                    {
                        circuit++;
                    }
                    else if (definition.EventType == RaceEventType.TimeTrial)
                    {
                        timeTrial++;
                    }
                }
            }

            return new CatalogCoverage(
                championships == null ? 0 : championships.Count,
                eventDefinitions == null ? 0 : eventDefinitions.Count,
                circuit,
                timeTrial,
                perEnvironment,
                perVariant,
                perConfiguration,
                perChampionship,
                unlockValid,
                hasCyclic,
                allReachable);
        }

        /// <summary>
        /// Validates the approved coverage policy independently of unlock-chain checks. Used by
        /// the coverage section of the complete validator and exposed for focused tests.
        /// </summary>
        public static IReadOnlyList<string> ValidateCoveragePolicy(
            IReadOnlyList<ChampionshipDefinition> championships,
            IReadOnlyList<RaceEventDefinition> eventDefinitions)
        {
            var errors = new List<string>();
            if (eventDefinitions == null)
            {
                errors.Add("Coverage: event definitions are null.");
                return errors;
            }

            var perEnvironment = new int[ChampionshipContentPolicy.RequiredEnvironmentCount];
            var perVariant = new int[ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment];
            var configurationUsed = new bool[ChampionshipContentPolicy.RequiredEnvironmentCount, ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment];
            var circuit = 0;
            var timeTrial = 0;
            var championshipEvents = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < eventDefinitions.Count; i++)
            {
                var definition = eventDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (!EnvironmentThemeRules.IsDefined(definition.EnvironmentTheme))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' has undefined EnvironmentTheme value {(int)definition.EnvironmentTheme}.");
                    continue;
                }

                if (!EnvironmentLayoutVariantRules.IsDefined(definition.LayoutVariant))
                {
                    errors.Add($"Coverage: event '{definition.EventId}' has undefined LayoutVariant value {(int)definition.LayoutVariant}.");
                    continue;
                }

                var themeIndex = (int)definition.EnvironmentTheme;
                var variantIndex = (int)definition.LayoutVariant;
                perEnvironment[themeIndex]++;
                perVariant[variantIndex]++;
                configurationUsed[themeIndex, variantIndex] = true;

                if (!string.IsNullOrEmpty(definition.ChampionshipId))
                {
                    Inc(championshipEvents, definition.ChampionshipId);
                }

                if (definition.EventType == RaceEventType.CircuitRace)
                {
                    circuit++;
                }
                else if (definition.EventType == RaceEventType.TimeTrial)
                {
                    timeTrial++;
                }
            }

            for (var themeIndex = 0; themeIndex < perEnvironment.Length; themeIndex++)
            {
                var theme = (EnvironmentTheme)themeIndex;
                if (perEnvironment[themeIndex] != ChampionshipContentPolicy.RequiredEventsPerEnvironment)
                {
                    errors.Add($"Coverage: environment '{EnvironmentThemeRules.GetDisplayName(theme)}' has {perEnvironment[themeIndex]} events (must be exactly {ChampionshipContentPolicy.RequiredEventsPerEnvironment}).");
                }

                for (var variantIndex = 0; variantIndex < perVariant.Length; variantIndex++)
                {
                    if (!configurationUsed[themeIndex, variantIndex])
                    {
                        var variant = (EnvironmentLayoutVariant)variantIndex;
                        errors.Add($"Coverage: environment '{EnvironmentThemeRules.GetDisplayName(theme)}' has no {EnvironmentLayoutVariantRules.GetDisplayName(variant)} variant events; both configurations are required.");
                    }
                }
            }

            if (circuit != ChampionshipContentPolicy.RequiredCircuitRaceEvents)
            {
                errors.Add($"Coverage: must contain exactly {ChampionshipContentPolicy.RequiredCircuitRaceEvents} CircuitRace events (found {circuit}).");
            }

            if (timeTrial != ChampionshipContentPolicy.RequiredTimeTrialEvents)
            {
                errors.Add($"Coverage: must contain exactly {ChampionshipContentPolicy.RequiredTimeTrialEvents} TimeTrial events (found {timeTrial}).");
            }

            if (championships != null)
            {
                for (var i = 0; i < championships.Count; i++)
                {
                    var championship = championships[i];
                    if (championship == null)
                    {
                        continue;
                    }

                    var count = championshipEvents.TryGetValue(championship.ChampionshipId, out var found)
                        ? found
                        : 0;
                    if (count != ChampionshipContentPolicy.EventsPerChampionship)
                    {
                        errors.Add($"Coverage: championship '{championship.ChampionshipId}' has {count} events (must be exactly {ChampionshipContentPolicy.EventsPerChampionship}).");
                    }
                }
            }

            return errors;
        }

        private static void AddIssues(string prefix, IReadOnlyList<string> issues, List<string> errors)
        {
            if (issues == null)
            {
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                errors.Add(prefix + ": " + issues[i]);
            }
        }

        private static void Inc(Dictionary<string, int> counts, string key)
        {
            counts.TryGetValue(key, out var current);
            counts[key] = current + 1;
        }

        private static string Describe(ChampionshipDefinition definition)
        {
            return string.IsNullOrEmpty(definition.ChampionshipId)
                ? definition.name
                : definition.ChampionshipId;
        }

        private static string Describe(RaceEventDefinition definition)
        {
            return string.IsNullOrEmpty(definition.EventId)
                ? definition.name
                : definition.EventId;
        }
    }
}
