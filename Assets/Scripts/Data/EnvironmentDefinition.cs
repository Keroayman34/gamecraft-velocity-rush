using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Stable, read-only definition for one Commercial V1.0 environment package (T-9.1, D-054).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This asset owns identity, the approved theme, the presentation palette, and references to
    /// the lighting profile and track layout. It deliberately carries no grip, hazard, damage, or
    /// vehicle-physics data: Phase 9 grip/hazard features are presentation-only (D-057), so the
    /// approved Phase-8 vehicle profiles remain the single source of truth for handling.
    /// </para>
    /// <para>
    /// One scene per environment is the approved architecture (D-057). The signature and reverse
    /// configurations share this definition and layout; the reverse variant only reverses sampled
    /// centerline order.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Environment Definition", fileName = "EnvironmentDefinition")]
    public sealed class EnvironmentDefinition : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Maximum accepted length of a stable environment id.</summary>
        public const int MaxEnvironmentIdLength = 48;

        /// <summary>Maximum accepted length of a localized display name.</summary>
        public const int MaxDisplayNameLength = 80;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Tooltip("Stable, lowercase ASCII id (for example desert-signature). Never rename after content references it.")]
        [SerializeField] private string environmentId = string.Empty;

        [Tooltip("English display name.")]
        [SerializeField] private string displayNameEn = string.Empty;

        [Tooltip("Arabic display name.")]
        [SerializeField] private string displayNameAr = string.Empty;

        [Tooltip("One of the five approved Commercial V1.0 themes.")]
        [SerializeField] private EnvironmentTheme theme = EnvironmentTheme.DesertCircuit;

        [Tooltip("Approved production order: Desert 1, Coastal 2, Mountain 3, Neon 4, World Arena 5.")]
        [SerializeField] private int productionOrder = 1;

        [Tooltip("Per-theme presentation palette.")]
        [SerializeField] private EnvironmentPalette palette;

        [Tooltip("Realtime-only lighting profile for this environment.")]
        [SerializeField] private EnvironmentLightingProfile lightingProfile;

        [Tooltip("Data-driven signature track layout.")]
        [SerializeField] private TrackLayout trackLayout;

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Stable, lowercase ASCII environment id.</summary>
        public string EnvironmentId => environmentId;

        /// <summary>English display name.</summary>
        public string DisplayNameEn => displayNameEn;

        /// <summary>Arabic display name.</summary>
        public string DisplayNameAr => displayNameAr;

        /// <summary>Approved environment theme.</summary>
        public EnvironmentTheme Theme => theme;

        /// <summary>Approved 1-based production order.</summary>
        public int ProductionOrder => productionOrder;

        /// <summary>Per-theme presentation palette.</summary>
        public EnvironmentPalette Palette => palette;

        /// <summary>Referenced realtime lighting profile.</summary>
        public EnvironmentLightingProfile LightingProfile => lightingProfile;

        /// <summary>Referenced signature track layout.</summary>
        public TrackLayout TrackLayout => trackLayout;

        /// <summary>
        /// Validates identity, theme/order consistency, the id prefix, palette, lighting profile,
        /// layout, and variant. Returns the first problem with a field-specific message.
        /// </summary>
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

        /// <summary>Returns every definition problem at once (cold authoring/validation path).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Validate()
        {
            var issues = new System.Collections.Generic.List<string>();
            if (schemaVersion != CurrentSchemaVersion)
            {
                issues.Add("EnvironmentDefinition: unsupported SchemaVersion " + schemaVersion +
                           "; expected " + CurrentSchemaVersion + ".");
            }

            if (!TryValidateEnvironmentId(environmentId, out var idError))
            {
                issues.Add("EnvironmentDefinition: " + idError);
            }

            ValidateDisplayName(displayNameEn, "DisplayNameEn", issues);
            ValidateDisplayName(displayNameAr, "DisplayNameAr", issues);

            if (!EnvironmentThemeRules.IsDefined(theme))
            {
                issues.Add("EnvironmentDefinition: Theme value " + (int)theme + " is not defined.");
            }
            else
            {
                var expectedOrder = EnvironmentThemeRules.GetProductionOrder(theme);
                if (productionOrder != expectedOrder)
                {
                    issues.Add("EnvironmentDefinition: ProductionOrder must be " + expectedOrder +
                               " for " + EnvironmentThemeRules.GetDisplayName(theme) +
                               " (found " + productionOrder + ").");
                }

                var expectedPrefix = EnvironmentThemeRules.GetStableFragment(theme) + "-";
                if (!string.IsNullOrEmpty(environmentId) &&
                    !environmentId.StartsWith(expectedPrefix, StringComparison.Ordinal))
                {
                    issues.Add("EnvironmentDefinition: EnvironmentId must start with '" +
                               expectedPrefix + "' for " + EnvironmentThemeRules.GetDisplayName(theme) + ".");
                }
            }

            var paletteIssues = palette.Validate();
            for (var i = 0; i < paletteIssues.Count; i++)
            {
                issues.Add(paletteIssues[i]);
            }

            if (lightingProfile == null)
            {
                issues.Add("EnvironmentDefinition: LightingProfile reference is missing.");
            }
            else if (!lightingProfile.TryValidate(out var lightingError))
            {
                issues.Add("EnvironmentDefinition: LightingProfile is invalid: " + lightingError);
            }

            if (trackLayout == null)
            {
                issues.Add("EnvironmentDefinition: TrackLayout reference is missing.");
            }
            else if (!trackLayout.TryValidate(out var layoutError))
            {
                issues.Add("EnvironmentDefinition: TrackLayout is invalid: " + layoutError);
            }

            return issues;
        }

        /// <summary>
        /// Validates the stable-id contract: non-empty, at most
        /// <see cref="MaxEnvironmentIdLength"/> characters, starts with a lowercase ASCII letter,
        /// and contains only lowercase ASCII letters, digits, and single hyphens.
        /// </summary>
        public static bool TryValidateEnvironmentId(string candidate, out string error)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "EnvironmentId must not be empty.";
                return false;
            }

            if (candidate.Length > MaxEnvironmentIdLength)
            {
                error = "EnvironmentId must be at most " + MaxEnvironmentIdLength + " characters.";
                return false;
            }

            if (candidate[0] < 'a' || candidate[0] > 'z')
            {
                error = "EnvironmentId must start with a lowercase ASCII letter.";
                return false;
            }

            var previousWasHyphen = false;
            for (var i = 0; i < candidate.Length; i++)
            {
                var character = candidate[i];
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                {
                    previousWasHyphen = false;
                    continue;
                }

                if (character == '-')
                {
                    if (previousWasHyphen || i == candidate.Length - 1)
                    {
                        error = "EnvironmentId must not contain consecutive or trailing hyphens.";
                        return false;
                    }

                    previousWasHyphen = true;
                    continue;
                }

                error = "EnvironmentId may contain only lowercase ASCII letters, digits, and single hyphens.";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateDisplayName(
            string value,
            string fieldName,
            System.Collections.Generic.List<string> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add("EnvironmentDefinition: " + fieldName + " must not be empty.");
                return;
            }

            if (value.Length > MaxDisplayNameLength)
            {
                issues.Add("EnvironmentDefinition: " + fieldName + " must be at most " +
                           MaxDisplayNameLength + " characters.");
                return;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                issues.Add("EnvironmentDefinition: " + fieldName +
                           " must not have leading or trailing whitespace.");
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    issues.Add("EnvironmentDefinition: " + fieldName +
                               " must not contain control characters.");
                    return;
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the deterministic environment builder and tests.
        /// Runtime code must treat definitions as read-only.
        /// </summary>
        public void ConfigureForAuthoring(
            string id,
            string nameEn,
            string nameAr,
            EnvironmentTheme environmentTheme,
            int order,
            EnvironmentPalette environmentPalette,
            EnvironmentLightingProfile lighting,
            TrackLayout layout)
        {
            schemaVersion = CurrentSchemaVersion;
            environmentId = id;
            displayNameEn = nameEn;
            displayNameAr = nameAr;
            theme = environmentTheme;
            productionOrder = order;
            palette = environmentPalette;
            lightingProfile = lighting;
            trackLayout = layout;
        }
#endif
    }
}
