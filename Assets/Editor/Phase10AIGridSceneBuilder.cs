using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Generates the deterministic Phase-10 AI-grid profiling scene (T-10.4.1, D-060).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scene is derived from the committed Desert signature environment so profiling uses a
    /// real production environment without modifying any authored environment scene. It adds one
    /// player slot plus four kinematic AI opponents (sixteen-car worst case is not used here; the
    /// approved default is five cars) and a wired <see cref="RaceGridController"/>.
    /// </para>
    /// <para>
    /// The four opponent rigs use the committed Sport rig prefab. Each instance is unpacked and
    /// receives an <see cref="AIVisualRig"/> and <see cref="AIDriver"/>; the inherited
    /// <see cref="VehicleController"/> is disabled at authoring time so a development build does
    /// not log a missing-input error before the visual rig binds.
    /// </para>
    /// <para>
    /// The vehicle-selection spawner is disabled in the derived scene: there is no selection
    /// context in a direct dev-build scene load, and the grid must own the template player.
    /// </para>
    /// </remarks>
    public static class Phase10AIGridSceneBuilder
    {
        private const string LogPrefix = "[VelocityRush.Phase10Grid] ";
        private const string SourceScenePath = "Assets/Scenes/Environments/desert-signature.unity";
        private const string OutputScenePath = "Assets/Scenes/Phase10AIGrid.unity";
        private const string OpponentRigPath = "Assets/Prefabs/Vehicles/Sport/sport-velocity-gt_Rig.prefab";
        private const int OpponentCount = 4;
        private const float StartLineDistance = 20f;
        private const float PlayerSpawnHeight = 0.5f;

        /// <summary>Menu entry that regenerates the profiling scene.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Phase 10 AI Grid Scene", false, 103)]
        public static void BuildFromMenu()
        {
            try
            {
                var scenePath = Build();
                EditorUtility.DisplayDialog(
                    "Velocity Rush - Phase 10 AI Grid",
                    "Generated " + scenePath + " (" + (OpponentCount + 1) + " participants).",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + exception);
                EditorUtility.DisplayDialog(
                    "Velocity Rush - Phase 10 AI Grid",
                    "Scene generation failed: " + exception.Message,
                    "OK");
            }
        }

        /// <summary>Batch entry point used by the Stage-4 build pipeline.</summary>
        public static void BuildFromCommandLine()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError(LogPrefix + "BuildFromCommandLine is a batch-mode entry point.");
                return;
            }

            try
            {
                var scenePath = Build();
                Debug.Log(LogPrefix + "Generated " + scenePath + ".");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Regenerates the profiling scene and returns its asset path.</summary>
        public static string Build()
        {
            if (!File.Exists(SourceScenePath))
            {
                throw new InvalidOperationException("Missing source scene: " + SourceScenePath);
            }

            if (!File.Exists(OpponentRigPath))
            {
                throw new InvalidOperationException("Missing opponent rig prefab: " + OpponentRigPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(OpponentRigPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not load opponent rig prefab: " + OpponentRigPath);
            }

            var scene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var context = FindSingle<EnvironmentSceneContext>(scene);
            var race = FindSingle<RaceController>(scene);
            var player = FindSingle<VehicleController>(scene);

            if (context == null || context.Definition == null || context.Definition.TrackLayout == null)
            {
                throw new InvalidOperationException("Desert scene has no valid EnvironmentSceneContext/TrackLayout.");
            }

            // The grid owns the template player; a direct dev-scene load has no garage selection.
            var spawners = UnityEngine.Object.FindObjectsByType<VehicleSelectionSpawner>(
                FindObjectsInactive.Include);
            for (var i = 0; i < spawners.Length; i++)
            {
                spawners[i].enabled = false;
            }

            var opponents = new AIDriver[OpponentCount];
            for (var i = 0; i < OpponentCount; i++)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate opponent rig " + i + ".");
                }

                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                instance.name = "AI Opponent " + i;

                var inheritedVehicle = instance.GetComponent<VehicleController>();
                if (inheritedVehicle != null)
                {
                    inheritedVehicle.enabled = false;
                }

                if (instance.GetComponent<AIVisualRig>() == null)
                {
                    instance.AddComponent<AIVisualRig>();
                }

                var driver = instance.GetComponent<AIDriver>();
                if (driver == null)
                {
                    driver = instance.AddComponent<AIDriver>();
                }

                var adapter = instance.GetComponent<AIVehicleInput>();
                if (adapter == null)
                {
                    adapter = instance.AddComponent<AIVehicleInput>();
                }

                if (inheritedVehicle != null)
                {
                    // Awake runs even on a disabled component; bind the adapter so a development
                    // build never logs the missing-input error before AIVisualRig disables it.
                    var serializedVehicle = new SerializedObject(inheritedVehicle);
                    var inputProperty = serializedVehicle.FindProperty("inputSource");
                    if (inputProperty != null)
                    {
                        inputProperty.objectReferenceValue = adapter;
                        serializedVehicle.ApplyModifiedPropertiesWithoutUndo();
                    }
                }

                opponents[i] = driver;
            }

            var gridObject = new GameObject("Race Grid");
            var grid = gridObject.AddComponent<RaceGridController>();
            grid.ConfigureForAuthoring(
                race,
                player,
                opponents,
                context.Definition.TrackLayout,
                EnvironmentLayoutVariant.Signature,
                StartLineDistance,
                PlayerSpawnHeight);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, OutputScenePath))
            {
                throw new InvalidOperationException("Could not save " + OutputScenePath + ".");
            }

            AssetDatabase.Refresh();
            Debug.Log(LogPrefix + "Scene: " + OutputScenePath +
                      " | participants " + (OpponentCount + 1) +
                      " | opponents " + OpponentCount +
                      " | track " + context.Definition.TrackLayout.TotalLength.ToString("0.0") + " m");
            return OutputScenePath;
        }

        private static T FindSingle<T>(Scene scene) where T : Component
        {
            var found = UnityEngine.Object.FindObjectsByType<T>(
                FindObjectsInactive.Include);
            T match = null;
            var count = 0;
            for (var i = 0; i < found.Length; i++)
            {
                if (found[i].gameObject.scene == scene)
                {
                    match = found[i];
                    count++;
                }
            }

            if (count != 1)
            {
                throw new InvalidOperationException(
                    "Expected exactly one " + typeof(T).Name + " in " + scene.path + ", found " + count + ".");
            }

            return match;
        }
    }
}
