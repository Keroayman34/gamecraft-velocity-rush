using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Build-readiness checks for the M8 championship/event content (T-10.7.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A build is ready only when every environment/configuration scene required by the approved
    /// 15-event matrix exists on disk and is listed in the <c>Phase10Championships</c> profile.
    /// The filter is deliberately fail-closed: missing Stage-3 content is reported, never ignored.
    /// </para>
    /// <para>
    /// <see cref="Evaluate"/> is a pure decision function so the rules can be tested without
    /// creating the ten Stage-3 scenes.
    /// </para>
    /// </remarks>
    public static class ChampionshipBuildReadinessTool
    {
        /// <summary>Approved build profile for the M8 championship build.</summary>
        public const string BuildProfileName = "Phase10Championships";

        /// <summary>Folder that owns the five signature and five derived reverse event scenes.</summary>
        public const string EventSceneFolder = "Assets/Scenes/Environments";

        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Check Championship Build Readiness";

        /// <summary>
        /// Menu entry for the interactive build-readiness check.
        /// </summary>
        [MenuItem(MenuPath, false, 201)]
        public static void CheckReadinessInteractive()
        {
            var result = RunReadinessCheck();
            var message =
                (result.IsReady ? "READY FOR BUILD\n\n" : "NOT READY\n\n") +
                $"Build profile: {BuildProfileName}\n" +
                $"Profile scenes: {result.RequiredSceneCount}\n" +
                $"Expected event scenes: {result.ExpectedEventSceneCount}\n" +
                $"Reverse scenes: {result.ReverseSceneCount}\n" +
                $"Build settings match: {result.AllScenesInBuildSettings}";

            if (result.Issues.Count > 0)
            {
                message += "\n\nIssues:\n" + string.Join("\n", result.Issues);
            }

            if (result.Warnings.Count > 0)
            {
                message += "\n\nWarnings:\n" + string.Join("\n", result.Warnings);
            }

            EditorUtility.DisplayDialog("Championship Build Readiness", message, "OK");
        }

        /// <summary>Batch-mode entry point; exits non-zero when the build is not ready.</summary>
        public static void CheckReadinessFromCommandLine()
        {
            var result = RunReadinessCheck();
            if (result.IsReady)
            {
                Debug.Log($"[ChampionshipBuildReadiness] READY - {result.ExpectedEventSceneCount} expected event scenes verified.");
                for (var i = 0; i < result.Warnings.Count; i++)
                {
                    Debug.LogWarning("[ChampionshipBuildReadiness] " + result.Warnings[i]);
                }

                EditorApplication.Exit(0);
                return;
            }

            Debug.LogError($"[ChampionshipBuildReadiness] NOT READY - {result.Issues.Count} issue(s):");
            for (var i = 0; i < result.Issues.Count; i++)
            {
                Debug.LogError("[ChampionshipBuildReadiness]   - " + result.Issues[i]);
            }

            EditorApplication.Exit(1);
        }

        /// <summary>
        /// Runs the readiness check against the project's real scene assets and event definitions.
        /// </summary>
        public static ReadinessResult RunReadinessCheck()
        {
            var issues = new List<string>();
            var warnings = new List<string>();

            if (!Enum.TryParse(BuildProfileName, out WebGLBuildProfile profile) ||
                !Enum.IsDefined(typeof(WebGLBuildProfile), profile))
            {
                issues.Add($"Build profile '{BuildProfileName}' is not defined in WebGLBuildProfile.");
                return new ReadinessResult(false, issues, warnings, 0, 0, 0, false);
            }

            var profileScenes = VelocityRushWebGLBuilder.GetScenePaths(profile);
            if (profileScenes == null || profileScenes.Length == 0)
            {
                issues.Add($"Build profile '{BuildProfileName}' has no scenes configured.");
                return new ReadinessResult(false, issues, warnings, 0, 0, 0, false);
            }

            var events = LoadAll<Data.RaceEventDefinition>("t:RaceEventDefinition");
            if (events.Count == 0)
            {
                issues.Add("No RaceEventDefinition assets exist; the Stage-3 event matrix has not been authored yet.");
            }

            var expectedEventScenes = GetExpectedEventScenePaths(events);
            var buildSettingsScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            return Evaluate(
                profileScenes,
                path => AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null,
                buildSettingsScenes,
                expectedEventScenes,
                issues,
                warnings);
        }

        /// <summary>
        /// Computes the distinct environment/configuration scene paths required by a set of events.
        /// </summary>
        public static IReadOnlyList<string> GetExpectedEventScenePaths(
            IReadOnlyList<Data.RaceEventDefinition> events)
        {
            var paths = new List<string>();
            if (events == null)
            {
                return paths;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < events.Count; i++)
            {
                var definition = events[i];
                if (definition == null ||
                    !VelocityRush.Data.EnvironmentThemeRules.IsDefined(definition.EnvironmentTheme) ||
                    !VelocityRush.Data.EnvironmentLayoutVariantRules.IsDefined(definition.LayoutVariant))
                {
                    continue;
                }

                var path = GetEventScenePath(definition.EnvironmentTheme, definition.LayoutVariant);
                if (seen.Add(path))
                {
                    paths.Add(path);
                }
            }

            paths.Sort(StringComparer.Ordinal);
            return paths;
        }

        /// <summary>Project-relative scene path for one environment/configuration pair.</summary>
        public static string GetEventScenePath(
            VelocityRush.Data.EnvironmentTheme theme,
            VelocityRush.Data.EnvironmentLayoutVariant variant)
        {
            if (!VelocityRush.Data.EnvironmentThemeRules.IsDefined(theme))
            {
                throw new ArgumentOutOfRangeException(nameof(theme));
            }

            if (!VelocityRush.Data.EnvironmentLayoutVariantRules.IsDefined(variant))
            {
                throw new ArgumentOutOfRangeException(nameof(variant));
            }

            var variantName = variant == VelocityRush.Data.EnvironmentLayoutVariant.Reverse
                ? "reverse"
                : "signature";
            return EventSceneFolder + "/" +
                   VelocityRush.Data.EnvironmentThemeRules.GetStableFragment(theme) + "-" +
                   variantName + ".unity";
        }

        /// <summary>
        /// Pure readiness decision. Every expected event scene must exist and be listed in the
        /// profile; every profile scene must exist. Enabled build-settings mismatches are warnings
        /// because the WebGL builder applies the profile immediately before a build.
        /// </summary>
        public static ReadinessResult Evaluate(
            IReadOnlyList<string> profileScenes,
            Func<string, bool> sceneExists,
            IReadOnlyList<string> buildSettingsScenes,
            IReadOnlyList<string> expectedEventScenes)
            => Evaluate(profileScenes, sceneExists, buildSettingsScenes, expectedEventScenes,
                new List<string>(), new List<string>());

        private static ReadinessResult Evaluate(
            IReadOnlyList<string> profileScenes,
            Func<string, bool> sceneExists,
            IReadOnlyList<string> buildSettingsScenes,
            IReadOnlyList<string> expectedEventScenes,
            List<string> issues,
            List<string> warnings)
        {
            if (profileScenes == null || profileScenes.Count == 0)
            {
                issues.Add("The championship build profile contains no scenes.");
            }

            if (sceneExists == null)
            {
                throw new ArgumentNullException(nameof(sceneExists));
            }

            var profileSet = new HashSet<string>(StringComparer.Ordinal);
            if (profileScenes != null)
            {
                for (var i = 0; i < profileScenes.Count; i++)
                {
                    var path = Normalize(profileScenes[i]);
                    if (string.IsNullOrEmpty(path))
                    {
                        issues.Add("The championship build profile contains an empty scene path.");
                        continue;
                    }

                    if (!profileSet.Add(path))
                    {
                        warnings.Add($"Duplicate scene in the championship build profile: {path}.");
                    }

                    if (!sceneExists(path))
                    {
                        issues.Add($"Profile scene does not exist: {path}.");
                    }
                }
            }

            var expectedSet = new HashSet<string>(StringComparer.Ordinal);
            var expected = expectedEventScenes ?? Array.Empty<string>();
            for (var i = 0; i < expected.Count; i++)
            {
                var path = Normalize(expected[i]);
                if (string.IsNullOrEmpty(path))
                {
                    issues.Add("An expected event scene path is empty.");
                    continue;
                }

                if (!expectedSet.Add(path))
                {
                    continue;
                }

                if (!sceneExists(path))
                {
                    issues.Add($"Required event scene does not exist: {path}.");
                }

                if (!profileSet.Contains(path))
                {
                    issues.Add($"Required event scene is not listed in the {BuildProfileName} profile: {path}.");
                }
            }

            if (expectedSet.Count != VelocityRush.Data.ChampionshipContentPolicy.RequiredConfigurationCount)
            {
                issues.Add(
                    $"Expected exactly {VelocityRush.Data.ChampionshipContentPolicy.RequiredConfigurationCount} distinct environment/configuration scenes (found {expectedSet.Count}).");
            }

            var buildSettingsSet = new HashSet<string>(StringComparer.Ordinal);
            if (buildSettingsScenes != null)
            {
                for (var i = 0; i < buildSettingsScenes.Count; i++)
                {
                    var path = Normalize(buildSettingsScenes[i]);
                    if (!string.IsNullOrEmpty(path))
                    {
                        buildSettingsSet.Add(path);
                    }
                }
            }

            var allScenesInBuildSettings = buildSettingsSet.SetEquals(profileSet);
            if (!allScenesInBuildSettings)
            {
                warnings.Add(
                    $"Enabled EditorBuildSettings scenes do not match the {BuildProfileName} profile. " +
                    "The WebGL builder applies the profile before building; apply it before a manual build.");
            }

            var reverseCount = 0;
            foreach (var path in expectedSet)
            {
                if (path.EndsWith("-reverse.unity", StringComparison.Ordinal))
                {
                    reverseCount++;
                }
            }

            return new ReadinessResult(
                issues.Count == 0,
                issues,
                warnings,
                profileSet.Count,
                expectedSet.Count,
                reverseCount,
                allScenesInBuildSettings);
        }

        private static List<T> LoadAll<T>(string filter) where T : UnityEngine.Object
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
                var assets = AssetDatabase.LoadAllAssetsAtPath(paths[i]);
                for (var assetIndex = 0; assetIndex < assets.Length; assetIndex++)
                {
                    if (assets[assetIndex] is T typed)
                    {
                        results.Add(typed);
                    }
                }
            }

            return results;
        }

        private static string Normalize(string path)
        {
            return string.IsNullOrEmpty(path)
                ? string.Empty
                : path.Replace('\\', '/').Trim();
        }

        /// <summary>Result of a build-readiness check.</summary>
        public readonly struct ReadinessResult
        {
            public readonly bool IsReady;
            public readonly IReadOnlyList<string> Issues;
            public readonly IReadOnlyList<string> Warnings;
            public readonly int RequiredSceneCount;
            public readonly int ExpectedEventSceneCount;
            public readonly int ReverseSceneCount;
            public readonly bool AllScenesInBuildSettings;

            public ReadinessResult(
                bool isReady,
                IReadOnlyList<string> issues,
                IReadOnlyList<string> warnings,
                int requiredSceneCount,
                int expectedEventSceneCount,
                int reverseSceneCount,
                bool allScenesInBuildSettings)
            {
                IsReady = isReady;
                Issues = issues ?? Array.Empty<string>();
                Warnings = warnings ?? Array.Empty<string>();
                RequiredSceneCount = requiredSceneCount;
                ExpectedEventSceneCount = expectedEventSceneCount;
                ReverseSceneCount = reverseSceneCount;
                AllScenesInBuildSettings = allScenesInBuildSettings;
            }
        }
    }
}
