using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NUnit.Framework;
using VelocityRush.Core;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Real EditMode tests for the implemented bootstrap foundation:
    /// deterministic ordering, fail-closed error handling, duplicate-name rejection,
    /// and the Bootstrapper's exactly-once entry-point behavior.
    /// </summary>
    public sealed class ApplicationStartupTests
    {
        [Test]
        public void Run_ExecutesStepsInRegistrationOrder()
        {
            var executed = new List<string>();
            var startup = new ApplicationStartup(new[]
            {
                new StartupStep("first", () => executed.Add("first")),
                new StartupStep("second", () => executed.Add("second")),
                new StartupStep("third", () => executed.Add("third")),
            });

            var report = startup.Run();

            Assert.That(executed, Is.EqualTo(new[] { "first", "second", "third" }), "Steps must run exactly in registration order.");
            Assert.That(SegmentNames(report.Segments), Is.EqualTo(new[] { "first", "second", "third" }), "Report segments must mirror the execution order.");
        }

        [Test]
        public void Run_OnFailingStep_ThrowsStartupExceptionNamingStepAndStopsLaterSteps()
        {
            var executed = new List<string>();
            var startup = new ApplicationStartup(new[]
            {
                new StartupStep("ok", () => executed.Add("ok")),
                new StartupStep("broken", () => throw new InvalidOperationException("boom")),
                new StartupStep("unreachable", () => executed.Add("unreachable")),
            });

            var failure = Assert.Throws<StartupException>(() => startup.Run());

            Assert.That(failure.StepName, Is.EqualTo("broken"), "The exception must name the failing step.");
            Assert.That(failure.InnerException, Is.InstanceOf<InvalidOperationException>(), "The original failure must be preserved.");
            Assert.That(failure.InnerException.Message, Is.EqualTo("boom"));
            Assert.That(executed, Is.EqualTo(new[] { "ok" }), "Execution must stop at the first failure — later steps are never run (fail-closed).");
        }

        [Test]
        public void Construction_WithDuplicateStepNames_IsRejected()
        {
            var duplicate = Assert.Throws<ArgumentException>(() => new ApplicationStartup(new[]
            {
                new StartupStep("step", () => { }),
                new StartupStep("step", () => { }),
            }));

            Assert.That(duplicate.Message, Does.Contain("unique"), "Rejecting duplicate step names keeps bootstrap diagnostics unambiguous.");
        }

        [Test]
        public void Construction_WithInvalidStepMembers_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new StartupStep(null, () => { }));
            Assert.Throws<ArgumentException>(() => new StartupStep("", () => { }));
            Assert.Throws<ArgumentNullException>(() => new StartupStep("named", null));
            Assert.Throws<ArgumentNullException>(() => new ApplicationStartup(null));
        }

        [Test]
        public void Run_WithZeroSteps_ProducesEmptyReport()
        {
            var report = new ApplicationStartup(Array.Empty<StartupStep>()).Run();

            Assert.That(report.Segments, Is.Empty);
            Assert.That(report.TotalMilliseconds, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void Report_IncludesPerStepAndTotalTimings()
        {
            var startup = new ApplicationStartup(new[]
            {
                new StartupStep("only", () => { }),
            });

            var report = startup.Run();

            Assert.That(report.Segments, Has.Count.EqualTo(1));
            Assert.That(report.Segments[0].Name, Is.EqualTo("only"));
            Assert.That(report.Segments[0].ElapsedMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(report.TotalMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(report.ToString(), Does.Contain("only=").And.Contain("total "));
        }

        [Test]
        public void Bootstrapper_SecondInitialize_IsRejectedWithDuplicateWarning()
        {
            var logs = new List<string>();
            void CaptureLog(string condition, string _, LogType __) => logs.Add(condition);

            Application.logMessageReceived += CaptureLog;
            var bootstrapper = new GameObject("BootstrapTestRoot").AddComponent<Bootstrapper>();
            try
            {
                bootstrapper.Initialize();
                bootstrapper.Initialize();
            }
            finally
            {
                Application.logMessageReceived -= CaptureLog;
                UnityEngine.Object.DestroyImmediate(bootstrapper.gameObject);
            }

            Assert.That(
                logs,
                Has.Some.Matches<string>(message =>
                    message.Contains("Bootstrap")
                    && message.Contains("already ran")
                    && message.Contains("duplicate")),
                "The duplicate initialization request must be rejected with a clear warning.");
            Assert.That(
                logs,
                Has.Some.Matches<string>(message =>
                    message.Contains("Bootstrap completed")
                    && message.Contains("segments:")
                    && message.Contains("environment-snapshot")),
                "The first run must complete and emit startup segment-timing evidence.");
        }

        private static string[] SegmentNames(IReadOnlyList<StartupSegment> segments)
        {
            return segments.Select(segment => segment.Name).ToArray();
        }
    }
}
