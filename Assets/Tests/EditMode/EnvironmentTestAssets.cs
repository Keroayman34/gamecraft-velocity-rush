using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Deterministic test fixtures for the Phase-9 environment contracts. Every created asset or
    /// scene object is tracked and destroyed in <see cref="Dispose"/>.
    /// </summary>
    internal sealed class EnvironmentTestAssets : IDisposable
    {
        private readonly List<Object> created = new List<Object>();

        public static EnvironmentPalette DefaultPalette()
        {
            return EnvironmentPalette.Create(
                new Color(0.16f, 0.16f, 0.18f, 1f),
                new Color(0.92f, 0.92f, 0.86f, 1f),
                new Color(0.55f, 0.45f, 0.32f, 1f),
                new Color(0.35f, 0.30f, 0.24f, 1f),
                new Color(0.72f, 0.72f, 0.74f, 1f),
                new Color(0.85f, 0.35f, 0.12f, 1f),
                new Color(2f, 0.7f, 0.2f, 1f));
        }

        public TrackLayout CreateLayout(
            TrackSegment[] segments,
            TrackElevationKey[] elevationKeys = null,
            float roadWidth = 10f,
            float barrierHeight = 0.8f,
            float barrierThickness = 0.3f,
            float sampleSpacing = 1f,
            bool closedLoop = false)
        {
            var layout = ScriptableObject.CreateInstance<TrackLayout>();
            layout.ConfigureForAuthoring(
                roadWidth,
                barrierHeight,
                barrierThickness,
                sampleSpacing,
                closedLoop,
                segments,
                elevationKeys);
            created.Add(layout);
            return layout;
        }

        public TrackLayout CreateDefaultLayout(TrackElevationKey[] elevationKeys = null)
        {
            return CreateLayout(
                new[]
                {
                    TrackSegment.Straight(100f),
                    TrackSegment.Arc(90f, 20f, -1),
                    TrackSegment.Straight(100f)
                },
                elevationKeys);
        }

        public EnvironmentLightingProfile CreateLighting(
            Color? skyTint = null,
            Color? groundColor = null,
            float atmosphereThickness = 1.1f,
            float sunSize = 0.04f,
            float sunSizeConvergence = 0.5f,
            float exposure = 1.3f,
            float ambientIntensity = 1f,
            Color? directionalColor = null,
            float directionalIntensity = 1.2f,
            Vector3? directionalEuler = null,
            bool shadowsEnabled = true,
            float shadowStrength = 0.8f,
            bool fogEnabled = true,
            Color? fogColor = null,
            float fogStartDistance = 180f,
            float fogEndDistance = 700f)
        {
            var profile = ScriptableObject.CreateInstance<EnvironmentLightingProfile>();
            profile.ConfigureForAuthoring(
                skyTint ?? new Color(0.45f, 0.55f, 0.72f, 1f),
                groundColor ?? new Color(0.32f, 0.28f, 0.22f, 1f),
                atmosphereThickness,
                sunSize,
                sunSizeConvergence,
                exposure,
                ambientIntensity,
                directionalColor ?? new Color(1f, 0.96f, 0.88f, 1f),
                directionalIntensity,
                directionalEuler ?? new Vector3(48f, -30f, 0f),
                shadowsEnabled,
                shadowStrength,
                fogEnabled,
                fogColor ?? new Color(0.70f, 0.72f, 0.75f, 1f),
                fogStartDistance,
                fogEndDistance);
            created.Add(profile);
            return profile;
        }

        public EnvironmentDefinition CreateDefinition(
            EnvironmentTheme theme = EnvironmentTheme.DesertCircuit,
            string environmentId = null,
            string displayNameEn = "Test Environment",
            string displayNameAr = "بيئة اختبار",
            int? productionOrder = null,
            EnvironmentPalette? palette = null,
            EnvironmentLightingProfile lightingProfile = null,
            TrackLayout trackLayout = null,
            bool missingLighting = false,
            bool missingLayout = false)
        {
            var id = environmentId ??
                     EnvironmentThemeRules.GetStableFragment(theme) + "-test";
            var definition = ScriptableObject.CreateInstance<EnvironmentDefinition>();
            definition.ConfigureForAuthoring(
                id,
                displayNameEn,
                displayNameAr,
                theme,
                productionOrder ?? EnvironmentThemeRules.GetProductionOrder(theme),
                palette ?? DefaultPalette(),
                missingLighting ? null : (lightingProfile ?? CreateLighting()),
                missingLayout ? null : (trackLayout ?? CreateDefaultLayout()));
            created.Add(definition);
            return definition;
        }

        public EnvironmentCatalog CreateCatalog(EnvironmentDefinition[] definitions)
        {
            var go = new GameObject("Test Environment Catalog");
            created.Add(go);
            var catalog = go.AddComponent<EnvironmentCatalog>();
            catalog.ConfigureForAuthoring(definitions);
            return catalog;
        }

        public EnvironmentSceneContext CreateSceneContext(EnvironmentDefinition definition)
        {
            var go = new GameObject("Test Environment Context");
            created.Add(go);
            var context = go.AddComponent<EnvironmentSceneContext>();
            context.ConfigureForAuthoring(definition);
            return context;
        }

        public static EnvironmentDefinition[] CreateFiveThemeDefinitions(EnvironmentTestAssets assets)
        {
            var themes = EnvironmentThemeRules.Themes;
            var definitions = new EnvironmentDefinition[themes.Count];
            for (var i = 0; i < themes.Count; i++)
            {
                definitions[i] = assets.CreateDefinition(
                    themes[i],
                    EnvironmentThemeRules.GetStableFragment(themes[i]) + "-test",
                    EnvironmentThemeRules.GetDisplayName(themes[i]),
                    EnvironmentThemeRules.GetDisplayName(themes[i]) + " AR");
            }

            return definitions;
        }

        public static void SetSerializedInt(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Missing property '" + propertyName + "'.");
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public void Dispose()
        {
            for (var i = created.Count - 1; i >= 0; i--)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }
    }
}
