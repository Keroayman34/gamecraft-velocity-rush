using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// The five owner-approved Commercial V1.0 environment themes (D-028, D-054).
    /// </summary>
    /// <remarks>
    /// Enum order is the approved production order: Desert Circuit → Coastal Circuit →
    /// Mountain Pass → Neon City → World Arena (OQ-A04 resolved 2026-11-16). The value is
    /// serialized in assets, so values are stable and must not be reordered.
    /// </remarks>
    public enum EnvironmentTheme
    {
        DesertCircuit = 0,
        CoastalCircuit = 1,
        MountainPass = 2,
        NeonCity = 3,
        WorldArena = 4
    }

    /// <summary>
    /// Theme metadata and the pure ordering rules shared by authoring tools and tests.
    /// </summary>
    public static class EnvironmentThemeRules
    {
        /// <summary>Number of environment themes in Commercial V1.0 (D-028).</summary>
        public const int RequiredThemeCount = 5;

        private static readonly EnvironmentTheme[] AllThemes =
        {
            EnvironmentTheme.DesertCircuit,
            EnvironmentTheme.CoastalCircuit,
            EnvironmentTheme.MountainPass,
            EnvironmentTheme.NeonCity,
            EnvironmentTheme.WorldArena
        };

        /// <summary>All themes in approved production order.</summary>
        public static IReadOnlyList<EnvironmentTheme> Themes => AllThemes;

        /// <summary>True when the value is one of the five approved themes.</summary>
        public static bool IsDefined(EnvironmentTheme theme)
        {
            return theme >= EnvironmentTheme.DesertCircuit && theme <= EnvironmentTheme.WorldArena;
        }

        /// <summary>English display name for a theme; "Unknown" for an undefined value.</summary>
        public static string GetDisplayName(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.DesertCircuit:
                    return "Desert Circuit";
                case EnvironmentTheme.CoastalCircuit:
                    return "Coastal Circuit";
                case EnvironmentTheme.MountainPass:
                    return "Mountain Pass";
                case EnvironmentTheme.NeonCity:
                    return "Neon City";
                case EnvironmentTheme.WorldArena:
                    return "World Arena";
                default:
                    return "Unknown";
            }
        }

        /// <summary>Stable folder/id fragment for a theme ("desert", "coastal", ...).</summary>
        public static string GetStableFragment(EnvironmentTheme theme)
        {
            switch (theme)
            {
                case EnvironmentTheme.DesertCircuit:
                    return "desert";
                case EnvironmentTheme.CoastalCircuit:
                    return "coastal";
                case EnvironmentTheme.MountainPass:
                    return "mountain-pass";
                case EnvironmentTheme.NeonCity:
                    return "neon-city";
                case EnvironmentTheme.WorldArena:
                    return "world-arena";
                default:
                    return "unknown";
            }
        }

        /// <summary>
        /// Approved production order as a 1-based value (Desert = 1 … World Arena = 5).
        /// Returns 0 for an undefined theme.
        /// </summary>
        public static int GetProductionOrder(EnvironmentTheme theme)
        {
            return IsDefined(theme) ? (int)theme + 1 : 0;
        }

        /// <summary>
        /// Returns every ordering problem in a theme sequence: duplicates and missing themes.
        /// </summary>
        public static IReadOnlyList<string> ValidateThemeSet(IReadOnlyList<EnvironmentTheme> themes)
        {
            var issues = new List<string>();
            if (themes == null)
            {
                issues.Add("Theme set is null.");
                return issues;
            }

            var seen = new bool[AllThemes.Length];
            for (var index = 0; index < themes.Count; index++)
            {
                var theme = themes[index];
                if (!IsDefined(theme))
                {
                    issues.Add("Theme entry " + index + " has undefined value " + (int)theme + ".");
                    continue;
                }

                var themeIndex = (int)theme;
                if (seen[themeIndex])
                {
                    issues.Add("Duplicate theme '" + GetDisplayName(theme) + "' at entry " + index + ".");
                }

                seen[themeIndex] = true;
            }

            for (var i = 0; i < AllThemes.Length; i++)
            {
                if (!seen[i])
                {
                    issues.Add("Missing required theme '" + GetDisplayName(AllThemes[i]) + "'.");
                }
            }

            return issues;
        }
    }
}
