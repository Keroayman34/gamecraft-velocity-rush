using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure distinctness rules for the five approved environments (OQ-20, T-9.12/T-9.13).
    /// </summary>
    /// <remarks>
    /// <para>
    /// D-054 requires the five themes to be visibly distinct in palette, lighting, atmosphere,
    /// landmark/decor language, and track character. Shared technical kits are allowed, but a
    /// cosmetic swap of one kit is not. These rules make the measurable part of that requirement a
    /// content gate: every theme must differ from every other theme in accent/emissive/ground
    /// colour, sky/fog colour, road width, and layout length.
    /// </para>
    /// <para>
    /// Landmark language is structural and is audited separately by the scene builder. This class
    /// is presentation data only and never touches handling (D-057).
    /// </para>
    /// </remarks>
    public static class EnvironmentDistinctnessRules
    {
        /// <summary>Minimum RGB distance for the signature accent colour.</summary>
        public const float MinimumAccentDistance = 0.12f;

        /// <summary>Minimum RGB distance for the emissive colour.</summary>
        public const float MinimumEmissiveDistance = 0.12f;

        /// <summary>Minimum RGB distance for the primary ground colour.</summary>
        public const float MinimumGroundDistance = 0.10f;

        /// <summary>Minimum RGB distance for the procedural sky tint.</summary>
        public const float MinimumSkyDistance = 0.06f;

        /// <summary>Minimum RGB distance for the fog colour.</summary>
        public const float MinimumFogDistance = 0.06f;

        /// <summary>Minimum road-width difference between any two themes, in meters.</summary>
        public const float MinimumRoadWidthDelta = 0.5f;

        /// <summary>Minimum total-length difference between any two themes, in meters.</summary>
        public const float MinimumLengthDelta = 10f;

        /// <summary>
        /// Validates pairwise theme separation for a complete catalog. Returns every issue at once.
        /// </summary>
        public static IReadOnlyList<string> ValidateThemeSeparation(
            IReadOnlyList<EnvironmentDefinition> definitions)
        {
            var issues = new List<string>();
            if (definitions == null)
            {
                issues.Add("EnvironmentDistinctnessRules: catalog is null.");
                return issues;
            }

            var byTheme = new EnvironmentDefinition[EnvironmentThemeRules.RequiredThemeCount];
            for (var index = 0; index < definitions.Count; index++)
            {
                var definition = definitions[index];
                if (definition == null || !EnvironmentThemeRules.IsDefined(definition.Theme))
                {
                    continue;
                }

                var themeIndex = (int)definition.Theme;
                if (byTheme[themeIndex] != null)
                {
                    issues.Add("EnvironmentDistinctnessRules: theme '" +
                               EnvironmentThemeRules.GetDisplayName(definition.Theme) +
                               "' appears more than once.");
                    continue;
                }

                byTheme[themeIndex] = definition;
            }

            for (var i = 0; i < byTheme.Length; i++)
            {
                if (byTheme[i] == null)
                {
                    issues.Add("EnvironmentDistinctnessRules: missing theme '" +
                               EnvironmentThemeRules.GetDisplayName((EnvironmentTheme)i) + "'.");
                }
            }

            if (issues.Count > 0)
            {
                return issues;
            }

            for (var left = 0; left < byTheme.Length; left++)
            {
                for (var right = left + 1; right < byTheme.Length; right++)
                {
                    ValidatePair(byTheme[left], byTheme[right], issues);
                }
            }

            return issues;
        }

        /// <summary>Validates distinctness and returns the first issue.</summary>
        public static bool TryValidateThemeSeparation(
            IReadOnlyList<EnvironmentDefinition> definitions,
            out string error)
        {
            var issues = ValidateThemeSeparation(definitions);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

        private static void ValidatePair(
            EnvironmentDefinition left,
            EnvironmentDefinition right,
            List<string> issues)
        {
            if (left.LightingProfile == null || right.LightingProfile == null ||
                left.TrackLayout == null || right.TrackLayout == null)
            {
                issues.Add("EnvironmentDistinctnessRules: cannot compare themes with missing " +
                           "lighting or layout references.");
                return;
            }

            var leftName = EnvironmentThemeRules.GetDisplayName(left.Theme);
            var rightName = EnvironmentThemeRules.GetDisplayName(right.Theme);
            var pair = leftName + " vs " + rightName;

            var leftPalette = left.Palette;
            var rightPalette = right.Palette;
            RequireDistance(
                ColorDistance(leftPalette.Accent, rightPalette.Accent),
                MinimumAccentDistance,
                pair + " accent",
                issues);
            RequireDistance(
                ColorDistance(leftPalette.Emissive, rightPalette.Emissive),
                MinimumEmissiveDistance,
                pair + " emissive",
                issues);
            RequireDistance(
                ColorDistance(leftPalette.GroundPrimary, rightPalette.GroundPrimary),
                MinimumGroundDistance,
                pair + " ground",
                issues);

            var leftLighting = left.LightingProfile.BuildSettings();
            var rightLighting = right.LightingProfile.BuildSettings();
            RequireDistance(
                ColorDistance(leftLighting.SkyTint, rightLighting.SkyTint),
                MinimumSkyDistance,
                pair + " sky",
                issues);
            RequireDistance(
                ColorDistance(leftLighting.FogColor, rightLighting.FogColor),
                MinimumFogDistance,
                pair + " fog",
                issues);

            var widthDelta = Mathf.Abs(left.TrackLayout.RoadWidth - right.TrackLayout.RoadWidth);
            if (widthDelta < MinimumRoadWidthDelta)
            {
                issues.Add("EnvironmentDistinctnessRules: " + pair +
                           " road widths differ by only " + widthDelta.ToString("0.00") +
                           " m (minimum " + MinimumRoadWidthDelta + ").");
            }

            var lengthDelta = Mathf.Abs(left.TrackLayout.TotalLength - right.TrackLayout.TotalLength);
            if (lengthDelta < MinimumLengthDelta)
            {
                issues.Add("EnvironmentDistinctnessRules: " + pair +
                           " layout lengths differ by only " + lengthDelta.ToString("0.0") +
                           " m (minimum " + MinimumLengthDelta + ").");
            }
        }

        private static void RequireDistance(
            float distance,
            float minimum,
            string label,
            List<string> issues)
        {
            if (distance < minimum)
            {
                issues.Add("EnvironmentDistinctnessRules: " + label +
                           " colours differ by only " + distance.ToString("0.000") +
                           " (minimum " + minimum.ToString("0.000") + ").");
            }
        }

        private static float ColorDistance(Color left, Color right)
        {
            var r = left.r - right.r;
            var g = left.g - right.g;
            var b = left.b - right.b;
            return Mathf.Sqrt((r * r) + (g * g) + (b * b));
        }
    }
}
