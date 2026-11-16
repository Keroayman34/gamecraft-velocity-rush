#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>Mechanical evidence produced by one environment scene build (T-9.5).</summary>
    public readonly struct EnvironmentBuildReport
    {
        public EnvironmentBuildReport(
            string environmentId,
            string scenePath,
            int segmentCount,
            float centerlineLength,
            int decorationCount,
            int materialCount,
            int cameraCount,
            int directionalLightCount,
            int missingScriptCount)
        {
            EnvironmentId = environmentId;
            ScenePath = scenePath;
            SegmentCount = segmentCount;
            CenterlineLength = centerlineLength;
            DecorationCount = decorationCount;
            MaterialCount = materialCount;
            CameraCount = cameraCount;
            DirectionalLightCount = directionalLightCount;
            MissingScriptCount = missingScriptCount;
        }

        public string EnvironmentId { get; }
        public string ScenePath { get; }
        public int SegmentCount { get; }
        public float CenterlineLength { get; }
        public int DecorationCount { get; }
        public int MaterialCount { get; }
        public int CameraCount { get; }
        public int DirectionalLightCount { get; }
        public int MissingScriptCount { get; }
    }

    /// <summary>
    /// Parameterized environment scene-builder template (T-9.5, D-057).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The builder consumes an <see cref="EnvironmentDefinition"/>: it validates the definition,
    /// generates the per-theme material set, builds the shared-kit track and dressing from the
    /// data-driven layout, applies the realtime lighting profile, and wires a scene context,
    /// spawn point, camera, and (when the full catalog is valid) the environment catalog.
    /// </para>
    /// <para>
    /// It is intentionally not run for the five production scenes in Stage 1. The template exists
    /// so Stage 2/3 can generate each scene from approved data without touching code. The builder
    /// audits its own output and fails if any missing script or required wiring is absent; it
    /// never edits <c>EditorBuildSettings</c>.
    /// </para>
    /// </remarks>
    public static class EnvironmentSceneBuilder
    {
        /// <summary>Folder that owns the five generated environment scenes.</summary>
        public const string ScenesFolder = "Assets/Scenes/Environments";

        private const string LogPrefix = "[VelocityRush.EnvironmentScene] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Build Environment Scene";

        /// <summary>Builds the scene for the currently selected environment definition.</summary>
        [MenuItem(MenuPath, false, 31)]
        public static void BuildFromMenu()
        {
            var definition = Selection.activeObject as EnvironmentDefinition;
            if (definition == null)
            {
                EditorUtility.DisplayDialog(
                    "Build environment scene",
                    "Select an EnvironmentDefinition asset in the Project window first. " +
                    "Five environment scenes are not generated in Stage 1.",
                    "OK");
                return;
            }

            var scenePath = GetScenePath(definition);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Rebuild environment scene?",
                        "This replaces " + scenePath + " and its generated meshes/materials.",
                        "Rebuild",
                        "Cancel"))
                {
                    return;
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build(definition, true);
        }

        /// <summary>
        /// Batch entry point. Requires <c>-environmentId &lt;stable-id&gt;</c>; exits 1 when the
        /// definition is absent or the build/audit fails.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                var environmentId = ReadArgument("-environmentId");
                if (string.IsNullOrEmpty(environmentId))
                {
                    Debug.LogError(LogPrefix + "Missing required -environmentId argument.");
                    EditorApplication.Exit(1);
                    return;
                }

                var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
                EnvironmentDefinition definition = null;
                for (var i = 0; i < definitions.Length; i++)
                {
                    if (string.Equals(
                            definitions[i].EnvironmentId,
                            environmentId,
                            StringComparison.Ordinal))
                    {
                        definition = definitions[i];
                        break;
                    }
                }

                if (definition == null)
                {
                    Debug.LogError(LogPrefix + "No environment definition found for id '" +
                                   environmentId + "'.");
                    EditorApplication.Exit(1);
                    return;
                }

                Build(definition, true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Build failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds and saves one environment scene from its definition.</summary>
        public static EnvironmentBuildReport Build(EnvironmentDefinition definition, bool overwrite)
        {
            ValidateDefinition(definition);

            var scenePath = GetScenePath(definition);
            if (!overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
            {
                throw new InvalidOperationException("Environment scene already exists: " + scenePath);
            }

            EnvironmentMaterialKit.EnsureFolder(EnvironmentMaterialKit.RootFolder);
            EnvironmentMaterialKit.EnsureFolder(ScenesFolder);
            EnvironmentMaterialKit.EnsureFolder(
                EnvironmentMaterialKit.GetEnvironmentFolder(definition.EnvironmentId));

            var materials = EnvironmentMaterialKit.EnsureMaterialSet(definition);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environmentRoot = new GameObject("Environment");
            var context = environmentRoot.AddComponent<EnvironmentSceneContext>();
            context.ConfigureForAuthoring(definition);

            var track = EnvironmentKit.BuildTrack(
                environmentRoot.transform,
                definition,
                materials,
                EnvironmentMaterialKit.GetEnvironmentFolder(definition.EnvironmentId),
                EnvironmentLayoutVariant.Signature);

            var light = EnvironmentLightingBuilder.Apply(definition, environmentRoot.transform);
            if (light == null)
            {
                throw new InvalidOperationException("Lighting builder did not create a directional light.");
            }

            var layout = definition.TrackLayout;
            var spawnPosition = layout.SampleAtDistance(track.Centerline, 0f, out var spawnTangent);
            var spawnForward = Flatten(spawnTangent);
            var spawnRotation = Quaternion.LookRotation(spawnForward, Vector3.up);

            var spawnGo = new GameObject("SpawnPoint");
            spawnGo.transform.SetPositionAndRotation(spawnPosition + (Vector3.up * 0.5f), spawnRotation);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 60f;
            cameraGo.AddComponent<AudioListener>();
            var cameraPosition = spawnPosition - (spawnForward * 7f) + (Vector3.up * 2.4f);
            var cameraTarget = spawnPosition + (spawnForward * 12f) + (Vector3.up * 1.1f);
            cameraGo.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation((cameraTarget - cameraPosition).normalized, Vector3.up));

            var allDefinitions = EnvironmentCatalogBuilder.LoadDefinitions();
            if (EnvironmentCatalogRules.TryValidateCatalog(allDefinitions, out _))
            {
                var catalogGo = new GameObject("EnvironmentCatalog");
                var catalog = catalogGo.AddComponent<EnvironmentCatalog>();
                catalog.ConfigureForAuthoring(allDefinitions);
            }

            var report = AuditScene(scene, definition);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, scenePath))
            {
                throw new InvalidOperationException("Failed to save " + scenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);

            Debug.Log(
                LogPrefix + "Built " + scenePath +
                " | theme=" + definition.Theme +
                " | segments=" + report.SegmentCount +
                " | length=" + report.CenterlineLength.ToString("0.0") + " m" +
                " | decor=" + report.DecorationCount +
                " | materials=" + report.MaterialCount +
                " | realtimeLighting=true bake=false" +
                " | cameras=" + report.CameraCount +
                " | directionalLights=" + report.DirectionalLightCount +
                " | missingScripts=" + report.MissingScriptCount);

            return report;
        }

        /// <summary>Validates a definition before any asset or scene is created.</summary>
        public static void ValidateDefinition(EnvironmentDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (!definition.TryValidate(out var error))
            {
                throw new InvalidOperationException("Environment definition is invalid: " + error);
            }

            if (definition.TrackLayout.ClosedLoop)
            {
                throw new InvalidOperationException(
                    "Stage-1 environment builder supports open test layouts only; closed-loop " +
                    "content is a Stage-2 authoring decision.");
            }
        }

        /// <summary>Project-relative scene path for a definition.</summary>
        public static string GetScenePath(EnvironmentDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return ScenesFolder + "/" + definition.EnvironmentId + ".unity";
        }

        /// <summary>
        /// Audits a generated scene: zero missing scripts, exactly one valid context, a camera,
        /// a realtime directional light, a Track/Road renderer with a material, and a skybox/sun.
        /// </summary>
        public static EnvironmentBuildReport AuditScene(
            Scene scene,
            EnvironmentDefinition definition)
        {
            return AuditRoots(scene.GetRootGameObjects(), definition, GetScenePath(definition));
        }

        /// <summary>
        /// Audits an explicit root set. This is the same check used for a generated scene, exposed
        /// separately so EditMode tests can verify the fail-closed wiring contract without creating
        /// or saving production scenes.
        /// </summary>
        public static EnvironmentBuildReport AuditRoots(
            GameObject[] roots,
            EnvironmentDefinition definition,
            string scenePath = null)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (roots == null)
            {
                throw new ArgumentNullException(nameof(roots));
            }

            var issues = new List<string>();
            var missingScripts = 0;

            for (var i = 0; i < roots.Length; i++)
            {
                var behaviours = roots[i].GetComponentsInChildren<MonoBehaviour>(true);
                for (var j = 0; j < behaviours.Length; j++)
                {
                    if (behaviours[j] == null)
                    {
                        missingScripts++;
                    }
                }

                var transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (var j = 0; j < transforms.Length; j++)
                {
                    missingScripts += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                        transforms[j].gameObject);
                }
            }

            if (missingScripts > 0)
            {
                issues.Add("Generated scene contains " + missingScripts + " missing script reference(s).");
            }

            var contexts = new List<EnvironmentSceneContext>();
            Collect(roots, contexts);
            if (contexts.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one EnvironmentSceneContext (found " +
                           contexts.Count + ").");
            }
            else if (contexts[0].Definition != definition)
            {
                issues.Add("EnvironmentSceneContext does not reference the built definition.");
            }
            else if (!contexts[0].TryValidate(out var contextError))
            {
                issues.Add(contextError);
            }

            var cameras = new List<Camera>();
            Collect(roots, cameras);
            if (cameras.Count < 1)
            {
                issues.Add("Generated scene has no camera.");
            }

            var lights = new List<Light>();
            Collect(roots, lights);
            var realtimeDirectional = 0;
            for (var i = 0; i < lights.Count; i++)
            {
                if (lights[i].type == LightType.Directional &&
                    lights[i].lightmapBakeType == LightmapBakeType.Realtime)
                {
                    realtimeDirectional++;
                }
            }

            if (realtimeDirectional != 1)
            {
                issues.Add("Generated scene must contain exactly one realtime directional light " +
                           "(found " + realtimeDirectional + ").");
            }

            if (RenderSettings.skybox == null)
            {
                issues.Add("Generated scene has no skybox material.");
            }

            if (RenderSettings.sun == null)
            {
                issues.Add("Generated scene has no RenderSettings sun.");
            }

            var road = FindTransform(roots, "Track/Road");
            if (road == null)
            {
                issues.Add("Generated scene is missing Track/Road.");
            }
            else
            {
                var renderer = road.GetComponent<MeshRenderer>();
                if (renderer == null || renderer.sharedMaterial == null)
                {
                    issues.Add("Track/Road has no MeshRenderer material.");
                }
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Environment scene audit failed:\n" + string.Join("\n", issues));
            }

            var decorationRoot = FindTransform(roots, "Track/Decor");
            var decorationCount = decorationRoot != null
                ? decorationRoot.childCount
                : 0;

            return new EnvironmentBuildReport(
                definition.EnvironmentId,
                string.IsNullOrEmpty(scenePath) ? GetScenePath(definition) : scenePath,
                definition.TrackLayout.Segments.Length,
                definition.TrackLayout.TotalLength,
                decorationCount,
                7,
                cameras.Count,
                realtimeDirectional,
                missingScripts);
        }

        private static void Collect<T>(GameObject[] roots, List<T> results) where T : Component
        {
            for (var i = 0; i < roots.Length; i++)
            {
                results.AddRange(roots[i].GetComponentsInChildren<T>(true));
            }
        }

        private static Transform FindTransform(GameObject[] roots, string path)
        {
            for (var i = 0; i < roots.Length; i++)
            {
                var child = roots[i].transform.Find(path);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static string ReadArgument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude < 0.000001f ? Vector3.forward : value.normalized;
        }
    }
}
#endif
