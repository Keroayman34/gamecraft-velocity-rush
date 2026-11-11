#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Phase-8 editor entry points for authoring and validating the Commercial V1.0 vehicle
    /// roster. The builder creates definitions only from owner-approved identities; it never
    /// invents names, classes, or statistics.
    /// </summary>
    /// <remarks>
    /// Validation is fail-closed: a missing roster folder, an incomplete roster, a duplicate id,
    /// a missing localized name, a null physics profile, or a wrong class distribution is an
    /// error. The menu path is deliberate so the same contract can be exercised in batch mode
    /// through <see cref="ValidateFromCommandLine"/>.
    /// </remarks>
    public static class VehicleRosterBuilder
    {
        /// <summary>Root folder for authored vehicle packages.</summary>
        public const string RosterFolder = "Assets/Data/Vehicles";

        private const string LogPrefix = "[VelocityRush.Roster] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Validate Vehicle Roster";

        /// <summary>Runs project-wide roster validation and reports every issue.</summary>
        [MenuItem(MenuPath, false, 22)]
        public static void ValidateFromMenu()
        {
            var issues = ValidateProjectRoster(out var definitionCount);
            if (issues.Count == 0)
            {
                Debug.Log(LogPrefix + "PASS Vehicle roster is complete: " + definitionCount +
                          " definitions match the 2/3/2/3 class distribution.");
            }
            else
            {
                foreach (var issue in issues)
                {
                    Debug.LogError(LogPrefix + "FAIL " + issue);
                }
            }

            if (Application.isBatchMode)
            {
                return;
            }

            var message = issues.Count == 0
                ? "PASS\n\n" + definitionCount + " definitions; 2/3/2/3 distribution."
                : "FAIL\n\n" + string.Join("\n", issues);
            EditorUtility.DisplayDialog("Vehicle Roster", message, "OK");
        }

        /// <summary>
        /// Batch entry point. Exits 0 only when the roster is complete and valid; exits 1 with
        /// per-issue error logs otherwise. Invoke with <c>-executeMethod</c> in batch mode.
        /// </summary>
        public static void ValidateFromCommandLine()
        {
            var issues = ValidateProjectRoster(out var definitionCount);
            if (issues.Count == 0)
            {
                Debug.Log(LogPrefix + "PASS Vehicle roster is complete: " + definitionCount +
                          " definitions match the 2/3/2/3 class distribution.");
                EditorApplication.Exit(0);
                return;
            }

            foreach (var issue in issues)
            {
                Debug.LogError(LogPrefix + "FAIL " + issue);
            }

            EditorApplication.Exit(1);
        }

        /// <summary>
        /// Loads every <see cref="VehicleDefinition"/> under <see cref="RosterFolder"/> and runs
        /// <see cref="VehicleRosterRules.ValidateRoster"/>. Ordering is deterministic (asset path)
        /// so duplicate-id diagnostics are stable between runs.
        /// </summary>
        public static IReadOnlyList<string> ValidateProjectRoster(out int definitionCount)
        {
            definitionCount = 0;
            if (!AssetDatabase.IsValidFolder(RosterFolder))
            {
                return new[] { "Roster folder is missing: " + RosterFolder + "." };
            }

            var guids = AssetDatabase.FindAssets("t:VehicleDefinition", new[] { RosterFolder });
            var paths = new List<string>(guids.Length);
            foreach (var guid in guids)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);

            var definitions = new List<VehicleDefinition>(paths.Count);
            var loadIssues = new List<string>();
            foreach (var path in paths)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(path);
                if (definition == null)
                {
                    loadIssues.Add("Could not load VehicleDefinition at " + path + ".");
                    continue;
                }

                definitions.Add(definition);
            }

            definitionCount = definitions.Count;
            var issues = new List<string>(loadIssues);
            issues.AddRange(VehicleRosterRules.ValidateRoster(definitions));
            return issues;
        }

        /// <summary>
        /// Creates one definition asset under
        /// <c>Assets/Data/Vehicles/&lt;Class&gt;/&lt;vehicleId&gt;/&lt;vehicleId&gt;_Definition.asset</c>.
        /// The definition is validated before any asset is written; invalid input throws and
        /// leaves the project untouched.
        /// </summary>
        /// <param name="vehicleId">Owner-approved stable id (lowercase ASCII, hyphens allowed).</param>
        /// <param name="displayNameEn">Owner-approved English name.</param>
        /// <param name="displayNameAr">Owner-approved Arabic name.</param>
        /// <param name="vehicleClass">Roster class from the D-014 planning distribution.</param>
        /// <param name="physicsProfile">Approved handling profile; values stay in that asset.</param>
        /// <param name="overwrite">Replace an existing definition at the same path.</param>
        public static VehicleDefinition CreateDefinition(
            string vehicleId,
            string displayNameEn,
            string displayNameAr,
            VehicleClass vehicleClass,
            VehiclePhysicsProfile physicsProfile,
            bool overwrite = false)
        {
            if (VehicleRosterRules.GetRequiredCount(vehicleClass) == 0)
            {
                throw new InvalidOperationException(
                    "VehicleClass value " + (int)vehicleClass + " is not a defined roster class.");
            }

            if (physicsProfile == null)
            {
                throw new InvalidOperationException("A non-null physics profile is required.");
            }

            var definition = ScriptableObject.CreateInstance<VehicleDefinition>();
            var serialized = new SerializedObject(definition);
            serialized.FindProperty("vehicleId").stringValue = vehicleId ?? string.Empty;
            serialized.FindProperty("displayNameEn").stringValue = displayNameEn ?? string.Empty;
            serialized.FindProperty("displayNameAr").stringValue = displayNameAr ?? string.Empty;
            serialized.FindProperty("vehicleClass").intValue = (int)vehicleClass;
            serialized.FindProperty("physicsProfile").objectReferenceValue = physicsProfile;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (!definition.TryValidate(out var validationError))
            {
                Object.DestroyImmediate(definition);
                throw new InvalidOperationException("Vehicle definition is invalid: " + validationError);
            }

            var vehicleFolder = RosterFolder + "/" + VehicleRosterRules.GetClassFolderName(vehicleClass) +
                                "/" + vehicleId;
            var assetPath = vehicleFolder + "/" + vehicleId + "_Definition.asset";
            var existing = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(assetPath);
            if (existing != null && !overwrite)
            {
                Object.DestroyImmediate(definition);
                throw new InvalidOperationException(
                    "A VehicleDefinition already exists at " + assetPath + "; pass overwrite to replace it.");
            }

            EnsureFolder(vehicleFolder);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            definition.name = vehicleId + "_Definition";
            AssetDatabase.CreateAsset(definition, assetPath);
            EditorUtility.SetDirty(definition);
            AssetDatabase.SaveAssets();
            Debug.Log(LogPrefix + "Created vehicle definition: " + assetPath);
            return definition;
        }

        internal static void EnsureFolder(string path)
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
