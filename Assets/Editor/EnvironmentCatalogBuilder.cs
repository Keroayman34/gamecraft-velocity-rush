#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Editor entry points for validating and authoring the five environment packages (T-9.1).
    /// </summary>
    /// <remarks>
    /// Validation is fail-closed: an empty folder, a missing theme, a duplicate id/order, or an
    /// invalid definition is an error. No environment content is invented by this tool; it only
    /// validates a folder and can create a definition from explicitly supplied owner data.
    /// </remarks>
    public static class EnvironmentCatalogBuilder
    {
        /// <summary>Root folder for authored environment packages.</summary>
        public const string EnvironmentFolder = "Assets/Data/Environments";

        private const string LogPrefix = "[VelocityRush.Environments] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Validate Environment Catalog";

        /// <summary>Runs project-wide catalog validation from the menu.</summary>
        [MenuItem(MenuPath, false, 30)]
        public static void ValidateFromMenu()
        {
            var definitions = LoadDefinitions();
            var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);
            if (issues.Count == 0)
            {
                Debug.Log(LogPrefix + "PASS — " + definitions.Length +
                          " environment definitions match the five-theme production order.");
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                Debug.LogError(LogPrefix + issues[i]);
            }
        }

        /// <summary>Batch entry point: exits 1 on any catalog defect.</summary>
        public static void ValidateFromCommandLine()
        {
            try
            {
                var definitions = LoadDefinitions();
                var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);
                if (issues.Count == 0)
                {
                    Debug.Log(LogPrefix + "PASS — " + definitions.Length +
                              " environment definitions match the five-theme production order.");
                    EditorApplication.Exit(0);
                    return;
                }

                for (var i = 0; i < issues.Count; i++)
                {
                    Debug.LogError(LogPrefix + issues[i]);
                }

                EditorApplication.Exit(1);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Validation failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Creates one environment definition from owner-supplied data after validating it.
        /// The production order is derived from the approved theme order.
        /// </summary>
        public static EnvironmentDefinition CreateDefinition(
            string environmentId,
            string displayNameEn,
            string displayNameAr,
            EnvironmentTheme theme,
            EnvironmentPalette palette,
            EnvironmentLightingProfile lightingProfile,
            TrackLayout trackLayout,
            bool overwrite)
        {
            var definition = ScriptableObject.CreateInstance<EnvironmentDefinition>();
            definition.ConfigureForAuthoring(
                environmentId,
                displayNameEn,
                displayNameAr,
                theme,
                EnvironmentThemeRules.GetProductionOrder(theme),
                palette,
                lightingProfile,
                trackLayout);

            if (!definition.TryValidate(out var error))
            {
                UnityEngine.Object.DestroyImmediate(definition);
                throw new InvalidOperationException("Environment definition is invalid: " + error);
            }

            var folder = EnvironmentFolder + "/" + theme + "/" + environmentId;
            var path = folder + "/" + environmentId + "_Definition.asset";
            EnvironmentMaterialKit.EnsureFolder(folder);

            if (AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(path) != null)
            {
                if (!overwrite)
                {
                    UnityEngine.Object.DestroyImmediate(definition);
                    throw new InvalidOperationException("Environment definition already exists: " + path);
                }

                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            Debug.Log(LogPrefix + "Created " + path);
            return definition;
        }

        /// <summary>
        /// Loads every authored definition under the environment folder, ordered by production
        /// order then stable id so validation output is deterministic.
        /// </summary>
        public static EnvironmentDefinition[] LoadDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:EnvironmentDefinition", new[] { EnvironmentFolder });
            var definitions = new List<EnvironmentDefinition>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(path);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            definitions.Sort(CompareDefinitions);
            return definitions.ToArray();
        }

        private static int CompareDefinitions(EnvironmentDefinition left, EnvironmentDefinition right)
        {
            var order = left.ProductionOrder.CompareTo(right.ProductionOrder);
            if (order != 0)
            {
                return order;
            }

            return string.Compare(left.EnvironmentId, right.EnvironmentId, StringComparison.Ordinal);
        }
    }
}
#endif
