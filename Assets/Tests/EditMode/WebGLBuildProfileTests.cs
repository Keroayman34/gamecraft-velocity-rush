using System;
using NUnit.Framework;
using VelocityRush.EditorTools;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode guards for the T-7.12 build-profile contract: the prototype profile is still the
    /// default, the vertical-slice profile targets Bootstrap → HandlingCourse, and profile
    /// resolution is explicit and fail-closed.
    /// </summary>
    public sealed class WebGLBuildProfileTests
    {
        private const string BootstrapScenePath = "Assets/Scenes/Bootstrap.unity";

        [Test]
        public void PrototypeProfile_KeepsTheApprovedT5SceneOrder()
        {
            var paths = VelocityRushWebGLBuilder.GetScenePaths(WebGLBuildProfile.Prototype);

            Assert.That(paths, Is.EqualTo(VelocityRushWebGLBuilder.PrototypeScenePaths));
            Assert.That(paths[0], Is.EqualTo(BootstrapScenePath));
            Assert.That(paths[1], Is.EqualTo(PrototypeSceneLoader.TestTrackScenePath));
            Assert.That(VelocityRushWebGLBuilder.ApprovedScenePaths, Is.EqualTo(paths));
        }

        [Test]
        public void VerticalSliceProfile_TargetsHandlingCourse()
        {
            var paths = VelocityRushWebGLBuilder.GetScenePaths(WebGLBuildProfile.VerticalSlice);

            Assert.That(paths[0], Is.EqualTo(BootstrapScenePath));
            Assert.That(paths[1], Is.EqualTo(PrototypeSceneLoader.HandlingCourseScenePath));
            Assert.That(paths.Length, Is.EqualTo(2));
        }

        [Test]
        public void DescribeSceneOrder_NamesTheProfileScenes()
        {
            Assert.That(
                VelocityRushWebGLBuilder.DescribeSceneOrder(WebGLBuildProfile.Prototype),
                Is.EqualTo("Bootstrap -> TestTrack"));
            Assert.That(
                VelocityRushWebGLBuilder.DescribeSceneOrder(WebGLBuildProfile.VerticalSlice),
                Is.EqualTo("Bootstrap -> HandlingCourse"));
        }

        [Test]
        public void TryParseProfile_AcceptsDocumentedTokens()
        {
            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("prototype", out var prototype), Is.True);
            Assert.That(prototype, Is.EqualTo(WebGLBuildProfile.Prototype));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("TestTrack", out var testTrack), Is.True);
            Assert.That(testTrack, Is.EqualTo(WebGLBuildProfile.Prototype));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("slice", out var slice), Is.True);
            Assert.That(slice, Is.EqualTo(WebGLBuildProfile.VerticalSlice));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("VerticalSlice", out var full), Is.True);
            Assert.That(full, Is.EqualTo(WebGLBuildProfile.VerticalSlice));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("HandlingCourse", out var course), Is.True);
            Assert.That(course, Is.EqualTo(WebGLBuildProfile.VerticalSlice));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("unknown", out _), Is.False);
            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("  ", out _), Is.False);
        }

        [Test]
        public void ResolveProfile_PrefersTheArgument_ThenTheEnvironment_ThenPrototype()
        {
            Assert.That(
                VelocityRushWebGLBuilder.ResolveProfile("slice", "prototype"),
                Is.EqualTo(WebGLBuildProfile.VerticalSlice));

            Assert.That(
                VelocityRushWebGLBuilder.ResolveProfile(null, "slice"),
                Is.EqualTo(WebGLBuildProfile.VerticalSlice));

            Assert.That(
                VelocityRushWebGLBuilder.ResolveProfile(null, null),
                Is.EqualTo(WebGLBuildProfile.Prototype));

            Assert.That(
                VelocityRushWebGLBuilder.ResolveProfile("  ", "  "),
                Is.EqualTo(WebGLBuildProfile.Prototype));
        }

        [Test]
        public void ResolveProfile_FailsClosedForAnUnknownToken()
        {
            Assert.Throws<InvalidOperationException>(() => VelocityRushWebGLBuilder.ResolveProfile("arcade", null));
        }
    }
}
