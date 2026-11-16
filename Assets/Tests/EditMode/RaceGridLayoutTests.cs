using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-3 coverage for the deterministic starting grid (T-10.3.1, D-060): path-distance and
    /// side-offset placement, on-road clamping, non-overlap, the seven-car cap, open-path bounds,
    /// and closed-loop wrapping.
    /// </summary>
    public sealed class RaceGridLayoutTests
    {
        private const float Tolerance = 0.05f;

        [Test]
        public void TryCreate_FiveCars_PlacesRowsBehindTheLineOnRoad()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            var created = RaceGridLayout.TryCreate(
                path,
                60f,
                5,
                RaceGridSettings.Default,
                slots,
                out var count,
                out var error);

            Assert.That(created, Is.True, error);
            Assert.That(count, Is.EqualTo(5));

            var settings = RaceGridSettings.Default;
            Assert.That(slots[0].Distance, Is.EqualTo(57f).Within(Tolerance));
            Assert.That(slots[1].Distance, Is.EqualTo(57f).Within(Tolerance));
            Assert.That(slots[2].Distance, Is.EqualTo(51f).Within(Tolerance));
            Assert.That(slots[3].Distance, Is.EqualTo(51f).Within(Tolerance));
            Assert.That(slots[4].Distance, Is.EqualTo(45f).Within(Tolerance));

            Assert.That(slots[0].LateralOffset, Is.EqualTo(-settings.LateralSpacing * 0.5f).Within(Tolerance));
            Assert.That(slots[1].LateralOffset, Is.EqualTo(settings.LateralSpacing * 0.5f).Within(Tolerance));
            Assert.That(slots[4].LateralOffset, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(slots[4].Row, Is.EqualTo(2));
            Assert.That(slots[4].Column, Is.EqualTo(0));

            for (var i = 0; i < count; i++)
            {
                Assert.That(
                    Mathf.Abs(slots[i].LateralOffset) + settings.VehicleHalfWidth,
                    Is.LessThanOrEqualTo(path.HalfWidth + 0.001f),
                    "Slot " + i + " must keep the whole car on the road.");
                Assert.That(
                    Vector3.Dot(slots[i].Rotation * Vector3.forward, slots[i].Tangent),
                    Is.GreaterThan(0.999f),
                    "Slot " + i + " must face along the path.");
            }

            AssertNoOverlap(slots, count, settings.VehicleHalfWidth * 2f);
        }

        [Test]
        public void TryCreate_OddCount_CentersTheLastCar()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 5, RaceGridSettings.Default, slots, out var count, out var error),
                Is.True,
                error);

            Assert.That(count, Is.EqualTo(5));
            Assert.That(slots[4].LateralOffset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void TryCreate_SevenCars_FillsTheApprovedCap()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 300f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 80f, RaceGridLayout.MaxParticipants, RaceGridSettings.Default, slots, out var count, out var error),
                Is.True,
                error);

            Assert.That(count, Is.EqualTo(RaceGridLayout.MaxParticipants));
            AssertNoOverlap(slots, count, RaceGridSettings.Default.VehicleHalfWidth * 2f);
        }

        [Test]
        public void TryCreate_RejectsMoreThanTheSevenCarCap()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 300f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants + 1];

            Assert.That(
                RaceGridLayout.TryCreate(path, 80f, RaceGridLayout.MaxParticipants + 1, RaceGridSettings.Default, slots, out var count, out var error),
                Is.False);
            Assert.That(count, Is.EqualTo(0));
            Assert.That(error, Does.Contain("1.." + RaceGridLayout.MaxParticipants));
        }

        [Test]
        public void TryCreate_RejectsNonPositiveParticipantCount()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 0, RaceGridSettings.Default, slots, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("participant count"));
        }

        [Test]
        public void TryCreate_RejectsTooSmallDestination()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[4];

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 5, RaceGridSettings.Default, slots, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("smaller"));
        }

        [Test]
        public void TryCreate_RejectsAnOpenPathWithoutEnoughRoomBehindTheLine()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 6f, 5, RaceGridSettings.Default, slots, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("road behind the start line"));
        }

        [Test]
        public void TryCreate_RejectsARoadTooNarrowForTheGrid()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(100f),
                    TrackSegment.Straight(100f)
                },
                sampleSpacing: 1f,
                roadWidth: 6f);
            var path = TrackPath.Create(layout);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 80f, 5, RaceGridSettings.Default, slots, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("too narrow"));
        }

        [Test]
        public void TryCreate_RejectsSpacingThatWouldOverlapCars()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];
            var invalid = new RaceGridSettings(6f, 1f, 3f, 0.9f, 4.4f, 0.35f);

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 5, invalid, slots, out _, out var error),
                Is.False);
            Assert.That(error, Does.Contain("LateralSpacing"));
        }

        [Test]
        public void TryCreate_OnAClosedLoop_WrapsBehindTheStartLine()
        {
            using var assets = new EnvironmentTestAssets();
            var path = TrackPath.Create(CreateClosedLoop(assets));
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 0f, 5, RaceGridSettings.Default, slots, out var count, out var error),
                Is.True,
                error);

            Assert.That(count, Is.EqualTo(5));
            for (var i = 0; i < count; i++)
            {
                Assert.That(slots[i].Distance, Is.GreaterThanOrEqualTo(0f));
                Assert.That(slots[i].Distance, Is.LessThan(path.TotalLength));
                Assert.That(
                    Mathf.Abs(slots[i].LateralOffset) + RaceGridSettings.Default.VehicleHalfWidth,
                    Is.LessThanOrEqualTo(path.HalfWidth + 0.001f));
            }

            // The first row wraps to the end of the loop rather than clamping onto the start.
            Assert.That(slots[0].Distance, Is.GreaterThan(path.TotalLength - 10f));
            AssertNoOverlap(slots, count, RaceGridSettings.Default.VehicleHalfWidth * 2f);
        }

        [Test]
        public void TryCreate_OneParticipant_PlacesThePlayerOnTheCenterline()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var slots = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 1, RaceGridSettings.Default, slots, out var count, out var error),
                Is.True,
                error);

            Assert.That(count, Is.EqualTo(1));
            Assert.That(slots[0].LateralOffset, Is.EqualTo(0f).Within(0.001f));
            Assert.That(slots[0].Distance, Is.EqualTo(57f).Within(Tolerance));
        }

        [Test]
        public void TryCreate_IsDeterministicForIdenticalInputs()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var first = new RaceGridSlot[RaceGridLayout.MaxParticipants];
            var second = new RaceGridSlot[RaceGridLayout.MaxParticipants];

            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 5, RaceGridSettings.Default, first, out var firstCount, out var error),
                Is.True,
                error);
            Assert.That(
                RaceGridLayout.TryCreate(path, 60f, 5, RaceGridSettings.Default, second, out var secondCount, out _),
                Is.True);

            Assert.That(secondCount, Is.EqualTo(firstCount));
            for (var i = 0; i < firstCount; i++)
            {
                Assert.That(second[i].Distance, Is.EqualTo(first[i].Distance));
                Assert.That(second[i].LateralOffset, Is.EqualTo(first[i].LateralOffset));
                Assert.That(second[i].Position, Is.EqualTo(first[i].Position));
                Assert.That(second[i].Rotation, Is.EqualTo(first[i].Rotation));
            }
        }

        [Test]
        public void ProjectDistance_ReturnsTheNearestWaypointDistance()
        {
            using var assets = new EnvironmentTestAssets();
            var path = CreateStraightPath(assets, 200f);
            var waypoint = path.WaypointAt(15);

            var projected = path.ProjectDistance(waypoint.Center + (Vector3.up * 2f));

            Assert.That(projected, Is.EqualTo(waypoint.Distance).Within(0.001f));
        }

        private static TrackPath CreateStraightPath(EnvironmentTestAssets assets, float length)
        {
            var layout = assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(length * 0.5f),
                    TrackSegment.Straight(length * 0.5f)
                },
                sampleSpacing: 1f,
                roadWidth: 10f);
            return TrackPath.Create(layout);
        }

        private static TrackLayout CreateClosedLoop(EnvironmentTestAssets assets)
        {
            return assets.CreateLayout(
                new[]
                {
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1)
                },
                sampleSpacing: 1f,
                roadWidth: 10f,
                closedLoop: true);
        }

        private static void AssertNoOverlap(RaceGridSlot[] slots, int count, float minimumCenterDistance)
        {
            for (var a = 0; a < count; a++)
            {
                for (var b = a + 1; b < count; b++)
                {
                    var pa = slots[a].Position;
                    var pb = slots[b].Position;
                    var dx = pb.x - pa.x;
                    var dz = pb.z - pa.z;
                    var distance = Mathf.Sqrt((dx * dx) + (dz * dz));
                    Assert.That(
                        distance,
                        Is.GreaterThanOrEqualTo(minimumCenterDistance),
                        "Slots " + a + " and " + b + " overlap.");
                }
            }
        }
    }
}
