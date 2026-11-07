using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VelocityRush.Core
{
    /// <summary>
    /// Runs the ordered bootstrap step sequence exactly once and records timing evidence.
    /// Execution is deterministic (registration order), fail-closed at the first failing step,
    /// and allocation happens only here at boot — never per frame (WebGL GC discipline, TDD §40).
    /// </summary>
    public sealed class ApplicationStartup
    {
        private readonly List<StartupStep> _steps;

        /// <summary>Steps in execution order. Registration order is the sole execution order.</summary>
        public IReadOnlyList<StartupStep> Steps => _steps;

        public ApplicationStartup(IEnumerable<StartupStep> steps)
        {
            if (steps == null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _steps = new List<StartupStep>(steps);
            var seenNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var step in _steps)
            {
                if (!seenNames.Add(step.Name))
                {
                    throw new ArgumentException(
                        $"Duplicate bootstrap step name '{step.Name}'; step names must be unique to keep diagnostics unambiguous.",
                        nameof(steps));
                }
            }
        }

        /// <summary>
        /// Executes every step in order. On the first failing step, throws <see cref="StartupException"/>
        /// naming that step; later steps are never executed.
        /// </summary>
        public StartupReport Run()
        {
            var segments = new List<StartupSegment>(_steps.Count);
            var total = Stopwatch.StartNew();

            foreach (var step in _steps)
            {
                var segment = Stopwatch.StartNew();
                try
                {
                    step.Execute();
                }
                catch (Exception failure)
                {
                    throw new StartupException(step.Name, failure);
                }

                segments.Add(new StartupSegment(step.Name, segment.ElapsedMilliseconds));
            }

            return new StartupReport(segments, total.ElapsedMilliseconds);
        }
    }
}
