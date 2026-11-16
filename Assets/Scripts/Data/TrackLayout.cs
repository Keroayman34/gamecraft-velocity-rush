using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>Planar centerline primitive used by a data-driven <see cref="TrackLayout"/>.</summary>
    public enum TrackSegmentType
    {
        Straight = 0,
        Arc = 1
    }

    /// <summary>
    /// Signature or reverse configuration of a theme layout (OQ-21 approved 2026-11-16).
    /// </summary>
    public enum EnvironmentLayoutVariant
    {
        Signature = 0,
        Reverse = 1
    }

    /// <summary>
    /// Immutable, serializable centerline segment. Arc length is derived from radius and sweep,
    /// so authoring cannot silently store a contradictory length.
    /// </summary>
    [Serializable]
    public struct TrackSegment
    {
        [SerializeField] private TrackSegmentType type;
        [SerializeField] private float length;
        [SerializeField] private float radius;
        [SerializeField] private float angleDegrees;
        [SerializeField] private int turnDirection;

        /// <summary>Straight or arc.</summary>
        public TrackSegmentType Type => type;

        /// <summary>Path length in meters (derived for arcs).</summary>
        public float Length => type == TrackSegmentType.Arc
            ? Mathf.Max(0f, angleDegrees) * Mathf.Deg2Rad * Mathf.Max(0.01f, radius)
            : Mathf.Max(0f, length);

        /// <summary>Arc radius in meters; zero for straights.</summary>
        public float Radius => radius;

        /// <summary>Arc sweep in degrees; zero for straights.</summary>
        public float AngleDegrees => angleDegrees;

        /// <summary>-1 for left turns, +1 for right turns, 0 for straights.</summary>
        public int TurnDirection => turnDirection;

        /// <summary>Creates a straight segment.</summary>
        public static TrackSegment Straight(float lengthMeters)
        {
            return new TrackSegment
            {
                type = TrackSegmentType.Straight,
                length = Mathf.Max(0f, lengthMeters),
                radius = 0f,
                angleDegrees = 0f,
                turnDirection = 0
            };
        }

        /// <summary>Creates an arc segment. <paramref name="turnDirection"/> must be -1 or +1.</summary>
        public static TrackSegment Arc(float sweepDegrees, float radiusMeters, int turnDirection)
        {
            var safeRadius = Mathf.Max(0.01f, radiusMeters);
            var safeAngle = Mathf.Max(0f, sweepDegrees);
            var direction = turnDirection < 0 ? -1 : 1;
            return new TrackSegment
            {
                type = TrackSegmentType.Arc,
                length = safeAngle * Mathf.Deg2Rad * safeRadius,
                radius = safeRadius,
                angleDegrees = safeAngle,
                turnDirection = direction
            };
        }

        /// <summary>Validates this segment with field-specific diagnostics.</summary>
        public bool TryValidate(out string error)
        {
            if (type != TrackSegmentType.Straight && type != TrackSegmentType.Arc)
            {
                error = "TrackSegment: undefined Type value " + (int)type + ".";
                return false;
            }

            if (!IsFinite(length) || !IsFinite(radius) || !IsFinite(angleDegrees))
            {
                error = "TrackSegment: Length, Radius, and AngleDegrees must be finite.";
                return false;
            }

            if (type == TrackSegmentType.Straight)
            {
                if (length <= 0.01f)
                {
                    error = "TrackSegment: Straight Length must be greater than 0.01 m.";
                    return false;
                }

                if (turnDirection != 0)
                {
                    error = "TrackSegment: Straight TurnDirection must be 0.";
                    return false;
                }

                if (radius != 0f || angleDegrees != 0f)
                {
                    error = "TrackSegment: Straight must not define Radius or AngleDegrees.";
                    return false;
                }
            }
            else
            {
                if (radius < 2f)
                {
                    error = "TrackSegment: Arc Radius must be at least 2 m.";
                    return false;
                }

                if (angleDegrees <= 0f || angleDegrees > 360f)
                {
                    error = "TrackSegment: Arc AngleDegrees must be within (0, 360].";
                    return false;
                }

                if (turnDirection != -1 && turnDirection != 1)
                {
                    error = "TrackSegment: Arc TurnDirection must be -1 or +1.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>A height key on the path-distance axis.</summary>
    [Serializable]
    public struct TrackElevationKey
    {
        [SerializeField] private float distance;
        [SerializeField] private float height;

        /// <summary>Path distance in meters from the start of the signature layout.</summary>
        public float Distance => distance;

        /// <summary>Height in meters above the layout origin.</summary>
        public float Height => height;

        /// <summary>Creates an elevation key.</summary>
        public static TrackElevationKey Create(float distanceMeters, float heightMeters)
        {
            return new TrackElevationKey
            {
                distance = distanceMeters,
                height = heightMeters
            };
        }
    }

    /// <summary>
    /// Data-driven track centerline asset (T-9.2). Generalizes the prototype
    /// <c>HandlingCourseLayout</c> without changing it: straight/arc segments plus an optional
    /// smooth elevation profile are sampled into a runtime-safe centerline.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Layouts are presentation/geometry data only. They carry no grip, damage, hazard, or
    /// vehicle-physics values; Phase 8 vehicles remain exactly as approved in D-052 (D-057).
    /// </para>
    /// <para>
    /// The signature layout is authored once per theme. A reverse configuration is the same
    /// asset sampled with <c>reverse: true</c>, which reverses point order (and therefore the
    /// elevation profile) without duplicating content.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Track Layout", fileName = "TrackLayout")]
    public sealed class TrackLayout : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Minimum accepted road width in meters.</summary>
        public const float MinRoadWidth = 6f;

        /// <summary>Maximum accepted road width in meters.</summary>
        public const float MaxRoadWidth = 30f;

        /// <summary>Minimum accepted total centerline length in meters.</summary>
        public const float MinTotalLength = 50f;

        /// <summary>Maximum accepted total centerline length in meters.</summary>
        public const float MaxTotalLength = 5000f;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Tooltip("Drivable road width in meters.")]
        [SerializeField] private float roadWidth = 10f;

        [Tooltip("Barrier height in meters.")]
        [SerializeField] private float barrierHeight = 0.8f;

        [Tooltip("Barrier thickness in meters.")]
        [SerializeField] private float barrierThickness = 0.3f;

        [Tooltip("Default centerline sample spacing in meters.")]
        [SerializeField] private float sampleSpacing = 0.5f;

        [Tooltip("True only when the authored layout closes on itself.")]
        [SerializeField] private bool closedLoop;

        [Tooltip("Planar centerline segments in driving order.")]
        [SerializeField] private TrackSegment[] segments = Array.Empty<TrackSegment>();

        [Tooltip("Optional elevation keys on the signature-path distance axis; leave empty for flat.")]
        [SerializeField] private TrackElevationKey[] elevationKeys = Array.Empty<TrackElevationKey>();

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Drivable road width in meters.</summary>
        public float RoadWidth => roadWidth;

        /// <summary>Barrier height in meters.</summary>
        public float BarrierHeight => barrierHeight;

        /// <summary>Barrier thickness in meters.</summary>
        public float BarrierThickness => barrierThickness;

        /// <summary>Default centerline sample spacing in meters.</summary>
        public float SampleSpacing => sampleSpacing;

        /// <summary>Whether the authored layout closes on itself.</summary>
        public bool ClosedLoop => closedLoop;

        /// <summary>Centerline segments in driving order; never null.</summary>
        public TrackSegment[] Segments => segments ?? Array.Empty<TrackSegment>();

        /// <summary>Elevation keys in ascending distance order; never null.</summary>
        public TrackElevationKey[] ElevationKeys => elevationKeys ?? Array.Empty<TrackElevationKey>();

        /// <summary>Total centerline length in meters.</summary>
        public float TotalLength
        {
            get
            {
                var data = Segments;
                var total = 0f;
                for (var i = 0; i < data.Length; i++)
                {
                    total += data[i].Length;
                }

                return total;
            }
        }

        /// <summary>
        /// Samples the centerline with the authored spacing. The returned points already include
        /// the elevation profile and are suitable for road/barrier mesh generation.
        /// </summary>
        public Vector3[] BuildCenterline()
        {
            return BuildCenterline(sampleSpacing, false);
        }

        /// <summary>Samples the signature (forward) centerline at an explicit spacing.</summary>
        public Vector3[] BuildCenterline(float spacing)
        {
            return BuildCenterline(spacing, false);
        }

        /// <summary>
        /// Samples the centerline for the approved signature or reverse configuration of this
        /// theme. Both configurations share the same asset and scene.
        /// </summary>
        public Vector3[] BuildCenterline(float spacing, EnvironmentLayoutVariant variant)
        {
            return BuildCenterline(spacing, variant == EnvironmentLayoutVariant.Reverse);
        }

        /// <summary>
        /// Samples the centerline. <paramref name="reverse"/> reverses point order for the approved
        /// reverse configuration; elevation stays attached to the same physical location.
        /// </summary>
        public Vector3[] BuildCenterline(float spacing, bool reverse)
        {
            var data = Segments;
            var safeSpacing = Mathf.Max(0.05f, spacing);
            var points = new List<Vector3>(2048) { Vector3.zero };
            var position = Vector3.zero;
            var yaw = 0f;

            for (var segmentIndex = 0; segmentIndex < data.Length; segmentIndex++)
            {
                var segment = data[segmentIndex];
                if (segment.Length <= 0.0001f)
                {
                    continue;
                }

                if (segment.Type == TrackSegmentType.Straight)
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

            if (reverse)
            {
                Array.Reverse(result);
            }

            return result;
        }

        /// <summary>
        /// Height in meters at a signature-path distance. Uses smooth-step interpolation between
        /// keys; before the first key and after the last key the endpoint height is held.
        /// </summary>
        public float ElevationAt(float distance)
        {
            var keys = ElevationKeys;
            if (keys.Length == 0)
            {
                return 0f;
            }

            if (keys.Length == 1)
            {
                return keys[0].Height;
            }

            if (distance <= keys[0].Distance)
            {
                return keys[0].Height;
            }

            for (var i = 1; i < keys.Length; i++)
            {
                var previous = keys[i - 1];
                var current = keys[i];
                if (distance > current.Distance)
                {
                    continue;
                }

                var span = current.Distance - previous.Distance;
                if (span <= 0.0001f)
                {
                    return current.Height;
                }

                var t = Mathf.Clamp01((distance - previous.Distance) / span);
                return Mathf.LerpUnclamped(previous.Height, current.Height, SmoothStep01(t));
            }

            return keys[keys.Length - 1].Height;
        }

        /// <summary>
        /// Interpolates a centerline position and forward tangent at a path distance. This mirrors
        /// the legacy prototype sampler but works for any authored layout.
        /// </summary>
        public Vector3 SampleAtDistance(Vector3[] centerline, float distance, out Vector3 tangent)
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
            tangent = centerline.Length > 1
                ? (last - centerline[centerline.Length - 2]).normalized
                : Vector3.forward;
            return last;
        }

        /// <summary>Validates the complete layout and returns the first issue.</summary>
        public bool TryValidate(out string error)
        {
            var issues = Validate();
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

        /// <summary>Returns every layout problem at once (cold authoring/validation path).</summary>
        public IReadOnlyList<string> Validate()
        {
            var issues = new List<string>();
            if (schemaVersion != CurrentSchemaVersion)
            {
                issues.Add("TrackLayout: unsupported SchemaVersion " + schemaVersion +
                           "; expected " + CurrentSchemaVersion + ".");
            }

            if (!TrackSegment.IsFinite(roadWidth))
            {
                issues.Add("TrackLayout: RoadWidth must be finite.");
            }
            else if (roadWidth < MinRoadWidth || roadWidth > MaxRoadWidth)
            {
                issues.Add("TrackLayout: RoadWidth must be within " + MinRoadWidth + ".." +
                           MaxRoadWidth + " m.");
            }

            if (!TrackSegment.IsFinite(barrierHeight) || barrierHeight < 0.3f || barrierHeight > 2f)
            {
                issues.Add("TrackLayout: BarrierHeight must be within 0.3..2 m.");
            }

            if (!TrackSegment.IsFinite(barrierThickness) ||
                barrierThickness < 0.1f || barrierThickness > 1f)
            {
                issues.Add("TrackLayout: BarrierThickness must be within 0.1..1 m.");
            }

            if (!TrackSegment.IsFinite(sampleSpacing) || sampleSpacing < 0.25f || sampleSpacing > 5f)
            {
                issues.Add("TrackLayout: SampleSpacing must be within 0.25..5 m.");
            }

            var data = Segments;
            if (data.Length < 2)
            {
                issues.Add("TrackLayout: at least two segments are required (found " + data.Length + ").");
            }

            for (var i = 0; i < data.Length; i++)
            {
                if (!data[i].TryValidate(out var segmentError))
                {
                    issues.Add("TrackLayout segment " + i + ": " + segmentError);
                }
            }

            var total = TotalLength;
            if (!TrackSegment.IsFinite(total))
            {
                issues.Add("TrackLayout: TotalLength must be finite.");
            }
            else if (total < MinTotalLength || total > MaxTotalLength)
            {
                issues.Add("TrackLayout: TotalLength must be within " + MinTotalLength + ".." +
                           MaxTotalLength + " m (found " + total.ToString("0.0") + ").");
            }

            ValidateElevation(issues, total);

            if (closedLoop && data.Length >= 2 && total > 0f)
            {
                var sampled = BuildCenterline(Mathf.Max(1f, sampleSpacing));
                if (sampled.Length > 1)
                {
                    var closure = FlatDistance(sampled[0], sampled[sampled.Length - 1]);
                    if (closure > 2f)
                    {
                        issues.Add("TrackLayout: ClosedLoop is true but the sampled centerline " +
                                   "does not close (gap " + closure.ToString("0.00") + " m).");
                    }
                }
            }

            return issues;
        }

        private void ValidateElevation(List<string> issues, float totalLength)
        {
            var keys = ElevationKeys;
            if (keys.Length == 0)
            {
                return;
            }

            if (Mathf.Abs(keys[0].Distance) > 0.001f)
            {
                issues.Add("TrackLayout: the first ElevationKey must be at distance 0.");
            }

            var previousDistance = float.NegativeInfinity;
            for (var i = 0; i < keys.Length; i++)
            {
                var key = keys[i];
                if (!TrackSegment.IsFinite(key.Distance) || !TrackSegment.IsFinite(key.Height))
                {
                    issues.Add("TrackLayout elevation key " + i + " must have finite values.");
                    continue;
                }

                if (key.Distance < 0f)
                {
                    issues.Add("TrackLayout elevation key " + i + " has a negative distance.");
                }

                if (key.Distance <= previousDistance)
                {
                    issues.Add("TrackLayout elevation key " + i +
                               " must be strictly ascending by distance.");
                }

                if (key.Height < -100f || key.Height > 300f)
                {
                    issues.Add("TrackLayout elevation key " + i + " height must be within -100..300 m.");
                }

                if (key.Distance > totalLength + 0.5f)
                {
                    issues.Add("TrackLayout elevation key " + i +
                               " lies beyond the layout length (" + totalLength.ToString("0.0") + " m).");
                }

                previousDistance = key.Distance;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by deterministic builders and tests. Runtime code must
        /// treat the layout as read-only.
        /// </summary>
        public void ConfigureForAuthoring(
            float width,
            float height,
            float thickness,
            float spacing,
            bool closesLoop,
            TrackSegment[] segmentData,
            TrackElevationKey[] elevationData)
        {
            schemaVersion = CurrentSchemaVersion;
            roadWidth = width;
            barrierHeight = height;
            barrierThickness = thickness;
            sampleSpacing = spacing;
            closedLoop = closesLoop;
            segments = segmentData ?? Array.Empty<TrackSegment>();
            elevationKeys = elevationData ?? Array.Empty<TrackElevationKey>();
        }
#endif

        private static Vector3 Forward(float yaw)
        {
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var x = a.x - b.x;
            var z = a.z - b.z;
            return Mathf.Sqrt((x * x) + (z * z));
        }

        private static float SmoothStep01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - (2f * t));
        }
    }
}
