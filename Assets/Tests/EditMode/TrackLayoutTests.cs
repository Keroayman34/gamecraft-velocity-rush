using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Geometry, elevation, reverse-configuration, and validation tests for T-9.2.</summary>
    public sealed class TrackLayoutTests
    {
        private const float Tolerance = 0.02f;

        [Test]
        public void StraightOnly_BuildsExpectedLengthAndEndpoint()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                sampleSpacing: 1f);

            var centerline = layout.BuildCenterline();

            Assert.That(layout.TotalLength, Is.EqualTo(200f).Within(0.01f));
            Assert.That(centerline.Length, Is.EqualTo(201));
            Assert.That(Vector3.Distance(centerline[0], Vector3.zero), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(centerline[centerline.Length - 1], new Vector3(0f, 0f, 200f)),
                Is.LessThan(Tolerance));
        }

        [Test]
        public void ArcLength_IsDerivedFromSweepAndRadius()
        {
            var arc = TrackSegment.Arc(90f, 10f, -1);

            Assert.That(arc.Type, Is.EqualTo(TrackSegmentType.Arc));
            Assert.That(arc.Length, Is.EqualTo(Mathf.PI * 5f).Within(0.001f));
            Assert.That(arc.TurnDirection, Is.EqualTo(-1));
        }

        [Test]
        public void QuarterArc_EndsAtExpectedPositionAndTangent()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(50f),
                    TrackSegment.Arc(90f, 10f, -1),
                    TrackSegment.Straight(50f)
                },
                sampleSpacing: 0.25f);

            var centerline = layout.BuildCenterline();
            var arcLength = Mathf.PI * 5f;
            var position = layout.SampleAtDistance(centerline, 50f + arcLength, out var tangent);

            Assert.That(position.x, Is.EqualTo(-10f).Within(0.2f));
            Assert.That(position.z, Is.EqualTo(60f).Within(0.2f));
            Assert.That(tangent.x, Is.EqualTo(-1f).Within(0.02f));
            Assert.That(tangent.z, Is.EqualTo(0f).Within(0.02f));
        }

        [Test]
        public void ElevationAt_UsesSmoothStepBetweenKeys()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 0f),
                    TrackElevationKey.Create(100f, 5f),
                    TrackElevationKey.Create(200f, 0f)
                });

            Assert.That(layout.ElevationAt(0f), Is.EqualTo(0f).Within(0.001f));
            Assert.That(layout.ElevationAt(50f), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(layout.ElevationAt(100f), Is.EqualTo(5f).Within(0.001f));
            Assert.That(layout.ElevationAt(150f), Is.EqualTo(2.5f).Within(0.001f));
            Assert.That(layout.ElevationAt(200f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ElevationAt_ClampsOutsideAuthoredKeys()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 2f),
                    TrackElevationKey.Create(100f, 6f)
                });

            Assert.That(layout.ElevationAt(-20f), Is.EqualTo(2f).Within(0.001f));
            Assert.That(layout.ElevationAt(500f), Is.EqualTo(6f).Within(0.001f));
        }

        [Test]
        public void BuildCenterline_AppliesElevationProfile()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 0f),
                    TrackElevationKey.Create(200f, 4f)
                },
                sampleSpacing: 1f);

            var centerline = layout.BuildCenterline();

            Assert.That(centerline[0].y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(centerline[100].y, Is.EqualTo(2f).Within(0.05f));
            Assert.That(centerline[centerline.Length - 1].y, Is.EqualTo(4f).Within(0.001f));
        }

        [Test]
        public void ReverseConfiguration_ReversesPointsAndElevation()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 0f),
                    TrackElevationKey.Create(200f, 8f)
                },
                sampleSpacing: 1f);

            var forward = layout.BuildCenterline(1f, EnvironmentLayoutVariant.Signature);
            var reverse = layout.BuildCenterline(1f, EnvironmentLayoutVariant.Reverse);

            Assert.That(Vector3.Distance(reverse[0], forward[forward.Length - 1]), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(reverse[reverse.Length - 1], forward[0]), Is.LessThan(0.001f));
            Assert.That(reverse[0].y, Is.EqualTo(8f).Within(0.001f));
            Assert.That(reverse[reverse.Length - 1].y, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void SampleAtDistance_ClampsToLayoutBounds()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                sampleSpacing: 1f);
            var centerline = layout.BuildCenterline();

            var start = layout.SampleAtDistance(centerline, -50f, out var startTangent);
            var end = layout.SampleAtDistance(centerline, 500f, out var endTangent);

            Assert.That(Vector3.Distance(start, Vector3.zero), Is.LessThan(Tolerance));
            Assert.That(Vector3.Distance(end, new Vector3(0f, 0f, 200f)), Is.LessThan(Tolerance));
            Assert.That(startTangent.z, Is.EqualTo(1f).Within(0.001f));
            Assert.That(endTangent.z, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void SampleAtDistance_HandlesNullCenterline()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateDefaultLayout();

            var position = layout.SampleAtDistance(null, 10f, out var tangent);

            Assert.That(position, Is.EqualTo(Vector3.zero));
            Assert.That(tangent, Is.EqualTo(Vector3.forward));
        }

        [Test]
        public void ClosedLoop_FailsWhenAuthoredLayoutDoesNotClose()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                closedLoop: true);

            var issues = layout.Validate();

            StringAssert.Contains("does not close", string.Join("\n", issues));
        }

        [Test]
        public void ClosedLoop_PassesForClosedSquare()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(100f),
                    TrackSegment.Arc(90f, 50f, 1),
                    TrackSegment.Straight(100f),
                    TrackSegment.Arc(90f, 50f, 1),
                    TrackSegment.Straight(100f),
                    TrackSegment.Arc(90f, 50f, 1),
                    TrackSegment.Straight(100f),
                    TrackSegment.Arc(90f, 50f, 1)
                },
                sampleSpacing: 0.5f,
                closedLoop: true);

            var issues = layout.Validate();

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void NoSegments_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(Array.Empty<TrackSegment>());

            var issues = layout.Validate();

            StringAssert.Contains("at least two segments", string.Join("\n", issues));
        }

        [Test]
        public void SingleSegment_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(new[] { TrackSegment.Straight(100f) });

            var issues = layout.Validate();

            StringAssert.Contains("at least two segments", string.Join("\n", issues));
        }

        [Test]
        public void ZeroLengthStraight_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(0f), TrackSegment.Straight(100f) });

            var issues = layout.Validate();

            StringAssert.Contains("segment 0", string.Join("\n", issues));
        }

        [Test]
        public void ArcRadiusTooSmall_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Arc(90f, 1f, 1) });

            var issues = layout.Validate();

            StringAssert.Contains("Radius", string.Join("\n", issues));
        }

        [Test]
        public void ArcAngleOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var zeroAngle = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Arc(0f, 10f, 1) });
            var overSweep = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Arc(400f, 10f, 1) });

            StringAssert.Contains("AngleDegrees", string.Join("\n", zeroAngle.Validate()));
            StringAssert.Contains("AngleDegrees", string.Join("\n", overSweep.Validate()));
        }

        [Test]
        public void ArcFactory_NormalizesTurnDirection()
        {
            Assert.That(TrackSegment.Arc(90f, 10f, 0).TurnDirection, Is.EqualTo(1));
            Assert.That(TrackSegment.Arc(90f, 10f, -9).TurnDirection, Is.EqualTo(-1));
        }

        [Test]
        public void ElevationKeys_Unsorted_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 0f),
                    TrackElevationKey.Create(50f, 2f),
                    TrackElevationKey.Create(40f, 1f)
                });

            var issues = layout.Validate();

            StringAssert.Contains("strictly ascending", string.Join("\n", issues));
        }

        [Test]
        public void ElevationKeys_FirstNotZero_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(10f, 0f),
                    TrackElevationKey.Create(50f, 2f)
                });

            var issues = layout.Validate();

            StringAssert.Contains("first ElevationKey must be at distance 0", string.Join("\n", issues));
        }

        [Test]
        public void ElevationKey_BeyondLayoutLength_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                new[]
                {
                    TrackElevationKey.Create(0f, 0f),
                    TrackElevationKey.Create(300f, 2f)
                });

            var issues = layout.Validate();

            StringAssert.Contains("beyond the layout length", string.Join("\n", issues));
        }

        [Test]
        public void RoadWidthOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                roadWidth: 5f);

            StringAssert.Contains("RoadWidth", string.Join("\n", layout.Validate()));
        }

        [Test]
        public void BarrierDimensionsOutOfRange_Fail()
        {
            using var assets = new EnvironmentTestAssets();
            var tall = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                barrierHeight: 2.5f);
            var thin = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                barrierThickness: 0.05f);

            StringAssert.Contains("BarrierHeight", string.Join("\n", tall.Validate()));
            StringAssert.Contains("BarrierThickness", string.Join("\n", thin.Validate()));
        }

        [Test]
        public void SampleSpacingOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(
                new[] { TrackSegment.Straight(100f), TrackSegment.Straight(100f) },
                sampleSpacing: 0.1f);

            StringAssert.Contains("SampleSpacing", string.Join("\n", layout.Validate()));
        }

        [Test]
        public void UnsupportedSchemaVersion_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateDefaultLayout();
            EnvironmentTestAssets.SetSerializedInt(layout, "schemaVersion", 99);

            Assert.That(layout.TryValidate(out var error), Is.False);
            StringAssert.Contains("SchemaVersion", error);
        }

        [Test]
        public void TryValidate_ReturnsFirstIssue()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(Array.Empty<TrackSegment>());

            Assert.That(layout.TryValidate(out var error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }
    }
}
