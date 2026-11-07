using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// A single unit of bootstrap work (TDD §30.6 step 2, S1 service-graph construction).
    /// Steps must be deterministic: their execution order is the registration order, and a step
    /// must not depend on steps registered after it.
    /// </summary>
    public sealed class StartupStep
    {
        /// <summary>Stable, human-readable step name used in diagnostics and timing segments.</summary>
        public string Name { get; }

        public Action Execute { get; }

        public StartupStep(string name, Action execute)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Startup step name must not be empty.", nameof(name));
            }

            Name = name;
            Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }
    }
}
