#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic builder for the Phase-8 Stage-4 Garage foundation scene (T-8.9).
    /// </summary>
    /// <remarks>
    /// The scene is a UI-only demonstrator: one camera, an EventSystem, a UIDocument using the
    /// existing slice panel settings, and the roster catalog. It is intentionally absent from
    /// <c>EditorBuildSettings</c>, so the validated prototype/slice build contracts are unchanged.
    /// No progression, save, ownership, or upgrade system is created.
    /// </remarks>
    public static class GarageSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Garage.unity";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Build Garage Scene";

        [MenuItem(MenuPath, false, 25)]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
                !EditorUtility.DisplayDialog(
                    "Rebuild Garage scene?",
                    "This replaces " + ScenePath + ". Build settings, the prototype, and the slice scene are not touched.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build(true);
        }

        /// <summary>Batch entry point; exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build(true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[VelocityRush.Garage] FAIL " + exception.Message);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds and saves the Garage foundation scene.</summary>
        public static void Build(bool overwrite)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                throw new InvalidOperationException("Garage scene already exists: " + ScenePath);
            }

            // Create the target scene before loading assets: NewScene unloads unused asset objects,
            // which invalidates any ScriptableObject reference loaded beforehand.
            SliceAssetBuilder.EnsureSliceAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(SliceAssetBuilder.PanelSettingsPath);
            if (panelSettings == null)
            {
                throw new InvalidOperationException("Missing panel settings: " + SliceAssetBuilder.PanelSettingsPath);
            }

            var roster = LoadRosterDefinitions();
            if (roster.Length != VehicleRosterRules.RequiredVehicleCount)
            {
                throw new InvalidOperationException(
                    "Expected " + VehicleRosterRules.RequiredVehicleCount + " definitions, found " + roster.Length + ".");
            }

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f);
            camera.cullingMask = 0;
            cameraGo.transform.position = new Vector3(0f, 1.5f, -6f);

            var uiGo = new GameObject("Garage UI");
            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            document.sortingOrder = 20;

            var catalogGo = new GameObject("VehicleRoster");
            var catalog = catalogGo.AddComponent<VehicleRosterCatalog>();
            catalog.ConfigureForAuthoring(roster);
            if (!catalog.TryValidate(out var catalogError))
            {
                throw new InvalidOperationException("Generated roster catalog is invalid: " + catalogError);
            }

            var screen = uiGo.AddComponent<GarageScreen>();
            var screenSerialized = new SerializedObject(screen);
            screenSerialized.FindProperty("document").objectReferenceValue = document;
            screenSerialized.FindProperty("catalog").objectReferenceValue = catalog;
            screenSerialized.ApplyModifiedPropertiesWithoutUndo();

            var debugGo = new GameObject("GarageDebugLauncher");
            var launcher = debugGo.AddComponent<GarageDebugLauncher>();
            var launcherSerialized = new SerializedObject(launcher);
            launcherSerialized.FindProperty("screen").objectReferenceValue = screen;
            launcherSerialized.FindProperty("courseSceneName").stringValue = "HandlingCourse";
            launcherSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VelocityRush.Garage] Built " + ScenePath + " | vehicles=" + roster.Length +
                      " | ui=UIToolkit | scope=browse+select+confirm | progression=none");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        private static VehicleDefinition[] LoadRosterDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleDefinition", new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);

            var definitions = new VehicleDefinition[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                definitions[i] = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[i]);
                if (definitions[i] == null)
                {
                    throw new InvalidOperationException("Could not load " + paths[i]);
                }
            }

            return definitions;
        }
    }
}
#endif
