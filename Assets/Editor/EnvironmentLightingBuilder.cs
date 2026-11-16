#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using VelocityRush.Data;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic realtime-only lighting builder for one environment (T-9.4, D-056).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Applies a procedural skybox, skybox ambient, one realtime directional light, and linear
    /// fog. No lightmap, lighting-data, reflection-probe, or bake-worker dependency is created or
    /// required. Every value comes from the authored <see cref="EnvironmentLightingProfile"/>.
    /// </para>
    /// <para>
    /// The builder is intentionally a single pass: no sampling, no time-of-day interpolation, and
    /// no random source, so the same profile always produces the same scene result.
    /// </para>
    /// </remarks>
    public static class EnvironmentLightingBuilder
    {
        /// <summary>Built-in procedural skybox shader name.</summary>
        public const string SkyboxShaderName = "Skybox/Procedural";

        /// <summary>Project-relative path of the generated skybox material.</summary>
        public static string GetSkyboxPath(EnvironmentDefinition definition)
        {
            return EnvironmentMaterialKit.GetEnvironmentFolder(definition.EnvironmentId) +
                   "/" + definition.EnvironmentId + "_Skybox.mat";
        }

        /// <summary>
        /// Applies the definition's lighting profile to the open scene and creates its directional
        /// light under <paramref name="parent"/>.
        /// </summary>
        public static Light Apply(EnvironmentDefinition definition, Transform parent)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!definition.TryValidate(out var definitionError))
            {
                throw new InvalidOperationException("Environment definition is invalid: " + definitionError);
            }

            var profile = definition.LightingProfile;
            if (!profile.TryValidate(out var profileError))
            {
                throw new InvalidOperationException("Lighting profile is invalid: " + profileError);
            }

            var settings = profile.BuildSettings();
            EnvironmentMaterialKit.EnsureFolder(
                EnvironmentMaterialKit.GetEnvironmentFolder(definition.EnvironmentId));

            var skybox = EnsureSkyboxMaterial(profile, GetSkyboxPath(definition));
            var light = CreateDirectionalLight(
                settings,
                parent,
                definition.DisplayNameEn + " Sun");

            ApplyRenderSettings(settings, skybox, light);
            DisableBaking();
            return light;
        }

        /// <summary>Creates or refreshes the procedural skybox material.</summary>
        public static Material EnsureSkyboxMaterial(
            EnvironmentLightingProfile profile,
            string assetPath)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                throw new ArgumentException("Skybox asset path must not be empty.", nameof(assetPath));
            }

            var shader = Shader.Find(SkyboxShaderName);
            if (shader == null)
            {
                throw new InvalidOperationException(
                    "Built-in " + SkyboxShaderName + " shader is unavailable.");
            }

            var settings = profile.BuildSettings();
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = profile.name + "_Skybox"
                };
                AssetDatabase.CreateAsset(material, assetPath);
            }

            material.shader = shader;
            material.SetColor("_SkyTint", settings.SkyTint);
            material.SetColor("_GroundColor", settings.GroundColor);
            material.SetFloat("_AtmosphereThickness", settings.AtmosphereThickness);
            material.SetFloat("_SunSize", settings.SunSize);
            material.SetFloat("_SunSizeConvergence", settings.SunSizeConvergence);
            material.SetFloat("_Exposure", settings.Exposure);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Creates the single realtime directional light from the profile values.</summary>
        public static Light CreateDirectionalLight(
            EnvironmentLightingSettings settings,
            Transform parent,
            string lightName)
        {
            var lightGo = new GameObject(lightName);
            if (parent != null)
            {
                lightGo.transform.SetParent(parent, false);
            }

            lightGo.transform.rotation = Quaternion.Euler(settings.DirectionalEuler);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.color = settings.DirectionalColor;
            light.intensity = settings.DirectionalIntensity;
            light.shadows = settings.ShadowsEnabled ? LightShadows.Soft : LightShadows.None;
            light.shadowStrength = settings.ShadowStrength;
            light.shadowBias = 0.05f;
            return light;
        }

        /// <summary>Writes the realtime render settings for the open scene.</summary>
        public static void ApplyRenderSettings(
            EnvironmentLightingSettings settings,
            Material skybox,
            Light sun)
        {
            RenderSettings.skybox = skybox;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientIntensity = settings.AmbientIntensity;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
            RenderSettings.reflectionIntensity = 1f;

            RenderSettings.fog = settings.FogEnabled;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = settings.FogColor;
            RenderSettings.fogStartDistance = settings.FogStartDistance;
            RenderSettings.fogEndDistance = settings.FogEndDistance;
        }

        /// <summary>Explicitly disables baked and realtime GI for the open scene.</summary>
        public static void DisableBaking()
        {
            Lightmapping.lightingSettings = null;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
        }
    }
}
#endif
