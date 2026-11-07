using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// Thrown when a bootstrap step fails. Startup is fail-closed (TDD §34.2 / §35.7):
    /// the run stops at the first failing step and diagnostics name the defect — nothing
    /// continues in a half-initialized state and nothing is silently patched.
    /// </summary>
    public sealed class StartupException : Exception
    {
        public string StepName { get; }

        public StartupException(string stepName, Exception innerException)
            : base($"Bootstrap failed at startup step '{stepName}': {innerException.Message}", innerException)
        {
            StepName = stepName;
        }
    }
}
