using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Development launcher for the fifteen M8 championship events (T-10.8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The menu window resolves an event through the authored catalogs, opens the matching
    /// environment/configuration scene, and creates a transient <see cref="RaceEventRuntime"/> in
    /// that scene before entering Play Mode. The scene asset is never saved, so the committed
    /// signature and derived reverse scenes stay unchanged.
    /// </para>
    /// <para>
    /// Batch mode is a resolution gate rather than a play session: it resolves the plan, verifies
    /// the launch scene exists, writes a JSON report, and exits non-zero on any problem.
    /// </para>
    /// </remarks>
    public static class ChampionshipRaceLauncher
    {
        /// <summary>Session key used to carry a launch request across the Play Mode domain reload.</summary>
        public const string PendingLaunchKey = "VelocityRush.ChampionshipLaunch.Pending";

        private const string LogPrefix = "[VelocityRush.ChampionshipLaunch] ";
        private const string RuntimeObjectName = "Race Event Runtime";
        private const string ReportSchemaVersion = "1.0";

        private static GameObject _probeRoot;
        private static ChampionshipCatalog _probeChampionships;
        private static EnvironmentCatalog _probeEnvironments;
        private static EventCatalog _probeEvents;

        /// <summary>Resolved launch description for one event.</summary>
        public readonly struct LaunchResolution
        {
            public LaunchResolution(
                RaceEventDefinition eventDefinition,
                ChampionshipDefinition championship,
                EnvironmentDefinition environment,
                EventLaunchPlan plan,
                string scenePath)
            {
                EventDefinition = eventDefinition;
                Championship = championship;
                Environment = environment;
                Plan = plan;
                ScenePath = scenePath;
            }

            public RaceEventDefinition EventDefinition { get; }
            public ChampionshipDefinition Championship { get; }
            public EnvironmentDefinition Environment { get; }
            public EventLaunchPlan Plan { get; }
            public string ScenePath { get; }
        }

        /// <summary>Opens the interactive event list.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Championship Event Launcher", false, 203)]
        public static void OpenLauncherWindow()
        {
            var window = EditorWindow.GetWindow<ChampionshipEventLauncherWindow>(
                false,
                "Event Launcher",
                true);
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        /// <summary>
        /// Batch entry point. Parses <c>-eventId &lt;id&gt;</c>, optional
        /// <c>-vehicleId &lt;id&gt;</c>, and optional <c>-championshipLaunchReportPath &lt;path&gt;</c>.
        /// </summary>
        public static void LaunchFromCommandLine()
        {
            try
            {
                var eventId = GetCommandLineValue("-eventId");
                if (string.IsNullOrEmpty(eventId))
                {
                    Debug.LogError(LogPrefix + "Missing required -eventId argument.");
                    EditorApplication.Exit(1);
                    return;
                }

                if (!TryResolve(eventId, GetCommandLineValue("-vehicleId"), out var resolution, out var error))
                {
                    Debug.LogError(LogPrefix + error);
                    WriteReport(null, error, GetCommandLineValue("-championshipLaunchReportPath"));
                    EditorApplication.Exit(1);
                    return;
                }

                WriteReport(resolution, null, GetCommandLineValue("-championshipLaunchReportPath"));
                Debug.Log(
                    LogPrefix + "Resolved '" + resolution.EventDefinition.EventId +
                    "' | scene=" + resolution.ScenePath +
                    " | variant=" + resolution.EventDefinition.LayoutVariant +
                    " | type=" + resolution.EventDefinition.EventType +
                    " | opponents=" + resolution.Plan.OpponentCount);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Launch resolution failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Resolves an event through the authored catalogs without changing the open scene.
        /// </summary>
        public static bool TryResolve(
            string eventId,
            string vehicleId,
            out LaunchResolution resolution,
            out string error)
        {
            resolution = default;
            error = null;

            if (!TryLoadCatalogs(
                    out var championshipCatalog,
                    out var environmentCatalog,
                    out var eventCatalog,
                    out error))
            {
                return false;
            }

            if (!EventLaunchPlanBuilder.TryBuild(
                    eventId,
                    eventCatalog,
                    championshipCatalog,
                    environmentCatalog,
                    out var plan,
                    out error))
            {
                return false;
            }

            var scenePath = ChampionshipBuildReadinessTool.GetEventScenePath(
                plan.EventDefinition.EnvironmentTheme,
                plan.EventDefinition.LayoutVariant);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                error = "Launch scene is missing: " + scenePath;
                return false;
            }

            var resolvedVehicleId = string.IsNullOrEmpty(vehicleId)
                ? SelectionContext.SelectedVehicleId
                : vehicleId.Trim();
            if (string.IsNullOrEmpty(resolvedVehicleId))
            {
                resolvedVehicleId = FindFirstVehicleId();
            }

            if (string.IsNullOrEmpty(resolvedVehicleId))
            {
                error = "No vehicle id was supplied and the roster contains no definitions.";
                return false;
            }

            resolution = new LaunchResolution(
                plan.EventDefinition,
                plan.ChampionshipDefinition,
                plan.EnvironmentDefinition,
                plan,
                scenePath);
            return true;
        }

        /// <summary>
        /// Prepares the event scene with a transient runtime object and enters Play Mode.
        /// </summary>
        public static bool LaunchFromMenu(string eventId, string vehicleId)
        {
            if (!TryResolve(eventId, vehicleId, out var resolution, out var error))
            {
                EditorUtility.DisplayDialog("Championship Event Launcher", error, "OK");
                return false;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return false;
            }

            var scene = EditorSceneManager.OpenScene(resolution.ScenePath, OpenSceneMode.Single);
            var runtimeObject = new GameObject(RuntimeObjectName);
            SceneManager.MoveGameObjectToScene(runtimeObject, scene);
            var runtime = runtimeObject.AddComponent<RaceEventRuntime>();

            if (!TryLoadCatalogs(
                    out var launchChampionships,
                    out var launchEnvironments,
                    out var launchEvents,
                    out var catalogError))
            {
                Debug.LogError(LogPrefix + catalogError);
                Object.DestroyImmediate(runtimeObject);
                return false;
            }

            var opponentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                ChampionshipContentBuilder.AiOpponentPrefabPath);
            var mode = resolution.EventDefinition.EventType == RaceEventType.CircuitRace
                ? RaceLaunchMode.Grid
                : RaceLaunchMode.Solo;

            runtime.ConfigureForAuthoring(
                launchChampionships,
                launchEnvironments,
                launchEvents,
                resolution.EventDefinition.EventId,
                mode,
                opponentPrefab);

            var resolvedVehicleId = string.IsNullOrEmpty(vehicleId)
                ? SelectionContext.SelectedVehicleId
                : vehicleId.Trim();
            if (string.IsNullOrEmpty(resolvedVehicleId))
            {
                resolvedVehicleId = FindFirstVehicleId();
            }

            SessionState.SetString(
                PendingLaunchKey,
                JsonUtility.ToJson(new PendingLaunch
                {
                    championshipId = resolution.Championship.ChampionshipId,
                    eventId = resolution.EventDefinition.EventId,
                    vehicleId = resolvedVehicleId
                }));

            Selection.activeObject = runtime;
            EditorGUIUtility.PingObject(runtime);
            Debug.Log(
                LogPrefix + "Prepared '" + resolution.EventDefinition.EventId + "' in " +
                resolution.ScenePath + " (" + mode + "). Entering Play Mode.");
            EditorApplication.isPlaying = true;
            return true;
        }

        private static void ApplyPendingSelection()
        {
            var json = SessionState.GetString(PendingLaunchKey, string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            SessionState.EraseString(PendingLaunchKey);
            PendingLaunch pending;
            try
            {
                pending = JsonUtility.FromJson<PendingLaunch>(json);
            }
            catch (Exception)
            {
                return;
            }

            if (pending == null ||
                string.IsNullOrEmpty(pending.championshipId) ||
                string.IsNullOrEmpty(pending.eventId) ||
                string.IsNullOrEmpty(pending.vehicleId))
            {
                return;
            }

            SelectionContext.SetCompleteSelection(
                pending.vehicleId,
                pending.championshipId,
                pending.eventId);
        }

        [InitializeOnLoadMethod]
        private static void RestorePendingSelectionOnLoad()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                ApplyPendingSelection();
            }
        }

        private static bool TryLoadCatalogs(
            out ChampionshipCatalog championshipCatalog,
            out EnvironmentCatalog environmentCatalog,
            out EventCatalog eventCatalog,
            out string error)
        {
            championshipCatalog = null;
            environmentCatalog = null;
            eventCatalog = null;

            if (!TryCreateCatalogProbe(out error))
            {
                return false;
            }

            championshipCatalog = _probeChampionships;
            environmentCatalog = _probeEnvironments;
            eventCatalog = _probeEvents;
            return true;
        }

        /// <summary>
        /// Builds a transient catalog probe from the authored asset folders and places it in the
        /// active scene. The probe is never saved; it exists so the editor launcher can resolve
        /// plans without depending on a catalog scene that may be closed or reloaded.
        /// </summary>
        private static bool TryCreateCatalogProbe(out string error)
        {
            if (_probeRoot != null &&
                _probeChampionships != null &&
                _probeEnvironments != null &&
                _probeEvents != null)
            {
                error = null;
                return true;
            }

            DestroyCatalogProbe();

            var championships = LoadAssets<ChampionshipDefinition>(
                ChampionshipContentBuilder.ChampionshipsFolder);
            var events = LoadAssets<RaceEventDefinition>(
                ChampionshipContentBuilder.EventsFolder);
            var environments = EnvironmentCatalogBuilder.LoadDefinitions();

            if (championships.Length == 0)
            {
                error = "No ChampionshipDefinition assets exist. Build the championship content first.";
                return false;
            }

            if (events.Length == 0)
            {
                error = "No RaceEventDefinition assets exist. Build the championship content first.";
                return false;
            }

            if (environments == null || environments.Length == 0)
            {
                error = "No EnvironmentDefinition assets exist. Generate the environment scenes first.";
                return false;
            }

            _probeRoot = new GameObject("Championship Launch Catalog Probe")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            _probeChampionships = _probeRoot.AddComponent<ChampionshipCatalog>();
            _probeChampionships.ConfigureForAuthoring(championships);
            if (!_probeChampionships.TryValidate(out var championshipError))
            {
                DestroyCatalogProbe();
                error = "Championship catalog is invalid: " + championshipError;
                return false;
            }

            _probeEvents = _probeRoot.AddComponent<EventCatalog>();
            _probeEvents.ConfigureForAuthoring(events);
            if (!_probeEvents.TryValidate(out var eventError))
            {
                DestroyCatalogProbe();
                error = "Event catalog is invalid: " + eventError;
                return false;
            }

            _probeEnvironments = _probeRoot.AddComponent<EnvironmentCatalog>();
            _probeEnvironments.ConfigureForAuthoring(environments);
            if (!_probeEnvironments.TryValidate(out var environmentError))
            {
                DestroyCatalogProbe();
                error = "Environment catalog is invalid: " + environmentError;
                return false;
            }

            error = null;
            return true;
        }

        private static void DestroyCatalogProbe()
        {
            if (_probeRoot != null)
            {
                Object.DestroyImmediate(_probeRoot);
            }

            _probeRoot = null;
            _probeChampionships = null;
            _probeEnvironments = null;
            _probeEvents = null;
        }

        private static T[] LoadAssets<T>(string folder) where T : Object
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return Array.Empty<T>();
            }

            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            var assets = new T[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
            }

            return assets;
        }

        private static string FindFirstVehicleId()
        {
            var guids = AssetDatabase.FindAssets(
                "t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            if (paths.Count == 0)
            {
                return null;
            }

            paths.Sort(StringComparer.Ordinal);
            var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[0]);
            return definition != null ? definition.VehicleId : null;
        }

        private static void WriteReport(
            LaunchResolution? resolution,
            string error,
            string outputPath)
        {
            var dto = new ReportDto
            {
                schemaVersion = ReportSchemaVersion,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                resolved = resolution.HasValue && string.IsNullOrEmpty(error),
                error = error ?? string.Empty
            };

            if (resolution.HasValue)
            {
                var value = resolution.Value;
                dto.eventId = value.EventDefinition.EventId;
                dto.championshipId = value.Championship.ChampionshipId;
                dto.environmentId = value.Environment.EnvironmentId;
                dto.layoutVariant = value.EventDefinition.LayoutVariant.ToString();
                dto.eventType = value.EventDefinition.EventType.ToString();
                dto.paceScale = value.Championship.PaceScale;
                dto.opponentCount = value.Plan.OpponentCount;
                dto.scenePath = value.ScenePath;
                dto.pathLength = value.Plan.TrackPath.TotalLength;
            }

            var path = string.IsNullOrEmpty(outputPath)
                ? Path.Combine(
                    Directory.GetParent(Application.dataPath).FullName,
                    "Logs",
                    "championship-launch.json")
                : outputPath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(dto, true), new UTF8Encoding(false));
            Debug.Log(LogPrefix + "Report written to: " + path);
        }

        private static string GetCommandLineValue(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], key, StringComparison.Ordinal))
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        [Serializable]
        private sealed class PendingLaunch
        {
            public string championshipId;
            public string eventId;
            public string vehicleId;
        }

        [Serializable]
        private sealed class ReportDto
        {
            public string schemaVersion;
            public string timestampUtc;
            public bool resolved;
            public string error;
            public string eventId;
            public string championshipId;
            public string environmentId;
            public string layoutVariant;
            public string eventType;
            public float paceScale;
            public int opponentCount;
            public string scenePath;
            public float pathLength;
        }
    }

    /// <summary>Simple development window listing the fifteen authored events.</summary>
    public sealed class ChampionshipEventLauncherWindow : EditorWindow
    {
        private Vector2 _scroll;
        private string _vehicleId;

        private void OnEnable()
        {
            _vehicleId = SelectionContext.SelectedVehicleId;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Championship Event Launcher", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "The selected event scene is opened read-only and a transient RaceEventRuntime is " +
                "added before Play Mode. No scene asset is saved.",
                MessageType.Info);

            _vehicleId = EditorGUILayout.TextField("Vehicle id (optional)", _vehicleId);

            var championships = LoadChampionships();
            if (championships.Length == 0)
            {
                EditorGUILayout.HelpBox(
                    "No ChampionshipDefinition assets were found. Run Tools/GameCraft Studio/" +
                    "Velocity Rush/Build Championship Content first.",
                    MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var i = 0; i < championships.Length; i++)
            {
                var championship = championships[i];
                if (championship == null)
                {
                    continue;
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField(
                    championship.ProductionOrder + ". " + championship.DisplayNameEn + "  (" +
                    championship.DisplayNameAr + ")  pace " + championship.PaceScale.ToString("0.00"),
                    EditorStyles.boldLabel);

                var events = LoadAllEvents();
                for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
                {
                    var definition = events[eventIndex];
                    if (definition == null ||
                        !string.Equals(
                            definition.ChampionshipId,
                            championship.ChampionshipId,
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var label =
                        "Round " + definition.RoundIndex + " — " + definition.DisplayNameEn +
                        " [" + RaceEventTypeRules.GetDisplayName(definition.EventType) + ", " +
                        EnvironmentThemeRules.GetDisplayName(definition.EnvironmentTheme) + " " +
                        EnvironmentLayoutVariantRules.GetDisplayName(definition.LayoutVariant) + "]";

                    if (GUILayout.Button(label))
                    {
                        ChampionshipRaceLauncher.LaunchFromMenu(
                            definition.EventId,
                            string.IsNullOrWhiteSpace(_vehicleId) ? null : _vehicleId);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private static ChampionshipDefinition[] LoadChampionships()
        {
            var guids = AssetDatabase.FindAssets(
                "t:ChampionshipDefinition",
                new[] { ChampionshipContentBuilder.ChampionshipsFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            var championships = new ChampionshipDefinition[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                championships[i] = AssetDatabase.LoadAssetAtPath<ChampionshipDefinition>(paths[i]);
            }

            return championships;
        }

        private static List<RaceEventDefinition> LoadAllEvents()
        {
            var guids = AssetDatabase.FindAssets(
                "t:RaceEventDefinition",
                new[] { ChampionshipContentBuilder.EventsFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            var events = new List<RaceEventDefinition>(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<RaceEventDefinition>(paths[i]);
                if (definition != null)
                {
                    events.Add(definition);
                }
            }

            return events;
        }
    }
}
