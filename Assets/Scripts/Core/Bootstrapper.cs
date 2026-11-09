using System;
using UnityEngine;

namespace VelocityRush.Core
{
    /// <summary>
    /// Composition root and sole application entry point (TDD §29 S1).
    /// Lives in the persistent root scene (§35.3 — scene-added, NOT DontDestroyOnLoad: the
    /// persistent root stays loaded for the whole app lifetime and race content loads additively).
    /// Nothing else constructs services; later systems attach their initializers to the ordered
    /// <see cref="ApplicationStartup"/> sequence instead of building parallel boot paths.
    /// Contains no gameplay logic.
    /// </summary>
    public sealed class Bootstrapper : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Boot] ";

        private bool _hasRun;

        /// <summary>
        /// True only when the ordered startup sequence completed without failure. The temporary
        /// prototype entry gate reads this before loading content; it stays false when startup
        /// fails closed and is unaffected by duplicate initialization requests.
        /// </summary>
        public bool StartupSucceeded { get; private set; }

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Runs the startup sequence once. Callers after the first run are rejected with a
        /// warning (duplicate-initialization guard; TDD §29 S1 "nothing else constructs services").
        /// </summary>
        public void Initialize()
        {
            if (_hasRun)
            {
                Debug.LogWarning(LogPrefix + "Bootstrap already ran for this application instance; duplicate initialization request ignored.", this);
                return;
            }

            _hasRun = true;

            ApplicationStartup startup;
            StartupReport report;
            try
            {
                startup = new ApplicationStartup(new[] { CreateEnvironmentSnapshotStep() });
                report = startup.Run();
                StartupSucceeded = true;
            }
            catch (StartupException failure)
            {
                // Fail-closed (§34.2/§35.7): name the failing step loudly and stop — the app must not
                // continue into later phases (gameplay, data loading) in a half-initialized state.
                Debug.LogError(LogPrefix + failure.Message + " Startup aborts (fail-closed).", this);
                return;
            }

            // S1 may log startup segment timings as profiling evidence (§29 S1; feeds the evidence log).
            Debug.Log(LogPrefix + "Bootstrap completed in " + report.TotalMilliseconds + " ms — " + report, this);
        }

        /// <summary>
        /// Current-phase step: capture the runtime environment identity once at boot as startup
        /// evidence (platform, Unity version, device). Later phases insert their ordered steps
        /// ahead of / behind this one via the same <see cref="ApplicationStartup"/> sequence.
        /// </summary>
        private StartupStep CreateEnvironmentSnapshotStep()
        {
            return new StartupStep("environment-snapshot", () =>
            {
                var model = SystemInfo.deviceModel;
                if (string.IsNullOrEmpty(model))
                {
                    model = "unavailable";
                }

                Debug.Log(
                    LogPrefix + "Environment: Unity " + Application.version
                    + " on " + Application.platform
                    + " (device model: " + model + ")",
                    this);
            });
        }
    }
}
