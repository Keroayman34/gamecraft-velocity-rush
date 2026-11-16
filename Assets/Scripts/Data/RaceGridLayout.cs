using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Engineering envelope for a starting grid (T-10.3.1, D-060).
    /// </summary>
    /// <remarks>
    /// Spacing values are provisional prototype geometry, not approved race balance. They are
    /// sized so two cars in a row and consecutive rows cannot overlap: lateral spacing must clear
    /// twice the vehicle half-width plus clearance, and row spacing must clear the vehicle length
    /// plus clearance. The layout is rejected if the road is too narrow rather than clamping cars
    /// into each other.
    /// </remarks>
    [Serializable]
    public struct RaceGridSettings
    {
        [SerializeField, Min(0.1f)] private float rowSpacing;
        [SerializeField, Min(0.1f)] private float lateralSpacing;
        [SerializeField, Min(0f)] private float firstRowBehindStart;
        [SerializeField, Min(0.01f)] private float vehicleHalfWidth;
        [SerializeField, Min(0.01f)] private float vehicleLength;
        [SerializeField, Min(0f)] private float clearance;

        /// <summary>Longitudinal spacing between grid rows in meters.</summary>
        public float RowSpacing => rowSpacing;

        /// <summary>Lateral spacing between the two columns in meters.</summary>
        public float LateralSpacing => lateralSpacing;

        /// <summary>Distance from the start line back to the first row in meters.</summary>
        public float FirstRowBehindStart => firstRowBehindStart;

        /// <summary>Half-width of one vehicle in meters; used for on-road validation.</summary>
        public float VehicleHalfWidth => vehicleHalfWidth;

        /// <summary>Length of one vehicle in meters; used for row-separation validation.</summary>
        public float VehicleLength => vehicleLength;

        /// <summary>Minimum gap between vehicle extents in meters.</summary>
        public float Clearance => clearance;

        /// <param name="rowSpacing">Longitudinal row spacing in meters.</param>
        /// <param name="lateralSpacing">Lateral column spacing in meters.</param>
        /// <param name="firstRowBehindStart">Distance from the start line to the first row in meters.</param>
        /// <param name="vehicleHalfWidth">Half-width of one vehicle in meters.</param>
        /// <param name="vehicleLength">Length of one vehicle in meters.</param>
        /// <param name="clearance">Minimum gap between vehicle extents in meters.</param>
        public RaceGridSettings(
            float rowSpacing,
            float lateralSpacing,
            float firstRowBehindStart,
            float vehicleHalfWidth,
            float vehicleLength,
            float clearance)
        {
            this.rowSpacing = rowSpacing;
            this.lateralSpacing = lateralSpacing;
            this.firstRowBehindStart = firstRowBehindStart;
            this.vehicleHalfWidth = vehicleHalfWidth;
            this.vehicleLength = vehicleLength;
            this.clearance = clearance;
        }

        /// <summary>
        /// Approved prototype envelope: 6 m rows, 2.6 m columns, 3 m behind the line, 0.9 m
        /// half-width, 4.4 m length, and 0.35 m clearance.
        /// </summary>
        public static RaceGridSettings Default =>
            new RaceGridSettings(6f, 2.6f, 3f, 0.9f, 4.4f, 0.35f);

        /// <summary>Validates the spacing envelope and returns the first problem.</summary>
        public bool TryValidate(out string error)
        {
            if (!IsFinite(rowSpacing) || rowSpacing <= 0.01f)
            {
                error = "RaceGridSettings: RowSpacing must be greater than 0.01 m.";
                return false;
            }

            if (!IsFinite(lateralSpacing) || lateralSpacing <= 0.01f)
            {
                error = "RaceGridSettings: LateralSpacing must be greater than 0.01 m.";
                return false;
            }

            if (!IsFinite(firstRowBehindStart) || firstRowBehindStart < 0f)
            {
                error = "RaceGridSettings: FirstRowBehindStart must be finite and non-negative.";
                return false;
            }

            if (!IsFinite(vehicleHalfWidth) || vehicleHalfWidth <= 0f)
            {
                error = "RaceGridSettings: VehicleHalfWidth must be positive.";
                return false;
            }

            if (!IsFinite(vehicleLength) || vehicleLength <= 0f)
            {
                error = "RaceGridSettings: VehicleLength must be positive.";
                return false;
            }

            if (!IsFinite(clearance) || clearance < 0f)
            {
                error = "RaceGridSettings: Clearance must be finite and non-negative.";
                return false;
            }

            var minimumLateral = (vehicleHalfWidth * 2f) + clearance;
            if (lateralSpacing < minimumLateral)
            {
                error = "RaceGridSettings: LateralSpacing must be at least " +
                        minimumLateral.ToString("0.###") + " m for non-overlapping columns.";
                return false;
            }

            var minimumRow = vehicleLength + clearance;
            if (rowSpacing < minimumRow)
            {
                error = "RaceGridSettings: RowSpacing must be at least " +
                        minimumRow.ToString("0.###") + " m for non-overlapping rows.";
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
    /// One computed starting-grid slot (T-10.3.1).
    /// </summary>
    /// <remarks>
    /// <see cref="Position"/> is the sampled road surface point before any chassis ride height is
    /// applied. <see cref="Distance"/> is the path distance after open-path clamping or closed-loop
    /// wrapping, so a caller can bind an AI driver to exactly the same point the slot represents.
    /// </remarks>
    public readonly struct RaceGridSlot
    {
        /// <summary>Path distance of the slot in meters.</summary>
        public float Distance { get; }

        /// <summary>Applied lateral offset in meters (positive is path right).</summary>
        public float LateralOffset { get; }

        /// <summary>Road-surface position of the slot.</summary>
        public Vector3 Position { get; }

        /// <summary>Unit forward tangent at the slot.</summary>
        public Vector3 Tangent { get; }

        /// <summary>Spawn rotation facing along the path tangent.</summary>
        public Quaternion Rotation { get; }

        /// <summary>Zero-based grid row, with row zero closest to the start line.</summary>
        public int Row { get; }

        /// <summary>Zero-based column within the row; the center of an odd final row is column zero.</summary>
        public int Column { get; }

        /// <summary>Creates a grid slot.</summary>
        public RaceGridSlot(
            float distance,
            float lateralOffset,
            Vector3 position,
            Vector3 tangent,
            Quaternion rotation,
            int row,
            int column)
        {
            Distance = distance;
            LateralOffset = lateralOffset;
            Position = position;
            Tangent = tangent;
            Rotation = rotation;
            Row = row;
            Column = column;
        }
    }

    /// <summary>
    /// Deterministic starting-grid placement derived from a <see cref="TrackPath"/> (T-10.3.1).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Rows are placed behind <c>startLineDistance</c> along the path, with two columns per row and
    /// a single centered car when the participant count is odd. Every slot is sampled through
    /// <see cref="TrackPath.SampleAtDistance(float, float)"/>, so the lateral bias is clamped by
    /// the path's on-road envelope. Generation is cold and allocates only the caller's destination
    /// array; no LINQ is used.
    /// </para>
    /// <para>
    /// Open point-to-point paths fail loudly when the requested start line does not leave enough
    /// road behind it. A closed loop wraps behind the start line, which keeps the standard grid
    /// convention on circuit layouts.
    /// </para>
    /// </remarks>
    public static class RaceGridLayout
    {
        /// <summary>Hard participant cap from D-060 (one player plus six opponents).</summary>
        public const int MaxParticipants = 7;

        /// <summary>
        /// Computes a grid into a caller-owned array.
        /// </summary>
        /// <param name="path">Validated path used for all sampling.</param>
        /// <param name="startLineDistance">Path distance of the start line in meters.</param>
        /// <param name="participantCount">Number of cars, including the player.</param>
        /// <param name="settings">Grid spacing envelope.</param>
        /// <param name="destination">Destination array; must hold at least <paramref name="participantCount"/> slots.</param>
        /// <param name="slotCount">Number of slots written on success.</param>
        /// <param name="error">First validation problem on failure.</param>
        /// <returns>True when every slot was placed on-road without overlap.</returns>
        public static bool TryCreate(
            TrackPath path,
            float startLineDistance,
            int participantCount,
            RaceGridSettings settings,
            RaceGridSlot[] destination,
            out int slotCount,
            out string error)
        {
            slotCount = 0;
            if (path == null)
            {
                error = "RaceGridLayout: path is null.";
                return false;
            }

            if (destination == null)
            {
                error = "RaceGridLayout: destination array is null.";
                return false;
            }

            if (participantCount < 1 || participantCount > MaxParticipants)
            {
                error = "RaceGridLayout: participant count must be within 1.." + MaxParticipants + ".";
                return false;
            }

            if (destination.Length < participantCount)
            {
                error = "RaceGridLayout: destination array is smaller than the participant count.";
                return false;
            }

            if (!IsFinite(startLineDistance))
            {
                error = "RaceGridLayout: start line distance must be finite.";
                return false;
            }

            if (!settings.TryValidate(out var settingsError))
            {
                error = "RaceGridLayout: " + settingsError;
                return false;
            }

            // TrackPath's lateral clamp is based on its authored margin. A car is only fully
            // on-road if that clamp plus the car's half-width still fits inside the road.
            if (path.MaxLateralOffset + settings.VehicleHalfWidth > path.HalfWidth + 0.001f)
            {
                error = "RaceGridLayout: the path lateral margin is smaller than the vehicle half-width.";
                return false;
            }

            var rows = (participantCount + 1) / 2;
            var gridDepth = settings.FirstRowBehindStart + ((rows - 1) * settings.RowSpacing);
            var frontRowDistance = startLineDistance - settings.FirstRowBehindStart;
            if (!path.IsClosedLoop && frontRowDistance - ((rows - 1) * settings.RowSpacing) < -0.001f)
            {
                error = "RaceGridLayout: an open path needs at least " +
                        gridDepth.ToString("0.###") + " m of road behind the start line.";
                return false;
            }

            var availableHalfWidth = Mathf.Max(0f, path.MaxLateralOffset - settings.VehicleHalfWidth);
            var requiredHalfWidth = settings.LateralSpacing * 0.5f;
            if (requiredHalfWidth > availableHalfWidth + 0.001f)
            {
                error = "RaceGridLayout: the road is too narrow for a " +
                        settings.LateralSpacing.ToString("0.###") + " m grid.";
                return false;
            }

            var hasCenterCar = (participantCount & 1) == 1;
            var lastIndex = participantCount - 1;
            for (var i = 0; i < participantCount; i++)
            {
                var row = i / 2;
                var column = i % 2;
                var lateral = column == 0
                    ? -settings.LateralSpacing * 0.5f
                    : settings.LateralSpacing * 0.5f;

                if (hasCenterCar && i == lastIndex)
                {
                    column = 0;
                    lateral = 0f;
                }

                var distance = frontRowDistance - (row * settings.RowSpacing);
                if (path.IsClosedLoop)
                {
                    distance = path.NormalizeDistance(distance);
                }

                var sample = path.SampleAtDistance(distance, lateral);
                var rotation = sample.Tangent.sqrMagnitude > 0.000001f
                    ? Quaternion.LookRotation(sample.Tangent, Vector3.up)
                    : Quaternion.identity;

                destination[i] = new RaceGridSlot(
                    sample.Distance,
                    sample.LateralOffset,
                    sample.Position,
                    sample.Tangent,
                    rotation,
                    row,
                    column);
            }

            // Sanity check: the spacing envelope is authoritative, but a path with an unusually
            // tight start curve can fold two rows toward each other. Reject only definite overlaps.
            var minimumCenterDistance = settings.VehicleHalfWidth * 2f;
            for (var a = 0; a < participantCount; a++)
            {
                for (var b = a + 1; b < participantCount; b++)
                {
                    var pa = destination[a].Position;
                    var pb = destination[b].Position;
                    var dx = pb.x - pa.x;
                    var dz = pb.z - pa.z;
                    if ((dx * dx) + (dz * dz) < minimumCenterDistance * minimumCenterDistance)
                    {
                        error = "RaceGridLayout: slots " + a + " and " + b +
                                " overlap on this start-line curve.";
                        return false;
                    }
                }
            }

            slotCount = participantCount;
            error = null;
            return true;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
