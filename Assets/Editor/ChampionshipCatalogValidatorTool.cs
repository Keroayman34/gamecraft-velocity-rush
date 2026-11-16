using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Editor menu and batch validator for the M8 championship catalog (T-10.7.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Interactive validation is available from
    /// <c>Tools/GameCraft Studio/Velocity Rush/Validate Championship Catalog</c>.
    /// Batch validation is invoked with
    /// <c>-executeMethod VelocityRush.EditorTools.ChampionshipCatalogValidatorTool.ValidateFromCommandLine</c>
    /// and exits non-zero on any error (or on warnings when
    /// <c>-championshipFailOnWarnings</c> is supplied).
    /// </para>
    /// <para>
    /// The JSON report is written to <c>-championshipReportPath &lt;path&gt;</c> when supplied,
    /// otherwise to <c>Logs/championship-catalog-validation.json</c>.
    /// </para>
    /// </remarks>
    public static class ChampionshipCatalogValidatorTool
    {
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Validate Championship Catalog";
        private const string ReportFileName = "championship-catalog-validation.json";
        private const string ReportSchemaVersion = "1.0";

        /// <summary>
        /// Menu entry for interactive validation in the Unity Editor.
        /// </summary>
        [MenuItem(MenuPath, false, 200)]
        public static void ValidateInteractive()
        {
            var result = RunValidation();
            var coverage = result.Coverage;

            if (result.IsValid)
            {
                var message =
                    "VALID\n\n" +
                    $"Championships: {coverage.ChampionshipCount}\n" +
                    $"Total events: {coverage.TotalEventCount}\n" +
                    $"Circuit Races: {coverage.CircuitRaceCount}\n" +
                    $"Time Trials: {coverage.TimeTrialCount}\n" +
                    $"Configurations covered: {coverage.CoveredConfigurationCount}/{Data.ChampionshipContentPolicy.RequiredConfigurationCount}\n" +
                    $"Unlock chain valid: {coverage.UnlockChainValid}\n" +
                    $"Cyclic unlock chain: {coverage.HasCyclicUnlock}\n" +
                    $"All events reachable: {coverage.AllEventsReachable}";
                if (result.Warnings.Count > 0)
                {
                    message += "\n\nWarnings:\n" + string.Join("\n", result.Warnings);
                }

                EditorUtility.DisplayDialog("Championship Catalog Validation", message, "OK");
                return;
            }

            var errorText = string.Join("\n", result.Errors);
            var warningText = result.Warnings.Count > 0
                ? "\n\nWarnings:\n" + string.Join("\n", result.Warnings)
                : string.Empty;
            EditorUtility.DisplayDialog(
                "Championship Catalog Validation",
                $"INVALID\n\nErrors:\n{errorText}{warningText}",
                "OK");
        }

        /// <summary>
        /// Unity <c>-executeMethod</c> entry point. Parses
        /// <c>-championshipReportPath</c> and <c>-championshipFailOnWarnings</c>.
        /// </summary>
        public static void ValidateFromCommandLine()
        {
            var reportPath = GetCommandLineValue("-championshipReportPath");
            var failOnWarnings = HasCommandLineFlag("-championshipFailOnWarnings");
            ValidateAndExit(reportPath, failOnWarnings);
        }

        /// <summary>
        /// Direct-call entry point used by tests and custom CI wrappers. Exits the editor.
        /// </summary>
        public static void ValidateAndExit(string outputPath, bool failOnWarnings)
        {
            var result = RunValidation();
            WriteReport(result, outputPath, failOnWarnings);
            LogResult(result, failOnWarnings);
            EditorApplication.Exit(ShouldFail(result, failOnWarnings) ? 1 : 0);
        }

        /// <summary>True when the result must fail the build or CI job.</summary>
        public static bool ShouldFail(
            Data.ChampionshipCatalogValidator.ValidationResult result,
            bool failOnWarnings)
        {
            return !result.IsValid || (failOnWarnings && result.Warnings.Count > 0);
        }

        /// <summary>
        /// Runs the complete validator against the project's catalog, environment catalog, and
        /// every <see cref="Data.RaceEventDefinition"/> asset.
        /// </summary>
        public static Data.ChampionshipCatalogValidator.ValidationResult RunValidation()
        {
            var catalog = FindComponent<Data.ChampionshipCatalog>();
            if (catalog == null)
            {
                return InvalidWith("ChampionshipCatalog not found in a loaded scene, prefab, or project asset.");
            }

            var environmentCatalog = FindComponent<Data.EnvironmentCatalog>();
            if (environmentCatalog == null)
            {
                return InvalidWith("EnvironmentCatalog not found in a loaded scene, prefab, or project asset.");
            }

            var eventDefinitions = LoadAllAssets<Data.RaceEventDefinition>("t:RaceEventDefinition");
            return Data.ChampionshipCatalogValidator.ValidateCompleteCatalog(
                catalog,
                eventDefinitions,
                environmentCatalog);
        }

        /// <summary>Builds the deterministic JSON report for a validation result.</summary>
        public static string BuildJsonReport(
            Data.ChampionshipCatalogValidator.ValidationResult result)
        {
            var coverage = result.Coverage;
            var dto = new ReportDto
            {
                schemaVersion = ReportSchemaVersion,
                isValid = result.IsValid,
                timestampUtc = DateTime.UtcNow.ToString("o"),
                errors = ToArray(result.Errors),
                warnings = ToArray(result.Warnings),
                coverage = new CoverageDto
                {
                    championshipCount = coverage.ChampionshipCount,
                    totalEventCount = coverage.TotalEventCount,
                    circuitRaceCount = coverage.CircuitRaceCount,
                    timeTrialCount = coverage.TimeTrialCount,
                    eventsPerEnvironment = Copy(coverage.EventsPerEnvironment),
                    eventsPerLayoutVariant = Copy(coverage.EventsPerLayoutVariant),
                    eventsPerConfiguration = Copy(coverage.EventsPerConfiguration),
                    eventsPerChampionship = Copy(coverage.EventsPerChampionship),
                    coveredConfigurationCount = coverage.CoveredConfigurationCount,
                    unlockChainValid = coverage.UnlockChainValid,
                    hasCyclicUnlock = coverage.HasCyclicUnlock,
                    allEventsReachable = coverage.AllEventsReachable
                }
            };

            return JsonUtility.ToJson(dto, true);
        }

        /// <summary>Writes the JSON report and returns the absolute path used.</summary>
        public static string WriteReport(
            Data.ChampionshipCatalogValidator.ValidationResult result,
            string outputPath,
            bool failOnWarnings)
        {
            var path = string.IsNullOrEmpty(outputPath) ? GetDefaultReportPath() : outputPath;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = BuildJsonReport(result);
            File.WriteAllText(path, json, new UTF8Encoding(false));
            Debug.Log($"[ChampionshipCatalogValidator] Report written to: {path} (failOnWarnings={failOnWarnings})");
            return path;
        }

        /// <summary>Default report path under the project's <c>Logs</c> folder.</summary>
        public static string GetDefaultReportPath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "Logs", ReportFileName);
        }

        private static void LogResult(
            Data.ChampionshipCatalogValidator.ValidationResult result,
            bool failOnWarnings)
        {
            if (result.IsValid)
            {
                Debug.Log("[ChampionshipCatalogValidator] VALID - the championship catalog passes every check.");
            }
            else
            {
                Debug.LogError("[ChampionshipCatalogValidator] INVALID - the championship catalog failed validation:");
            }

            for (var i = 0; i < result.Errors.Count; i++)
            {
                Debug.LogError("[ChampionshipCatalogValidator]   - " + result.Errors[i]);
            }

            for (var i = 0; i < result.Warnings.Count; i++)
            {
                Debug.LogWarning("[ChampionshipCatalogValidator]   - " + result.Warnings[i]);
            }

            if (failOnWarnings && result.Warnings.Count > 0)
            {
                Debug.LogError("[ChampionshipCatalogValidator] Warnings are treated as errors for this run.");
            }
        }

        private static Data.ChampionshipCatalogValidator.ValidationResult InvalidWith(string error)
        {
            var coverage = new Data.ChampionshipCatalogValidator.CatalogCoverage(
                0,
                0,
                0,
                0,
                new int[Data.ChampionshipContentPolicy.RequiredEnvironmentCount],
                new int[Data.ChampionshipContentPolicy.RequiredConfigurationsPerEnvironment],
                new int[Data.ChampionshipContentPolicy.RequiredConfigurationCount],
                new int[Data.ChampionshipContentPolicy.RequiredChampionshipCount],
                false,
                false,
                false);
            return Data.ChampionshipCatalogValidator.ValidationResult.Invalid(
                new[] { error },
                coverage);
        }

        private static List<T> LoadAllAssets<T>(string filter) where T : Object
        {
            var results = new List<T>();
            var paths = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }

            paths.Sort(StringComparer.Ordinal);
            for (var i = 0; i < paths.Count; i++)
            {
                var loaded = AssetDatabase.LoadAllAssetsAtPath(paths[i]);
                for (var assetIndex = 0; assetIndex < loaded.Length; assetIndex++)
                {
                    if (loaded[assetIndex] is T typed)
                    {
                        results.Add(typed);
                    }
                }
            }

            return results;
        }

        private static T FindComponent<T>() where T : Component
        {
            var loaded = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (loaded != null)
            {
                return loaded;
            }

            var assets = LoadAllAssets<T>("t:" + typeof(T).Name);
            if (assets.Count > 0)
            {
                return assets[0];
            }

            if (!Application.isBatchMode)
            {
                return null;
            }

            // Batch mode has no guaranteed open scene; scan the project scene folder once.
            var sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });
            var scenePaths = new List<string>();
            for (var i = 0; i < sceneGuids.Length; i++)
            {
                scenePaths.Add(AssetDatabase.GUIDToAssetPath(sceneGuids[i]));
            }

            scenePaths.Sort(StringComparer.Ordinal);
            for (var i = 0; i < scenePaths.Count; i++)
            {
                var scene = EditorSceneManager.OpenScene(scenePaths[i], OpenSceneMode.Additive);
                var found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
                if (found != null)
                {
                    return found;
                }

                EditorSceneManager.CloseScene(scene, true);
            }

            return null;
        }

        private static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<string>();
            }

            var copy = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
            {
                copy[i] = values[i];
            }

            return copy;
        }

        private static int[] Copy(int[] values)
        {
            if (values == null)
            {
                return Array.Empty<int>();
            }

            var copy = new int[values.Length];
            Array.Copy(values, copy, values.Length);
            return copy;
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

        private static bool HasCommandLineFlag(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        [Serializable]
        private sealed class ReportDto
        {
            public string schemaVersion;
            public bool isValid;
            public string timestampUtc;
            public string[] errors;
            public string[] warnings;
            public CoverageDto coverage;
        }

        [Serializable]
        private sealed class CoverageDto
        {
            public int championshipCount;
            public int totalEventCount;
            public int circuitRaceCount;
            public int timeTrialCount;
            public int[] eventsPerEnvironment;
            public int[] eventsPerLayoutVariant;
            public int[] eventsPerConfiguration;
            public int[] eventsPerChampionship;
            public int coveredConfigurationCount;
            public bool unlockChainValid;
            public bool hasCyclicUnlock;
            public bool allEventsReachable;
        }
    }
}
