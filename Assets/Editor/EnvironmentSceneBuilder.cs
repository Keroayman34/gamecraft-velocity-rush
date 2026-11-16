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
    /// <summary>Mechanical evidence produced by one environment scene build (T-9.5/T-9.11).</summary>
    public readonly struct EnvironmentBuildReport
    {
        public EnvironmentBuildReport(
            string environmentId,
            EnvironmentTheme theme,
            string scenePath,
            int segmentCount,
            float centerlineLength,
            int decorationCount,
            int landmarkCount,
            int waterCount,
            int materialCount,
            int cameraCount,
            int directionalLightCount,
            int checkpointCount,
            int hudCount,
            int audioControllerCount,
            int vfxCount,
            int spawnerCount,
            int missingScriptCount,
            int missingReferenceCount)
        {
            EnvironmentId = environmentId;
            Theme = theme;
            ScenePath = scenePath;
            SegmentCount = segmentCount;
            CenterlineLength = centerlineLength;
            DecorationCount = decorationCount;
            LandmarkCount = landmarkCount;
            WaterCount = waterCount;
            MaterialCount = materialCount;
            CameraCount = cameraCount;
            DirectionalLightCount = directionalLightCount;
            CheckpointCount = checkpointCount;
            HudCount = hudCount;
            AudioControllerCount = audioControllerCount;
            VfxCount = vfxCount;
            SpawnerCount = spawnerCount;
            MissingScriptCount = missingScriptCount;
            MissingReferenceCount = missingReferenceCount;
        }

        public string EnvironmentId { get; }
        public EnvironmentTheme Theme { get; }
        public string ScenePath { get; }
        public int SegmentCount { get; }
        public float CenterlineLength { get; }
        public int DecorationCount { get; }
        public int LandmarkCount { get; }
        public int WaterCount { get; }
        public int MaterialCount { get; }
        public int CameraCount { get; }
        public int DirectionalLightCount { get; }
        public int CheckpointCount { get; }
        public int HudCount { get; }
        public int AudioControllerCount { get; }
        public int VfxCount { get; }
        public int SpawnerCount { get; }
        public int MissingScriptCount { get; }
        public int MissingReferenceCount { get; }
    }

    /// <summary>
    /// Parameterized environment scene builder (T-9.5, T-9.11, D-057).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The builder consumes an <see cref="EnvironmentDefinition"/>, generates the per-theme
    /// material set, builds the shared-kit track and dressing from the data-driven layout, applies
    /// the realtime lighting profile, and wires the complete approved race stack: vehicle, input,
    /// camera rig, race controller, ordered gates, HUD/pause/results UI, audio, VFX, performance
    /// probe, vehicle roster/rig catalogs, and the selection spawner.
    /// </para>
    /// <para>
    /// The audit is fail-closed: missing scripts, missing serialized references, an invalid gate
    /// set, or any collider under decorative content stops the build before the scene is saved. It
    /// never edits <c>EditorBuildSettings</c>.
    /// </para>
    /// </remarks>
    public static class EnvironmentSceneBuilder
    {
        /// <summary>Folder that owns the five generated environment scenes.</summary>
        public const string ScenesFolder = "Assets/Scenes/Environments";

        private const string LogPrefix = "[VelocityRush.EnvironmentScene] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Build Environment Scene";
        private const string VehiclePrefabPath = "Assets/Prefabs/PrototypeVehicle.prefab";
        private const string DefaultVehicleProfilePath = "Assets/Data/Tuning/CandidateA_Grip_Vehicle.asset";
        private const string BaselineCameraPath = "Assets/Data/Tuning/Baseline_Camera.asset";
        private const int TrackLayer = 8;

        /// <summary>Builds the scene for the currently selected environment definition.</summary>
        [MenuItem(MenuPath, false, 31)]
        public static void BuildFromMenu()
        {
            var definition = Selection.activeObject as EnvironmentDefinition;
            if (definition == null)
            {
                EditorUtility.DisplayDialog(
                    "Build environment scene",
                    "Select an EnvironmentDefinition asset in the Project window first.",
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
        /// Batch entry point for one environment. Requires <c>-environmentId &lt;stable-id&gt;</c>;
        /// exits 1 when the definition is absent or the build/audit fails.
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

        /// <summary>
        /// Batch entry point that validates the complete catalog and builds every environment scene
        /// in approved production order. Exits 1 on any catalog, distinctness, race, or audit
        /// failure.
        /// </summary>
        public static void BuildAllFromCommandLine()
        {
            try
            {
                var reports = BuildAll();
                Debug.Log(LogPrefix + "Built " + reports.Count + " environment scenes.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Build-all failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Validates the five-theme catalog, distinctness, and gate rules, then generates every
        /// scene in production order.
        /// </summary>
        public static IReadOnlyList<EnvironmentBuildReport> BuildAll()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            var issues = new List<string>(EnvironmentCatalogRules.ValidateCatalog(definitions));
            issues.AddRange(EnvironmentDistinctnessRules.ValidateThemeSeparation(definitions));
            for (var i = 0; i < definitions.Length; i++)
            {
                issues.AddRange(EnvironmentRaceRules.Validate(definitions[i].TrackLayout));
            }

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Environment catalog is not ready for scene generation:\n" +
                    string.Join("\n", issues));
            }

            var reports = new List<EnvironmentBuildReport>(definitions.Length);
            var paths = new string[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                paths[i] = AssetDatabase.GetAssetPath(definitions[i]);
            }

            for (var i = 0; i < paths.Length; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(paths[i]);
                if (definition == null)
                {
                    throw new InvalidOperationException(
                        "Environment definition could not be reloaded: " + paths[i]);
                }

                reports.Add(Build(definition, true));
            }

            return reports;
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

            var environmentId = definition.EnvironmentId;
            var definitionPath = AssetDatabase.GetAssetPath(definition);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Creating the new scene releases editor-only asset handles. Refresh and re-author the
            // generated assets after the reset, then reload the definition once more so no wiring
            // step can observe a fake-null asset reference.
            AssetDatabase.Refresh();
            definition = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(definitionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Environment definition could not be reloaded after scene reset: " + definitionPath);
            }

            var environmentMaterials = EnvironmentMaterialKit.EnsureMaterialSet(definition);
            var sliceMaterials = SliceTextureKit.EnsureMaterialSet();
            var audioClips = SliceAudioKit.EnsureClips();
            TuningPresetBuilder.EnsurePresets();

            definition = AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(definitionPath);
            if (definition == null)
            {
                throw new InvalidOperationException(
                    "Environment definition could not be reloaded after asset generation: " +
                    definitionPath);
            }

            var environmentRoot = new GameObject("Environment");
            var context = environmentRoot.AddComponent<EnvironmentSceneContext>();
            context.ConfigureForAuthoring(definition);

            var track = EnvironmentKit.BuildTrack(
                environmentRoot.transform,
                definition,
                environmentMaterials,
                EnvironmentMaterialKit.GetEnvironmentFolder(definition.EnvironmentId),
                EnvironmentLayoutVariant.Signature);

            var light = EnvironmentLightingBuilder.Apply(definition, environmentRoot.transform);
            if (light == null)
            {
                throw new InvalidOperationException("Lighting builder did not create a directional light.");
            }

            var layout = definition.TrackLayout;
            var spawnPosition =
                layout.SampleAtDistance(track.Centerline, 0f, out var spawnTangent) + (Vector3.up * 0.5f);
            var spawnForward = Flatten(spawnTangent);
            var spawnRotation = Quaternion.LookRotation(spawnForward, Vector3.up);

            var inputGo = new GameObject("PrototypeInput");
            var input = inputGo.AddComponent<LegacyKeyboardInputAdapter>();

            var vehiclePrefab = LoadRequired<GameObject>(VehiclePrefabPath, "vehicle prefab");
            var vehicleProfile = LoadRequired<VehiclePhysicsProfile>(
                DefaultVehicleProfilePath,
                "vehicle tuning preset");
            var baselineCamera = LoadRequired<CameraProfile>(BaselineCameraPath, "camera tuning preset");
            var roster = LoadRosterDefinitions();
            var rigPrefabs = LoadRigPrefabs(roster);

            var vehicleGo = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab, scene);
            vehicleGo.name = "PrototypeVehicle";
            vehicleGo.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var vehicle = vehicleGo.GetComponent<VehicleController>();
            if (vehicle == null)
            {
                throw new InvalidOperationException("Prototype vehicle prefab has no VehicleController.");
            }

            SetObject(vehicle, "profile", vehicleProfile);
            SetObject(vehicle, "inputSource", input);

            // Hero presentation is a scene-local addition on the prefab instance: the shared
            // prefab, its colliders, and every Phase-8 physics value remain untouched (D-057).
            SliceHeroVehicleKit.Apply(vehicleGo, sliceMaterials);

            var smokeAnchors = new[]
            {
                vehicleGo.transform.Find("Wheel RL Visual"),
                vehicleGo.transform.Find("Wheel RR Visual")
            };
            var vfx = SliceVfxKit.Build(vehicleGo, sliceMaterials, smokeAnchors);

            var engineGo = new GameObject("Engine Audio");
            engineGo.transform.SetParent(vehicleGo.transform, false);
            var engineSource = engineGo.AddComponent<AudioSource>();
            engineSource.clip = audioClips.EngineLoop;
            engineSource.loop = true;
            engineSource.playOnAwake = false;
            engineSource.spatialBlend = 1f;
            engineSource.rolloffMode = AudioRolloffMode.Linear;
            engineSource.minDistance = 4f;
            engineSource.maxDistance = 80f;
            engineSource.dopplerLevel = 0f;
            var engineAudio = engineGo.AddComponent<EngineAudioController>();
            SetObject(engineAudio, "telemetrySource", vehicle);
            SetObject(engineAudio, "source", engineSource);

            var tyreGo = new GameObject("Tyre Audio");
            tyreGo.transform.SetParent(vehicleGo.transform, false);
            tyreGo.transform.localPosition = new Vector3(0f, -0.15f, -1.2f);
            var tyreSource = tyreGo.AddComponent<AudioSource>();
            tyreSource.clip = audioClips.TyreScreechLoop;
            tyreSource.loop = true;
            tyreSource.playOnAwake = false;
            tyreSource.spatialBlend = 1f;
            tyreSource.rolloffMode = AudioRolloffMode.Linear;
            tyreSource.minDistance = 3f;
            tyreSource.maxDistance = 55f;
            tyreSource.dopplerLevel = 0f;
            var tyreAudio = tyreGo.AddComponent<TyreAudioController>();
            SetObject(tyreAudio, "telemetrySource", vehicle);
            SetObject(tyreAudio, "source", tyreSource);

            var impactAudio = vehicleGo.AddComponent<ImpactAudioController>();
            SetObject(impactAudio, "impactClip", audioClips.Impact);
            SetObject(impactAudio, "sparkTarget", vfx);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 60f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.SetPositionAndRotation(
                spawnPosition - (spawnForward * 7f) + (Vector3.up * 2.5f),
                spawnRotation);
            var cameraRig = cameraGo.AddComponent<VehicleCameraRig>();
            SetObject(cameraRig, "profile", baselineCamera);
            SetObject(cameraRig, "target", vehicleGo.transform);
            SetObject(cameraRig, "telemetrySource", vehicle);
            SetInt(cameraRig, "obstructionMask", 1 << TrackLayer);

            var raceGo = new GameObject("Race");
            var race = raceGo.AddComponent<RaceController>();
            SetFloat(race, "countdownSeconds", 3f);
            SetObject(race, "vehicle", vehicle);
            SetObject(race, "appInputSource", input);

            var pauseDriver = raceGo.AddComponent<SlicePauseDriver>();
            SetObject(pauseDriver, "race", race);

            var uiAudio = raceGo.AddComponent<SliceUiAudioController>();
            SetObject(uiAudio, "race", race);
            SetObject(uiAudio, "countdownBeep", audioClips.CountdownBeep);
            SetObject(uiAudio, "countdownGo", audioClips.CountdownGo);
            SetObject(uiAudio, "pauseBeep", audioClips.PauseBeep);
            SetObject(uiAudio, "resumeBeep", audioClips.ResumeBeep);
            SetObject(uiAudio, "finishSting", audioClips.FinishSting);

            var gateRoot = new GameObject("Race Gates").transform;
            gateRoot.SetParent(environmentRoot.transform, false);
            var checkpointCount = EnvironmentRaceRules.CheckpointCount;
            var checkpoints = new CheckpointTrigger[checkpointCount];
            for (var i = 0; i < checkpoints.Length; i++)
            {
                var gatePosition = layout.SampleAtDistance(
                    track.Centerline,
                    EnvironmentRaceRules.GetCheckpointDistance(layout, i),
                    out var gateTangent);
                checkpoints[i] = CreateCheckpoint(
                    gateRoot,
                    gatePosition,
                    gateTangent,
                    i,
                    layout,
                    race,
                    environmentMaterials);
            }

            var finishPosition = layout.SampleAtDistance(
                track.Centerline,
                EnvironmentRaceRules.GetFinishDistance(layout),
                out var finishTangent);
            var finishTrigger = CreateFinish(
                gateRoot,
                finishPosition,
                finishTangent,
                layout,
                race,
                environmentMaterials);

            var raceSerialized = new SerializedObject(race);
            var checkpointArray = raceSerialized.FindProperty("checkpoints");
            if (checkpointArray == null)
            {
                throw new InvalidOperationException("RaceController has no serialized checkpoints array.");
            }

            checkpointArray.arraySize = checkpoints.Length;
            for (var i = 0; i < checkpoints.Length; i++)
            {
                checkpointArray.GetArrayElementAtIndex(i).objectReferenceValue = checkpoints[i];
            }

            var finishProperty = raceSerialized.FindProperty("finishTrigger");
            if (finishProperty == null)
            {
                throw new InvalidOperationException("RaceController has no serialized finishTrigger field.");
            }

            finishProperty.objectReferenceValue = finishTrigger;
            raceSerialized.ApplyModifiedPropertiesWithoutUndo();

            CreateStartFinishLine(
                environmentRoot.transform,
                layout,
                track.Centerline,
                spawnPosition,
                finishPosition,
                finishTangent,
                environmentMaterials);

            var overlayGo = new GameObject("TelemetryOverlay");
            var overlay = overlayGo.AddComponent<VehicleTelemetryOverlay>();
            SetObject(overlay, "telemetrySource", vehicle);

            // Slice UI assets are created and refreshed only after every generated mesh and
            // material has been written, so the loads below cannot observe fake-null objects.
            SliceAssetBuilder.EnsureSliceAssets();
            var panelSettings = LoadRequired<PanelSettings>(
                SliceAssetBuilder.PanelSettingsPath,
                "slice panel settings");

            var hudGo = new GameObject("SliceHud");
            var uiDocument = hudGo.AddComponent<UIDocument>();
            uiDocument.panelSettings = panelSettings;
            uiDocument.sortingOrder = 10;
            var presenter = hudGo.AddComponent<SliceHudPresenter>();
            SetObject(presenter, "race", race);
            SetObject(presenter, "telemetrySource", vehicle);
            SetObject(presenter, "document", uiDocument);

            var probeGo = new GameObject("PerformanceProbe");
            probeGo.AddComponent<PrototypePerformanceProbe>();

            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            var rosterGo = new GameObject("VehicleRoster");
            var rosterCatalog = rosterGo.AddComponent<VehicleRosterCatalog>();
            rosterCatalog.ConfigureForAuthoring(roster);
            if (!rosterCatalog.TryValidate(out var rosterError))
            {
                throw new InvalidOperationException("Generated roster catalog is invalid: " + rosterError);
            }

            var rigsGo = new GameObject("VehicleRigs");
            var rigCatalog = rigsGo.AddComponent<VehicleRigCatalog>();
            rigCatalog.ConfigureForAuthoring(roster, rigPrefabs);
            if (!rigCatalog.TryValidate(out var rigError))
            {
                throw new InvalidOperationException("Generated rig catalog is invalid: " + rigError);
            }

            var spawnerGo = new GameObject("VehicleSelectionSpawner");
            var spawner = spawnerGo.AddComponent<VehicleSelectionSpawner>();
            SetObject(spawner, "rigCatalog", rigCatalog);
            SetObject(spawner, "templateVehicle", vehicleGo);
            SetObject(spawner, "appInputSource", input);
            SetObject(spawner, "race", race);
            SetObject(spawner, "cameraRig", cameraRig);
            SetObject(spawner, "hudPresenter", presenter);
            SetObject(spawner, "telemetryOverlay", overlay);
            SetString(spawner, "debugGarageScene", "Garage");

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
                " | landmarks=" + report.LandmarkCount +
                " | water=" + report.WaterCount +
                " | materials=" + report.MaterialCount +
                " | realtimeLighting=true bake=false" +
                " | cameras=" + report.CameraCount +
                " | directionalLights=" + report.DirectionalLightCount +
                " | checkpoints=" + report.CheckpointCount +
                " | hud=" + report.HudCount +
                " | audio=" + report.AudioControllerCount +
                " | vfx=" + report.VfxCount +
                " | spawner=" + report.SpawnerCount +
                " | missingScripts=" + report.MissingScriptCount +
                " | missingReferences=" + report.MissingReferenceCount);

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
                    "Environment content is authored as an open point-to-point layout; " +
                    "closed-loop layouts are not supported by the approved race format (D-057).");
            }

            var raceIssues = EnvironmentRaceRules.Validate(definition.TrackLayout);
            if (raceIssues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Environment race rules failed:\n" + string.Join("\n", raceIssues));
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
        /// Audits a generated scene: zero missing scripts/references, one valid context, the
        /// complete race stack, one realtime directional light, a skybox/sun, a readable road, and
        /// no colliders under decorative content.
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
            var missingScripts = CountMissingScripts(roots);
            if (missingScripts > 0)
            {
                issues.Add("Generated scene contains " + missingScripts + " missing script reference(s).");
            }

            var missingReferences = 0;
            missingReferences += AuditSceneContext(roots, definition, issues);
            missingReferences += AuditCamera(roots, issues);
            missingReferences += AuditRace(roots, issues);
            missingReferences += AuditHud(roots, issues);
            missingReferences += AuditAudio(roots, issues);
            missingReferences += AuditVehicleSelection(roots, issues);
            missingReferences += AuditLighting(roots, issues);
            missingReferences += AuditReadableTrack(roots, definition, issues);

            if (issues.Count > 0)
            {
                throw new InvalidOperationException(
                    "Environment scene audit failed:\n" + string.Join("\n", issues));
            }

            var decorationRoot = FindTransform(roots, "Track/Decor");
            var landmarkRoot = FindTransform(roots, "Track/Landmarks");
            var waterRoot = FindTransform(roots, "Track/Water");
            var cameras = new List<Camera>();
            Collect(roots, cameras);
            var lights = new List<Light>();
            Collect(roots, lights);
            var checkpoints = new List<CheckpointTrigger>();
            Collect(roots, checkpoints);
            var hud = new List<SliceHudPresenter>();
            Collect(roots, hud);
            var vfx = new List<VehicleVfxController>();
            Collect(roots, vfx);
            var spawners = new List<VehicleSelectionSpawner>();
            Collect(roots, spawners);

            return new EnvironmentBuildReport(
                definition.EnvironmentId,
                definition.Theme,
                string.IsNullOrEmpty(scenePath) ? GetScenePath(definition) : scenePath,
                definition.TrackLayout.Segments.Length,
                definition.TrackLayout.TotalLength,
                decorationRoot != null ? decorationRoot.childCount : 0,
                landmarkRoot != null ? landmarkRoot.childCount : 0,
                waterRoot != null ? waterRoot.childCount : 0,
                CountMaterials(roots),
                cameras.Count,
                CountRealtimeDirectionalLights(lights),
                checkpoints.Count,
                hud.Count,
                CountAudioControllers(roots),
                vfx.Count,
                spawners.Count,
                missingScripts,
                missingReferences);
        }

        private static int AuditSceneContext(
            GameObject[] roots,
            EnvironmentDefinition definition,
            List<string> issues)
        {
            var contexts = new List<EnvironmentSceneContext>();
            Collect(roots, contexts);
            if (contexts.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one EnvironmentSceneContext (found " +
                           contexts.Count + ").");
                return 0;
            }

            var references = 0;
            if (contexts[0].Definition != definition)
            {
                issues.Add("EnvironmentSceneContext does not reference the built definition.");
                references++;
            }

            if (!contexts[0].TryValidate(out var contextError))
            {
                issues.Add(contextError);
            }

            return references;
        }

        private static int AuditCamera(GameObject[] roots, List<string> issues)
        {
            var references = 0;
            var cameras = new List<Camera>();
            Collect(roots, cameras);
            if (cameras.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one camera (found " + cameras.Count + ").");
            }

            var listeners = new List<AudioListener>();
            Collect(roots, listeners);
            if (listeners.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one AudioListener (found " +
                           listeners.Count + ").");
            }

            var rig = RequireSingle<VehicleCameraRig>(roots, "VehicleCameraRig", issues);
            if (rig != null)
            {
                references += RequireReferences(
                    rig,
                    "VehicleCameraRig",
                    issues,
                    "profile",
                    "target",
                    "telemetrySource");
            }

            return references;
        }

        private static int AuditRace(GameObject[] roots, List<string> issues)
        {
            var references = 0;
            var race = RequireSingle<RaceController>(roots, "RaceController", issues);
            if (race != null)
            {
                references += RequireReferences(
                    race,
                    "RaceController",
                    issues,
                    "vehicle",
                    "appInputSource");
                references += RequireReferenceArray(
                    race,
                    "RaceController",
                    "checkpoints",
                    EnvironmentRaceRules.CheckpointCount,
                    issues);
                references += RequireReferences(race, "RaceController", issues, "finishTrigger");
            }

            var checkpoints = new List<CheckpointTrigger>();
            Collect(roots, checkpoints);
            if (checkpoints.Count != EnvironmentRaceRules.CheckpointCount)
            {
                issues.Add("Generated scene must contain exactly " +
                           EnvironmentRaceRules.CheckpointCount + " checkpoint gates (found " +
                           checkpoints.Count + ").");
            }

            var seen = new bool[EnvironmentRaceRules.CheckpointCount];
            for (var i = 0; i < checkpoints.Count; i++)
            {
                var index = checkpoints[i].Index;
                if (index < 0 || index >= seen.Length || seen[index])
                {
                    issues.Add("Checkpoint gate has an out-of-range or duplicate index " + index + ".");
                }
                else
                {
                    seen[index] = true;
                }

                references += RequireReferences(checkpoints[i], "CheckpointTrigger " + index, issues, "race");
                var collider = checkpoints[i].GetComponent<BoxCollider>();
                if (collider == null || !collider.isTrigger)
                {
                    issues.Add("Checkpoint gate " + index + " has no trigger BoxCollider.");
                }
            }

            var finishes = new List<FinishTrigger>();
            Collect(roots, finishes);
            if (finishes.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one FinishTrigger (found " +
                           finishes.Count + ").");
            }
            else
            {
                references += RequireReferences(finishes[0], "FinishTrigger", issues, "race");
                var collider = finishes[0].GetComponent<BoxCollider>();
                if (collider == null || !collider.isTrigger)
                {
                    issues.Add("Finish gate has no trigger BoxCollider.");
                }
            }

            var pause = RequireSingle<SlicePauseDriver>(roots, "SlicePauseDriver", issues);
            if (pause != null)
            {
                references += RequireReferences(pause, "SlicePauseDriver", issues, "race");
            }

            return references;
        }

        private static int AuditHud(GameObject[] roots, List<string> issues)
        {
            var references = 0;
            var presenter = RequireSingle<SliceHudPresenter>(roots, "SliceHudPresenter", issues);
            if (presenter != null)
            {
                references += RequireReferences(
                    presenter,
                    "SliceHudPresenter",
                    issues,
                    "race",
                    "telemetrySource",
                    "document");
            }

            var documents = new List<UIDocument>();
            Collect(roots, documents);
            if (documents.Count != 1)
            {
                issues.Add("Generated scene must contain exactly one UIDocument (found " +
                           documents.Count + ").");
            }
            else if (documents[0].panelSettings == null)
            {
                issues.Add("UIDocument has no PanelSettings reference.");
                references++;
            }

            var overlay = RequireSingle<VehicleTelemetryOverlay>(roots, "VehicleTelemetryOverlay", issues);
            if (overlay != null)
            {
                references += RequireReferences(
                    overlay,
                    "VehicleTelemetryOverlay",
                    issues,
                    "telemetrySource");
            }

            if (RequireSingle<PrototypePerformanceProbe>(roots, "PrototypePerformanceProbe", issues) != null)
            {
                // Presence is the contract; the probe has no required object references.
            }

            return references;
        }

        private static int AuditAudio(GameObject[] roots, List<string> issues)
        {
            var references = 0;

            var engine = RequireSingle<EngineAudioController>(roots, "EngineAudioController", issues);
            if (engine != null)
            {
                references += RequireReferences(
                    engine,
                    "EngineAudioController",
                    issues,
                    "telemetrySource",
                    "source");
                references += RequireAudioClip(engine, "source", "EngineAudioController", issues);
            }

            var tyre = RequireSingle<TyreAudioController>(roots, "TyreAudioController", issues);
            if (tyre != null)
            {
                references += RequireReferences(
                    tyre,
                    "TyreAudioController",
                    issues,
                    "telemetrySource",
                    "source");
                references += RequireAudioClip(tyre, "source", "TyreAudioController", issues);
            }

            var impact = RequireSingle<ImpactAudioController>(roots, "ImpactAudioController", issues);
            if (impact != null)
            {
                references += RequireReferences(
                    impact,
                    "ImpactAudioController",
                    issues,
                    "impactClip",
                    "sparkTarget");
            }

            var uiAudio = RequireSingle<SliceUiAudioController>(roots, "SliceUiAudioController", issues);
            if (uiAudio != null)
            {
                references += RequireReferences(
                    uiAudio,
                    "SliceUiAudioController",
                    issues,
                    "race",
                    "countdownBeep",
                    "countdownGo",
                    "pauseBeep",
                    "resumeBeep",
                    "finishSting");
            }

            if (RequireSingle<VehicleVfxController>(roots, "VehicleVfxController", issues) != null)
            {
                // Presence is the contract; VFX system references are authored by SliceVfxKit.
            }

            return references;
        }

        private static int AuditVehicleSelection(GameObject[] roots, List<string> issues)
        {
            var references = 0;
            var vehicle = RequireSingle<VehicleController>(roots, "VehicleController", issues);
            if (vehicle != null)
            {
                references += RequireReferences(
                    vehicle,
                    "VehicleController",
                    issues,
                    "profile",
                    "inputSource");
            }

            var roster = RequireSingle<VehicleRosterCatalog>(roots, "VehicleRosterCatalog", issues);
            if (roster != null && !roster.TryValidate(out var rosterError))
            {
                issues.Add("VehicleRosterCatalog is invalid: " + rosterError);
            }

            var rigCatalog = RequireSingle<VehicleRigCatalog>(roots, "VehicleRigCatalog", issues);
            if (rigCatalog != null && !rigCatalog.TryValidate(out var rigError))
            {
                issues.Add("VehicleRigCatalog is invalid: " + rigError);
            }

            var spawner = RequireSingle<VehicleSelectionSpawner>(roots, "VehicleSelectionSpawner", issues);
            if (spawner != null)
            {
                references += RequireReferences(
                    spawner,
                    "VehicleSelectionSpawner",
                    issues,
                    "rigCatalog",
                    "templateVehicle",
                    "appInputSource",
                    "race",
                    "cameraRig",
                    "hudPresenter",
                    "telemetryOverlay");
            }

            return references;
        }

        private static int AuditLighting(GameObject[] roots, List<string> issues)
        {
            var references = 0;
            var lights = new List<Light>();
            Collect(roots, lights);
            var realtimeDirectional = CountRealtimeDirectionalLights(lights);
            if (realtimeDirectional != 1)
            {
                issues.Add("Generated scene must contain exactly one realtime directional light " +
                           "(found " + realtimeDirectional + ").");
            }

            if (RenderSettings.skybox == null)
            {
                issues.Add("Generated scene has no skybox material.");
                references++;
            }

            if (RenderSettings.sun == null)
            {
                issues.Add("Generated scene has no RenderSettings sun.");
                references++;
            }

            return references;
        }

        private static int AuditReadableTrack(
            GameObject[] roots,
            EnvironmentDefinition definition,
            List<string> issues)
        {
            var references = 0;
            var road = FindTransform(roots, "Track/Road");
            if (road == null)
            {
                issues.Add("Generated scene is missing Track/Road.");
                return references;
            }

            var renderer = road.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
            {
                issues.Add("Track/Road has no MeshRenderer material.");
                references++;
            }

            var collider = road.GetComponent<MeshCollider>();
            if (collider == null || collider.sharedMesh == null)
            {
                issues.Add("Track/Road has no drivable MeshCollider.");
                references++;
            }

            references += RequireNoColliders(roots, "Track/Decor", issues);
            references += RequireNoColliders(roots, "Track/Landmarks", issues);
            references += RequireNoColliders(roots, "Track/Water", issues);
            references += RequireNoColliders(roots, "Track/Neon Lane Markers", issues);

            var gates = FindTransform(roots, "Race Gates");
            if (gates == null)
            {
                issues.Add("Generated scene is missing Race Gates.");
                return references;
            }

            // Gate readability: the trigger corridor must be clear of decorative colliders. The
            // kit removes every decoration collider, so this is a structural guarantee rather than
            // a geometric approximation.
            for (var i = 0; i < gates.childCount; i++)
            {
                var gate = gates.GetChild(i);
                var trigger = gate.GetComponent<Collider>();
                if (trigger == null)
                {
                    issues.Add("Gate '" + gate.name + "' has no trigger collider.");
                    references++;
                }

                var visuals = gate.GetComponentsInChildren<Collider>(true);
                if (visuals.Length != 1)
                {
                    issues.Add("Gate '" + gate.name +
                               "' must contain exactly one collider (its trigger); found " +
                               visuals.Length + ".");
                    references++;
                }
            }

            if (definition.TrackLayout.TotalLength < EnvironmentRaceRules.MinimumGateSpacingMeters * 2f)
            {
                issues.Add("Layout is too short for the approved gate format.");
            }

            return references;
        }

        private static int RequireNoColliders(GameObject[] roots, string path, List<string> issues)
        {
            var root = FindTransform(roots, path);
            if (root == null)
            {
                return 0;
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            if (colliders.Length == 0)
            {
                return 0;
            }

            issues.Add(path + " contains " + colliders.Length +
                       " collider(s); decorative content must be collider-free (D-057).");
            return colliders.Length;
        }

        private static int RequireReferences(
            Component component,
            string label,
            List<string> issues,
            params string[] fields)
        {
            var references = 0;
            var serialized = new SerializedObject(component);
            for (var i = 0; i < fields.Length; i++)
            {
                var property = serialized.FindProperty(fields[i]);
                if (property == null)
                {
                    issues.Add(label + " is missing serialized field '" + fields[i] + "'.");
                    references++;
                }
                else if (property.objectReferenceValue == null)
                {
                    issues.Add(label + " has an unassigned '" + fields[i] + "' reference.");
                    references++;
                }
            }

            return references;
        }

        private static int RequireReferenceArray(
            Component component,
            string label,
            string field,
            int expectedSize,
            List<string> issues)
        {
            var references = 0;
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                issues.Add(label + " is missing serialized array '" + field + "'.");
                return 1;
            }

            if (property.arraySize != expectedSize)
            {
                issues.Add(label + "." + field + " must contain " + expectedSize +
                           " entries (found " + property.arraySize + ").");
                references++;
            }

            for (var i = 0; i < property.arraySize; i++)
            {
                if (property.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    issues.Add(label + "." + field + "[" + i + "] is unassigned.");
                    references++;
                }
            }

            return references;
        }

        private static int RequireAudioClip(
            Component component,
            string sourceField,
            string label,
            List<string> issues)
        {
            var serialized = new SerializedObject(component);
            var property = serialized.FindProperty(sourceField);
            var source = property != null ? property.objectReferenceValue as AudioSource : null;
            if (source == null)
            {
                issues.Add(label + " has no AudioSource reference.");
                return 1;
            }

            if (source.clip == null)
            {
                issues.Add(label + " AudioSource has no clip.");
                return 1;
            }

            return 0;
        }

        private static T RequireSingle<T>(GameObject[] roots, string label, List<string> issues)
            where T : Component
        {
            var components = new List<T>();
            Collect(roots, components);
            if (components.Count == 1)
            {
                return components[0];
            }

            issues.Add("Generated scene must contain exactly one " + label +
                       " (found " + components.Count + ").");
            return components.Count > 0 ? components[0] : null;
        }

        private static int CountMissingScripts(GameObject[] roots)
        {
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

            return missingScripts;
        }

        private static int CountMaterials(GameObject[] roots)
        {
            var environment = FindTransform(roots, "Environment");
            var renderers = new List<Renderer>();
            if (environment != null)
            {
                renderers.AddRange(environment.GetComponentsInChildren<Renderer>(true));
            }
            else
            {
                Collect(roots, renderers);
            }

            var materials = new HashSet<Material>();
            for (var i = 0; i < renderers.Count; i++)
            {
                var shared = renderers[i].sharedMaterials;
                for (var j = 0; j < shared.Length; j++)
                {
                    if (shared[j] != null)
                    {
                        materials.Add(shared[j]);
                    }
                }
            }

            return materials.Count;
        }

        private static int CountAudioControllers(GameObject[] roots)
        {
            var count = 0;
            count += Count<EngineAudioController>(roots);
            count += Count<TyreAudioController>(roots);
            count += Count<ImpactAudioController>(roots);
            count += Count<SliceUiAudioController>(roots);
            return count;
        }

        private static int Count<T>(GameObject[] roots) where T : Component
        {
            var components = new List<T>();
            Collect(roots, components);
            return components.Count;
        }

        private static int CountRealtimeDirectionalLights(List<Light> lights)
        {
            var count = 0;
            for (var i = 0; i < lights.Count; i++)
            {
                if (lights[i].type == LightType.Directional &&
                    lights[i].lightmapBakeType == LightmapBakeType.Realtime)
                {
                    count++;
                }
            }

            return count;
        }

        private static CheckpointTrigger CreateCheckpoint(
            Transform parent,
            Vector3 position,
            Vector3 tangent,
            int index,
            TrackLayout layout,
            RaceController race,
            EnvironmentMaterialSet materials)
        {
            var gate = CreateGate(
                parent,
                "Checkpoint " + index,
                position,
                tangent,
                layout,
                materials,
                materials.Accent,
                true);
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(
                EnvironmentRaceRules.GetGateWidth(layout),
                EnvironmentRaceRules.GateHeight,
                EnvironmentRaceRules.GateDepth);
            box.center = new Vector3(0f, EnvironmentRaceRules.GateHeight * 0.5f, 0f);
            var checkpoint = gate.AddComponent<CheckpointTrigger>();
            SetInt(checkpoint, "index", index);
            SetObject(checkpoint, "race", race);
            return checkpoint;
        }

        private static FinishTrigger CreateFinish(
            Transform parent,
            Vector3 position,
            Vector3 tangent,
            TrackLayout layout,
            RaceController race,
            EnvironmentMaterialSet materials)
        {
            var gate = CreateGate(
                parent,
                "Finish",
                position,
                tangent,
                layout,
                materials,
                materials.Emissive,
                false);
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(
                EnvironmentRaceRules.GetGateWidth(layout),
                EnvironmentRaceRules.GateHeight,
                EnvironmentRaceRules.GateDepth);
            box.center = new Vector3(0f, EnvironmentRaceRules.GateHeight * 0.5f, 0f);
            var finish = gate.AddComponent<FinishTrigger>();
            SetObject(finish, "race", race);
            return finish;
        }

        private static GameObject CreateGate(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 tangent,
            TrackLayout layout,
            EnvironmentMaterialSet materials,
            Material accent,
            bool checkpoint)
        {
            var gate = new GameObject(name);
            gate.layer = TrackLayer;
            gate.transform.SetParent(parent, false);
            gate.transform.SetPositionAndRotation(
                position,
                Quaternion.LookRotation(Flatten(tangent), Vector3.up));

            var halfRoad = layout.RoadWidth * 0.5f;
            var pillarOffset = halfRoad + 0.75f;
            for (var side = -1; side <= 1; side += 2)
            {
                AddChildPrimitive(
                    gate.transform,
                    "Gate Pillar " + (side > 0 ? "R" : "L"),
                    PrimitiveType.Cube,
                    new Vector3(side * pillarOffset, 2.6f, 0f),
                    Quaternion.identity,
                    new Vector3(0.4f, 5.2f, 0.4f),
                    materials.Barrier);
            }

            AddChildPrimitive(
                gate.transform,
                "Gate Beam",
                PrimitiveType.Cube,
                new Vector3(0f, 5.1f, 0f),
                Quaternion.identity,
                new Vector3((pillarOffset * 2f) + 0.4f, 0.5f, 0.5f),
                materials.Barrier);

            AddChildPrimitive(
                gate.transform,
                checkpoint ? "Gate Marker" : "Finish Marker",
                PrimitiveType.Cube,
                new Vector3(0f, 4.55f, 0f),
                Quaternion.identity,
                new Vector3(layout.RoadWidth, 0.55f, 0.12f),
                accent);

            AddChildPrimitive(
                gate.transform,
                "Gate Floor Strip",
                PrimitiveType.Cube,
                new Vector3(0f, 0.04f, 0f),
                Quaternion.identity,
                new Vector3(layout.RoadWidth - 0.3f, 0.03f, 1.1f),
                accent);

            return gate;
        }

        private static void CreateStartFinishLine(
            Transform parent,
            TrackLayout layout,
            Vector3[] centerline,
            Vector3 spawnPosition,
            Vector3 finishPosition,
            Vector3 finishTangent,
            EnvironmentMaterialSet materials)
        {
            var startLine = new GameObject("Start Line");
            startLine.transform.SetParent(parent, false);
            var startRotation = Quaternion.LookRotation(
                Flatten(layout.SampleAtDistance(centerline, 0f, out var startTangent)),
                Vector3.up);
            startLine.transform.SetPositionAndRotation(
                spawnPosition - (Vector3.up * 0.5f),
                startRotation);
            AddChildPrimitive(
                startLine.transform,
                "Start Line Surface",
                PrimitiveType.Cube,
                new Vector3(0f, 0.04f, 0f),
                Quaternion.identity,
                new Vector3(layout.RoadWidth, 0.03f, 1.4f),
                materials.RoadEdge);

            var finishLine = new GameObject("Finish Line");
            finishLine.transform.SetParent(parent, false);
            finishLine.transform.SetPositionAndRotation(
                finishPosition,
                Quaternion.LookRotation(Flatten(finishTangent), Vector3.up));
            AddChildPrimitive(
                finishLine.transform,
                "Finish Line Surface",
                PrimitiveType.Cube,
                new Vector3(0f, 0.04f, 0f),
                Quaternion.identity,
                new Vector3(layout.RoadWidth, 0.03f, 1.4f),
                materials.Emissive);
        }

        private static void AddChildPrimitive(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.layer = 0;
            GameObjectUtility.SetStaticEditorFlags(
                go,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
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
                if (string.Equals(roots[i].name, path, StringComparison.Ordinal))
                {
                    return roots[i].transform;
                }

                var child = roots[i].transform.Find(path);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }

        private static VehicleDefinition[] LoadRosterDefinitions()
        {
            var guids = AssetDatabase.FindAssets(
                "t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);

            var definitions = new VehicleDefinition[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                definitions[i] = LoadRequired<VehicleDefinition>(paths[i], "vehicle definition");
            }

            return definitions;
        }

        private static GameObject[] LoadRigPrefabs(VehicleDefinition[] roster)
        {
            var prefabs = new GameObject[roster.Length];
            for (var i = 0; i < roster.Length; i++)
            {
                prefabs[i] = LoadRequired<GameObject>(
                    VehicleRigBuilder.GetVariantPath(roster[i]),
                    "rig prefab for " + roster[i].VehicleId);
            }

            return prefabs;
        }

        private static T LoadRequired<T>(string path, string description) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing " + description + ": " + path);
            }

            return asset;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
