using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-1 coverage for the allocation-free AI racing path (T-10.1.1–T-10.1.7).
    /// </summary>
    /// <remarks>
    /// The tests exercise synthetic straight/arc/loop layouts for the math and the five authored
    /// environments for production-shaped content. Sampling and cursor tests assert exact values;
    /// allocation tests compare <see cref="GC.GetAllocatedBytesForCurrentThread"/> after warm-up.
    /// </remarks>
    public sealed class TrackPathTests
    {
        private const float Tolerance = 0.02f;

        [Test]
        public void Create_StraightLayout_BuildsOrderedWaypointsAtRequestedSpacing()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(50f), TrackSegment.Straight(50f) },
                sampleSpacing: 1f);

            var path = TrackPath.Create(layout);

            Assert.That(path.Count, Is.EqualTo(51));
            Assert.That(path.TotalLength, Is.EqualTo(100f).Within(Tolerance));
            Assert.That(path.WaypointAt(0).Distance, Is.EqualTo(0f).Within(0.001f));
            Assert.That(path.WaypointAt(1).Distance, Is.EqualTo(2f).Within(0.001f));
            Assert.That(path.WaypointAt(path.Count - 1).Distance, Is.EqualTo(100f).Within(Tolerance));
            Assert.That(path.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void Create_StraightLayout_HasZeroCurvatureAndMaximumSpeed()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(50f), TrackSegment.Straight(50f) },
                sampleSpacing: 1f);
            var path = TrackPath.Create(layout);

            for (var i = 0; i < path.Count; i++)
            {
                var waypoint = path.WaypointAt(i);
                Assert.That(Mathf.Abs(waypoint.Curvature), Is.LessThan(0.0001f));
                Assert.That(waypoint.TargetSpeed, Is.EqualTo(path.Settings.MaxSpeed).Within(0.001f));
            }

            var sample = path.SampleAtDistance(10f);
            Assert.That(sample.TargetSpeed, Is.EqualTo(path.Settings.MaxSpeed).Within(0.001f));
        }

        [Test]
        public void Create_RightArc_HasPositiveCurvatureMatchingRadius()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);
            var arcLength = Mathf.PI * 10f;
            var sample = path.SampleAtDistance(60f + arcLength * 0.5f);

            Assert.That(sample.Curvature, Is.GreaterThan(0f));
            Assert.That(sample.Curvature, Is.EqualTo(1f / 20f).Within(0.002f));
        }

        [Test]
        public void Create_LeftArc_HasNegativeCurvatureAndInsideRacingOffset()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: -1);
            var path = TrackPath.Create(layout);
            var arcLength = Mathf.PI * 10f;
            var sample = path.SampleAtDistance(60f + arcLength * 0.5f);

            Assert.That(sample.Curvature, Is.LessThan(0f));
            Assert.That(sample.Curvature, Is.EqualTo(-1f / 20f).Within(0.002f));
            Assert.That(sample.RacingOffset, Is.LessThan(0f));
        }

        [Test]
        public void Create_RightArc_OffsetsRacingLineTowardTheInside()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);
            var arcLength = Mathf.PI * 10f;
            var sample = path.SampleAtDistance(60f + arcLength * 0.5f);

            Assert.That(sample.RacingOffset, Is.GreaterThan(0f));
            Assert.That(sample.RacingOffset, Is.LessThanOrEqualTo(path.MaxLateralOffset));
            Assert.That(Vector3.Distance(sample.Position, sample.Center), Is.EqualTo(sample.RacingOffset).Within(0.001f));
        }

        [Test]
        public void Create_TightCorner_IsSlowerThanStraight()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);
            var arcLength = Mathf.PI * 10f;
            var corner = path.SampleAtDistance(60f + arcLength * 0.5f);
            var straight = path.SampleAtDistance(20f);

            Assert.That(corner.TargetSpeed, Is.LessThan(straight.TargetSpeed));
            Assert.That(corner.TargetSpeed, Is.GreaterThanOrEqualTo(path.Settings.MinSpeed));
        }

        [Test]
        public void Create_SpeedProfile_StaysInsideEnvelopeEverywhere()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);

            for (var i = 0; i < path.Count; i++)
            {
                var speed = path.WaypointAt(i).TargetSpeed;
                Assert.That(speed, Is.GreaterThanOrEqualTo(path.Settings.MinSpeed - 0.001f));
                Assert.That(speed, Is.LessThanOrEqualTo(path.Settings.MaxSpeed + 0.001f));
            }
        }

        [Test]
        public void Create_RacingOffsets_StayInsideTheRoad()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);

            for (var i = 0; i < path.Count; i++)
            {
                Assert.That(
                    Mathf.Abs(path.WaypointAt(i).RacingOffset),
                    Is.LessThanOrEqualTo(path.MaxLateralOffset + 0.001f));
            }
        }

        [Test]
        public void Sample_LateralBias_IsClampedToTheRoad()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateDefaultLayout();
            var path = TrackPath.Create(layout);

            var left = path.SampleAtDistance(50f, -1000f);
            var right = path.SampleAtDistance(50f, 1000f);

            Assert.That(left.LateralOffset, Is.EqualTo(-path.MaxLateralOffset).Within(0.001f));
            Assert.That(right.LateralOffset, Is.EqualTo(path.MaxLateralOffset).Within(0.001f));
            Assert.That(Vector3.Distance(left.Position, left.Center),
                Is.EqualTo(path.MaxLateralOffset).Within(0.001f));
        }

        [Test]
        public void Sample_OpenLayout_ClampsAtBothEnds()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(50f), TrackSegment.Straight(50f) },
                sampleSpacing: 1f);
            var path = TrackPath.Create(layout);

            var before = path.SampleAtDistance(-25f);
            var after = path.SampleAtDistance(500f);
            var first = path.WaypointAt(0);
            var last = path.WaypointAt(path.Count - 1);

            Assert.That(Vector3.Distance(before.Center, first.Center), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(after.Center, last.Center), Is.LessThan(0.001f));
            Assert.That(after.Distance, Is.EqualTo(path.TotalLength).Within(0.001f));
        }

        [Test]
        public void Create_ClosedLoop_DropsDuplicateAndWrapsSampling()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateClosedLoop(assets);
            var path = TrackPath.Create(layout);

            Assert.That(path.IsClosedLoop, Is.True);
            Assert.That(path.TotalLength, Is.EqualTo(4f * Mathf.PI * 10f).Within(0.1f));

            var nearStart = path.SampleAtDistance(5f);
            var wrapped = path.SampleAtDistance(path.TotalLength + 5f);
            Assert.That(Vector3.Distance(nearStart.Position, wrapped.Position), Is.LessThan(0.02f));
            Assert.That(wrapped.Distance, Is.EqualTo(5f).Within(0.001f));
        }

        [Test]
        public void Cursor_OnClosedLoop_WrapsPastTheFinish()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateClosedLoop(assets);
            var path = TrackPath.Create(layout);
            var cursor = path.CreateCursor(path.TotalLength - 1f);

            cursor.Advance(2f);

            Assert.That(cursor.Distance, Is.LessThanOrEqualTo(2f));
            Assert.That(cursor.TrySample(0f, out var sample), Is.True);
            Assert.That(sample.Distance, Is.EqualTo(cursor.Distance).Within(0.001f));
        }

        [Test]
        public void Cursor_OnOpenLayout_ClampsAtTheEnd()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(50f), TrackSegment.Straight(50f) },
                sampleSpacing: 1f);
            var path = TrackPath.Create(layout);
            var cursor = path.CreateCursor(99f);

            cursor.Advance(50f);

            Assert.That(cursor.Distance, Is.EqualTo(path.TotalLength).Within(0.001f));
            Assert.That(cursor.WaypointIndex, Is.EqualTo(path.Count - 1));
        }

        [Test]
        public void Cursor_Advance_IsMonotonicAndAllocationFree()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateDefaultLayout();
            var path = TrackPath.Create(layout);
            var cursor = path.CreateCursor();
            var previous = cursor.Distance;

            for (var i = 0; i < 100; i++)
            {
                cursor.Advance(0.75f);
                Assert.That(cursor.Distance, Is.GreaterThanOrEqualTo(previous));
                previous = cursor.Distance;
            }
        }

        [Test]
        public void SampleAndCursor_AfterWarmup_AllocateZeroBytes()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);
            var path = TrackPath.Create(layout);
            var cursor = path.CreateCursor(3f);

            for (var i = 0; i < 200; i++)
            {
                cursor.Advance(0.37f);
                cursor.TrySample(0.4f, out _);
                cursor.TrySampleAhead(12f, 0.4f, out _);
                path.SampleAtDistance(cursor.Distance, 0.4f);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                cursor.Advance(0.37f);
                cursor.TrySample(0.4f, out var sample);
                cursor.TrySampleAhead(12f, 0.4f, out var ahead);
                path.SampleAtDistance(cursor.Distance, 0.4f);
            }

            var after = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(after - before, Is.EqualTo(0));
        }

        [Test]
        public void Create_IsDeterministicForIdenticalInputs()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);

            var first = TrackPath.Create(layout);
            var second = TrackPath.Create(layout);

            Assert.That(second.Count, Is.EqualTo(first.Count));
            Assert.That(second.TotalLength.Equals(first.TotalLength), Is.True);
            for (var i = 0; i < first.Count; i++)
            {
                var a = first.WaypointAt(i);
                var b = second.WaypointAt(i);
                Assert.That(b.Center.Equals(a.Center), Is.True, "center " + i);
                Assert.That(b.Tangent.Equals(a.Tangent), Is.True, "tangent " + i);
                Assert.That(b.Distance.Equals(a.Distance), Is.True, "distance " + i);
                Assert.That(b.Curvature.Equals(a.Curvature), Is.True, "curvature " + i);
                Assert.That(b.TargetSpeed.Equals(a.TargetSpeed), Is.True, "speed " + i);
                Assert.That(b.RacingOffset.Equals(a.RacingOffset), Is.True, "offset " + i);
            }
        }

        [Test]
        public void Create_Reverse_ReversesPointOrderAndKeepsLength()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = CreateArcLayout(assets, turnDirection: 1);

            var forward = TrackPath.Create(layout, EnvironmentLayoutVariant.Signature);
            var reverse = TrackPath.Create(layout, EnvironmentLayoutVariant.Reverse);

            Assert.That(reverse.Variant, Is.EqualTo(EnvironmentLayoutVariant.Reverse));
            Assert.That(reverse.TotalLength, Is.EqualTo(forward.TotalLength).Within(0.05f));
            Assert.That(
                Vector3.Distance(reverse.WaypointAt(0).Center, forward.WaypointAt(forward.Count - 1).Center),
                Is.LessThan(0.01f));
            Assert.That(reverse.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void WaypointAt_WrapsForClosedLayouts()
        {
            using var assets = new EnvironmentTestAssets();
            var path = TrackPath.Create(CreateClosedLoop(assets));

            Assert.That(
                Vector3.Distance(path.WaypointAt(-1).Center, path.WaypointAt(path.Count - 1).Center),
                Is.LessThan(0.001f));
            Assert.That(
                Vector3.Distance(path.WaypointAt(path.Count).Center, path.WaypointAt(0).Center),
                Is.LessThan(0.001f));
        }

        [Test]
        public void Create_NullLayout_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => TrackPath.Create(null));
        }

        [Test]
        public void Create_InvalidSettings_Throw()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateDefaultLayout();
            var invalid = new TrackPathBuildSettings(
                0f, 58f, 5f, 14f, 7f, 14f, 0.5f, 40f, 1.2f);

            Assert.Throws<ArgumentException>(
                () => TrackPath.Create(layout, EnvironmentLayoutVariant.Signature, invalid));
        }

        [Test]
        public void AuthoredEnvironments_BuildValidSignatureAndReversePaths()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            Assert.That(definitions.Length, Is.EqualTo(5));

            var variants = new[]
            {
                EnvironmentLayoutVariant.Signature,
                EnvironmentLayoutVariant.Reverse
            };

            for (var d = 0; d < definitions.Length; d++)
            {
                var definition = definitions[d];
                for (var v = 0; v < variants.Length; v++)
                {
                    var path = TrackPath.Create(definition.TrackLayout, variants[v]);
                    var label = definition.EnvironmentId + " " + variants[v];

                    Assert.That(path.Count, Is.GreaterThan(50), label + " waypoint count");
                    Assert.That(path.TryValidate(out var error), Is.True, label + ": " + error);
                    Assert.That(
                        path.TotalLength,
                        Is.EqualTo(definition.TrackLayout.TotalLength).Within(definition.TrackLayout.TotalLength * 0.01f),
                        label + " length");
                    Assert.That(path.Variant, Is.EqualTo(variants[v]), label + " variant");
                }
            }
        }

        [Test]
        public void AuthoredEnvironments_KeepSamplesOnTheRoad()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                var layout = definitions[i].TrackLayout;
                var path = TrackPath.Create(layout);
                for (var step = 0; step < 20; step++)
                {
                    var distance = path.TotalLength * (step / 20f);
                    var sample = path.SampleAtDistance(distance, path.MaxLateralOffset);
                    Assert.That(
                        Vector3.Distance(sample.Position, sample.Center),
                        Is.LessThanOrEqualTo(path.MaxLateralOffset + 0.001f),
                        definitions[i].EnvironmentId + " at " + distance.ToString("0.0") + " m");
                }
            }
        }

        private static TrackLayout CreateArcLayout(EnvironmentTestAssets assets, int turnDirection)
        {
            return assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(60f),
                    TrackSegment.Arc(90f, 20f, turnDirection),
                    TrackSegment.Straight(60f)
                },
                sampleSpacing: 1f,
                roadWidth: 10f);
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
    }
}
