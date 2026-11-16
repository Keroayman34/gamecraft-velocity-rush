using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-authored reference set for the three M8 championships (T-10.6.x, G1/G2/G6).
    /// </summary>
    /// <remarks>
    /// The catalog holds references only. Identity, display names, pace scale, and unlock
    /// policy live in each <see cref="ChampionshipDefinition"/>; this component is the
    /// runtime/editor resolution seam and never becomes a second source of truth.
    /// Validation delegates to <see cref="ChampionshipCatalogRules"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ChampionshipCatalog : MonoBehaviour
    {
        [Tooltip("The three approved championship definitions in production order (C1, C2, C3).")]
        [SerializeField] private ChampionshipDefinition[] championships = Array.Empty<ChampionshipDefinition>();

        /// <summary>Read-only catalog view; never null.</summary>
        public ChampionshipDefinition[] Championships =>
            championships ?? Array.Empty<ChampionshipDefinition>();

        /// <summary>Number of definitions in the catalog.</summary>
        public int Count => Championships.Length;

        /// <summary>
        /// Resolves a definition by stable id (ordinal comparison). Returns null when absent.
        /// </summary>
        public ChampionshipDefinition FindById(string championshipId)
        {
            if (string.IsNullOrEmpty(championshipId))
            {
                return null;
            }

            var data = Championships;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null &&
                    string.Equals(definition.ChampionshipId, championshipId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>Resolves a definition by production order (1=C1, 2=C2, 3=C3). Returns null when absent.</summary>
        public ChampionshipDefinition FindByProductionOrder(int order)
        {
            var data = Championships;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null && definition.ProductionOrder == order)
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>Validates the complete catalog, returning every issue at once.</summary>
        public bool TryValidate(out string error)
        {
            var issues = ChampionshipCatalogRules.ValidateCatalog(Championships);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = string.Join("\n", issues);
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam for deterministic scene builders. Runtime code must treat
        /// the catalog as read-only.
        /// </summary>
        public void ConfigureForAuthoring(ChampionshipDefinition[] definitions)
        {
            championships = definitions ?? Array.Empty<ChampionshipDefinition>();
        }
#endif
    }
}