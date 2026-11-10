#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>Segment primitives used to describe the prototype handling course.</summary>
    public enum HandlingCourseSegmentType
    {
        Straight,
        Arc
    }

    /// <summary>
    /// Immutable centerline segment. Arc length is precomputed from the radius and angle.
    /// </summary>
    public readonly struct HandlingCourseSegment
    {
        private HandlingCourseSegment(
            HandlingCourseSegmentType type,
            float length,
            float radius,
            float angleDegrees,
            int turnDirection)
        {
            Type = type;
            Length = length;
            Radius = radius;
            AngleDegrees = angleDegrees;
            TurnDirection = turnDirection;
        }

        /// <summary>Straight or arc.</summary>
        public HandlingCourseSegmentType Type { get; }

        /// <summary>Path length in meters (arc length for arcs).</summary>
        public float Length { get; }

        /// <summary>Arc radius in meters; zero for straights.</summary>
        public float Radius { get; }

        /// <summary>Arc sweep in degrees; zero for straights.</summary>
        public float AngleDegrees { get; }

        /// <summary>-1 for left turns, +1 for right turns, 0 for straights.</summary>
        public int TurnDirection { get; }

        /// <summary>Creates a straight segment.</summary>
        public static HandlingCourseSegment Straight(float length)
        {
            return new HandlingCourseSegment(HandlingCourseSegmentType.Straight, Mathf.Max(0f, length), 0f, 0f, 0);
        }

        /// <summary>Creates an arc segment. <paramref name="turnDirection"/> must be -1 or +1.</summary>
        public static HandlingCourseSegment Arc(float angleDegrees, float radius, int turnDirection)
        {
            var safeRadius = Mathf.Max(0.01f, radius);
            var safeAngle = Mathf.Max(0f, angleDegrees);
            var direction = turnDirection < 0 ? -1 : 1;
            var length = safeAngle * Mathf.Deg2Rad * safeRadius;
            return new HandlingCourseSegment(
                HandlingCourseSegmentType.Arc,
                length,
                safeRadius,
                safeAngle,
                direction);
        }
    }

    /// <summary>
    /// Deterministic handling-course centerline and elevation profile (Phase 6, development
    /// prototype). Editor-only so the WebGL player never pays for its static data.
    /// </summary>
    /// <remarks>
    /// Layout is a proposal for owner playtesting, not final track content: 100 m start straight,
    /// T1 tight left, short link, T2 medium right sweeper with a 2.5 m rise, crest bump, T3 tight
    /// left, and a 90 m return straight. No values here are approved vehicle tuning.
    /// </remarks>
    public static class HandlingCourseLayout
    {
        /// <summary>Drivable width in meters (matches the Phase 5 straight track).</summary>
        public const float RoadWidth = 10f;

        /// <summary>Barrier height in meters.</summary>
        public const float BarrierHeight = 0.8f;

        /// <summary>Barrier thickness in meters.</summary>
        public const float BarrierThickness = 0.3f;

        /// <summary>Default centerline sample spacing in meters.</summary>
        public const float DefaultSampleSpacing = 0.5f;

        /// <summary>Number of ordered checkpoint gates in the prototype run.</summary>
        public const int CheckpointCount = 3;

        /// <summary>Checkpoint positions as fractions of the total path length.</summary>
        public static readonly float[] CheckpointFractions = { 0.35f, 0.65f, 0.88f };

        private static readonly HandlingCourseSegment[] SegmentData =
        {
            HandlingCourseSegment.Straight(100f),
            HandlingCourseSegment.Arc(90f, 10f, -1),
            HandlingCourseSegment.Straight(25f),
            HandlingCourseSegment.Arc(120f, 30f, 1),
            HandlingCourseSegment.Straight(40f),
            HandlingCourseSegment.Arc(70f, 11f, -1),
            HandlingCourseSegment.Straight(90f)
        };

        /// <summary>Total centerline length in meters.</summary>
        public static float TotalLength { get; } = ComputeTotalLength();

        /// <summary>The immutable segment list (safe to read; do not mutate).</summary>
        public static HandlingCourseSegment[] Segments => SegmentData;

        private static readonly float SweeperStart = 100f + ArcLength(90f, 10f) + 25f;
        private static readonly float SweeperEnd = SweeperStart + ArcLength(120f, 30f);
        private static readonly float CrestStart = SweeperEnd + 10f;
        private static readonly float CrestEnd = CrestStart + 8f;
        private static readonly float DescentEnd = CrestEnd + 10f;

        /// <summary>
        /// Samples the centerline at <paramref name="spacing"/> meters. The returned array uses
        /// world X/Z with the elevation profile applied to Y.
        /// </summary>
        public static Vector3[] BuildCenterline(float spacing = DefaultSampleSpacing)
        {
            var safeSpacing = Mathf.Max(0.05f, spacing);
            var points = new List<Vector3>(2048) { Vector3.zero };
            var position = Vector3.zero;
            var yaw = 0f;

            for (var segmentIndex = 0; segmentIndex < SegmentData.Length; segmentIndex++)
            {
                var segment = SegmentData[segmentIndex];
                if (segment.Length <= 0.0001f)
                {
                    continue;
                }

                if (segment.Type == HandlingCourseSegmentType.Straight)
                {
                    var steps = Mathf.Max(1, Mathf.CeilToInt(segment.Length / safeSpacing));
                    var step = segment.Length / steps;
                    for (var i = 0; i < steps; i++)
                    {
                        position += Forward(yaw) * step;
                        points.Add(position);
                    }
                }
                else
                {
                    var steps = Mathf.Max(2, Mathf.CeilToInt(segment.Length / safeSpacing));
                    var step = segment.Length / steps;
                    var deltaYaw = segment.TurnDirection * segment.AngleDegrees * Mathf.Deg2Rad / steps;
                    for (var i = 0; i < steps; i++)
                    {
                        yaw += deltaYaw;
                        position += Forward(yaw) * step;
                        points.Add(position);
                    }
                }
            }

            var result = points.ToArray();
            var distance = 0f;
            for (var i = 0; i < result.Length; i++)
            {
                if (i > 0)
                {
                    distance += FlatDistance(result[i - 1], result[i]);
                }

                result[i].y = ElevationAt(distance);
            }

            return result;
        }

        /// <summary>Height profile in meters at a path distance.</summary>
        public static float ElevationAt(float distance)
        {
            if (distance <= SweeperStart)
            {
                return 0f;
            }

            if (distance <= SweeperEnd)
            {
                return 2.5f * SmoothStep01((distance - SweeperStart) / ArcLength(120f, 30f));
            }

            if (distance <= CrestStart)
            {
                return 2.5f;
            }

            if (distance <= CrestEnd)
            {
                var t = (distance - CrestStart) / (CrestEnd - CrestStart);
                var bump = Mathf.Sin(Mathf.PI * t);
                return 2.5f + (0.22f * bump * bump);
            }

            if (distance <= DescentEnd)
            {
                var t = (distance - CrestEnd) / (DescentEnd - CrestEnd);
                return 2.5f * (1f - SmoothStep01(t));
            }

            return 0f;
        }

        /// <summary>Distance to a checkpoint gate by ordered index.</summary>
        public static float GetCheckpointDistance(int index)
        {
            if (index < 0 || index >= CheckpointFractions.Length)
            {
                return 0f;
            }

            return TotalLength * CheckpointFractions[index];
        }

        /// <summary>Distance to the finish gate (just inside the final sample).</summary>
        public static float GetFinishDistance()
        {
            return Mathf.Max(0f, TotalLength - 1f);
        }

        /// <summary>
        /// Interpolates a centerline position and forward tangent at a path distance.
        /// </summary>
        public static Vector3 SampleAtDistance(Vector3[] centerline, float distance, out Vector3 tangent)
        {
            tangent = Vector3.forward;
            if (centerline == null || centerline.Length == 0)
            {
                return Vector3.zero;
            }

            if (centerline.Length == 1)
            {
                return centerline[0];
            }

            var clamped = Mathf.Clamp(distance, 0f, TotalLength);
            var travelled = 0f;

            for (var i = 1; i < centerline.Length; i++)
            {
                var a = centerline[i - 1];
                var b = centerline[i];
                var segmentLength = FlatDistance(a, b);
                if (segmentLength <= 0.00001f)
                {
                    continue;
                }

                if (travelled + segmentLength >= clamped)
                {
                    var t = (clamped - travelled) / segmentLength;
                    tangent = (b - a).normalized;
                    return Vector3.LerpUnclamped(a, b, t);
                }

                travelled += segmentLength;
            }

            var last = centerline[centerline.Length - 1];
            tangent = (last - centerline[centerline.Length - 2]).normalized;
            return last;
        }

        private static float ComputeTotalLength()
        {
            var total = 0f;
            for (var i = 0; i < SegmentData.Length; i++)
            {
                total += SegmentData[i].Length;
            }

            return total;
        }

        private static float ArcLength(float angleDegrees, float radius)
        {
            return angleDegrees * Mathf.Deg2Rad * radius;
        }

        private static Vector3 Forward(float yaw)
        {
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            return Mathf.Sqrt(
                ((a.x - b.x) * (a.x - b.x)) +
                ((a.z - b.z) * (a.z - b.z)));
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }
    }
}
#endif
