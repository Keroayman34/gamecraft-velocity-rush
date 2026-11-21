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
        public void Phase8DebugProfile_TargetsGarageThenHandlingCourse()
        {
            var paths = VelocityRushWebGLBuilder.GetScenePaths(WebGLBuildProfile.Phase8Debug);

            Assert.That(paths[0], Is.EqualTo("Assets/Scenes/Garage.unity"));
            Assert.That(paths[1], Is.EqualTo(PrototypeSceneLoader.HandlingCourseScenePath));
            Assert.That(paths.Length, Is.EqualTo(2));
            Assert.That(
                VelocityRushWebGLBuilder.DescribeSceneOrder(WebGLBuildProfile.Phase8Debug),
                Is.EqualTo("Garage -> HandlingCourse"));
        }

        [Test]
        public void Phase9DebugProfile_TargetsBootstrapGarageAndFiveEnvironments()
        {
            var paths = VelocityRushWebGLBuilder.GetScenePaths(WebGLBuildProfile.Phase9Debug);

            Assert.That(paths[0], Is.EqualTo(BootstrapScenePath));
            Assert.That(paths[1], Is.EqualTo("Assets/Scenes/Garage.unity"));
            Assert.That(paths.Length, Is.EqualTo(7));
            Assert.That(paths, Is.EquivalentTo(new[]
            {
                BootstrapScenePath,
                "Assets/Scenes/Garage.unity",
                "Assets/Scenes/Environments/desert-signature.unity",
                "Assets/Scenes/Environments/coastal-signature.unity",
                "Assets/Scenes/Environments/mountain-pass-signature.unity",
                "Assets/Scenes/Environments/neon-city-signature.unity",
                "Assets/Scenes/Environments/world-arena-signature.unity"
            }));
            Assert.That(
                VelocityRushWebGLBuilder.DescribeSceneOrder(WebGLBuildProfile.Phase9Debug),
                Is.EqualTo("Bootstrap -> world-arena-signature (7 scenes)"));
        }

        [Test]
        public void Phase11CommercialProfile_TargetsBootstrapGarageCatalogAndTenEnvironments()
        {
            var paths = VelocityRushWebGLBuilder.GetScenePaths(WebGLBuildProfile.Phase11Commercial);

            Assert.That(paths[0], Is.EqualTo(BootstrapScenePath));
            Assert.That(paths[1], Is.EqualTo("Assets/Scenes/Garage.unity"));
            Assert.That(paths[2], Is.EqualTo("Assets/Scenes/ChampionshipCatalog.unity"));
            Assert.That(paths.Length, Is.EqualTo(13));
            Assert.That(
                VelocityRushWebGLBuilder.DescribeSceneOrder(WebGLBuildProfile.Phase11Commercial),
                Is.EqualTo("Bootstrap -> world-arena-reverse (13 scenes)"));
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

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("phase8", out var phase8), Is.True);
            Assert.That(phase8, Is.EqualTo(WebGLBuildProfile.Phase8Debug));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("Garage", out var garage), Is.True);
            Assert.That(garage, Is.EqualTo(WebGLBuildProfile.Phase8Debug));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("phase9", out var phase9), Is.True);
            Assert.That(phase9, Is.EqualTo(WebGLBuildProfile.Phase9Debug));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("all-environments", out var all), Is.True);
            Assert.That(all, Is.EqualTo(WebGLBuildProfile.Phase9Debug));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("phase11", out var phase11), Is.True);
            Assert.That(phase11, Is.EqualTo(WebGLBuildProfile.Phase11Commercial));

            Assert.That(VelocityRushWebGLBuilder.TryParseProfile("commercial", out var commercial), Is.True);
            Assert.That(commercial, Is.EqualTo(WebGLBuildProfile.Phase11Commercial));

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
