using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Per-theme colour palette for the shared environment kit (T-9.1/T-9.3, D-054/D-055).
    /// </summary>
    /// <remarks>
    /// The palette is presentation data only. It contains no surface-grip, hazard, or vehicle
    /// physics values: grip and hazards stay visual-only for Phase 9 (D-057), so the Phase-8
    /// physics profiles and the approved D-052 balance are untouched.
    /// </remarks>
    [Serializable]
    public struct EnvironmentPalette
    {
        [Tooltip("Main road surface colour.")]
        [SerializeField] private Color roadSurface;

        [Tooltip("Edge-line colour. Must read against the road surface at gameplay speed.")]
        [SerializeField] private Color roadEdge;

        [Tooltip("Primary ground/terrain colour.")]
        [SerializeField] private Color groundPrimary;

        [Tooltip("Secondary ground detail colour (rocks, verges, distant terrain).")]
        [SerializeField] private Color groundSecondary;

        [Tooltip("Barrier / track-furniture colour.")]
        [SerializeField] private Color barrier;

        [Tooltip("Signature accent colour for landmarks and static decor.")]
        [SerializeField] private Color accent;

        [Tooltip("Emissive colour for signage and night lighting. Values above 1 are HDR.")]
        [SerializeField] private Color emissive;

        /// <summary>Main road surface colour.</summary>
        public Color RoadSurface => roadSurface;

        /// <summary>Edge-line colour.</summary>
        public Color RoadEdge => roadEdge;

        /// <summary>Primary ground colour.</summary>
        public Color GroundPrimary => groundPrimary;

        /// <summary>Secondary ground/detail colour.</summary>
        public Color GroundSecondary => groundSecondary;

        /// <summary>Barrier colour.</summary>
        public Color Barrier => barrier;

        /// <summary>Signature accent colour.</summary>
        public Color Accent => accent;

        /// <summary>Emissive colour (HDR allowed).</summary>
        public Color Emissive => emissive;

        /// <summary>Creates a palette from explicit colours.</summary>
        public static EnvironmentPalette Create(
            Color roadSurface,
            Color roadEdge,
            Color groundPrimary,
            Color groundSecondary,
            Color barrier,
            Color accent,
            Color emissive)
        {
            return new EnvironmentPalette
            {
                roadSurface = roadSurface,
                roadEdge = roadEdge,
                groundPrimary = groundPrimary,
                groundSecondary = groundSecondary,
                barrier = barrier,
                accent = accent,
                emissive = emissive
            };
        }

        /// <summary>
        /// Validates every colour channel, the RGB range, and the road/edge contrast needed for
        /// gameplay readability. Returns the first issue with a field-specific message.
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

        /// <summary>Returns every palette problem at once (cold authoring/validation path).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Validate()
        {
            var issues = new System.Collections.Generic.List<string>();
            ValidateColor(roadSurface, "RoadSurface", 8f, issues);
            ValidateColor(roadEdge, "RoadEdge", 8f, issues);
            ValidateColor(groundPrimary, "GroundPrimary", 8f, issues);
            ValidateColor(groundSecondary, "GroundSecondary", 8f, issues);
            ValidateColor(barrier, "Barrier", 8f, issues);
            ValidateColor(accent, "Accent", 8f, issues);
            ValidateColor(emissive, "Emissive", 32f, issues);

            if (issues.Count == 0)
            {
                var contrast = Mathf.Abs(Luminance(roadEdge) - Luminance(roadSurface));
                if (contrast < 0.08f)
                {
                    issues.Add("EnvironmentPalette: RoadEdge must contrast with RoadSurface " +
                               "(relative luminance delta " + contrast.ToString("0.000") +
                               ", minimum 0.080).");
                }
            }

            return issues;
        }

        private static void ValidateColor(
            Color color,
            string fieldName,
            float maxRgb,
            System.Collections.Generic.List<string> issues)
        {
            if (IsInvalid(color.r) || IsInvalid(color.g) || IsInvalid(color.b))
            {
                issues.Add("EnvironmentPalette: " + fieldName + " has a non-finite RGB channel.");
                return;
            }

            if (color.r < 0f || color.g < 0f || color.b < 0f ||
                color.r > maxRgb || color.g > maxRgb || color.b > maxRgb)
            {
                issues.Add("EnvironmentPalette: " + fieldName +
                           " RGB channels must be within 0.." + maxRgb + ".");
            }

            if (IsInvalid(color.a))
            {
                issues.Add("EnvironmentPalette: " + fieldName + " has a non-finite alpha channel.");
                return;
            }

            if (color.a < 0f || color.a > 1f)
            {
                issues.Add("EnvironmentPalette: " + fieldName + " alpha must be within 0..1.");
            }
        }

        private static bool IsInvalid(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }

        private static float Luminance(Color color)
        {
            return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        }
    }
}
