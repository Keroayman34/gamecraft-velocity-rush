#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>Procedural material set shared by one environment theme (T-9.3, D-055).</summary>
    public sealed class EnvironmentMaterialSet
    {
        public Material RoadSurface;
        public Material RoadEdge;
        public Material GroundPrimary;
        public Material GroundSecondary;
        public Material Barrier;
        public Material Accent;
        public Material Emissive;
    }

    /// <summary>
    /// Deterministic per-theme material generator for the shared environment kit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every material is internal-only art: Built-in <c>Standard</c> shader, palette-driven flat
    /// colours, no external texture or package. This extends the approved Phase-7/8 provenance
    /// route to environments (D-055/OQ-A05).
    /// </para>
    /// <para>
    /// Materials are written under <c>Assets/Generated/Environments/&lt;id&gt;/Materials</c> so
    /// the generated set is explicit, reproducible, and never confused with authored content.
    /// </para>
    /// </remarks>
    public static class EnvironmentMaterialKit
    {
        /// <summary>Root folder for generated environment content.</summary>
        public const string RootFolder = "Assets/Generated/Environments";

        /// <summary>Project-relative folder for one environment's generated content.</summary>
        public static string GetEnvironmentFolder(string environmentId)
        {
            if (string.IsNullOrEmpty(environmentId))
            {
                throw new ArgumentException("Environment id must not be empty.", nameof(environmentId));
            }

            return RootFolder + "/" + environmentId;
        }

        /// <summary>Project-relative material folder for one environment.</summary>
        public static string GetMaterialFolder(string environmentId)
        {
            return GetEnvironmentFolder(environmentId) + "/Materials";
        }

        /// <summary>Project-relative path of a generated material.</summary>
        public static string GetMaterialPath(string environmentId, string materialName)
        {
            return GetMaterialFolder(environmentId) + "/" + environmentId + "_" + materialName + ".mat";
        }

        /// <summary>
        /// Creates or refreshes the seven shared materials from the definition palette. Re-running
        /// the builder updates values in place instead of creating duplicate assets.
        /// </summary>
        public static EnvironmentMaterialSet EnsureMaterialSet(EnvironmentDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!definition.TryValidate(out var definitionError))
            {
                throw new InvalidOperationException("Environment definition is invalid: " + definitionError);
            }

            var id = definition.EnvironmentId;
            EnsureFolder(GetMaterialFolder(id));

            var palette = definition.Palette;
            return new EnvironmentMaterialSet
            {
                RoadSurface = EnsureMaterial(id, "RoadSurface", palette.RoadSurface, 0f, 0.18f),
                RoadEdge = EnsureMaterial(id, "RoadEdge", palette.RoadEdge, 0f, 0.35f),
                GroundPrimary = EnsureMaterial(id, "GroundPrimary", palette.GroundPrimary, 0f, 0.08f),
                GroundSecondary = EnsureMaterial(id, "GroundSecondary", palette.GroundSecondary, 0f, 0.06f),
                Barrier = EnsureMaterial(id, "Barrier", palette.Barrier, 0.45f, 0.32f),
                Accent = EnsureMaterial(id, "Accent", palette.Accent, 0.2f, 0.36f),
                Emissive = EnsureEmissiveMaterial(id, "Emissive", palette.Emissive)
            };
        }

        /// <summary>Loads the generated material set, or null when it is incomplete.</summary>
        public static EnvironmentMaterialSet LoadMaterialSet(string environmentId)
        {
            var set = new EnvironmentMaterialSet
            {
                RoadSurface = Load(environmentId, "RoadSurface"),
                RoadEdge = Load(environmentId, "RoadEdge"),
                GroundPrimary = Load(environmentId, "GroundPrimary"),
                GroundSecondary = Load(environmentId, "GroundSecondary"),
                Barrier = Load(environmentId, "Barrier"),
                Accent = Load(environmentId, "Accent"),
                Emissive = Load(environmentId, "Emissive")
            };

            return set.RoadSurface != null &&
                   set.RoadEdge != null &&
                   set.GroundPrimary != null &&
                   set.Barrier != null
                ? set
                : null;
        }

        /// <summary>Creates every missing folder along a project-relative path.</summary>
        public static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
            {
                throw new ArgumentException("Folder path must not be empty.", nameof(folder));
            }

            var parts = folder.Replace('\\', '/').Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Material Load(string environmentId, string materialName)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(
                GetMaterialPath(environmentId, materialName));
        }

        private static Material EnsureMaterial(
            string environmentId,
            string materialName,
            Color color,
            float metallic,
            float smoothness)
        {
            var path = GetMaterialPath(environmentId, materialName);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
                if (shader == null)
                {
                    throw new InvalidOperationException(
                        "No usable Built-in shader found for environment material " + materialName + ".");
                }

                material = new Material(shader)
                {
                    name = environmentId + "_" + materialName
                };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.mainTexture = null;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Mode", 0f);
            material.DisableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material EnsureEmissiveMaterial(
            string environmentId,
            string materialName,
            Color emission)
        {
            var material = EnsureMaterial(environmentId, materialName, Color.white, 0.1f, 0.5f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
#endif
