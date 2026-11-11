#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Bakes the Desert Circuit GI and applies the procedural skybox/linear fog presentation
    /// required by the D-006 slice bar (T-7.8).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Baking is a separate, explicit step (menu or batch entry point) because it is slow and
    /// because GPU lightmapping may be unavailable under <c>-nographics</c>. The baker first tries
    /// the compute GPU lightmapper and falls back to the CPU progressive lightmapper; it only
    /// reports success when a lighting-data asset and at least one lightmap were produced.
    /// </para>
    /// <para>
    /// Built-in Render Pipeline only (D-032). The scene stays out of <c>EditorBuildSettings</c>;
    /// this tool opens and saves only <c>HandlingCourse.unity</c>.
    /// </para>
    /// </remarks>
    public static class SliceLightingBaker
    {
        public const string ScenePath = "Assets/Scenes/HandlingCourse.unity";
        public const string SkyboxPath = SliceTextureKit.GeneratedFolder + "/DesertSkybox.mat";

        private const string LogPrefix = "[VelocityRush.SliceLighting] ";

        /// <summary>Bake-menu entry with an explicit confirmation prompt.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Bake Handling Course Lighting", false, 40)]
        public static void BakeFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Bake handling course lighting?",
                    "This opens " + ScenePath + ", bakes GI (GPU with CPU fallback), and saves the scene. " +
                    "Other scenes and build settings are not touched.",
                    "Bake",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Bake();
        }

        /// <summary>Batch entry point: exits 0 when baked, 2 when baking is unavailable.</summary>
        public static void BakeFromCommandLine()
        {
            try
            {
                var baked = Bake();
                EditorApplication.Exit(baked ? 0 : 2);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Bake failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Opens the slice scene, applies presentation, bakes GI, and saves the scene.</summary>
        /// <returns>True only when lightmaps were produced.</returns>
        public static bool Bake()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Refresh the committed settings asset so a rebuild cannot bake against stale quality.
            SliceAssetBuilder.EnsureSliceAssets();

            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(SliceAssetBuilder.LightingSettingsPath);
            if (settings == null)
            {
                throw new InvalidOperationException("Missing lighting settings: " + SliceAssetBuilder.LightingSettingsPath);
            }

            var skybox = EnsureSkyboxMaterial();
            RenderSettings.skybox = skybox;
            RenderSettings.sun = FindDirectionalLight();
            Lightmapping.lightingSettings = settings;

            var baked = TryBake(settings, LightingSettings.Lightmapper.UnityComputeGPU);
            if (!baked)
            {
                Debug.LogWarning(
                    LogPrefix + "Compute GPU lightmapper unavailable; retrying with the CPU progressive lightmapper.",
                    settings);

                // Unity 6 marks the CPU progressive enum obsolete in favour of the Unity Compute
                // lightmapper, but the CPU path is the only fallback available in a headless
                // (-nographics) editor. The suppression is scoped to this single fallback.
#pragma warning disable CS0618
                baked = TryBake(settings, LightingSettings.Lightmapper.ProgressiveCPU);
#pragma warning restore CS0618
            }

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath + " after baking.");
            }

            AssetDatabase.SaveAssets();

            var lightmapCount = LightmapSettings.lightmaps != null ? LightmapSettings.lightmaps.Length : 0;
            var hasData = Lightmapping.lightingDataAsset != null;

            if (baked && hasData && lightmapCount > 0)
            {
                Debug.Log(
                    LogPrefix + "Bake complete | lightmaps=" + lightmapCount +
                    " | lightingData=" + (hasData ? "yes" : "no") +
                    " | lightmapper=" + settings.lightmapper,
                    settings);
                return true;
            }

            Debug.LogError(
                LogPrefix + "Bake did not produce lightmaps (lightmaps=" + lightmapCount +
                ", lightingData=" + (hasData ? "yes" : "no") + "). " +
                "Re-run the bake in a GPU-capable editor via the Bake Handling Course Lighting menu.",
                settings);
            return false;
        }

        /// <summary>Creates or refreshes the procedural desert skybox material.</summary>
        public static Material EnsureSkyboxMaterial()
        {
            var shader = Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                throw new InvalidOperationException("Built-in Skybox/Procedural shader is unavailable.");
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(SkyboxPath);
            if (material == null)
            {
                material = new Material(shader) { name = "DesertSkybox" };
                AssetDatabase.CreateAsset(material, SkyboxPath);
            }

            material.SetFloat("_SunDisk", 2f);
            material.SetFloat("_SunSize", 0.042f);
            material.SetFloat("_SunSizeConvergence", 5f);
            material.SetFloat("_AtmosphereThickness", 0.62f);
            material.SetColor("_SkyTint", new Color(0.52f, 0.62f, 0.78f));
            material.SetColor("_GroundColor", new Color(0.34f, 0.25f, 0.17f));
            material.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static bool TryBake(LightingSettings settings, LightingSettings.Lightmapper lightmapper)
        {
            settings.lightmapper = lightmapper;
            EditorUtility.SetDirty(settings);

            try
            {
                return Lightmapping.Bake();
            }
            catch (Exception exception)
            {
                Debug.LogWarning(LogPrefix + "Lightmapper " + lightmapper + " threw: " + exception.Message, settings);
                return false;
            }
        }

        private static Light FindDirectionalLight()
        {
            var lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude);
            for (var i = 0; i < lights.Length; i++)
            {
                if (lights[i].type == LightType.Directional)
                {
                    return lights[i];
                }
            }

            return null;
        }
    }
}
#endif
