using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-authored reference set for the fifteen M8 race events (T-10.8.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The catalog holds references only. Identity, type, environment, variant, unlock policy,
    /// and pace scale live in each <see cref="RaceEventDefinition"/>; this component is the
    /// runtime/editor resolution seam used by <see cref="EventLaunchPlanBuilder"/> and
    /// <c>RaceEventRuntime</c>. It never becomes a second source of truth.
    /// </para>
    /// <para>
    /// The catalog is deliberately small and lookup-based: resolution runs once per race launch
    /// and never allocates per frame. Missing or duplicate ids fail closed instead of silently
    /// resolving to a different event.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EventCatalog : MonoBehaviour
    {
        [Tooltip("The fifteen approved race events in championship/round order.")]
        [SerializeField] private RaceEventDefinition[] events = Array.Empty<RaceEventDefinition>();

        /// <summary>Read-only catalog view; never null.</summary>
        public RaceEventDefinition[] Events => events ?? Array.Empty<RaceEventDefinition>();

        /// <summary>Number of definitions in the catalog.</summary>
        public int Count => Events.Length;

        /// <summary>
        /// Resolves an event by stable id (ordinal comparison). Returns null when absent.
        /// </summary>
        public RaceEventDefinition FindById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
            {
                return null;
            }

            var data = Events;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null &&
                    string.Equals(definition.EventId, eventId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>
        /// Copies the events of one championship, ordered by round, into the caller's list.
        /// Returns the number copied. The catalog allocates nothing for the copy itself.
        /// </summary>
        public int CopyChampionshipEvents(string championshipId, List<RaceEventDefinition> destination)
        {
            if (destination == null || string.IsNullOrEmpty(championshipId))
            {
                return 0;
            }

            destination.Clear();
            var data = Events;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null &&
                    string.Equals(definition.ChampionshipId, championshipId, StringComparison.Ordinal))
                {
                    destination.Add(definition);
                }
            }

            destination.Sort((left, right) => left.RoundIndex.CompareTo(right.RoundIndex));
            return destination.Count;
        }

        /// <summary>
        /// Validates the catalog: exact event count, non-null entries, valid definitions, unique
        /// ids, and unique (championship, round) pairs. Returns every issue at once.
        /// </summary>
        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            var data = Events;
            if (data.Length != ChampionshipContentPolicy.RequiredTotalEvents)
            {
                issues.Add(
                    $"EventCatalog: must contain exactly " +
                    $"{ChampionshipContentPolicy.RequiredTotalEvents} events (found {data.Length}).");
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var rounds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition == null)
                {
                    issues.Add($"EventCatalog: entry {i} is null.");
                    continue;
                }

                var definitionIssues = definition.Validate();
                for (var issueIndex = 0; issueIndex < definitionIssues.Count; issueIndex++)
                {
                    issues.Add($"EventCatalog entry {i} ('{definition.EventId}'): {definitionIssues[issueIndex]}");
                }

                if (!string.IsNullOrEmpty(definition.EventId) && !ids.Add(definition.EventId))
                {
                    issues.Add($"EventCatalog: duplicate EventId '{definition.EventId}'.");
                }

                var roundKey = definition.ChampionshipId + "#" + definition.RoundIndex;
                if (!rounds.Add(roundKey))
                {
                    issues.Add(
                        $"EventCatalog: duplicate round {definition.RoundIndex} in championship " +
                        $"'{definition.ChampionshipId}'.");
                }
            }

            return issues;
        }

        /// <summary>Validates the catalog and returns the first issue.</summary>
        public bool TryValidate(out string error)
        {
            var issues = Validate();
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam for deterministic content builders. Runtime code must treat
        /// the catalog as read-only.
        /// </summary>
        public void ConfigureForAuthoring(RaceEventDefinition[] definitions)
        {
            events = definitions ?? Array.Empty<RaceEventDefinition>();
        }
#endif
    }
}
