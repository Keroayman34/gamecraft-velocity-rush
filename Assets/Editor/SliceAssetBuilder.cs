#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic, idempotent creation of the vertical-slice UI Toolkit and baked-lighting
    /// assets (T-7.5/T-7.5b).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The theme stylesheet is a plain text asset committed under <c>Assets/UI/Slice</c>; this tool
    /// only creates the <see cref="PanelSettings"/> and <see cref="LightingSettings"/> assets that
    /// cannot be authored as text. Running it repeatedly is safe: existing assets are updated in
    /// place rather than replaced.
    /// </para>
    /// <para>
    /// Built-in Render Pipeline only (D-032). Baked GI is enabled with the CPU progressive
    /// lightmapper and mixed lighting in Indirect-Only mode so real-time direct light and shadows
    /// stay available without the cost of shadow masks under WebGL.
    /// </para>
    /// </remarks>
    public static class SliceAssetBuilder
    {
        public const string ThemePath = "Assets/UI/Slice/UnityDefaultRuntimeTheme.tss";
        public const string PanelSettingsPath = "Assets/UI/Slice/SlicePanelSettings.asset";
        public const string LightingSettingsPath = "Assets/Settings/Lighting/DesertSliceLighting.lighting";

        /// <summary>Menu entry that creates or refreshes the slice assets.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Slice UI & Lighting Assets", false, 30)]
        public static void EnsureFromMenu()
        {
            EnsureSliceAssets();
            Debug.Log(
                "[VelocityRush.Slice] Assets ready | panel=" + PanelSettingsPath +
                " | lighting=" + LightingSettingsPath);
        }

        /// <summary>Creates the slice UI and lighting assets when absent and refreshes their settings.</summary>
        /// <exception cref="InvalidOperationException">Thrown when the committed theme stylesheet is missing.</exception>
        public static void EnsureSliceAssets()
        {
            EnsureFolder("Assets/UI");
            EnsureFolder("Assets/UI/Slice");
            EnsureFolder("Assets/Settings");
            EnsureFolder("Assets/Settings/Lighting");

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
            {
                throw new InvalidOperationException("Missing UI Toolkit theme stylesheet: " + ThemePath);
            }

            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelSettingsPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(panel, PanelSettingsPath);
            }

            panel.themeStyleSheet = theme;
            panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panel.referenceResolution = new Vector2Int(1920, 1080);
            panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            panel.match = 0.5f;
            panel.sortingOrder = 10;
            EditorUtility.SetDirty(panel);

            var lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(LightingSettingsPath);
            if (lighting == null)
            {
                lighting = new LightingSettings();
                AssetDatabase.CreateAsset(lighting, LightingSettingsPath);
            }

            lighting.bakedGI = true;
            lighting.realtimeGI = false;
            lighting.lightmapper = LightingSettings.Lightmapper.UnityComputeGPU;
            lighting.mixedBakeMode = MixedLightingMode.IndirectOnly;
            lighting.lightmapResolution = 20f;
            lighting.lightmapMaxSize = 1024;
            lighting.lightmapPadding = 2;
            lighting.ao = true;
            lighting.indirectScale = 1f;
            lighting.albedoBoost = 1f;
            lighting.maxBounces = 1;
            EditorUtility.SetDirty(lighting);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
