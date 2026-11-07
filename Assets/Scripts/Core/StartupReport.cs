using System;
using System.Collections.Generic;
using System.Text;

namespace VelocityRush.Core
{
    /// <summary>Timing evidence for one completed bootstrap step (TDD §29 S1 segment timings).</summary>
    public readonly struct StartupSegment
    {
        public string Name { get; }
        public long ElapsedMilliseconds { get; }

        public StartupSegment(string name, long elapsedMilliseconds)
        {
            Name = name;
            ElapsedMilliseconds = elapsedMilliseconds;
        }
    }

    /// <summary>
    /// Immutable result of a completed bootstrap run: per-step timing segments plus the total.
    /// A report only exists for runs that finished without a failure — failures throw instead.
    /// </summary>
    public sealed class StartupReport
    {
        public IReadOnlyList<StartupSegment> Segments { get; }

        public long TotalMilliseconds { get; }

        public StartupReport(IReadOnlyList<StartupSegment> segments, long totalMilliseconds)
        {
            Segments = segments;
            TotalMilliseconds = totalMilliseconds;
        }

        public override string ToString()
        {
            var builder = new StringBuilder("segments:");
            for (var i = 0; i < Segments.Count; i++)
            {
                builder.Append(i == 0 ? " " : ", ");
                builder.Append(Segments[i].Name);
                builder.Append('=');
                builder.Append(Segments[i].ElapsedMilliseconds);
                builder.Append(" ms");
            }

            builder.Append(" | total ");
            builder.Append(TotalMilliseconds);
            builder.Append(" ms");
            return builder.ToString();
        }
    }
}
