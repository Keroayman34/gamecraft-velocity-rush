using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-authored reference set for the five approved environments (T-9.1, D-054).
    /// </summary>
    /// <remarks>
    /// The catalog holds references only. Identity, palette, lighting, and layout live in each
    /// <see cref="EnvironmentDefinition"/>; this component is the runtime/editor resolution seam
    /// and never becomes a second source of truth. Validation delegates to
    /// <see cref="EnvironmentCatalogRules"/>.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EnvironmentCatalog : MonoBehaviour
    {
        [Tooltip("The five approved environment definitions in production order.")]
        [SerializeField] private EnvironmentDefinition[] environments = Array.Empty<EnvironmentDefinition>();

        /// <summary>Read-only catalog view; never null.</summary>
        public EnvironmentDefinition[] Environments =>
            environments ?? Array.Empty<EnvironmentDefinition>();

        /// <summary>Number of definitions in the catalog.</summary>
        public int Count => Environments.Length;

        /// <summary>
        /// Resolves a definition by stable id (ordinal comparison). Returns null when absent.
        /// </summary>
        public EnvironmentDefinition FindById(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                return null;
            }

            var data = Environments;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null &&
                    string.Equals(definition.EnvironmentId, environmentId, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>Resolves a definition by approved theme. Returns null when absent.</summary>
        public EnvironmentDefinition FindByTheme(EnvironmentTheme theme)
        {
            var data = Environments;
            for (var i = 0; i < data.Length; i++)
            {
                var definition = data[i];
                if (definition != null && definition.Theme == theme)
                {
                    return definition;
                }
            }

            return null;
        }

        /// <summary>Validates the complete catalog, returning every issue at once.</summary>
        public bool TryValidate(out string error)
        {
            var issues = EnvironmentCatalogRules.ValidateCatalog(Environments);
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
        public void ConfigureForAuthoring(EnvironmentDefinition[] definitions)
        {
            environments = definitions ?? Array.Empty<EnvironmentDefinition>();
        }
#endif
    }
}
