using NUnit.Framework;
using UnityEngine;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Deterministic geometry checks for the generated Phase-6 handling course.</summary>
    public sealed class HandlingCourseLayoutTests
    {
        [Test]
        public void Layout_HasExpectedPrototypeSegments()
        {
            var segments = HandlingCourseLayout.Segments;

            Assert.That(segments.Length, Is.EqualTo(7));
            Assert.That(segments[0].Type, Is.EqualTo(HandlingCourseSegmentType.Straight));
            Assert.That(segments[0].Length, Is.EqualTo(100f).Within(0.001f));
            Assert.That(segments[1].Type, Is.EqualTo(HandlingCourseSegmentType.Arc));
            Assert.That(segments[1].TurnDirection, Is.EqualTo(-1));
            Assert.That(segments[3].TurnDirection, Is.EqualTo(1));
            Assert.That(segments[5].TurnDirection, Is.EqualTo(-1));
            Assert.That(segments[6].Length, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void TotalLength_MatchesSegmentSum()
        {
            var sum = 0f;
            for (var i = 0; i < HandlingCourseLayout.Segments.Length; i++)
            {
                sum += HandlingCourseLayout.Segments[i].Length;
            }

            Assert.That(HandlingCourseLayout.TotalLength, Is.EqualTo(sum).Within(0.001f));
            Assert.That(HandlingCourseLayout.TotalLength, Is.GreaterThan(300f));
        }

        [Test]
        public void Centerline_IsContinuousAtPrototypeSpacing()
        {
            var centerline = HandlingCourseLayout.BuildCenterline(1f);

            Assert.That(centerline.Length, Is.GreaterThan(300));
            for (var i = 1; i < centerline.Length; i++)
            {
                var a = new Vector3(centerline[i - 1].x, 0f, centerline[i - 1].z);
                var b = new Vector3(centerline[i].x, 0f, centerline[i].z);
                Assert.That(Vector3.Distance(a, b), Is.LessThanOrEqualTo(1.05f));
            }
        }

        [Test]
        public void Centerline_EndsNearTotalLength()
        {
            var centerline = HandlingCourseLayout.BuildCenterline(0.5f);
            var travelled = 0f;
            for (var i = 1; i < centerline.Length; i++)
            {
                var a = centerline[i - 1];
                var b = centerline[i];
                travelled += Mathf.Sqrt(((a.x - b.x) * (a.x - b.x)) + ((a.z - b.z) * (a.z - b.z)));
            }

            Assert.That(travelled, Is.EqualTo(HandlingCourseLayout.TotalLength).Within(1f));
        }

        [Test]
        public void Elevation_StartsFlat_Rises_AndReturnsToZero()
        {
            Assert.That(HandlingCourseLayout.ElevationAt(0f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(HandlingCourseLayout.ElevationAt(HandlingCourseLayout.TotalLength), Is.EqualTo(0f).Within(1e-4f));

            var max = 0f;
            for (var d = 0f; d <= HandlingCourseLayout.TotalLength; d += 1f)
            {
                max = Mathf.Max(max, HandlingCourseLayout.ElevationAt(d));
            }

            Assert.That(max, Is.GreaterThan(2.5f), "The sweeper/crest must include the proposed elevation.");
            Assert.That(max, Is.LessThan(3f));
        }

        [Test]
        public void CheckpointDistances_AreOrderedInsideTheCourse()
        {
            var previous = -1f;
            for (var i = 0; i < HandlingCourseLayout.CheckpointCount; i++)
            {
                var distance = HandlingCourseLayout.GetCheckpointDistance(i);
                Assert.That(distance, Is.GreaterThan(previous));
                Assert.That(distance, Is.LessThan(HandlingCourseLayout.GetFinishDistance()));
                previous = distance;
            }
        }

        [Test]
        public void SampleAtDistance_ReturnsFinitePoseAndTangent()
        {
            var centerline = HandlingCourseLayout.BuildCenterline(0.5f);
            var probes = new[]
            {
                0f,
                HandlingCourseLayout.TotalLength * 0.25f,
                HandlingCourseLayout.TotalLength * 0.5f,
                HandlingCourseLayout.GetFinishDistance(),
                HandlingCourseLayout.TotalLength
            };

            for (var i = 0; i < probes.Length; i++)
            {
                var position = HandlingCourseLayout.SampleAtDistance(centerline, probes[i], out var tangent);
                Assert.That(IsFinite(position.x), Is.True);
                Assert.That(IsFinite(position.y), Is.True);
                Assert.That(IsFinite(position.z), Is.True);
                Assert.That(tangent.sqrMagnitude, Is.GreaterThan(0.5f));
            }
        }

        [Test]
        public void RoadAndBarrierDimensions_ArePositive()
        {
            Assert.That(HandlingCourseLayout.RoadWidth, Is.EqualTo(10f));
            Assert.That(HandlingCourseLayout.BarrierHeight, Is.GreaterThan(0f));
            Assert.That(HandlingCourseLayout.BarrierThickness, Is.GreaterThan(0f));
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
