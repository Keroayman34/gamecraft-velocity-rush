#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic authoring entry point for the five Commercial V1.0 environment packages and
    /// their generated scenes (Phase 9 Stage 2/3, T-9.6–T-9.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The builder writes the approved layout, lighting, palette, and definition assets under
    /// <c>Assets/Data/Environments/&lt;Theme&gt;/&lt;id&gt;/</c>, validates the complete catalog,
    /// theme distinctness, and race-gate rules, then generates one scene per environment through
    /// <see cref="EnvironmentSceneBuilder"/>.
    /// </para>
    /// <para>
    /// Re-running the builder refreshes assets in place so GUIDs and scene references survive.
    /// Grips and hazards are presentation-only; no vehicle-physics value is written here (D-057).
    /// </para>
    /// </remarks>
    public static class EnvironmentContentBuilder
    {
        /// <summary>Folder that owns the five authored environment packages.</summary>
        public const string ContentFolder = EnvironmentCatalogBuilder.EnvironmentFolder;

        private const string LogPrefix = "[VelocityRush.EnvironmentContent] ";

        /// <summary>Menu entry for a full content + scene generation pass.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Environment Content", false, 29)]
        public static void BuildFromMenu()
        {
            try
            {
                var reports = BuildAll();
                Debug.Log(LogPrefix + "PASS — authored " + reports.Count +
                          " environment packages and generated their scenes.");
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Build failed: " + exception);
            }
        }

        /// <summary>Batch entry point: exits 1 on any authoring, validation, or scene failure.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                var reports = BuildAll();
                Debug.Log(LogPrefix + "PASS — authored " + reports.Count +
                          " environment packages and generated their scenes.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Build failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Authors every package, validates the catalog/distinctness/race gates, then generates the
        /// five scenes in approved production order.
        /// </summary>
        public static IReadOnlyList<EnvironmentBuildReport> BuildAll()
        {
            var specs = CreateSpecs();
            var definitions = new EnvironmentDefinition[specs.Length];
            var paths = new string[specs.Length];
            for (var i = 0; i < specs.Length; i++)
            {
                definitions[i] = Author(specs[i]);
                paths[i] = AssetDatabase.GetAssetPath(definitions[i]);
            }

            var issues = new List<string>(EnvironmentCatalogRules.ValidateCatalog(definitions));
            issues.AddRange(EnvironmentDistinctnessRules.ValidateThemeSeparation(definitions));
            for (var i = 0; i < definitions.Length; i++)
            {
                issues.AddRange(EnvironmentRaceRules.Validate(definitions[i].TrackLayout));
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Authored environment content failed validation:\n" + string.Join("\n", issues));
            }

            var reports = new List<EnvironmentBuildReport>(definitions.Length);
            for (var i = 0; i < paths.Length; i++)
            {
                // Building a scene performs asset import cycles that release handles to other
                // project assets. Reload each definition immediately before its build.
                var definition = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(paths[i]);
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        "Environment definition could not be reloaded: " + paths[i]);
                }

                reports.Add(EnvironmentSceneBuilder.Build(definition, true));
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return reports;
        }

        private static EnvironmentDefinition Author(EnvironmentSpec spec)
        {
            var folder = ContentFolder + "/" + spec.Theme + "/" + spec.Id;
            EnvironmentMaterialKit.EnsureFolder(folder);

            var layout = EnsureAsset<TrackLayout>(
                folder + "/" + spec.Id + "_TrackLayout.asset",
                asset => asset.ConfigureForAuthoring(
                    spec.RoadWidth,
                    spec.BarrierHeight,
                    spec.BarrierThickness,
                    1f,
                    false,
                    spec.Segments,
                    spec.ElevationKeys));

            var lighting = EnsureAsset<EnvironmentLightingProfile>(
                folder + "/" + spec.Id + "_Lighting.asset",
                asset => asset.ConfigureForAuthoring(
                    spec.SkyTint,
                    spec.GroundTint,
                    spec.AtmosphereThickness,
                    spec.SunSize,
                    spec.SunSizeConvergence,
                    spec.Exposure,
                    spec.AmbientIntensity,
                    spec.DirectionalColor,
                    spec.DirectionalIntensity,
                    spec.DirectionalEuler,
                    spec.ShadowsEnabled,
                    spec.ShadowStrength,
                    spec.FogEnabled,
                    spec.FogColor,
                    spec.FogStartDistance,
                    spec.FogEndDistance));

            if (!layout.TryValidate(out var layoutError))
            {
                throw new InvalidOperationException(spec.Id + " layout is invalid: " + layoutError);
            }

            if (!lighting.TryValidate(out var lightingError))
            {
                throw new InvalidOperationException(spec.Id + " lighting is invalid: " + lightingError);
            }

            return EnvironmentCatalogBuilder.CreateDefinition(
                spec.Id,
                spec.NameEn,
                spec.NameAr,
                spec.Theme,
                spec.Palette,
                lighting,
                layout,
                true);
        }

        private static T EnsureAsset<T>(string path, Action<T> configure)
            where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            configure(asset);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static EnvironmentSpec[] CreateSpecs()
        {
            return new[]
            {
                new EnvironmentSpec
                {
                    Id = "desert-signature",
                    NameEn = "Desert Circuit",
                    NameAr = "حلبة الصحراء",
                    Theme = EnvironmentTheme.DesertCircuit,
                    RoadWidth = 12f,
                    BarrierHeight = 0.9f,
                    BarrierThickness = 0.3f,
                    Segments = new[]
                    {
                        TrackSegment.Straight(120f),
                        TrackSegment.Arc(70f, 60f, 1),
                        TrackSegment.Straight(140f),
                        TrackSegment.Arc(60f, 55f, -1),
                        TrackSegment.Straight(160f),
                        TrackSegment.Arc(45f, 80f, 1),
                        TrackSegment.Straight(100f)
                    },
                    Palette = EnvironmentPalette.Create(
                        new Color(0.17f, 0.16f, 0.15f, 1f),
                        new Color(0.93f, 0.90f, 0.78f, 1f),
                        new Color(0.72f, 0.56f, 0.34f, 1f),
                        new Color(0.52f, 0.36f, 0.22f, 1f),
                        new Color(0.80f, 0.42f, 0.20f, 1f),
                        new Color(0.92f, 0.45f, 0.14f, 1f),
                        new Color(2.2f, 1.0f, 0.35f, 1f)),
                    SkyTint = new Color(0.52f, 0.66f, 0.86f, 1f),
                    GroundTint = new Color(0.55f, 0.44f, 0.30f, 1f),
                    AtmosphereThickness = 1.1f,
                    SunSize = 0.05f,
                    SunSizeConvergence = 0.5f,
                    Exposure = 1.35f,
                    AmbientIntensity = 1.15f,
                    DirectionalColor = new Color(1.0f, 0.96f, 0.86f, 1f),
                    DirectionalIntensity = 1.45f,
                    DirectionalEuler = new Vector3(55f, -35f, 0f),
                    ShadowsEnabled = true,
                    ShadowStrength = 0.85f,
                    FogEnabled = true,
                    FogColor = new Color(0.88f, 0.80f, 0.62f, 1f),
                    FogStartDistance = 240f,
                    FogEndDistance = 900f
                },
                new EnvironmentSpec
                {
                    Id = "coastal-signature",
                    NameEn = "Coastal Circuit",
                    NameAr = "حلبة الساحل",
                    Theme = EnvironmentTheme.CoastalCircuit,
                    RoadWidth = 11f,
                    BarrierHeight = 0.8f,
                    BarrierThickness = 0.3f,
                    Segments = new[]
                    {
                        TrackSegment.Straight(110f),
                        TrackSegment.Arc(40f, 90f, 1),
                        TrackSegment.Straight(90f),
                        TrackSegment.Arc(35f, 80f, -1),
                        TrackSegment.Straight(100f),
                        TrackSegment.Arc(50f, 100f, 1),
                        TrackSegment.Straight(100f),
                        TrackSegment.Arc(30f, 70f, -1),
                        TrackSegment.Straight(46f)
                    },
                    Palette = EnvironmentPalette.Create(
                        new Color(0.16f, 0.17f, 0.20f, 1f),
                        new Color(0.95f, 0.95f, 0.92f, 1f),
                        new Color(0.78f, 0.72f, 0.55f, 1f),
                        new Color(0.35f, 0.44f, 0.38f, 1f),
                        new Color(0.86f, 0.86f, 0.90f, 1f),
                        new Color(0.10f, 0.55f, 0.72f, 1f),
                        new Color(0.35f, 1.5f, 2.4f, 1f)),
                    SkyTint = new Color(0.78f, 0.62f, 0.52f, 1f),
                    GroundTint = new Color(0.30f, 0.36f, 0.40f, 1f),
                    AtmosphereThickness = 1.5f,
                    SunSize = 0.08f,
                    SunSizeConvergence = 0.6f,
                    Exposure = 1.25f,
                    AmbientIntensity = 1f,
                    DirectionalColor = new Color(1.0f, 0.78f, 0.55f, 1f),
                    DirectionalIntensity = 1.2f,
                    DirectionalEuler = new Vector3(22f, -115f, 0f),
                    ShadowsEnabled = true,
                    ShadowStrength = 0.7f,
                    FogEnabled = true,
                    FogColor = new Color(0.72f, 0.66f, 0.64f, 1f),
                    FogStartDistance = 140f,
                    FogEndDistance = 620f
                },
                new EnvironmentSpec
                {
                    Id = "mountain-pass-signature",
                    NameEn = "Mountain Pass",
                    NameAr = "ممر الجبل",
                    Theme = EnvironmentTheme.MountainPass,
                    RoadWidth = 8f,
                    BarrierHeight = 0.9f,
                    BarrierThickness = 0.3f,
                    Segments = new[]
                    {
                        TrackSegment.Straight(80f),
                        TrackSegment.Arc(50f, 40f, 1),
                        TrackSegment.Straight(70f),
                        TrackSegment.Arc(45f, 35f, -1),
                        TrackSegment.Straight(60f),
                        TrackSegment.Arc(55f, 45f, 1),
                        TrackSegment.Straight(60f),
                        TrackSegment.Arc(40f, 40f, -1),
                        TrackSegment.Straight(104f)
                    },
                    ElevationKeys = new[]
                    {
                        TrackElevationKey.Create(0f, 0f),
                        TrackElevationKey.Create(120f, 8f),
                        TrackElevationKey.Create(250f, 18f),
                        TrackElevationKey.Create(380f, 10f),
                        TrackElevationKey.Create(500f, 22f)
                    },
                    Palette = EnvironmentPalette.Create(
                        new Color(0.20f, 0.21f, 0.22f, 1f),
                        new Color(0.94f, 0.95f, 0.97f, 1f),
                        new Color(0.86f, 0.90f, 0.94f, 1f),
                        new Color(0.30f, 0.34f, 0.31f, 1f),
                        new Color(0.58f, 0.60f, 0.62f, 1f),
                        new Color(0.18f, 0.48f, 0.30f, 1f),
                        new Color(1.2f, 1.6f, 2.0f, 1f)),
                    SkyTint = new Color(0.38f, 0.46f, 0.62f, 1f),
                    GroundTint = new Color(0.24f, 0.27f, 0.30f, 1f),
                    AtmosphereThickness = 2.0f,
                    SunSize = 0.03f,
                    SunSizeConvergence = 0.4f,
                    Exposure = 1.1f,
                    AmbientIntensity = 0.85f,
                    DirectionalColor = new Color(0.85f, 0.88f, 1.0f, 1f),
                    DirectionalIntensity = 0.9f,
                    DirectionalEuler = new Vector3(12f, 105f, 0f),
                    ShadowsEnabled = true,
                    ShadowStrength = 0.75f,
                    FogEnabled = true,
                    FogColor = new Color(0.58f, 0.64f, 0.72f, 1f),
                    FogStartDistance = 90f,
                    FogEndDistance = 460f
                },
                new EnvironmentSpec
                {
                    Id = "neon-city-signature",
                    NameEn = "Neon City",
                    NameAr = "مدينة النيون",
                    Theme = EnvironmentTheme.NeonCity,
                    RoadWidth = 9f,
                    BarrierHeight = 1f,
                    BarrierThickness = 0.3f,
                    Segments = new[]
                    {
                        TrackSegment.Straight(30f),
                        TrackSegment.Arc(90f, 20f, 1),
                        TrackSegment.Straight(40f),
                        TrackSegment.Arc(90f, 22f, -1),
                        TrackSegment.Straight(40f),
                        TrackSegment.Arc(85f, 20f, 1),
                        TrackSegment.Straight(40f),
                        TrackSegment.Arc(95f, 24f, -1),
                        TrackSegment.Straight(35f),
                        TrackSegment.Arc(90f, 20f, 1),
                        TrackSegment.Straight(40f),
                        TrackSegment.Arc(90f, 22f, -1),
                        TrackSegment.Straight(58.7f)
                    },
                    Palette = EnvironmentPalette.Create(
                        new Color(0.06f, 0.06f, 0.09f, 1f),
                        new Color(0.85f, 0.20f, 0.80f, 1f),
                        new Color(0.10f, 0.09f, 0.13f, 1f),
                        new Color(0.17f, 0.14f, 0.22f, 1f),
                        new Color(0.22f, 0.20f, 0.30f, 1f),
                        new Color(0.95f, 0.12f, 0.55f, 1f),
                        new Color(3.0f, 0.20f, 2.2f, 1f)),
                    SkyTint = new Color(0.03f, 0.03f, 0.07f, 1f),
                    GroundTint = new Color(0.05f, 0.04f, 0.08f, 1f),
                    AtmosphereThickness = 0.35f,
                    SunSize = 0f,
                    SunSizeConvergence = 0.3f,
                    Exposure = 0.85f,
                    AmbientIntensity = 0.45f,
                    DirectionalColor = new Color(0.55f, 0.62f, 1.0f, 1f),
                    DirectionalIntensity = 0.35f,
                    DirectionalEuler = new Vector3(35f, -140f, 0f),
                    ShadowsEnabled = true,
                    ShadowStrength = 0.5f,
                    FogEnabled = true,
                    FogColor = new Color(0.05f, 0.04f, 0.09f, 1f),
                    FogStartDistance = 120f,
                    FogEndDistance = 520f
                },
                new EnvironmentSpec
                {
                    Id = "world-arena-signature",
                    NameEn = "World Arena",
                    NameAr = "الساحة العالمية",
                    Theme = EnvironmentTheme.WorldArena,
                    RoadWidth = 10f,
                    BarrierHeight = 1.1f,
                    BarrierThickness = 0.32f,
                    Segments = new[]
                    {
                        TrackSegment.Straight(90f),
                        TrackSegment.Arc(60f, 50f, 1),
                        TrackSegment.Straight(100f),
                        TrackSegment.Arc(45f, 60f, -1),
                        TrackSegment.Straight(110f),
                        TrackSegment.Arc(70f, 40f, 1),
                        TrackSegment.Straight(100f),
                        TrackSegment.Arc(40f, 55f, -1),
                        TrackSegment.Straight(20f)
                    },
                    Palette = EnvironmentPalette.Create(
                        new Color(0.18f, 0.19f, 0.22f, 1f),
                        new Color(0.96f, 0.96f, 0.90f, 1f),
                        new Color(0.20f, 0.42f, 0.24f, 1f),
                        new Color(0.34f, 0.28f, 0.20f, 1f),
                        new Color(0.85f, 0.85f, 0.88f, 1f),
                        new Color(0.95f, 0.80f, 0.12f, 1f),
                        new Color(2.6f, 2.4f, 0.6f, 1f)),
                    SkyTint = new Color(0.07f, 0.09f, 0.16f, 1f),
                    GroundTint = new Color(0.10f, 0.13f, 0.10f, 1f),
                    AtmosphereThickness = 0.5f,
                    SunSize = 0f,
                    SunSizeConvergence = 0.3f,
                    Exposure = 1f,
                    AmbientIntensity = 0.75f,
                    DirectionalColor = new Color(0.92f, 0.94f, 1.0f, 1f),
                    DirectionalIntensity = 0.9f,
                    DirectionalEuler = new Vector3(58f, -20f, 0f),
                    ShadowsEnabled = true,
                    ShadowStrength = 0.65f,
                    FogEnabled = true,
                    FogColor = new Color(0.08f, 0.10f, 0.14f, 1f),
                    FogStartDistance = 180f,
                    FogEndDistance = 700f
                }
            };
        }
    }

    /// <summary>Owner-approved authoring data for one environment package.</summary>
    internal sealed class EnvironmentSpec
    {
        public string Id;
        public string NameEn;
        public string NameAr;
        public EnvironmentTheme Theme;
        public float RoadWidth;
        public float BarrierHeight;
        public float BarrierThickness;
        public TrackSegment[] Segments;
        public TrackElevationKey[] ElevationKeys = Array.Empty<TrackElevationKey>();
        public EnvironmentPalette Palette;

        public Color SkyTint;
        public Color GroundTint;
        public float AtmosphereThickness;
        public float SunSize;
        public float SunSizeConvergence;
        public float Exposure;
        public float AmbientIntensity;
        public Color DirectionalColor;
        public float DirectionalIntensity;
        public Vector3 DirectionalEuler;
        public bool ShadowsEnabled;
        public float ShadowStrength;
        public bool FogEnabled;
        public Color FogColor;
        public float FogStartDistance;
        public float FogEndDistance;
    }
}
#endif
