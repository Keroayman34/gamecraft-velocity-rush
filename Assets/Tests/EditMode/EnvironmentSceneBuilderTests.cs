using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Validation and audit coverage for the parameterized scene-builder template (T-9.5).
    /// The five production scenes are deliberately not generated in Stage 1.
    /// </summary>
    public sealed class EnvironmentSceneBuilderTests
    {
        [Test]
        public void ValidateDefinition_AcceptsValidDefinition()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();

            Assert.DoesNotThrow(() => EnvironmentSceneBuilder.ValidateDefinition(definition));
        }

        [Test]
        public void ValidateDefinition_RejectsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => EnvironmentSceneBuilder.ValidateDefinition(null));
        }

        [Test]
        public void ValidateDefinition_RejectsInvalidLayout()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(Array.Empty<TrackSegment>());
            var definition = assets.CreateDefinition(trackLayout: layout);

            Assert.Throws<InvalidOperationException>(
                () => EnvironmentSceneBuilder.ValidateDefinition(definition));
        }

        [Test]
        public void ValidateDefinition_RejectsClosedLoopStage1Layout()
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
            var definition = assets.CreateDefinition(trackLayout: layout);

            Assert.Throws<InvalidOperationException>(
                () => EnvironmentSceneBuilder.ValidateDefinition(definition));
        }

        [Test]
        public void ScenePath_UsesStableEnvironmentId()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();

            var path = EnvironmentSceneBuilder.GetScenePath(definition);

            Assert.That(path, Is.EqualTo("Assets/Scenes/Environments/desert-test.unity"));
        }

        [Test]
        public void AuditRoots_EmptySet_FailsClosed()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();

            var exception = Assert.Throws<InvalidOperationException>(
                () => EnvironmentSceneBuilder.AuditRoots(Array.Empty<GameObject>(), definition));

            StringAssert.Contains("EnvironmentSceneContext", exception.Message);
        }

        [Test]
        public void AuditRoots_ContextWithoutWiring_FailsClosed()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();
            var contextGo = new GameObject("Environment");
            contextGo.AddComponent<EnvironmentSceneContext>().ConfigureForAuthoring(definition);

            try
            {
                var exception = Assert.Throws<InvalidOperationException>(
                    () => EnvironmentSceneBuilder.AuditRoots(new[] { contextGo }, definition));
                StringAssert.Contains("camera", exception.Message.ToLowerInvariant());
                StringAssert.Contains("directional light", exception.Message.ToLowerInvariant());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(contextGo);
            }
        }
    }
}
