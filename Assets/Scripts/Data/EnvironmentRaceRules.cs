using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Race-gate geometry and validation rules shared by the environment scene builder and tests
    /// (T-9.6–T-9.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every generated environment runs the same approved one-way format as the Phase-7 slice:
    /// three ordered checkpoints followed by a finish gate. Distances are derived from the
    /// authoritative <see cref="TrackLayout"/> length, so content cannot author a gate outside the
    /// drivable centerline.
    /// </para>
    /// <para>
    /// The rules contain geometry only. They carry no grip, hazard, damage, or vehicle-physics
    /// values; D-057 keeps those presentation-only.
    /// </para>
    /// </remarks>
    public static class EnvironmentRaceRules
    {
        /// <summary>Ordered checkpoints required before the finish is accepted.</summary>
        public const int CheckpointCount = 3;

        /// <summary>Distance from the layout end to the finish gate center, in meters.</summary>
        public const float FinishInsetMeters = 1f;

        /// <summary>Minimum spacing between consecutive gates, in meters.</summary>
        public const float MinimumGateSpacingMeters = 20f;

        /// <summary>Extra gate width beyond the road width, in meters.</summary>
        public const float GateWidthPadding = 2f;

        /// <summary>Gate trigger height, in meters.</summary>
        public const float GateHeight = 4f;

        /// <summary>Gate trigger depth, in meters.</summary>
        public const float GateDepth = 2f;

        /// <summary>1-based fraction of the layout used for checkpoint <paramref name="index"/>.</summary>
        public static float GetCheckpointFraction(int index)
        {
            if (index < 0 || index >= CheckpointCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (index + 1f) / (CheckpointCount + 1f);
        }

        /// <summary>Path distance of checkpoint <paramref name="index"/>.</summary>
        public static float GetCheckpointDistance(TrackLayout layout, int index)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            return layout.TotalLength * GetCheckpointFraction(index);
        }

        /// <summary>Path distance of the finish gate.</summary>
        public static float GetFinishDistance(TrackLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            return Mathf.Max(0f, layout.TotalLength - FinishInsetMeters);
        }

        /// <summary>Drivable gate width for a layout (road width plus padding).</summary>
        public static float GetGateWidth(TrackLayout layout)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            return layout.RoadWidth + GateWidthPadding;
        }

        /// <summary>
        /// Validates that every gate lies on the layout, is ordered, and keeps the minimum spacing.
        /// Returns every problem at once.
        /// </summary>
        public static IReadOnlyList<string> Validate(TrackLayout layout)
        {
            var issues = new List<string>();
            if (layout == null)
            {
                issues.Add("EnvironmentRaceRules: layout is null.");
                return issues;
            }

            var total = layout.TotalLength;
            if (!TrackSegment.IsFinite(total) || total <= 0f)
            {
                issues.Add("EnvironmentRaceRules: layout length must be finite and positive.");
                return issues;
            }

            var previous = 0f;
            for (var index = 0; index < CheckpointCount; index++)
            {
                var distance = GetCheckpointDistance(layout, index);
                if (distance <= previous + MinimumGateSpacingMeters)
                {
                    issues.Add("EnvironmentRaceRules: checkpoint " + index +
                               " must be at least " + MinimumGateSpacingMeters +
                               " m after the previous gate.");
                }

                if (distance >= total)
                {
                    issues.Add("EnvironmentRaceRules: checkpoint " + index +
                               " lies outside the layout (" + distance.ToString("0.0") +
                               " of " + total.ToString("0.0") + " m).");
                }

                previous = distance;
            }

            var finish = GetFinishDistance(layout);
            if (finish <= previous + MinimumGateSpacingMeters)
            {
                issues.Add("EnvironmentRaceRules: finish must be at least " +
                           MinimumGateSpacingMeters + " m after the final checkpoint.");
            }

            return issues;
        }

        /// <summary>Validates the race-gate layout and returns the first issue.</summary>
        public static bool TryValidate(TrackLayout layout, out string error)
        {
            var issues = Validate(layout);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }
    }
}
