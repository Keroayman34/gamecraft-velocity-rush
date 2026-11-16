using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// One precomputed waypoint on a <see cref="TrackPath"/> (T-10.1.1).
    /// </summary>
    /// <remarks>
    /// Waypoints are generated once at load from the authored <see cref="TrackLayout"/> and are
    /// immutable afterwards. <see cref="Center"/> is the road centerline point; the AI racing
    /// position is derived by applying <see cref="RacingOffset"/> (curvature-derived) plus the
    /// per-opponent lateral bias at sample time. Curvature is signed: positive means a right
    /// turn, matching <see cref="TrackSegment.TurnDirection"/> (+1 = right).
    /// </remarks>
    public readonly struct PathWaypoint
    {
        /// <summary>Road centerline position, including the authored elevation profile.</summary>
        public Vector3 Center { get; }

        /// <summary>Unit forward tangent in the driving direction.</summary>
        public Vector3 Tangent { get; }

        /// <summary>Unit lateral right vector (<c>Cross(up, Tangent)</c>).</summary>
        public Vector3 Right { get; }

        /// <summary>Path distance in meters from the start of this configuration.</summary>
        public float Distance { get; }

        /// <summary>Signed horizontal curvature in 1/m; positive is a right turn.</summary>
        public float Curvature { get; }

        /// <summary>Curvature-limited target speed in meters/second.</summary>
        public float TargetSpeed { get; }

        /// <summary>
        /// Curvature-derived racing-line offset in meters along <see cref="Right"/>; positive is
        /// toward the inside of a right turn. Per-opponent lane bias is added at sample time.
        /// </summary>
        public float RacingOffset { get; }

        /// <summary>Creates a waypoint.</summary>
        public PathWaypoint(
            Vector3 center,
            Vector3 tangent,
            Vector3 right,
            float distance,
            float curvature,
            float targetSpeed,
            float racingOffset)
        {
            Center = center;
            Tangent = tangent;
            Right = right;
            Distance = distance;
            Curvature = curvature;
            TargetSpeed = targetSpeed;
            RacingOffset = racingOffset;
        }
    }

    /// <summary>
    /// Allocation-free interpolated sample produced by <see cref="TrackPath"/> (T-10.1.5).
    /// </summary>
    public readonly struct PathSample
    {
        /// <summary>Interpolated road centerline position.</summary>
        public Vector3 Center { get; }

        /// <summary>Racing-line position after applying offsets and clamping to the road.</summary>
        public Vector3 Position { get; }

        /// <summary>Unit forward tangent in the driving direction.</summary>
        public Vector3 Tangent { get; }

        /// <summary>Unit lateral right vector.</summary>
        public Vector3 Right { get; }

        /// <summary>Path distance in meters from the start of this configuration.</summary>
        public float Distance { get; }

        /// <summary>Interpolated signed curvature in 1/m; positive is a right turn.</summary>
        public float Curvature { get; }

        /// <summary>Interpolated curvature-limited target speed in meters/second.</summary>
        public float TargetSpeed { get; }

        /// <summary>Interpolated curvature-derived racing-line offset in meters.</summary>
        public float RacingOffset { get; }

        /// <summary>Applied lateral offset in meters, clamped so the sample stays on the road.</summary>
        public float LateralOffset { get; }

        /// <summary>Index of the waypoint that starts the sampled segment.</summary>
        public int WaypointIndex { get; }

        /// <summary>Creates a sample.</summary>
        public PathSample(
            Vector3 center,
            Vector3 position,
            Vector3 tangent,
            Vector3 right,
            float distance,
            float curvature,
            float targetSpeed,
            float racingOffset,
            float lateralOffset,
            int waypointIndex)
        {
            Center = center;
            Position = position;
            Tangent = tangent;
            Right = right;
            Distance = distance;
            Curvature = curvature;
            TargetSpeed = targetSpeed;
            RacingOffset = racingOffset;
            LateralOffset = lateralOffset;
            WaypointIndex = waypointIndex;
        }
    }

    /// <summary>
    /// Engineering envelope used to generate a <see cref="TrackPath"/> (T-10.1.2).
    /// </summary>
    /// <remarks>
    /// These are provisional engineering defaults, not approved race balance. They bound the
    /// geometric speed profile so Stage 2 can drive the path without inventing corner speeds;
    /// per-event/per-difficulty tuning is owner-gated in later stages. Distances are planar
    /// (matching <see cref="TrackLayout.TotalLength"/> and the race-gate distance axis).
    /// </remarks>
    public readonly struct TrackPathBuildSettings
    {
        /// <summary>Waypoint spacing in meters. Two meters keeps steering stable without bloat.</summary>
        public float Spacing { get; }

        /// <summary>Maximum target speed in meters/second on a straight.</summary>
        public float MaxSpeed { get; }

        /// <summary>Minimum target speed floor in meters/second.</summary>
        public float MinSpeed { get; }

        /// <summary>Lateral acceleration budget used to derive corner speed in m/s².</summary>
        public float LateralAcceleration { get; }

        /// <summary>Forward acceleration budget used by the speed-profile pass in m/s².</summary>
        public float ForwardAcceleration { get; }

        /// <summary>Braking deceleration budget used by the speed-profile pass in m/s².</summary>
        public float BrakingDeceleration { get; }

        /// <summary>Fraction of the usable half-width used at full racing-line saturation.</summary>
        public float RacingLineStrength { get; }

        /// <summary>Corner radius in meters at which the racing-line offset saturates.</summary>
        public float RacingLineRadius { get; }

        /// <summary>Safety margin in meters kept between the racing line and the road edge.</summary>
        public float LateralMargin { get; }

        /// <summary>Creates a settings envelope.</summary>
        public TrackPathBuildSettings(
            float spacing,
            float maxSpeed,
            float minSpeed,
            float lateralAcceleration,
            float forwardAcceleration,
            float brakingDeceleration,
            float racingLineStrength,
            float racingLineRadius,
            float lateralMargin)
        {
            Spacing = spacing;
            MaxSpeed = maxSpeed;
            MinSpeed = minSpeed;
            LateralAcceleration = lateralAcceleration;
            ForwardAcceleration = forwardAcceleration;
            BrakingDeceleration = brakingDeceleration;
            RacingLineStrength = racingLineStrength;
            RacingLineRadius = racingLineRadius;
            LateralMargin = lateralMargin;
        }

        /// <summary>
        /// Approved Stage-1 envelope: 2.0 m spacing, 58 m/s ceiling, 14 m/s² lateral and
        /// 7/14 m/s² forward/braking budgets, half-width racing line saturated at 40 m radius,
        /// 1.2 m edge margin.
        /// </summary>
        public static TrackPathBuildSettings Default =>
            new TrackPathBuildSettings(2f, 58f, 5f, 14f, 7f, 14f, 0.5f, 40f, 1.2f);

        /// <summary>Validates the envelope and returns the first problem.</summary>
        public bool TryValidate(out string error)
        {
            if (!IsFinite(Spacing) || Spacing <= 0.01f)
            {
                error = "TrackPathBuildSettings: Spacing must be greater than 0.01 m.";
                return false;
            }

            if (!IsFinite(MaxSpeed) || MaxSpeed <= 0f)
            {
                error = "TrackPathBuildSettings: MaxSpeed must be positive.";
                return false;
            }

            if (!IsFinite(MinSpeed) || MinSpeed <= 0f || MinSpeed > MaxSpeed)
            {
                error = "TrackPathBuildSettings: MinSpeed must be positive and at most MaxSpeed.";
                return false;
            }

            if (!IsFinite(LateralAcceleration) || LateralAcceleration <= 0f ||
                !IsFinite(ForwardAcceleration) || ForwardAcceleration <= 0f ||
                !IsFinite(BrakingDeceleration) || BrakingDeceleration <= 0f)
            {
                error = "TrackPathBuildSettings: acceleration and braking budgets must be positive.";
                return false;
            }

            if (!IsFinite(RacingLineStrength) || RacingLineStrength < 0f || RacingLineStrength > 1f)
            {
                error = "TrackPathBuildSettings: RacingLineStrength must be within 0..1.";
                return false;
            }

            if (!IsFinite(RacingLineRadius) || RacingLineRadius <= 0f)
            {
                error = "TrackPathBuildSettings: RacingLineRadius must be positive.";
                return false;
            }

            if (!IsFinite(LateralMargin) || LateralMargin < 0f)
            {
                error = "TrackPathBuildSettings: LateralMargin must not be negative.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Precomputed AI racing path generated from a <see cref="TrackLayout"/> (T-10.1.1–T-10.1.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Generation is a cold, load-time operation: it samples the authoritative centerline, derives
    /// signed curvature, a curvature-limited speed profile, and a curvature-derived racing line.
    /// All runtime sampling is array/index based and provably free of managed allocation, which is
    /// the 0 B/frame contract for AI sensors and logic (D-060). No LINQ is used anywhere.
    /// </para>
    /// <para>
    /// Open layouts clamp at both ends; closed layouts wrap sampling, cursor advance, and the speed
    /// profile. Reverse configurations are generated from the same asset through
    /// <see cref="TrackLayout.BuildCenterline(float, EnvironmentLayoutVariant)"/>, so no second
    /// path asset is authored.
    /// </para>
    /// <para>
    /// The path carries geometry and an engineering speed envelope only. It is not a handling
    /// model, it does not touch <c>WheelCollider</c> state, and it does not change the approved
    /// Phase-8 player vehicle (D-052/D-059).
    /// </para>
    /// </remarks>
    public sealed class TrackPath
    {
        private const float Epsilon = 0.0001f;

        private readonly Vector3[] _points;
        private readonly Vector3[] _tangents;
        private readonly Vector3[] _rights;
        private readonly float[] _distances;
        private readonly float[] _segmentLengths;
        private readonly float[] _curvatures;
        private readonly float[] _targetSpeeds;
        private readonly float[] _racingOffsets;

        private TrackPath(
            Vector3[] points,
            Vector3[] tangents,
            Vector3[] rights,
            float[] distances,
            float[] segmentLengths,
            float[] curvatures,
            float[] targetSpeeds,
            float[] racingOffsets,
            float totalLength,
            TrackLayout layout,
            EnvironmentLayoutVariant variant,
            TrackPathBuildSettings settings)
        {
            _points = points;
            _tangents = tangents;
            _rights = rights;
            _distances = distances;
            _segmentLengths = segmentLengths;
            _curvatures = curvatures;
            _targetSpeeds = targetSpeeds;
            _racingOffsets = racingOffsets;
            TotalLength = totalLength;
            RoadWidth = layout.RoadWidth;
            HalfWidth = layout.RoadWidth * 0.5f;
            MaxLateralOffset = Mathf.Max(0f, HalfWidth - settings.LateralMargin);
            Spacing = settings.Spacing;
            Settings = settings;
            IsClosedLoop = layout.ClosedLoop;
            Variant = variant;
        }

        /// <summary>Number of generated waypoints (at least two).</summary>
        public int Count => _points.Length;

        /// <summary>Total path length in meters, including the closing segment of a loop.</summary>
        public float TotalLength { get; }

        /// <summary>Authored road width in meters.</summary>
        public float RoadWidth { get; }

        /// <summary>Half the authored road width in meters.</summary>
        public float HalfWidth { get; }

        /// <summary>Maximum lateral offset a sample may apply, keeping the car inside the road.</summary>
        public float MaxLateralOffset { get; }

        /// <summary>Requested waypoint spacing in meters.</summary>
        public float Spacing { get; }

        /// <summary>The engineering envelope used to build this path.</summary>
        public TrackPathBuildSettings Settings { get; }

        /// <summary>Whether the source layout closes on itself.</summary>
        public bool IsClosedLoop { get; }

        /// <summary>Signature or reverse configuration this path was generated from.</summary>
        public EnvironmentLayoutVariant Variant { get; }

        /// <summary>
        /// Generates a path with <see cref="TrackPathBuildSettings.Default"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="layout"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The layout fails <see cref="TrackLayout.TryValidate"/>.</exception>
        public static TrackPath Create(
            TrackLayout layout,
            EnvironmentLayoutVariant variant = EnvironmentLayoutVariant.Signature)
        {
            return Create(layout, variant, TrackPathBuildSettings.Default);
        }

        /// <summary>
        /// Generates a path from the authored layout. Allocates only during this call.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="layout"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="settings"/> is invalid.</exception>
        /// <exception cref="InvalidOperationException">The layout fails <see cref="TrackLayout.TryValidate"/>.</exception>
        public static TrackPath Create(
            TrackLayout layout,
            EnvironmentLayoutVariant variant,
            TrackPathBuildSettings settings)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (!settings.TryValidate(out var settingsError))
            {
                throw new ArgumentException(settingsError, nameof(settings));
            }

            if (!layout.TryValidate(out var layoutError))
            {
                throw new InvalidOperationException(layoutError);
            }

            var sampled = layout.BuildCenterline(settings.Spacing, variant);
            var closed = layout.ClosedLoop;
            var count = CollapseClosedDuplicate(sampled, closed, settings.Spacing);
            if (count < 2)
            {
                throw new InvalidOperationException(
                    "TrackPath: the layout produced fewer than two usable centerline points.");
            }

            var points = new Vector3[count];
            Array.Copy(sampled, points, count);

            var distances = BuildDistances(points);
            var segmentLengths = BuildSegmentLengths(points, distances, closed);
            var tangents = BuildTangents(points, distances, closed);
            var rights = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                rights[i] = BuildRight(tangents[i]);
            }

            var curvatures = BuildCurvatures(points, closed);
            var targetSpeeds = BuildSpeedProfile(
                curvatures,
                segmentLengths,
                closed,
                settings);
            var racingOffsets = BuildRacingOffsets(
                curvatures,
                settings,
                layout.RoadWidth * 0.5f);

            var totalLength = closed
                ? distances[count - 1] + segmentLengths[count - 1]
                : distances[count - 1];

            return new TrackPath(
                points,
                tangents,
                rights,
                distances,
                segmentLengths,
                curvatures,
                targetSpeeds,
                racingOffsets,
                totalLength,
                layout,
                variant,
                settings);
        }

        /// <summary>Returns the waypoint at <paramref name="index"/> (wrapped for loops).</summary>
        public PathWaypoint WaypointAt(int index)
        {
            var resolved = ResolveIndex(index);
            return new PathWaypoint(
                _points[resolved],
                _tangents[resolved],
                _rights[resolved],
                _distances[resolved],
                _curvatures[resolved],
                _targetSpeeds[resolved],
                _racingOffsets[resolved]);
        }

        /// <summary>
        /// Returns the waypoint index whose segment contains <paramref name="distance"/>.
        /// O(log n) binary search; never allocates.
        /// </summary>
        public int IndexForDistance(float distance)
        {
            var resolved = NormalizeDistance(distance);
            var last = _points.Length - 1;
            if (resolved <= 0f)
            {
                return 0;
            }

            if (resolved >= _distances[last])
            {
                return last;
            }

            var low = 0;
            var high = last;
            while (low + 1 < high)
            {
                var middle = (low + high) >> 1;
                if (_distances[middle] <= resolved)
                {
                    low = middle;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        /// <summary>
        /// Samples the path at a distance with an extra per-opponent lateral bias. Never allocates.
        /// </summary>
        public PathSample SampleAtDistance(float distance, float lateralBias = 0f)
        {
            var resolved = NormalizeDistance(distance);
            return SampleSegment(IndexForDistance(resolved), resolved, lateralBias);
        }

        /// <summary>
        /// Creates a reusable cursor. The cursor stores only a path reference, index, and distance,
        /// so it does not allocate after construction of the path.
        /// </summary>
        public TrackPathCursor CreateCursor(float startDistance = 0f)
        {
            return new TrackPathCursor(this, startDistance);
        }

        /// <summary>Normalizes an arbitrary distance into this path's valid range.</summary>
        public float NormalizeDistance(float distance)
        {
            if (float.IsNaN(distance))
            {
                return 0f;
            }

            if (IsClosedLoop && TotalLength > Epsilon)
            {
                var wrapped = distance % TotalLength;
                if (wrapped < 0f)
                {
                    wrapped += TotalLength;
                }

                return wrapped;
            }

            return Mathf.Clamp(distance, 0f, TotalLength);
        }

        /// <summary>Validates the generated arrays and returns the first problem.</summary>
        public bool TryValidate(out string error)
        {
            var count = _points.Length;
            if (count < 2)
            {
                error = "TrackPath: fewer than two waypoints.";
                return false;
            }

            if (!IsFinite(TotalLength) || TotalLength <= 0f)
            {
                error = "TrackPath: TotalLength must be finite and positive.";
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                if (!IsFinite(_distances[i]) || (i > 0 && _distances[i] <= _distances[i - 1]))
                {
                    error = "TrackPath: distances must be finite and strictly increasing at index " + i + ".";
                    return false;
                }

                if (!IsFinite(_curvatures[i]) || !IsFinite(_targetSpeeds[i]) ||
                    !IsFinite(_racingOffsets[i]))
                {
                    error = "TrackPath: non-finite derived value at index " + i + ".";
                    return false;
                }

                if (_targetSpeeds[i] < Settings.MinSpeed - 0.001f ||
                    _targetSpeeds[i] > Settings.MaxSpeed + 0.001f)
                {
                    error = "TrackPath: target speed out of envelope at index " + i + ".";
                    return false;
                }

                if (Mathf.Abs(_racingOffsets[i]) > MaxLateralOffset + 0.001f)
                {
                    error = "TrackPath: racing offset outside the road at index " + i + ".";
                    return false;
                }
            }

            error = null;
            return true;
        }

        internal PathSample SampleSegment(int segmentIndex, float distance, float lateralBias)
        {
            var count = _points.Length;
            var index = segmentIndex < 0 ? 0 : (segmentIndex >= count ? count - 1 : segmentIndex);
            var next = index + 1;
            float segmentLength;

            if (next < count)
            {
                segmentLength = _distances[next] - _distances[index];
            }
            else if (IsClosedLoop)
            {
                segmentLength = _segmentLengths[index];
                next = 0;
            }
            else
            {
                return BuildSample(
                    index,
                    index,
                    _points[index],
                    1f,
                    lateralBias,
                    distance);
            }

            var start = _distances[index];
            var t = segmentLength > Epsilon
                ? Mathf.Clamp01((distance - start) / segmentLength)
                : 0f;

            return BuildSample(index, next, _points[index], t, lateralBias, distance);
        }

        private PathSample BuildSample(
            int index,
            int next,
            Vector3 a,
            float t,
            float lateralBias,
            float distance)
        {
            var b = _points[next];
            var center = t <= 0f ? a : (t >= 1f ? b : Vector3.LerpUnclamped(a, b, t));

            var tangentA = _tangents[index];
            var tangentB = _tangents[next];
            var tangent = t <= 0f
                ? tangentA
                : (t >= 1f ? tangentB : Vector3.LerpUnclamped(tangentA, tangentB, t).normalized);
            if (tangent.sqrMagnitude < Epsilon)
            {
                tangent = tangentA;
            }

            var curvature = Lerp(_curvatures[index], _curvatures[next], t);
            var targetSpeed = Lerp(_targetSpeeds[index], _targetSpeeds[next], t);
            var racingOffset = Lerp(_racingOffsets[index], _racingOffsets[next], t);
            var totalOffset = Mathf.Clamp(racingOffset + lateralBias, -MaxLateralOffset, MaxLateralOffset);
            var right = BuildRight(tangent);
            var position = center + right * totalOffset;

            return new PathSample(
                center,
                position,
                tangent,
                right,
                distance,
                curvature,
                targetSpeed,
                racingOffset,
                totalOffset,
                index);
        }

        private int ResolveIndex(int index)
        {
            var count = _points.Length;
            if (IsClosedLoop)
            {
                var wrapped = index % count;
                return wrapped < 0 ? wrapped + count : wrapped;
            }

            return index < 0 ? 0 : (index >= count ? count - 1 : index);
        }

        private static int CollapseClosedDuplicate(Vector3[] sampled, bool closed, float spacing)
        {
            if (!closed || sampled.Length < 3)
            {
                return sampled.Length;
            }

            // A closed layout is sampled back to (near) its origin. Drop the duplicate so the
            // closing segment is represented once by the wrap segment instead of a zero-length
            // pair that would distort curvature at the seam.
            var gap = FlatDistance(sampled[0], sampled[sampled.Length - 1]);
            return gap < spacing * 0.5f ? sampled.Length - 1 : sampled.Length;
        }

        private static float[] BuildDistances(Vector3[] points)
        {
            var distances = new float[points.Length];
            for (var i = 1; i < points.Length; i++)
            {
                distances[i] = distances[i - 1] + FlatDistance(points[i - 1], points[i]);
            }

            return distances;
        }

        private static float[] BuildSegmentLengths(
            Vector3[] points,
            float[] distances,
            bool closed)
        {
            var count = points.Length;
            var lengths = new float[closed ? count : count - 1];
            for (var i = 0; i < count - 1; i++)
            {
                lengths[i] = Mathf.Max(0f, distances[i + 1] - distances[i]);
            }

            if (closed)
            {
                lengths[count - 1] = FlatDistance(points[count - 1], points[0]);
            }

            return lengths;
        }

        private static Vector3[] BuildTangents(Vector3[] points, float[] distances, bool closed)
        {
            var count = points.Length;
            var tangents = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var previous = i > 0 ? i - 1 : (closed ? count - 1 : i);
                var next = i < count - 1 ? i + 1 : (closed ? 0 : i);
                var direction = points[next] - points[previous];
                tangents[i] = direction.sqrMagnitude > Epsilon
                    ? direction.normalized
                    : Vector3.forward;
            }

            return tangents;
        }

        private static float[] BuildCurvatures(Vector3[] points, bool closed)
        {
            var count = points.Length;
            var curvatures = new float[count];
            for (var i = 0; i < count; i++)
            {
                var previous = i > 0 ? i - 1 : (closed ? count - 1 : 0);
                var next = i < count - 1 ? i + 1 : (closed ? 0 : count - 1);
                if (previous == i || next == i)
                {
                    curvatures[i] = 0f;
                    continue;
                }

                var a = points[previous];
                var b = points[i];
                var c = points[next];
                var ab = b - a;
                var bc = c - b;
                var ca = a - c;
                var abLength = FlatLength(ab);
                var bcLength = FlatLength(bc);
                var caLength = FlatLength(ca);
                var denominator = abLength * bcLength * caLength;
                if (denominator <= Epsilon)
                {
                    curvatures[i] = 0f;
                    continue;
                }

                // 3D cross-product Y component in the XZ plane; positive is a right turn.
                // Menger curvature is 2·area / (|ab|·|bc|·|ca|), and 2·area = |cross|.
                var cross = ab.z * bc.x - ab.x * bc.z;
                curvatures[i] = 2f * cross / denominator;
            }

            return curvatures;
        }

        private static float[] BuildSpeedProfile(
            float[] curvatures,
            float[] segmentLengths,
            bool closed,
            TrackPathBuildSettings settings)
        {
            var count = curvatures.Length;
            var speeds = new float[count];
            for (var i = 0; i < count; i++)
            {
                var curvature = Mathf.Abs(curvatures[i]);
                var limit = curvature > Epsilon
                    ? Mathf.Sqrt(settings.LateralAcceleration / curvature)
                    : settings.MaxSpeed;
                speeds[i] = Mathf.Clamp(limit, settings.MinSpeed, settings.MaxSpeed);
            }

            var segmentCount = closed ? count : count - 1;
            var passes = closed ? 2 : 1;

            // Forward: a car cannot accelerate beyond the forward budget between waypoints.
            for (var pass = 0; pass < passes; pass++)
            {
                for (var segment = 0; segment < segmentCount; segment++)
                {
                    var a = segment;
                    var b = (segment + 1) % count;
                    var reachable = Mathf.Sqrt(
                        speeds[a] * speeds[a] + 2f * settings.ForwardAcceleration * segmentLengths[segment]);
                    if (speeds[b] > reachable)
                    {
                        speeds[b] = reachable;
                    }
                }
            }

            // Backward: a car must be able to brake into the next corner.
            for (var pass = 0; pass < passes; pass++)
            {
                for (var segment = segmentCount - 1; segment >= 0; segment--)
                {
                    var a = segment;
                    var b = (segment + 1) % count;
                    var reachable = Mathf.Sqrt(
                        speeds[b] * speeds[b] + 2f * settings.BrakingDeceleration * segmentLengths[segment]);
                    if (speeds[a] > reachable)
                    {
                        speeds[a] = reachable;
                    }
                }
            }

            for (var i = 0; i < count; i++)
            {
                speeds[i] = Mathf.Clamp(speeds[i], settings.MinSpeed, settings.MaxSpeed);
            }

            return speeds;
        }

        private static float[] BuildRacingOffsets(
            float[] curvatures,
            TrackPathBuildSettings settings,
            float halfWidth)
        {
            var count = curvatures.Length;
            var offsets = new float[count];
            var maxOffset = Mathf.Max(0f, halfWidth - settings.LateralMargin);
            var amplitude = Mathf.Min(maxOffset, halfWidth * settings.RacingLineStrength);
            for (var i = 0; i < count; i++)
            {
                var curvature = curvatures[i];
                if (Mathf.Abs(curvature) <= Epsilon)
                {
                    offsets[i] = 0f;
                    continue;
                }

                // Turn toward the inside: positive curvature (right) offsets along +Right.
                var saturation = Mathf.Clamp01(Mathf.Abs(curvature) * settings.RacingLineRadius);
                offsets[i] = Mathf.Sign(curvature) * amplitude * saturation;
            }

            return offsets;
        }

        private static Vector3 BuildRight(Vector3 tangent)
        {
            var right = Vector3.Cross(Vector3.up, tangent);
            return right.sqrMagnitude > Epsilon ? right.normalized : Vector3.right;
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        private static float FlatLength(Vector3 value)
        {
            return Mathf.Sqrt(value.x * value.x + value.z * value.z);
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var dx = b.x - a.x;
            var dz = b.z - a.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    /// <summary>
    /// Reusable, allocation-free sampling cursor over a <see cref="TrackPath"/> (T-10.1.5).
    /// </summary>
    /// <remarks>
    /// The cursor owns only an index and a distance. <see cref="Advance"/> is monotonic and wraps
    /// for closed layouts; <see cref="TrySample"/> and <see cref="TrySampleAhead"/> write a
    /// <see cref="PathSample"/> by value. Nothing here allocates, so the Stage-2 AI controller can
    /// sample every fixed step inside the 0 B/frame contract.
    /// </remarks>
    public struct TrackPathCursor
    {
        private TrackPath _path;
        private int _index;
        private float _distance;

        /// <summary>Creates a cursor at <paramref name="startDistance"/>.</summary>
        public TrackPathCursor(TrackPath path, float startDistance)
        {
            _path = path;
            _distance = path != null ? path.NormalizeDistance(startDistance) : 0f;
            _index = path != null ? path.IndexForDistance(_distance) : 0;
        }

        /// <summary>The path being sampled; null for a default cursor.</summary>
        public TrackPath Path => _path;

        /// <summary>Current path distance in meters.</summary>
        public float Distance => _distance;

        /// <summary>Index of the segment that starts at the current distance.</summary>
        public int WaypointIndex => _index;

        /// <summary>Moves the cursor to a new distance, wrapping or clamping per the path.</summary>
        public void Reset(float distance)
        {
            if (_path == null)
            {
                _distance = 0f;
                _index = 0;
                return;
            }

            _distance = _path.NormalizeDistance(distance);
            _index = _path.IndexForDistance(_distance);
        }

        /// <summary>
        /// Advances by <paramref name="deltaDistance"/> meters. Negative values are allowed; closed
        /// paths wrap and open paths clamp.
        /// </summary>
        public void Advance(float deltaDistance)
        {
            if (_path == null)
            {
                return;
            }

            Reset(_distance + deltaDistance);
        }

        /// <summary>Samples the current position with an optional per-opponent lateral bias.</summary>
        public bool TrySample(float lateralBias, out PathSample sample)
        {
            if (_path == null)
            {
                sample = default;
                return false;
            }

            sample = _path.SampleSegment(_index, _distance, lateralBias);
            return true;
        }

        /// <summary>
        /// Samples a position ahead of the cursor without moving it. Used for steering lookahead.
        /// </summary>
        public bool TrySampleAhead(float aheadDistance, float lateralBias, out PathSample sample)
        {
            if (_path == null)
            {
                sample = default;
                return false;
            }

            sample = _path.SampleAtDistance(_distance + aheadDistance, lateralBias);
            return true;
        }
    }
}
