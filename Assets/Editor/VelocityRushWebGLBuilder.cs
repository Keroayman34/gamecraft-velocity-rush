using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Selects which approved two-scene build contract a WebGL build targets (T-7.12).
    /// </summary>
    /// <remarks>
    /// The profiles are opt-in data, not a global switch: the tool never edits
    /// <c>EditorBuildSettings</c> by itself. A build validates that the project's configured scene
    /// list exactly matches the selected profile and fails closed otherwise.
    /// </remarks>
    public enum WebGLBuildProfile
    {
        /// <summary>Phase-5/6 prototype: Bootstrap → TestTrack.</summary>
        Prototype = 0,

        /// <summary>Phase-7 vertical slice: Bootstrap → HandlingCourse.</summary>
        VerticalSlice = 1,

        /// <summary>Phase-8 debug loop: Garage → HandlingCourse (Garage selects, course spawns).</summary>
        Phase8Debug = 2
    }

    /// <summary>
    /// Permanent editor-only WebGL <b>Development</b> build tool for Velocity Rush (T5.5.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Scope: a repeatable local WebGL development build for the approved two-scene prototype
    /// order (<c>Bootstrap</c>, then <c>TestTrack</c>). The tool validates the editor, the WebGL
    /// module, the active build target, and the build-scene list before building. It never edits
    /// ProjectSettings/PlayerSettings, packages, scenes, or layer/tag/timestep/quality/graphics
    /// settings, and it never deletes previous build output.
    /// </para>
    /// <para>
    /// Output is either an explicitly chosen folder or the documented default outside the Unity
    /// project (<c>~/VelocityRushBuilds/WebGL/dev-yyyyMMdd-HHmmss</c>). Every build writes a
    /// machine-readable JSON report next to the player (result, duration, BuildReport summary,
    /// and artifact sizes for index.html, wasm, data, framework.js, and loader.js where present).
    /// </para>
    /// <para>
    /// Editor-only by construction: this file lives under <c>Assets/Editor</c> and compiles into
    /// the predefined editor assembly. It adds no runtime assembly reference, gameplay code, or
    /// Playgama Bridge coupling.
    /// </para>
    /// </remarks>
    public static class VelocityRushWebGLBuilder
    {
        /// <summary>Required menu entry for the interactive build window.</summary>
        public const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/WebGL Build";

        /// <summary>Non-destructive preflight-only menu entry.</summary>
        public const string ValidateMenuPath = "Tools/GameCraft Studio/Velocity Rush/Validate WebGL Setup";

        /// <summary>Explicit menu entry that switches the build-scene list to the slice profile.</summary>
        public const string ApplySliceScenesMenuPath =
            "Tools/GameCraft Studio/Velocity Rush/Apply Vertical Slice Build Scenes";

        /// <summary>Explicit menu entry that restores the prototype build-scene list.</summary>
        public const string RestorePrototypeScenesMenuPath =
            "Tools/GameCraft Studio/Velocity Rush/Restore Prototype Build Scenes";

        private const string LogPrefix = "[VelocityRush.WebGL] ";
        private const string BuildReportFileName = "velocity-rush-webgl-build-report.json";
        private const string PreflightReportFileName = "velocity-rush-webgl-preflight-report.json";
        private const int SchemaVersion = 1;

        /// <summary>Environment variable for the batch output folder (lower precedence than <c>-vrOutput</c>).</summary>
        public const string OutputEnvironmentVariable = "VR_WEBGL_OUTPUT";

        /// <summary>Environment variable that opts in to overwriting a non-empty output folder in batch mode.</summary>
        public const string AllowNonEmptyEnvironmentVariable = "VR_WEBGL_ALLOW_NONEMPTY";

        /// <summary>Batch command-line argument for the output folder.</summary>
        public const string OutputArgument = "-vrOutput";

        /// <summary>Batch command-line argument that opts in to overwriting a non-empty output folder.</summary>
        public const string AllowNonEmptyArgument = "-vrAllowNonEmptyOutput";

        /// <summary>Batch command-line argument selecting the build profile (<c>prototype</c> or <c>slice</c>).</summary>
        public const string ProfileArgument = "-vrProfile";

        /// <summary>Environment variable selecting the build profile (lower precedence than <c>-vrProfile</c>).</summary>
        public const string ProfileEnvironmentVariable = "VR_WEBGL_PROFILE";

        /// <summary>The prototype build-scene order (kept as the default contract, T5.5).</summary>
        public static string[] ApprovedScenePaths => PrototypeScenePaths;

        /// <summary>Phase-5/6 prototype scene order.</summary>
        public static readonly string[] PrototypeScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/TestTrack.unity"
        };

        /// <summary>Phase-7 vertical slice scene order.</summary>
        public static readonly string[] VerticalSliceScenePaths =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/HandlingCourse.unity"
        };

        /// <summary>Phase-8 debug selection loop scene order.</summary>
        public static readonly string[] Phase8DebugScenePaths =
        {
            "Assets/Scenes/Garage.unity",
            "Assets/Scenes/HandlingCourse.unity"
        };

        private static readonly ArtifactCategory[] Categories =
        {
            new ArtifactCategory("indexHtml", "index.html"),
            new ArtifactCategory("wasm", "wasm"),
            new ArtifactCategory("data", "data"),
            new ArtifactCategory("frameworkJs", "framework.js"),
            new ArtifactCategory("loaderJs", "loader.js")
        };

        /// <summary>Absolute Unity project root (parent of <see cref="Application.dataPath"/>).</summary>
        public static string ProjectRoot
        {
            get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); }
        }

        /// <summary>
        /// Documented default output: a timestamped folder outside the Unity project, so a build can
        /// never pollute version control or replace a previous run implicitly. If the timestamped
        /// name already exists (for example two builds in the same second), a numeric suffix is
        /// appended so the default path is always collision-free.
        /// </summary>
        public static string CreateDefaultOutputDirectory()
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(home))
            {
                home = Path.GetTempPath();
            }

            var candidate = Path.Combine(
                home,
                "VelocityRushBuilds",
                "WebGL",
                "dev-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

            return MakeUniqueOutputPath(candidate);
        }

        /// <summary>Resolves a user-entered path to a stable absolute form for comparisons.</summary>
        public static string NormalizeOutputPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch (Exception)
            {
                return path.Trim();
            }
        }

        /// <summary>Returns the approved scene order for a profile.</summary>
        public static string[] GetScenePaths(WebGLBuildProfile profile)
        {
            switch (profile)
            {
                case WebGLBuildProfile.VerticalSlice:
                    return VerticalSliceScenePaths;
                case WebGLBuildProfile.Phase8Debug:
                    return Phase8DebugScenePaths;
                default:
                    return PrototypeScenePaths;
            }
        }

        /// <summary>Stable, human-readable profile name used in reports and dialogs.</summary>
        public static string GetProfileDisplayName(WebGLBuildProfile profile)
        {
            switch (profile)
            {
                case WebGLBuildProfile.VerticalSlice:
                    return "Vertical Slice";
                case WebGLBuildProfile.Phase8Debug:
                    return "Phase 8 Debug";
                default:
                    return "Prototype";
            }
        }

        /// <summary>Scene-order summary used in preflight detail and dialogs.</summary>
        public static string DescribeSceneOrder(WebGLBuildProfile profile)
        {
            var paths = GetScenePaths(profile);
            var last = paths[paths.Length - 1];
            return Path.GetFileNameWithoutExtension(paths[0]) + " -> " + Path.GetFileNameWithoutExtension(last);
        }

        /// <summary>
        /// Parses a profile token. Accepts <c>prototype</c>/<c>testtrack</c> and
        /// <c>slice</c>/<c>verticalslice</c>/<c>handlingcourse</c> (case-insensitive).
        /// </summary>
        public static bool TryParseProfile(string value, out WebGLBuildProfile profile)
        {
            profile = WebGLBuildProfile.Prototype;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var token = value.Trim();
            if (string.Equals(token, "prototype", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "testtrack", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "0", StringComparison.Ordinal))
            {
                profile = WebGLBuildProfile.Prototype;
                return true;
            }

            if (string.Equals(token, "slice", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "verticalslice", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "vertical-slice", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "handlingcourse", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "1", StringComparison.Ordinal))
            {
                profile = WebGLBuildProfile.VerticalSlice;
                return true;
            }

            if (string.Equals(token, "phase8", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "phase8debug", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "garage", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "debug", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "2", StringComparison.Ordinal))
            {
                profile = WebGLBuildProfile.Phase8Debug;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves the batch profile with argument precedence over environment. Defaults to the
        /// prototype contract when neither is set, preserving the T5.5 behaviour.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown for an unrecognized profile token.</exception>
        public static WebGLBuildProfile ResolveProfile(string argumentValue, string environmentValue)
        {
            var token = !string.IsNullOrWhiteSpace(argumentValue) ? argumentValue : environmentValue;
            if (string.IsNullOrWhiteSpace(token))
            {
                return WebGLBuildProfile.Prototype;
            }

            if (TryParseProfile(token, out var profile))
            {
                return profile;
            }

            throw new InvalidOperationException(
                "Unknown WebGL build profile '" + token + "'. Use 'prototype', 'slice', or 'phase8'.");
        }

        /// <summary>
        /// Returns the profile whose scene order exactly matches the current build settings, or
        /// <see cref="WebGLBuildProfile.Prototype"/> when nothing matches.
        /// </summary>
        public static WebGLBuildProfile DetectConfiguredProfile()
        {
            var configured = EditorBuildSettings.scenes;
            if (MatchesSceneOrder(configured, Phase8DebugScenePaths))
            {
                return WebGLBuildProfile.Phase8Debug;
            }

            if (MatchesSceneOrder(configured, VerticalSliceScenePaths))
            {
                return WebGLBuildProfile.VerticalSlice;
            }

            return WebGLBuildProfile.Prototype;
        }

        private static bool MatchesSceneOrder(EditorBuildSettingsScene[] configured, string[] expected)
        {
            if (configured == null || configured.Length != expected.Length)
            {
                return false;
            }

            for (var i = 0; i < expected.Length; i++)
            {
                if (configured[i].path != expected[i] || !configured[i].enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private static string MakeUniqueOutputPath(string candidate)
        {
            if (!Directory.Exists(candidate) && !File.Exists(candidate))
            {
                return candidate;
            }

            for (var suffix = 2; suffix < 1000; suffix++)
            {
                var next = candidate + "-" + suffix.ToString("00", CultureInfo.InvariantCulture);
                if (!Directory.Exists(next) && !File.Exists(next))
                {
                    return next;
                }
            }

            throw new InvalidOperationException(
                "Could not find a collision-free build output path based on " + candidate + ".");
        }

        /// <summary>Opens the build window. In batch mode it only proves the menu target resolves.</summary>
        [MenuItem(MenuPath, false, 100)]
        public static void OpenBuildWindow()
        {
            if (Application.isBatchMode)
            {
                Debug.Log(LogPrefix + "Menu item resolved in batch mode: " + MenuPath);
                return;
            }

            var window = EditorWindow.GetWindow<VelocityRushWebGLBuildWindow>(false, "Velocity Rush WebGL", true);
            window.minSize = new Vector2(560f, 480f);
            window.Show();
        }

        /// <summary>Runs the non-destructive preflight and reports the result without building.</summary>
        [MenuItem(ValidateMenuPath, false, 101)]
        public static void ValidateFromMenu()
        {
            var profile = DetectConfiguredProfile();
            var report = RunPreflight(CreateDefaultOutputDirectory(), false, false, profile);

            if (report.Passed)
            {
                Debug.Log(LogPrefix + "Preflight passed for the approved WebGL development build setup (" + profile + ").");
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Velocity Rush WebGL preflight",
                        "Preflight passed.\n\nProfile: " + GetProfileDisplayName(profile) +
                        "\nScenes: " + DescribeSceneOrder(profile) +
                        "\nWebGL module: present\nActive target: WebGL\n\nNo build was started and no files were changed.",
                        "OK");
                }

                return;
            }

            var summary = report.FailuresSummary;
            Debug.LogError(LogPrefix + "Preflight failed:\n" + summary);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Velocity Rush WebGL preflight", "Preflight failed:\n\n" + summary, "OK");
            }
        }

        /// <summary>
        /// Explicitly switches the build-scene list to Bootstrap → HandlingCourse. Never called
        /// automatically: builds and tests rely on the prototype list by default.
        /// </summary>
        [MenuItem(ApplySliceScenesMenuPath, false, 102)]
        public static void ApplyVerticalSliceScenesFromMenu()
        {
            ApplySceneSet(
                WebGLBuildProfile.VerticalSlice,
                "Apply vertical slice build scenes?",
                "This rewrites EditorBuildSettings to Bootstrap -> HandlingCourse so a WebGL slice build can run.\n\n" +
                "The Phase-5/6 EditMode suite asserts the prototype scene list; run '" +
                RestorePrototypeScenesMenuPath + "' before that suite.");
        }

        /// <summary>Explicitly restores Bootstrap → TestTrack in the build-scene list.</summary>
        [MenuItem(RestorePrototypeScenesMenuPath, false, 103)]
        public static void RestorePrototypeScenesFromMenu()
        {
            ApplySceneSet(
                WebGLBuildProfile.Prototype,
                "Restore prototype build scenes?",
                "This rewrites EditorBuildSettings back to Bootstrap -> TestTrack (the Phase-5/6 contract).");
        }

        /// <summary>Batch helper: applies the Phase-8 debug scene set (Garage → HandlingCourse).</summary>
        public static void ApplyPhase8DebugScenesFromCommandLine()
        {
            ApplySceneSet(WebGLBuildProfile.Phase8Debug, string.Empty, string.Empty);
            EditorApplication.Exit(0);
        }

        /// <summary>Batch helper: restores the prototype scene set (Bootstrap → TestTrack).</summary>
        public static void RestorePrototypeScenesFromCommandLine()
        {
            ApplySceneSet(WebGLBuildProfile.Prototype, string.Empty, string.Empty);
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Applies a profile's scene list to <c>EditorBuildSettings</c> after an explicit user
        /// confirmation. This is the only code path that writes build settings.
        /// </summary>
        /// <returns>True when the scene list was written.</returns>
        public static bool ApplySceneSet(
            WebGLBuildProfile profile,
            string dialogTitle,
            string dialogMessage)
        {
            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(dialogTitle, dialogMessage, "Apply", "Cancel"))
            {
                return false;
            }

            var paths = GetScenePaths(profile);
            var scenes = new EditorBuildSettingsScene[paths.Length];
            for (var i = 0; i < paths.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            }

            EditorBuildSettings.scenes = scenes;
            Debug.Log(LogPrefix + "Applied " + GetProfileDisplayName(profile) + " build scenes: " + DescribeSceneOrder(profile));
            return true;
        }

        /// <summary>
        /// Runs every blocking pre-build check. Read-only: it never creates, deletes, or writes files.
        /// </summary>
        /// <param name="outputDirectory">Chosen output folder (validated, not created).</param>
        /// <param name="isBuildRequest">True when the caller intends to build (adds the non-empty-output guard).</param>
        /// <param name="allowNonEmptyOutput">True only after the user explicitly confirmed overwrite.</param>
        /// <param name="profile">Approved two-scene contract to validate against.</param>
        public static WebGLPreflightReport RunPreflight(
            string outputDirectory,
            bool isBuildRequest,
            bool allowNonEmptyOutput,
            WebGLBuildProfile profile = WebGLBuildProfile.Prototype)
        {
            var report = new WebGLPreflightReport();

            var editorBusy =
                EditorApplication.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                BuildPipeline.isBuildingPlayer;

            Add(
                report,
                "Editor is idle (not playing, compiling, updating, or building)",
                !editorBusy,
                WebGLPreflightCode.EditorBusy,
                editorBusy ? "Wait for the current editor operation to finish, then retry." : "Editor ready.");

            var webGLSupported = BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            Add(
                report,
                "WebGL Build Support module installed",
                webGLSupported,
                WebGLPreflightCode.WebGLSupportMissing,
                webGLSupported
                    ? "WebGL playback engine is available."
                    : "Install WebGL Build Support through Unity Hub before building.");

            var activeTarget = EditorUserBuildSettings.activeBuildTarget;
            Add(
                report,
                "Active build target is WebGL",
                activeTarget == BuildTarget.WebGL,
                WebGLPreflightCode.ActiveBuildTargetMismatch,
                activeTarget == BuildTarget.WebGL
                    ? "Active target: WebGL."
                    : "Active target is " + activeTarget + ". Switch it in Build Settings; this tool never switches targets automatically.");

            ValidateScenes(report, profile);
            ValidateOutput(report, outputDirectory, isBuildRequest, allowNonEmptyOutput);

            return report;
        }

        /// <summary>
        /// Builds the selected approved scene set as a WebGL Development player into a validated
        /// folder and writes the JSON report. Callers must have confirmed a non-empty output folder
        /// explicitly.
        /// </summary>
        /// <param name="outputDirectory">Chosen output folder.</param>
        /// <param name="allowNonEmptyOutput">True only after explicit overwrite confirmation.</param>
        /// <param name="profile">Approved two-scene contract to build.</param>
        public static WebGLBuildOutcome Build(
            string outputDirectory,
            bool allowNonEmptyOutput,
            WebGLBuildProfile profile = WebGLBuildProfile.Prototype)
        {
            var preflight = RunPreflight(outputDirectory, true, allowNonEmptyOutput, profile);
            if (!preflight.Passed)
            {
                return new WebGLBuildOutcome
                {
                    Attempted = false,
                    Succeeded = false,
                    Result = "PreflightFailed",
                    FailureReason = preflight.FailuresSummary,
                    OutputDirectory = outputDirectory
                };
            }

            var fullOutput = Path.GetFullPath(outputDirectory);
            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(profile),
                locationPathName = fullOutput,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = BuildOptions.Development
            };

            BuildReport buildReport = null;
            try
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayProgressBar(
                        "Velocity Rush - WebGL Development Build",
                        "Building WebGL player. This can take several minutes...",
                        0.5f);
                }

                buildReport = BuildPipeline.BuildPlayer(options);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Build threw an exception: " + exception);
                var failed = new WebGLBuildOutcome
                {
                    Attempted = true,
                    Succeeded = false,
                    Result = "Exception",
                    FailureReason = exception.Message,
                    OutputDirectory = fullOutput
                };
                TryWriteReport(fullOutput, preflight, null, failed, profile);
                return failed;
            }
            finally
            {
                if (!Application.isBatchMode)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            var outcome = CreateOutcome(buildReport, fullOutput);
            TryWriteReport(fullOutput, preflight, buildReport, outcome, profile);
            LogOutcome(outcome);
            ShowOutcomeDialog(outcome);
            return outcome;
        }

        /// <summary>
        /// Permanent batch-mode build entry point (T5.5.3). Invoke with:
        /// <c>Unity -batchmode -nographics -projectPath &lt;project&gt; -executeMethod
        /// VelocityRush.EditorTools.VelocityRushWebGLBuilder.BuildFromCommandLine
        /// -vrOutput &lt;folder&gt; [-vrAllowNonEmptyOutput] -quit</c>.
        /// The output/overwrite values may also come from <see cref="OutputEnvironmentVariable"/>
        /// and <see cref="AllowNonEmptyEnvironmentVariable"/>. Exits 0 only when the build succeeds.
        /// </summary>
        public static void BuildFromCommandLine()
        {
            if (!Application.isBatchMode)
            {
                Debug.LogError(
                    LogPrefix + "BuildFromCommandLine is a batch-mode entry point. Open the build window instead.");
                return;
            }

            var outputDirectory = ResolveBatchOutputDirectory();
            var allowNonEmptyOutput = ResolveBatchNonEmptyOutputFlag();

            WebGLBuildProfile profile;
            try
            {
                profile = ResolveBatchProfile();
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + exception.Message);
                EditorApplication.Exit(1);
                return;
            }

            Debug.Log(
                LogPrefix + "Batch build requested | profile " + GetProfileDisplayName(profile) +
                " (" + DescribeSceneOrder(profile) + ")" +
                " | output " + outputDirectory +
                " | allowNonEmptyOutput " + (allowNonEmptyOutput ? "true" : "false"));

            WebGLBuildOutcome outcome;
            try
            {
                outcome = Build(outputDirectory, allowNonEmptyOutput, profile);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Batch build threw an exception: " + exception);
                EditorApplication.Exit(1);
                return;
            }

            var exitCode = outcome != null && outcome.Succeeded ? 0 : 1;
            Debug.Log(LogPrefix + "Batch build exit code: " + exitCode);
            EditorApplication.Exit(exitCode);
        }

        private static string ResolveBatchOutputDirectory()
        {
            var arguments = Environment.GetCommandLineArgs();
            var fromArgument = FindArgumentValue(arguments, OutputArgument);
            if (!string.IsNullOrWhiteSpace(fromArgument))
            {
                return Path.GetFullPath(fromArgument);
            }

            var fromEnvironment = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return Path.GetFullPath(fromEnvironment);
            }

            return CreateDefaultOutputDirectory();
        }

        private static bool ResolveBatchNonEmptyOutputFlag()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length; i++)
            {
                if (string.Equals(arguments[i], AllowNonEmptyArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var value = Environment.GetEnvironmentVariable(AllowNonEmptyEnvironmentVariable);
            return !string.IsNullOrWhiteSpace(value)
                   && !string.Equals(value, "0", StringComparison.Ordinal)
                   && !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves <c>-vrProfile</c> / <c>VR_WEBGL_PROFILE</c>. Absent input keeps the prototype
        /// contract, so existing automation is unchanged.
        /// </summary>
        public static WebGLBuildProfile ResolveBatchProfile()
        {
            var arguments = Environment.GetCommandLineArgs();
            var fromArgument = FindArgumentValue(arguments, ProfileArgument);
            var fromEnvironment = Environment.GetEnvironmentVariable(ProfileEnvironmentVariable);
            return ResolveProfile(fromArgument, fromEnvironment);
        }

        private static string FindArgumentValue(string[] arguments, string name)
        {
            if (arguments == null)
            {
                return null;
            }

            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (!string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = arguments[i + 1];
                if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("-", StringComparison.Ordinal))
                {
                    return value;
                }
            }

            return null;
        }

        /// <summary>
        /// Batch-mode validation entry point used by the T5.5.1/T5.5.3 acceptance runs. It proves
        /// the menu item registers, runs preflight, exercises JSON report generation into a
        /// temporary folder, and checks the output-safety guards. It never starts a build and never
        /// touches existing build output.
        /// </summary>
        public static void ValidateOnlyForAutomation()
        {
            var failures = new List<string>();

            WebGLBuildProfile profile;
            try
            {
                profile = ResolveBatchProfile();
            }
            catch (Exception exception)
            {
                profile = WebGLBuildProfile.Prototype;
                failures.Add(exception.Message);
            }

            Debug.Log(LogPrefix + "Automation validation profile: " + GetProfileDisplayName(profile));

            var menuResolved = EditorApplication.ExecuteMenuItem(MenuPath);
            if (menuResolved)
            {
                Debug.Log(LogPrefix + "Menu item resolved: " + MenuPath);
            }
            else
            {
                failures.Add("Menu item did not resolve: " + MenuPath);
            }

            var outputDirectory = Path.Combine(
                Path.GetTempPath(),
                "VelocityRushWebGLPreflight-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));

            var preflight = RunPreflight(outputDirectory, false, false, profile);
            foreach (var check in preflight.Checks)
            {
                var line = LogPrefix + (check.Passed ? "PASS " : "FAIL ") + check.Name + " -- " + check.Detail;
                if (check.Passed)
                {
                    Debug.Log(line);
                }
                else
                {
                    Debug.LogError(line);
                }
            }

            if (!preflight.Passed)
            {
                failures.Add("Preflight failed: " + preflight.FailuresSummary);
            }

            var outcome = new WebGLBuildOutcome
            {
                Attempted = false,
                Succeeded = false,
                Result = "NotAttempted",
                OutputDirectory = outputDirectory
            };

            try
            {
                Directory.CreateDirectory(outputDirectory);
                var reportPath = Path.Combine(outputDirectory, PreflightReportFileName);
                WriteReport(reportPath, preflight, null, outcome, profile);
                Debug.Log(LogPrefix + "Preflight report: " + reportPath);
            }
            catch (Exception exception)
            {
                failures.Add("Report generation failed: " + exception.Message);
                Debug.LogError(LogPrefix + "Report generation failed: " + exception);
            }

            // Non-destructive negative checks: unsafe output paths must be rejected and an explicit
            // overwrite confirmation must clear only the non-empty guard. Nothing outside the temp
            // folder is touched.
            var insideProject = Path.Combine(ProjectRoot, "WebGLBuildOutputMustBeRejected");
            var insideProjectPreflight = RunPreflight(insideProject, false, false, profile);
            if (!insideProjectPreflight.HasFailure(WebGLPreflightCode.OutputInsideProject))
            {
                failures.Add("Output-inside-project guard did not trigger.");
            }
            else
            {
                Debug.Log(LogPrefix + "PASS Negative guard: output inside the project is rejected.");
            }

            var refusedBuild = Build(insideProject, false, profile);
            if (refusedBuild.Attempted || refusedBuild.Result != "PreflightFailed")
            {
                failures.Add("Build with an unsafe output path was not refused.");
            }
            else
            {
                Debug.Log(LogPrefix + "PASS Negative guard: build is not started when preflight fails.");
            }

            var containingProject = Path.GetDirectoryName(
                ProjectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (!string.IsNullOrEmpty(containingProject))
            {
                var containingProjectPreflight = RunPreflight(containingProject, false, false, profile);
                if (!containingProjectPreflight.HasFailure(WebGLPreflightCode.OutputContainsProject))
                {
                    failures.Add("Output-containing-project guard did not trigger.");
                }
                else
                {
                    Debug.Log(LogPrefix + "PASS Negative guard: output that contains the project is rejected.");
                }
            }

            string uniqueCandidate = null;
            try
            {
                uniqueCandidate = CreateDefaultOutputDirectory();
                Directory.CreateDirectory(uniqueCandidate);
                var secondCandidate = CreateDefaultOutputDirectory();
                if (string.Equals(
                        NormalizeOutputPath(uniqueCandidate),
                        NormalizeOutputPath(secondCandidate),
                        StringComparison.Ordinal))
                {
                    failures.Add("Default output directory is not collision-free.");
                }
                else
                {
                    Debug.Log(LogPrefix + "PASS Negative guard: default output directories are unique.");
                }
            }
            catch (Exception exception)
            {
                failures.Add("Default output uniqueness check failed: " + exception.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(uniqueCandidate))
                {
                    try
                    {
                        if (Directory.Exists(uniqueCandidate)
                            && Directory.GetFileSystemEntries(uniqueCandidate).Length == 0)
                        {
                            Directory.Delete(uniqueCandidate, false);
                        }
                    }
                    catch (Exception)
                    {
                        // The collision check must never fail a validation run on cleanup.
                    }
                }
            }

            var nonEmptyDirectory = Path.Combine(outputDirectory, "non-empty");
            Directory.CreateDirectory(nonEmptyDirectory);
            File.WriteAllText(Path.Combine(nonEmptyDirectory, "sentinel.txt"), "sentinel");
            var nonEmptyPreflight = RunPreflight(nonEmptyDirectory, true, false);
            if (!nonEmptyPreflight.HasFailure(WebGLPreflightCode.OutputNotEmpty))
            {
                failures.Add("Non-empty output guard did not trigger.");
            }
            else
            {
                Debug.Log(LogPrefix + "PASS Negative guard: non-empty output requires explicit confirmation.");
            }

            var confirmedPreflight = RunPreflight(nonEmptyDirectory, true, true);
            if (confirmedPreflight.HasFailure(WebGLPreflightCode.OutputNotEmpty))
            {
                failures.Add("Confirmed non-empty output guard did not clear.");
            }
            else
            {
                var confirmedCheck = confirmedPreflight.Checks.Find(
                    check => check.Code == WebGLPreflightCode.OutputNotEmpty);
                if (confirmedCheck == null
                    || !confirmedCheck.Passed
                    || confirmedCheck.Detail.IndexOf("Explicit overwrite confirmation", StringComparison.Ordinal) < 0)
                {
                    failures.Add("Confirmed overwrite detail is missing or still asks for confirmation.");
                }
                else
                {
                    Debug.Log(LogPrefix + "PASS Negative guard: confirmed overwrite clears the guard and reports the confirmation.");
                }
            }

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    "Velocity Rush WebGL tool validation failed: " + string.Join(" | ", failures));
            }

            Debug.Log(LogPrefix + "VALIDATION PASS: menu resolved, preflight passed, JSON report generated (no build).");
        }

        private static void ValidateScenes(WebGLPreflightReport report, WebGLBuildProfile profile)
        {
            var scenePaths = GetScenePaths(profile);
            var configured = EditorBuildSettings.scenes;
            var matches = configured != null && configured.Length == scenePaths.Length;

            if (matches)
            {
                for (var i = 0; i < scenePaths.Length; i++)
                {
                    if (configured[i].path != scenePaths[i] || !configured[i].enabled)
                    {
                        matches = false;
                        break;
                    }
                }
            }

            Add(
                report,
                "Build scenes match the approved " + GetProfileDisplayName(profile) + " order exactly",
                matches,
                WebGLPreflightCode.SceneConfigurationMismatch,
                matches
                    ? DescribeSceneOrder(profile) + " (both enabled)."
                    : "Expected exactly: [0] " + scenePaths[0] + " (enabled), [1] " + scenePaths[1] +
                      " (enabled). Actual: " + DescribeScenes(configured) +
                      ". The tool never adds, removes, reorders, or enables scenes.");

            for (var i = 0; i < scenePaths.Length; i++)
            {
                var path = scenePaths[i];
                var absolute = Path.Combine(ProjectRoot, path);
                var exists = File.Exists(absolute) && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null;
                Add(
                    report,
                    "Scene asset exists: " + path,
                    exists,
                    WebGLPreflightCode.SceneAssetMissing,
                    exists ? "Found and loadable." : "Missing or not importable at " + absolute + ".");
            }
        }

        private static void ValidateOutput(
            WebGLPreflightReport report,
            string outputDirectory,
            bool isBuildRequest,
            bool allowNonEmptyOutput)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                Add(report, "Output folder is specified", false, WebGLPreflightCode.OutputInvalid, "Choose an output folder.");
                return;
            }

            string fullOutput;
            try
            {
                fullOutput = Path.GetFullPath(outputDirectory);
            }
            catch (Exception exception)
            {
                Add(report, "Output folder is a valid path", false, WebGLPreflightCode.OutputInvalid, exception.Message);
                return;
            }

            Add(report, "Output folder is a valid path", true, WebGLPreflightCode.OutputInvalid, fullOutput);

            var insideProject = IsSameOrChildPath(ProjectRoot, fullOutput);
            Add(
                report,
                "Output folder is outside the Unity project",
                !insideProject,
                WebGLPreflightCode.OutputInsideProject,
                insideProject
                    ? "Refusing to write build output inside the project: " + fullOutput
                    : "Output is outside " + ProjectRoot + ".");

            if (!insideProject)
            {
                var containsProject = IsSameOrChildPath(fullOutput, ProjectRoot);
                Add(
                    report,
                    "Output folder does not contain the Unity project",
                    !containsProject,
                    WebGLPreflightCode.OutputContainsProject,
                    containsProject
                        ? "Refusing to write build output to a parent of the project: " + fullOutput +
                          ". Choose a dedicated build folder instead."
                        : "Output does not contain the Unity project.");
            }

            if (File.Exists(fullOutput))
            {
                Add(
                    report,
                    "Output path is a folder, not a file",
                    false,
                    WebGLPreflightCode.OutputIsFile,
                    "A file already exists at the output path: " + fullOutput);
                return;
            }

            Add(report, "Output path is a folder, not a file", true, WebGLPreflightCode.OutputIsFile, "Folder path is usable.");

            if (!isBuildRequest)
            {
                return;
            }

            if (!Directory.Exists(fullOutput))
            {
                Add(
                    report,
                    "Output folder is empty or explicitly confirmed",
                    true,
                    WebGLPreflightCode.OutputNotEmpty,
                    "Folder does not exist yet; it will be created by the build.");
                return;
            }

            var hasEntries = Directory.GetFileSystemEntries(fullOutput).Length > 0;
            string detail;
            if (!hasEntries)
            {
                detail = "Folder is empty.";
            }
            else if (allowNonEmptyOutput)
            {
                detail = "Explicit overwrite confirmation received for a non-empty folder: " + fullOutput;
            }
            else
            {
                detail = "The folder already contains files. Confirm overwrite explicitly or choose an empty folder: " + fullOutput;
            }

            Add(
                report,
                "Output folder is empty or explicitly confirmed",
                allowNonEmptyOutput || !hasEntries,
                WebGLPreflightCode.OutputNotEmpty,
                detail);
        }

        private static WebGLBuildOutcome CreateOutcome(BuildReport buildReport, string outputDirectory)
        {
            if (buildReport == null)
            {
                return new WebGLBuildOutcome
                {
                    Attempted = true,
                    Succeeded = false,
                    Result = "NoReport",
                    FailureReason = "BuildPipeline returned no BuildReport.",
                    OutputDirectory = outputDirectory
                };
            }

            var summary = buildReport.summary;
            return new WebGLBuildOutcome
            {
                Attempted = true,
                Succeeded = summary.result == BuildResult.Succeeded,
                Result = summary.result.ToString(),
                DurationSeconds = summary.totalTime.TotalSeconds,
                TotalSizeBytes = summary.totalSize,
                TotalErrors = summary.totalErrors,
                TotalWarnings = summary.totalWarnings,
                OutputDirectory = outputDirectory
            };
        }

        private static void TryWriteReport(
            string outputDirectory,
            WebGLPreflightReport preflight,
            BuildReport buildReport,
            WebGLBuildOutcome outcome,
            WebGLBuildProfile profile)
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
                var path = Path.Combine(outputDirectory, BuildReportFileName);

                // Set the path before serializing: the report includes its own location, and writing
                // it afterwards produced an empty "build.reportPath" (T5.5.3 defect).
                outcome.ReportPath = path;
                WriteReport(path, preflight, buildReport, outcome, profile);
                Debug.Log(LogPrefix + "JSON report: " + path);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Failed to write JSON report: " + exception);
            }
        }

        private static void LogOutcome(WebGLBuildOutcome outcome)
        {
            Debug.Log(
                LogPrefix + "Build result: " + outcome.Result +
                " | duration " + outcome.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "s" +
                " | size " + outcome.TotalSizeBytes.ToString(CultureInfo.InvariantCulture) + " bytes" +
                " | errors " + outcome.TotalErrors +
                " | warnings " + outcome.TotalWarnings +
                " | output " + outcome.OutputDirectory);

            var artifacts = CollectArtifacts(outcome.OutputDirectory);
            if (artifacts.Count == 0)
            {
                Debug.Log(LogPrefix + "Artifacts: none found.");
                return;
            }

            for (var i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                var parts = new List<string>();
                for (var a = 0; a < artifacts.Count; a++)
                {
                    if (artifacts[a].Category == category.Key)
                    {
                        parts.Add(artifacts[a].RelativePath + "=" + artifacts[a].Bytes.ToString(CultureInfo.InvariantCulture) + " B");
                    }
                }

                Debug.Log(
                    LogPrefix + "Artifact " + category.Label + ": " +
                    (parts.Count == 0 ? "not present" : string.Join("; ", parts)));
            }
        }

        private static void ShowOutcomeDialog(WebGLBuildOutcome outcome)
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var artifacts = CollectArtifacts(outcome.OutputDirectory);
            var details = new StringBuilder();
            details.AppendLine("Result: " + outcome.Result);
            details.AppendLine("Duration: " + outcome.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "s");
            details.AppendLine("BuildReport size: " + outcome.TotalSizeBytes.ToString(CultureInfo.InvariantCulture) + " bytes");
            details.AppendLine("Errors: " + outcome.TotalErrors + " | Warnings: " + outcome.TotalWarnings);
            details.AppendLine("Output: " + outcome.OutputDirectory);
            if (!string.IsNullOrEmpty(outcome.ReportPath))
            {
                details.AppendLine("Report: " + outcome.ReportPath);
            }

            for (var i = 0; i < Categories.Length; i++)
            {
                var category = Categories[i];
                var categoryBytes = 0L;
                var present = 0;
                for (var a = 0; a < artifacts.Count; a++)
                {
                    if (artifacts[a].Category == category.Key)
                    {
                        categoryBytes += artifacts[a].Bytes;
                        present++;
                    }
                }

                details.AppendLine(
                    category.Label + ": " + (present == 0
                        ? "not present"
                        : categoryBytes.ToString(CultureInfo.InvariantCulture) + " bytes (" + present + " file(s))"));
            }

            if (!string.IsNullOrEmpty(outcome.FailureReason))
            {
                details.AppendLine();
                details.AppendLine(outcome.FailureReason);
            }

            EditorUtility.DisplayDialog(
                outcome.Succeeded ? "WebGL build succeeded" : "WebGL build failed",
                details.ToString(),
                "OK");
        }

        private static List<WebGLArtifact> CollectArtifacts(string outputDirectory)
        {
            var artifacts = new List<WebGLArtifact>();
            if (string.IsNullOrEmpty(outputDirectory) || !Directory.Exists(outputDirectory))
            {
                return artifacts;
            }

            var root = Path.GetFullPath(outputDirectory);
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var category = Classify(Path.GetFileName(file));
                if (category == null)
                {
                    continue;
                }

                long bytes;
                try
                {
                    bytes = new FileInfo(file).Length;
                }
                catch (IOException)
                {
                    continue;
                }

                artifacts.Add(new WebGLArtifact
                {
                    Category = category,
                    RelativePath = Path.GetRelativePath(root, file).Replace('\\', '/'),
                    Bytes = bytes
                });
            }

            artifacts.Sort((left, right) => string.CompareOrdinal(left.RelativePath, right.RelativePath));
            return artifacts;
        }

        private static string Classify(string fileName)
        {
            if (string.Equals(fileName, "index.html", StringComparison.OrdinalIgnoreCase) ||
                fileName.StartsWith("index.html.", StringComparison.OrdinalIgnoreCase))
            {
                return "indexHtml";
            }

            if (fileName.IndexOf(".framework.js", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "frameworkJs";
            }

            if (fileName.IndexOf(".loader.js", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "loaderJs";
            }

            if (fileName.IndexOf(".wasm", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "wasm";
            }

            if (fileName.IndexOf(".data", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "data";
            }

            return null;
        }

        private static bool IsSameOrChildPath(string parent, string candidate)
        {
            var relative = Path.GetRelativePath(parent, candidate);
            return relative == "." ||
                   (!Path.IsPathRooted(relative) &&
                    !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                    relative != "..");
        }

        private static string DescribeScenes(EditorBuildSettingsScene[] scenes)
        {
            if (scenes == null || scenes.Length == 0)
            {
                return "(no scenes configured)";
            }

            var parts = new string[scenes.Length];
            for (var i = 0; i < scenes.Length; i++)
            {
                parts[i] = "[" + i + "] " + scenes[i].path + (scenes[i].enabled ? " (enabled)" : " (disabled)");
            }

            return string.Join(", ", parts);
        }

        private static void Add(
            WebGLPreflightReport report,
            string name,
            bool passed,
            WebGLPreflightCode code,
            string detail)
        {
            report.Checks.Add(new WebGLPreflightCheck(name, passed, code, detail));
        }

        private static void WriteReport(
            string path,
            WebGLPreflightReport preflight,
            BuildReport buildReport,
            WebGLBuildOutcome outcome,
            WebGLBuildProfile profile)
        {
            var scenePaths = GetScenePaths(profile);
            var json = new StringBuilder(8192);
            json.Append("{\n");
            Property(json, 1, "schemaVersion", SchemaVersion.ToString(CultureInfo.InvariantCulture), true);
            StringProperty(json, 1, "tool", "VelocityRushWebGLBuilder", true);
            StringProperty(json, 1, "kind", outcome.Attempted ? "build" : "preflight", true);
            StringProperty(json, 1, "generatedAtUtc", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture), true);
            StringProperty(json, 1, "unityVersion", Application.unityVersion, true);
            StringProperty(json, 1, "projectRoot", ProjectRoot, true);
            StringProperty(json, 1, "buildTarget", "WebGL", true);
            StringProperty(json, 1, "profile", profile.ToString(), true);
            StringProperty(json, 1, "profileScenes", DescribeSceneOrder(profile), true);
            Property(json, 1, "developmentBuild", "true", true);
            Property(json, 1, "playerSettingsModified", "false", true);
            StringProperty(json, 1, "outputDirectory", outcome.OutputDirectory, true);

            json.Append("  \"scenes\": [\n");
            var configured = EditorBuildSettings.scenes;
            for (var i = 0; i < scenePaths.Length; i++)
            {
                var enabled = configured != null && i < configured.Length && configured[i].enabled;
                var actualPath = configured != null && i < configured.Length ? configured[i].path : string.Empty;
                json.Append("    {\n");
                Property(json, 3, "order", i.ToString(CultureInfo.InvariantCulture), true);
                StringProperty(json, 3, "approvedPath", scenePaths[i], true);
                StringProperty(json, 3, "configuredPath", actualPath, true);
                Property(json, 3, "configuredEnabled", enabled ? "true" : "false", false);
                json.Append("    }").Append(i == scenePaths.Length - 1 ? "\n" : ",\n");
            }

            json.Append("  ],\n");

            json.Append("  \"preflight\": {\n");
            Property(json, 2, "passed", preflight.Passed ? "true" : "false", true);
            json.Append("    \"checks\": [\n");
            for (var i = 0; i < preflight.Checks.Count; i++)
            {
                var check = preflight.Checks[i];
                json.Append("      {\n");
                StringProperty(json, 4, "name", check.Name, true);
                Property(json, 4, "passed", check.Passed ? "true" : "false", true);
                StringProperty(json, 4, "code", check.Code.ToString(), true);
                StringProperty(json, 4, "detail", check.Detail, false);
                json.Append("      }").Append(i == preflight.Checks.Count - 1 ? "\n" : ",\n");
            }

            json.Append("    ]\n");
            json.Append("  },\n");

            json.Append("  \"build\": ");
            if (!outcome.Attempted)
            {
                json.Append("null,\n");
            }
            else
            {
                json.Append("{\n");
                Property(json, 2, "attempted", "true", true);
                StringProperty(json, 2, "result", outcome.Result, true);
                Property(json, 2, "succeeded", outcome.Succeeded ? "true" : "false", true);
                Property(json, 2, "durationSeconds", outcome.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture), true);
                Property(json, 2, "totalSizeBytes", outcome.TotalSizeBytes.ToString(CultureInfo.InvariantCulture), true);
                Property(json, 2, "totalErrors", outcome.TotalErrors.ToString(CultureInfo.InvariantCulture), true);
                Property(json, 2, "totalWarnings", outcome.TotalWarnings.ToString(CultureInfo.InvariantCulture), true);
                StringProperty(json, 2, "outputPath", buildReport != null ? buildReport.summary.outputPath : outcome.OutputDirectory, true);
                StringProperty(
                    json,
                    2,
                    "buildStartedAtUtc",
                    buildReport != null ? buildReport.summary.buildStartedAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) : string.Empty,
                    true);
                StringProperty(
                    json,
                    2,
                    "buildEndedAtUtc",
                    buildReport != null ? buildReport.summary.buildEndedAt.ToUniversalTime().ToString("o", CultureInfo.InvariantCulture) : string.Empty,
                    true);
                StringProperty(json, 2, "reportPath", outcome.ReportPath ?? string.Empty, true);
                StringProperty(json, 2, "failureReason", outcome.FailureReason ?? string.Empty, false);
                json.Append("  },\n");
            }

            var artifacts = CollectArtifacts(outcome.OutputDirectory);
            json.Append("  \"artifacts\": {\n");
            for (var c = 0; c < Categories.Length; c++)
            {
                var category = Categories[c];
                json.Append("    ");
                StringPropertyInline(json, category.Key);
                json.Append(": [");

                var first = true;
                for (var a = 0; a < artifacts.Count; a++)
                {
                    if (artifacts[a].Category != category.Key)
                    {
                        continue;
                    }

                    if (!first)
                    {
                        json.Append(", ");
                    }

                    first = false;
                    json.Append("{\"path\": ");
                    AppendJsonString(json, artifacts[a].RelativePath);
                    json.Append(", \"bytes\": ").Append(artifacts[a].Bytes.ToString(CultureInfo.InvariantCulture)).Append('}');
                }

                json.Append(']').Append(c == Categories.Length - 1 ? "\n" : ",\n");
            }

            json.Append("  }\n");
            json.Append("}\n");

            File.WriteAllText(path, json.ToString(), new UTF8Encoding(false));
        }

        private static void Property(StringBuilder json, int indent, string name, string rawValue, bool comma)
        {
            json.Append(' ', indent * 2).Append('"').Append(name).Append("\": ").Append(rawValue);
            json.Append(comma ? ",\n" : "\n");
        }

        private static void StringProperty(StringBuilder json, int indent, string name, string value, bool comma)
        {
            json.Append(' ', indent * 2);
            AppendJsonString(json, name);
            json.Append(": ");
            AppendJsonString(json, value);
            json.Append(comma ? ",\n" : "\n");
        }

        private static void StringPropertyInline(StringBuilder json, string name)
        {
            AppendJsonString(json, name);
        }

        private static void AppendJsonString(StringBuilder json, string value)
        {
            json.Append('"');
            if (value != null)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    var character = value[i];
                    switch (character)
                    {
                        case '"':
                            json.Append("\\\"");
                            break;
                        case '\\':
                            json.Append("\\\\");
                            break;
                        case '\b':
                            json.Append("\\b");
                            break;
                        case '\f':
                            json.Append("\\f");
                            break;
                        case '\n':
                            json.Append("\\n");
                            break;
                        case '\r':
                            json.Append("\\r");
                            break;
                        case '\t':
                            json.Append("\\t");
                            break;
                        default:
                            if (character < ' ')
                            {
                                json.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                json.Append(character);
                            }

                            break;
                    }
                }
            }

            json.Append('"');
        }

        private sealed class ArtifactCategory
        {
            public ArtifactCategory(string key, string label)
            {
                Key = key;
                Label = label;
            }

            public string Key { get; private set; }

            public string Label { get; private set; }
        }

        private sealed class WebGLArtifact
        {
            public string Category;
            public string RelativePath;
            public long Bytes;
        }
    }

    /// <summary>Preflight outcome codes used to distinguish actionable failures.</summary>
    public enum WebGLPreflightCode
    {
        None = 0,
        EditorBusy = 1,
        WebGLSupportMissing = 2,
        ActiveBuildTargetMismatch = 3,
        SceneConfigurationMismatch = 4,
        SceneAssetMissing = 5,
        OutputInvalid = 6,
        OutputInsideProject = 7,
        OutputNotEmpty = 8,
        OutputIsFile = 9,
        OutputContainsProject = 10
    }

    /// <summary>One blocking or informational pre-build check.</summary>
    public sealed class WebGLPreflightCheck
    {
        public WebGLPreflightCheck(string name, bool passed, WebGLPreflightCode code, string detail)
        {
            Name = name;
            Passed = passed;
            Code = code;
            Detail = detail;
        }

        public string Name { get; private set; }

        public bool Passed { get; private set; }

        public WebGLPreflightCode Code { get; private set; }

        public string Detail { get; private set; }
    }

    /// <summary>Complete preflight result; <see cref="Passed"/> is true only when every check passes.</summary>
    public sealed class WebGLPreflightReport
    {
        public WebGLPreflightReport()
        {
            Checks = new List<WebGLPreflightCheck>();
        }

        public List<WebGLPreflightCheck> Checks { get; private set; }

        public bool Passed
        {
            get
            {
                for (var i = 0; i < Checks.Count; i++)
                {
                    if (!Checks[i].Passed)
                    {
                        return false;
                    }
                }

                return Checks.Count > 0;
            }
        }

        public bool HasFailure(WebGLPreflightCode code)
        {
            for (var i = 0; i < Checks.Count; i++)
            {
                if (!Checks[i].Passed && Checks[i].Code == code)
                {
                    return true;
                }
            }

            return false;
        }

        public string FailuresSummary
        {
            get
            {
                var lines = new List<string>();
                for (var i = 0; i < Checks.Count; i++)
                {
                    if (!Checks[i].Passed)
                    {
                        lines.Add("- " + Checks[i].Name + ": " + Checks[i].Detail);
                    }
                }

                return lines.Count == 0 ? "No failures." : string.Join("\n", lines.ToArray());
            }
        }
    }

    /// <summary>Outcome of a build attempt (or of a preflight-only automation run).</summary>
    public sealed class WebGLBuildOutcome
    {
        public bool Attempted;
        public bool Succeeded;
        public string Result = "NotAttempted";
        public double DurationSeconds;
        public ulong TotalSizeBytes;
        public int TotalErrors;
        public int TotalWarnings;
        public string OutputDirectory;
        public string ReportPath;
        public string FailureReason;
    }

    /// <summary>
    /// Minimal interactive window for the WebGL development build tool. Opening the window never
    /// builds; the user must run preflight and press the build button explicitly.
    /// </summary>
    public sealed class VelocityRushWebGLBuildWindow : EditorWindow
    {
        private string _outputDirectory;
        private string _displayedOutputDirectory;
        private string _confirmedOutputDirectory;
        private WebGLPreflightReport _preflight;
        private WebGLBuildOutcome _outcome;
        private Vector2 _scroll;
        private string _message;
        private MessageType _messageType = MessageType.Info;
        private WebGLBuildProfile _profile = WebGLBuildProfile.Prototype;

        private void OnEnable()
        {
            // A domain reload or reopened window must never show a previous run's summary.
            _preflight = null;
            _outcome = null;
            _message = null;
            _confirmedOutputDirectory = null;

            if (string.IsNullOrEmpty(_outputDirectory))
            {
                _outputDirectory = VelocityRushWebGLBuilder.CreateDefaultOutputDirectory();
            }

            _profile = VelocityRushWebGLBuilder.DetectConfiguredProfile();
            _displayedOutputDirectory = VelocityRushWebGLBuilder.NormalizeOutputPath(_outputDirectory);
        }

        private void OnGUI()
        {
            // Editing the output path invalidates the previous preflight/result. Keeping them would
            // show a stale summary for a folder that is no longer selected.
            var currentOutputDirectory = VelocityRushWebGLBuilder.NormalizeOutputPath(_outputDirectory);
            if (!string.Equals(currentOutputDirectory, _displayedOutputDirectory, StringComparison.Ordinal))
            {
                _displayedOutputDirectory = currentOutputDirectory;
                _preflight = null;
                _outcome = null;
                _message = null;
                _confirmedOutputDirectory = null;
            }

            EditorGUILayout.LabelField("Velocity Rush - WebGL Development Build", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Editor-only tool. Validates the approved scene order and WebGL setup, builds a Development WebGL player, " +
                "and writes a JSON report. It never edits ProjectSettings/PlayerSettings, packages, scenes, or previous builds.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build profile (scene contract)");
            var selectedProfile = (WebGLBuildProfile)EditorGUILayout.EnumPopup(_profile);
            if (selectedProfile != _profile)
            {
                _profile = selectedProfile;
                _preflight = null;
                _outcome = null;
                _message = null;
                _confirmedOutputDirectory = null;
            }

            EditorGUILayout.LabelField(
                "Required build scenes: " + VelocityRushWebGLBuilder.DescribeSceneOrder(_profile),
                EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Output folder (must be outside the Unity project)");
            using (new EditorGUILayout.HorizontalScope())
            {
                _outputDirectory = EditorGUILayout.TextField(_outputDirectory);
                if (GUILayout.Button("Browse...", GUILayout.Width(84f)))
                {
                    var selected = EditorUtility.SaveFolderPanel(
                        "Select WebGL output folder",
                        Directory.Exists(_outputDirectory) ? _outputDirectory : VelocityRushWebGLBuilder.ProjectRoot,
                        string.Empty);
                    if (!string.IsNullOrEmpty(selected))
                    {
                        _outputDirectory = selected;
                    }
                }

                if (GUILayout.Button("Default", GUILayout.Width(84f)))
                {
                    _outputDirectory = VelocityRushWebGLBuilder.CreateDefaultOutputDirectory();
                }
            }

            EditorGUILayout.LabelField(
                "Default (outside project): " + VelocityRushWebGLBuilder.CreateDefaultOutputDirectory(),
                EditorStyles.miniLabel);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Preflight (no build)", GUILayout.Height(30f)))
                {
                    _preflight = VelocityRushWebGLBuilder.RunPreflight(_outputDirectory, false, false, _profile);
                    _outcome = null;
                    _confirmedOutputDirectory = null;
                    _message = _preflight.Passed ? "Preflight passed. No build was started." : "Preflight failed.";
                    _messageType = _preflight.Passed ? MessageType.Info : MessageType.Error;
                }

                if (GUILayout.Button("Build WebGL Development", GUILayout.Height(30f)))
                {
                    BuildWithConfirmation();
                }
            }

            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, _messageType);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preflight", EditorStyles.boldLabel);
            if (_preflight == null)
            {
                EditorGUILayout.LabelField("Run preflight to see the checks.", EditorStyles.miniLabel);
            }
            else
            {
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(180f));
                for (var i = 0; i < _preflight.Checks.Count; i++)
                {
                    var check = _preflight.Checks[i];
                    EditorGUILayout.LabelField(
                        (check.Passed ? "PASS" : "FAIL") + "  " + check.Name,
                        check.Passed ? EditorStyles.label : EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("      " + check.Detail, EditorStyles.miniLabel);
                }

                EditorGUILayout.EndScrollView();
            }

            if (_outcome != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Last build", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Result: " + _outcome.Result);
                EditorGUILayout.LabelField(
                    "Duration: " + _outcome.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture) + "s");
                EditorGUILayout.LabelField(
                    "BuildReport size: " + _outcome.TotalSizeBytes.ToString(CultureInfo.InvariantCulture) + " bytes");
                EditorGUILayout.LabelField(
                    "Errors: " + _outcome.TotalErrors + " | Warnings: " + _outcome.TotalWarnings);
                EditorGUILayout.LabelField("Output: " + _outcome.OutputDirectory);
                if (!string.IsNullOrEmpty(_outcome.ReportPath))
                {
                    EditorGUILayout.LabelField("Report: " + _outcome.ReportPath);
                }
            }
        }

        private void BuildWithConfirmation()
        {
            var outputPath = VelocityRushWebGLBuilder.NormalizeOutputPath(_outputDirectory);
            var preflight = VelocityRushWebGLBuilder.RunPreflight(_outputDirectory, true, false, _profile);

            if (!preflight.Passed && preflight.HasFailure(WebGLPreflightCode.OutputNotEmpty))
            {
                var proceed = EditorUtility.DisplayDialog(
                    "Output folder is not empty",
                    "The selected folder already contains files:\n\n" + _outputDirectory +
                    "\n\nBuilding may overwrite files with the same names. No previous files are deleted. Continue?",
                    "Continue and overwrite",
                    "Cancel");
                if (!proceed)
                {
                    _preflight = preflight;
                    _outcome = null;
                    _message = "Build cancelled. The existing output folder was not touched.";
                    _messageType = MessageType.Warning;
                    return;
                }

                _confirmedOutputDirectory = outputPath;
                preflight = VelocityRushWebGLBuilder.RunPreflight(_outputDirectory, true, true, _profile);
            }

            _preflight = preflight;
            if (!preflight.Passed)
            {
                _outcome = null;
                _message = "Preflight failed. The build was not started.";
                _messageType = MessageType.Error;
                return;
            }

            // Overwrite consent is scoped to the exact folder the user confirmed. A path edit
            // clears it, so Build() cannot overwrite a different non-empty folder by accident.
            var allowNonEmptyOutput =
                !string.IsNullOrEmpty(_confirmedOutputDirectory) &&
                string.Equals(outputPath, _confirmedOutputDirectory, StringComparison.Ordinal);

            _outcome = VelocityRushWebGLBuilder.Build(_outputDirectory, allowNonEmptyOutput, _profile);
            if (_outcome.Succeeded)
            {
                _confirmedOutputDirectory = null;
            }

            _message = _outcome.Succeeded ? "Build succeeded." : "Build failed: " + _outcome.FailureReason;
            _messageType = _outcome.Succeeded ? MessageType.Info : MessageType.Error;
            Repaint();
        }
    }
}
