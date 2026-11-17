using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic M8 content builder (T-10.8.3–T-10.8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Creates the three championship assets, the fifteen event assets, the event catalog scene,
    /// and the preauthored AI opponent prefab, then derives the five reverse configuration scenes
    /// through <see cref="EnvironmentSceneBuilder"/> (signature scenes stay read-only, D-064).
    /// </para>
    /// <para>
    /// The builder is idempotent: existing assets are reconfigured in place so a rebuild cannot
    /// fork the content into duplicate ids. It validates the complete catalog and the build
    /// readiness gate before exiting successfully.
    /// </para>
    /// </remarks>
    public static class ChampionshipContentBuilder
    {
        /// <summary>Folder that owns the three championship definitions.</summary>
        public const string ChampionshipsFolder = "Assets/Data/Championships";

        /// <summary>Folder that owns the fifteen event definitions.</summary>
        public const string EventsFolder = "Assets/Data/Events";

        /// <summary>Folder that owns the preauthored AI opponent prefab.</summary>
        public const string EventPrefabsFolder = "Assets/Prefabs/Events";

        /// <summary>Project-relative scene that owns the runtime resolution catalogs.</summary>
        public const string CatalogScenePath = "Assets/Scenes/ChampionshipCatalog.unity";

        /// <summary>Preauthored AI opponent prefab used by every grid launch.</summary>
        public const string AiOpponentPrefabPath = EventPrefabsFolder + "/AIOpponent.prefab";

        private const string LogPrefix = "[VelocityRush.ChampionshipContent] ";
        private const string SportRigPath = "Assets/Prefabs/Vehicles/Sport/sport-velocity-gt_Rig.prefab";
        private const string CatalogRootName = "Championship Catalog";

        private const string C1 = "c1-ignition-series";
        private const string C2 = "c2-apex-challenger";
        private const string C3 = "c3-velocity-masters";

        private static readonly string[] ChampionshipIds = { C1, C2, C3 };
        private static readonly string[] ChampionshipNamesEn =
        {
            "Ignition Series",
            "Apex Challenger",
            "Velocity Masters"
        };

        private static readonly string[] ChampionshipNamesAr =
        {
            "سلسلة الانطلاق",
            "تحدي القمة",
            "أسياد السرعة"
        };

        private static readonly string[] DifficultyNamesEn = { "Low", "Medium", "High" };
        private static readonly string[] DifficultyNamesAr = { "منخفضة", "متوسطة", "عالية" };

        private static readonly float[] PaceScales = { 0.90f, 0.97f, 1.03f };

        /// <summary>
        /// The approved event matrix: three championship indexes × five rounds, covering all ten
        /// environment/configuration pairs with exactly 9 Circuit Races and 6 Time Trials.
        /// </summary>
        private static readonly EventSpec[] Matrix =
        {
            // C1 — Ignition Series (pace 0.90)
            new EventSpec(0, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.TimeTrial),
            new EventSpec(0, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(0, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial),
            new EventSpec(0, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(0, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Reverse, RaceEventType.CircuitRace),

            // C2 — Apex Challenger (pace 0.97)
            new EventSpec(1, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse, RaceEventType.CircuitRace),
            new EventSpec(1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial),
            new EventSpec(1, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(1, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial),
            new EventSpec(1, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),

            // C3 — Velocity Masters (pace 1.03)
            new EventSpec(2, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(2, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(2, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial),
            new EventSpec(2, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace),
            new EventSpec(2, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial)
        };

        /// <summary>Summary of one complete content build.</summary>
        public readonly struct ContentBuildReport
        {
            public ContentBuildReport(
                int championshipCount,
                int eventCount,
                int reverseSceneCount,
                int circuitRaceCount,
                int timeTrialCount)
            {
                ChampionshipCount = championshipCount;
                EventCount = eventCount;
                ReverseSceneCount = reverseSceneCount;
                CircuitRaceCount = circuitRaceCount;
                TimeTrialCount = timeTrialCount;
            }

            public int ChampionshipCount { get; }
            public int EventCount { get; }
            public int ReverseSceneCount { get; }
            public int CircuitRaceCount { get; }
            public int TimeTrialCount { get; }
        }

        /// <summary>Menu entry that rebuilds all Stage-3 content.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Championship Content", false, 202)]
        public static void BuildFromMenu()
        {
            try
            {
                var report = BuildAll();
                EditorUtility.DisplayDialog(
                    "Championship content",
                    "Built " + report.ChampionshipCount + " championships, " + report.EventCount +
                    " events (" + report.CircuitRaceCount + " circuit / " + report.TimeTrialCount +
                    " time trial), and " + report.ReverseSceneCount + " reverse scenes.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + exception);
                EditorUtility.DisplayDialog(
                    "Championship content",
                    "Content build failed: " + exception.Message,
                    "OK");
            }
        }

        /// <summary>Batch entry point; exits 1 on any content, catalog, or readiness failure.</summary>
        public static void BuildAllFromCommandLine()
        {
            try
            {
                var report = BuildAll();
                Debug.Log(
                    LogPrefix + "Built " + report.ChampionshipCount + " championships, " +
                    report.EventCount + " events (" + report.CircuitRaceCount + " circuit / " +
                    report.TimeTrialCount + " time trial), and " + report.ReverseSceneCount +
                    " reverse scenes.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Content build failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Creates or updates every Stage-3 asset and scene, then validates the complete catalog
        /// and build readiness. Throws on the first blocking problem.
        /// </summary>
        public static ContentBuildReport BuildAll()
        {
            EnsureFolder(ChampionshipsFolder);
            EnsureFolder(EventsFolder);
            EnsureFolder(EventPrefabsFolder);

            var championships = EnsureChampionships();
            var events = EnsureEvents(championships);
            var aiPrefab = EnsureAiOpponentPrefab();
            EnsureCatalogScene(championships, events);

            // Reverse scenes are derived after the content assets so the readiness gate can see
            // the complete event matrix in the same run.
            var reverseReports = EnvironmentSceneBuilder.BuildReverseAll();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ValidateContent();

            var circuit = 0;
            var timeTrial = 0;
            for (var i = 0; i < events.Length; i++)
            {
                if (events[i].EventType == RaceEventType.CircuitRace)
                {
                    circuit++;
                }
                else if (events[i].EventType == RaceEventType.TimeTrial)
                {
                    timeTrial++;
                }
            }

            Debug.Log(LogPrefix + "AI opponent prefab: " + AssetDatabase.GetAssetPath(aiPrefab));

            return new ContentBuildReport(
                championships.Length,
                events.Length,
                reverseReports.Count,
                circuit,
                timeTrial);
        }

        private static ChampionshipDefinition[] EnsureChampionships()
        {
            var championships = new ChampionshipDefinition[ChampionshipIds.Length];
            for (var i = 0; i < ChampionshipIds.Length; i++)
            {
                var id = ChampionshipIds[i];
                var path = ChampionshipsFolder + "/" + id + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<ChampionshipDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<ChampionshipDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                var order = i + 1;
                var unlock = ChampionshipContentPolicy.GetExpectedChampionshipUnlockPolicy(order);
                definition.ConfigureForAuthoring(
                    id,
                    ChampionshipNamesEn[i],
                    ChampionshipNamesAr[i],
                    DifficultyNamesEn[i],
                    DifficultyNamesAr[i],
                    order,
                    PaceScales[i],
                    unlock,
                    "M8 championship " + order + " (" + id + ").",
                    i > 0 ? championships[i - 1] : null);

                EditorUtility.SetDirty(definition);
                championships[i] = definition;
            }

            var catalogProbe = new GameObject("Championship Catalog Probe");
            try
            {
                var catalog = catalogProbe.AddComponent<ChampionshipCatalog>();
                catalog.ConfigureForAuthoring(championships);
                if (!catalog.TryValidate(out var error))
                {
                    throw new InvalidOperationException("Championship assets are invalid: " + error);
                }
            }
            finally
            {
                Object.DestroyImmediate(catalogProbe);
            }

            return championships;
        }

        private static RaceEventDefinition[] EnsureEvents(ChampionshipDefinition[] championships)
        {
            var events = new RaceEventDefinition[Matrix.Length];
            for (var i = 0; i < Matrix.Length; i++)
            {
                var spec = Matrix[i];
                var championship = championships[spec.ChampionshipIndex];
                var id = championship.ChampionshipId + "-round-" + spec.Round;
                var path = EventsFolder + "/" + id + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<RaceEventDefinition>(path);
                if (definition == null)
                {
                    definition = ScriptableObject.CreateInstance<RaceEventDefinition>();
                    AssetDatabase.CreateAsset(definition, path);
                }

                var unlock = spec.Round == 1
                    ? ChampionshipContentPolicy.GetExpectedChampionshipUnlockPolicy(
                        championship.ProductionOrder)
                    : EventUnlockPolicy.RequiresPreviousEvent;

                definition.ConfigureForAuthoring(
                    id,
                    ChampionshipNamesEn[spec.ChampionshipIndex] + " - Round " + spec.Round,
                    ChampionshipNamesAr[spec.ChampionshipIndex] + " - الجولة " + spec.Round,
                    spec.Type,
                    spec.Theme,
                    spec.Variant,
                    championship.ChampionshipId,
                    spec.Round,
                    ChampionshipContentPolicy.GetExpectedOpponentMode(spec.Type),
                    unlock,
                    championship.PaceScale,
                    "M8 " + RaceEventTypeRules.GetDisplayName(spec.Type) + " on " +
                    EnvironmentThemeRules.GetDisplayName(spec.Theme) + " (" +
                    EnvironmentLayoutVariantRules.GetDisplayName(spec.Variant) + ").");

                EditorUtility.SetDirty(definition);
                events[i] = definition;
            }

            return events;
        }

        private static GameObject EnsureAiOpponentPrefab()
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(SportRigPath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing Sport rig prefab: " + SportRigPath);
            }

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(AiOpponentPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            var temp = PrefabUtility.InstantiatePrefab(source) as GameObject;
            if (temp == null)
            {
                throw new InvalidOperationException(
                    "Could not instantiate the opponent source prefab: " + SportRigPath);
            }

            try
            {
                PrefabUtility.UnpackPrefabInstance(
                    temp,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
                temp.name = "AIOpponent";

                var vehicle = temp.GetComponent<VehicleController>();
                if (vehicle == null)
                {
                    throw new InvalidOperationException(
                        "The opponent source prefab has no VehicleController.");
                }

                vehicle.enabled = false;

                if (temp.GetComponent<AIVisualRig>() == null)
                {
                    temp.AddComponent<AIVisualRig>();
                }

                if (temp.GetComponent<AIDriver>() == null)
                {
                    temp.AddComponent<AIDriver>();
                }

                var adapter = temp.GetComponent<AIVehicleInput>();
                if (adapter == null)
                {
                    adapter = temp.AddComponent<AIVehicleInput>();
                }

                // A disabled VehicleController never runs Awake, but binding the adapter keeps a
                // later manual enable safe and mirrors the Stage-4 grid scene wiring.
                var serialized = new SerializedObject(vehicle);
                var input = serialized.FindProperty("inputSource");
                if (input != null)
                {
                    input.objectReferenceValue = adapter;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                var prefab = PrefabUtility.SaveAsPrefabAsset(temp, AiOpponentPrefabPath);
                if (prefab == null)
                {
                    throw new InvalidOperationException(
                        "Failed to save the AI opponent prefab: " + AiOpponentPrefabPath);
                }

                return prefab;
            }
            finally
            {
                Object.DestroyImmediate(temp);
            }
        }

        private static ChampionshipCatalog EnsureCatalogScene(
            ChampionshipDefinition[] championships,
            RaceEventDefinition[] events)
        {
            var environmentDefinitions = EnvironmentCatalogBuilder.LoadDefinitions();
            var environmentProbe = new GameObject("Environment Catalog Probe");
            try
            {
                var probeCatalog = environmentProbe.AddComponent<EnvironmentCatalog>();
                probeCatalog.ConfigureForAuthoring(environmentDefinitions);
                if (!probeCatalog.TryValidate(out var environmentError))
                {
                    throw new InvalidOperationException(
                        "Environment catalog is invalid: " + environmentError);
                }
            }
            finally
            {
                Object.DestroyImmediate(environmentProbe);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(CatalogRootName);

            var championshipCatalog = root.AddComponent<ChampionshipCatalog>();
            championshipCatalog.ConfigureForAuthoring(championships);

            var eventCatalog = root.AddComponent<EventCatalog>();
            eventCatalog.ConfigureForAuthoring(events);
            if (!eventCatalog.TryValidate(out var eventError))
            {
                throw new InvalidOperationException("Event catalog is invalid: " + eventError);
            }

            var environmentCatalog = root.AddComponent<EnvironmentCatalog>();
            environmentCatalog.ConfigureForAuthoring(environmentDefinitions);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, CatalogScenePath))
            {
                throw new InvalidOperationException("Failed to save " + CatalogScenePath);
            }

            return championshipCatalog;
        }

        private static void ValidateContent()
        {
            // The catalog scene was just saved; load its components from the scene asset so the
            // validation reads exactly what a build would serialize.
            var scene = EditorSceneManager.OpenScene(CatalogScenePath, OpenSceneMode.Additive);
            try
            {
                var loadedCatalog = FindInScene<ChampionshipCatalog>(scene);
                var loadedEvents = FindInScene<EventCatalog>(scene);
                var environmentCatalog = FindInScene<EnvironmentCatalog>(scene);
                if (loadedCatalog == null || loadedEvents == null || environmentCatalog == null)
                {
                    throw new InvalidOperationException(
                        "The championship catalog scene is missing one of its three catalogs.");
                }

                var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(
                    loadedCatalog,
                    new List<RaceEventDefinition>(loadedEvents.Events),
                    environmentCatalog);
                if (!result.IsValid)
                {
                    throw new InvalidOperationException(
                        "Championship catalog validation failed:\n" +
                        string.Join("\n", result.Errors));
                }

                if (result.Warnings.Count > 0)
                {
                    Debug.LogWarning(
                        LogPrefix + "Catalog warnings:\n" + string.Join("\n", result.Warnings));
                }

                var readiness = ChampionshipBuildReadinessTool.RunReadinessCheck();
                if (!readiness.IsReady)
                {
                    throw new InvalidOperationException(
                        "Championship build readiness failed:\n" +
                        string.Join("\n", readiness.Issues));
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static T FindInScene<T>(UnityEngine.SceneManagement.Scene scene)
            where T : Component
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentInChildren<T>(true);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parts = folder.Split('/');
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

        private readonly struct EventSpec
        {
            public EventSpec(
                int championshipIndex,
                int round,
                EnvironmentTheme theme,
                EnvironmentLayoutVariant variant,
                RaceEventType type)
            {
                ChampionshipIndex = championshipIndex;
                Round = round;
                Theme = theme;
                Variant = variant;
                Type = type;
            }

            public int ChampionshipIndex { get; }
            public int Round { get; }
            public EnvironmentTheme Theme { get; }
            public EnvironmentLayoutVariant Variant { get; }
            public RaceEventType Type { get; }
        }
    }
}
