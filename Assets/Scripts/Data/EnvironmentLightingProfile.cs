using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure, deterministic lighting settings derived from an <see cref="EnvironmentLightingProfile"/>.
    /// </summary>
    /// <remarks>
    /// This struct is the single hand-off between data and the editor scene builder. It contains
    /// no engine objects and no baked-lighting state, so it can be asserted directly in EditMode.
    /// </remarks>
    public readonly struct EnvironmentLightingSettings
    {
        public EnvironmentLightingSettings(
            Color skyTint,
            Color groundColor,
            float atmosphereThickness,
            float sunSize,
            float sunSizeConvergence,
            float exposure,
            float ambientIntensity,
            Color directionalColor,
            float directionalIntensity,
            Vector3 directionalEuler,
            bool shadowsEnabled,
            float shadowStrength,
            bool fogEnabled,
            Color fogColor,
            float fogStartDistance,
            float fogEndDistance)
        {
            SkyTint = skyTint;
            GroundColor = groundColor;
            AtmosphereThickness = atmosphereThickness;
            SunSize = sunSize;
            SunSizeConvergence = sunSizeConvergence;
            Exposure = exposure;
            AmbientIntensity = ambientIntensity;
            DirectionalColor = directionalColor;
            DirectionalIntensity = directionalIntensity;
            DirectionalEuler = directionalEuler;
            ShadowsEnabled = shadowsEnabled;
            ShadowStrength = shadowStrength;
            FogEnabled = fogEnabled;
            FogColor = fogColor;
            FogStartDistance = fogStartDistance;
            FogEndDistance = fogEndDistance;
        }

        /// <summary>Procedural skybox tint.</summary>
        public Color SkyTint { get; }

        /// <summary>Procedural skybox ground colour.</summary>
        public Color GroundColor { get; }

        /// <summary>Procedural skybox atmosphere thickness.</summary>
        public float AtmosphereThickness { get; }

        /// <summary>Procedural skybox sun disc size.</summary>
        public float SunSize { get; }

        /// <summary>Procedural skybox sun size convergence.</summary>
        public float SunSizeConvergence { get; }

        /// <summary>Procedural skybox exposure.</summary>
        public float Exposure { get; }

        /// <summary>Skybox-based ambient intensity.</summary>
        public float AmbientIntensity { get; }

        /// <summary>Directional-light colour.</summary>
        public Color DirectionalColor { get; }

        /// <summary>Directional-light intensity.</summary>
        public float DirectionalIntensity { get; }

        /// <summary>Directional-light Euler rotation.</summary>
        public Vector3 DirectionalEuler { get; }

        /// <summary>True when the directional light casts shadows.</summary>
        public bool ShadowsEnabled { get; }

        /// <summary>Directional-light shadow strength.</summary>
        public float ShadowStrength { get; }

        /// <summary>True when linear fog is enabled.</summary>
        public bool FogEnabled { get; }

        /// <summary>Linear fog colour.</summary>
        public Color FogColor { get; }

        /// <summary>Linear fog start distance.</summary>
        public float FogStartDistance { get; }

        /// <summary>Linear fog end distance.</summary>
        public float FogEndDistance { get; }
    }

    /// <summary>
    /// Owner-approved realtime-only lighting profile for one environment theme (T-9.4, D-056).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Phase 9 uses **realtime lighting only** (OQ-A10 resolved 2026-11-16). The profile drives a
    /// procedural skybox, skybox ambient, one realtime directional light, and linear fog; it has
    /// no baked-GI, lightmap, or reflection-probe dependency. This removes the bake-worker
    /// blocker recorded for the Phase-7 slice (D-048).
    /// </para>
    /// <para>
    /// Every value is authored data; <see cref="BuildSettings"/> is a pure, deterministic
    /// projection so the scene result is reproducible and EditMode-testable.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Environment Lighting Profile", fileName = "EnvironmentLightingProfile")]
    public sealed class EnvironmentLightingProfile : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Phase-9 lighting is realtime-only; no bake is supported by this contract.</summary>
        public const bool SupportsBakedLighting = false;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Header("Procedural skybox")]
        [SerializeField] private Color skyTint = new Color(0.45f, 0.55f, 0.72f, 1f);
        [SerializeField] private Color groundColor = new Color(0.32f, 0.28f, 0.22f, 1f);
        [SerializeField, Range(0.1f, 5f)] private float atmosphereThickness = 1.1f;
        [SerializeField, Range(0f, 1f)] private float sunSize = 0.04f;
        [SerializeField, Range(0f, 1f)] private float sunSizeConvergence = 0.5f;
        [SerializeField, Range(0.1f, 8f)] private float exposure = 1.3f;

        [Header("Ambient (skybox)")]
        [SerializeField, Range(0f, 3f)] private float ambientIntensity = 1f;

        [Header("Realtime directional light")]
        [SerializeField] private Color directionalColor = new Color(1f, 0.96f, 0.88f, 1f);
        [SerializeField, Range(0f, 8f)] private float directionalIntensity = 1.2f;
        [SerializeField] private Vector3 directionalEuler = new Vector3(48f, -30f, 0f);
        [SerializeField] private bool shadowsEnabled = true;
        [SerializeField, Range(0f, 1f)] private float shadowStrength = 0.8f;

        [Header("Linear fog")]
        [SerializeField] private bool fogEnabled = true;
        [SerializeField] private Color fogColor = new Color(0.70f, 0.72f, 0.75f, 1f);
        [SerializeField, Min(0f)] private float fogStartDistance = 180f;
        [SerializeField, Min(1f)] private float fogEndDistance = 700f;

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Procedural skybox tint.</summary>
        public Color SkyTint => skyTint;

        /// <summary>Procedural skybox ground colour.</summary>
        public Color GroundColor => groundColor;

        /// <summary>Directional-light colour.</summary>
        public Color DirectionalColor => directionalColor;

        /// <summary>Linear fog colour.</summary>
        public Color FogColor => fogColor;

        /// <summary>
        /// Pure projection of the authored profile into engine-ready values. The method reads no
        /// scene state, clock, or random source, so repeated calls are byte-for-byte equivalent.
        /// </summary>
        public EnvironmentLightingSettings BuildSettings()
        {
            return new EnvironmentLightingSettings(
                skyTint,
                groundColor,
                atmosphereThickness,
                sunSize,
                sunSizeConvergence,
                exposure,
                ambientIntensity,
                directionalColor,
                directionalIntensity,
                directionalEuler,
                shadowsEnabled,
                shadowStrength,
                fogEnabled,
                fogColor,
                fogStartDistance,
                fogEndDistance);
        }

        /// <summary>Validates the complete profile and returns the first issue.</summary>
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

        /// <summary>Returns every profile problem at once (cold authoring/validation path).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Validate()
        {
            var issues = new System.Collections.Generic.List<string>();
            if (schemaVersion != CurrentSchemaVersion)
            {
                issues.Add("EnvironmentLightingProfile: unsupported SchemaVersion " + schemaVersion +
                           "; expected " + CurrentSchemaVersion + ".");
            }

            ValidateColor(skyTint, "SkyTint", 8f, issues);
            ValidateColor(groundColor, "GroundColor", 8f, issues);
            ValidateColor(directionalColor, "DirectionalColor", 8f, issues);
            ValidateColor(fogColor, "FogColor", 8f, issues);

            ValidateRange(atmosphereThickness, 0.1f, 5f, "AtmosphereThickness", issues);
            ValidateRange(sunSize, 0f, 1f, "SunSize", issues);
            ValidateRange(sunSizeConvergence, 0f, 1f, "SunSizeConvergence", issues);
            ValidateRange(exposure, 0.1f, 8f, "Exposure", issues);
            ValidateRange(ambientIntensity, 0f, 3f, "AmbientIntensity", issues);
            ValidateRange(directionalIntensity, 0f, 8f, "DirectionalIntensity", issues);
            ValidateRange(shadowStrength, 0f, 1f, "ShadowStrength", issues);

            if (IsInvalid(directionalEuler.x) || IsInvalid(directionalEuler.y) ||
                IsInvalid(directionalEuler.z))
            {
                issues.Add("EnvironmentLightingProfile: DirectionalEuler must be finite.");
            }

            if (IsInvalid(fogStartDistance) || fogStartDistance < 0f)
            {
                issues.Add("EnvironmentLightingProfile: FogStartDistance must be finite and non-negative.");
            }

            if (IsInvalid(fogEndDistance) || fogEndDistance <= fogStartDistance)
            {
                issues.Add("EnvironmentLightingProfile: FogEndDistance must be finite and greater " +
                           "than FogStartDistance.");
            }

            return issues;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only authoring seam used by deterministic builders and tests.</summary>
        public void ConfigureForAuthoring(
            Color sky,
            Color ground,
            float atmosphere,
            float sun,
            float sunConvergence,
            float skyExposure,
            float ambient,
            Color directional,
            float directionalPower,
            Vector3 directionalRotation,
            bool shadows,
            float shadowPower,
            bool fog,
            Color fogTint,
            float fogStart,
            float fogEnd)
        {
            schemaVersion = CurrentSchemaVersion;
            skyTint = sky;
            groundColor = ground;
            atmosphereThickness = atmosphere;
            sunSize = sun;
            sunSizeConvergence = sunConvergence;
            exposure = skyExposure;
            ambientIntensity = ambient;
            directionalColor = directional;
            directionalIntensity = directionalPower;
            directionalEuler = directionalRotation;
            shadowsEnabled = shadows;
            shadowStrength = shadowPower;
            fogEnabled = fog;
            fogColor = fogTint;
            fogStartDistance = fogStart;
            fogEndDistance = fogEnd;
        }
#endif

        private static void ValidateColor(
            Color color,
            string fieldName,
            float maxRgb,
            System.Collections.Generic.List<string> issues)
        {
            if (IsInvalid(color.r) || IsInvalid(color.g) || IsInvalid(color.b) || IsInvalid(color.a))
            {
                issues.Add("EnvironmentLightingProfile: " + fieldName + " has a non-finite channel.");
                return;
            }

            if (color.r < 0f || color.g < 0f || color.b < 0f ||
                color.r > maxRgb || color.g > maxRgb || color.b > maxRgb)
            {
                issues.Add("EnvironmentLightingProfile: " + fieldName +
                           " RGB channels must be within 0.." + maxRgb + ".");
            }

            if (color.a < 0f || color.a > 1f)
            {
                issues.Add("EnvironmentLightingProfile: " + fieldName + " alpha must be within 0..1.");
            }
        }

        private static void ValidateRange(
            float value,
            float minimum,
            float maximum,
            string fieldName,
            System.Collections.Generic.List<string> issues)
        {
            if (IsInvalid(value))
            {
                issues.Add("EnvironmentLightingProfile: " + fieldName + " must be finite.");
                return;
            }

            if (value < minimum || value > maximum)
            {
                issues.Add("EnvironmentLightingProfile: " + fieldName + " must be within " +
                           minimum + ".." + maximum + ".");
            }
        }

        private static bool IsInvalid(float value)
        {
            return float.IsNaN(value) || float.IsInfinity(value);
        }
    }
}
