using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.EditorTools;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage 2/3 coverage for the five authored environment packages and their generated scenes
    /// (T-9.6–T-9.13). The tests read the committed content only; they never regenerate it.
    /// </summary>
    public sealed class EnvironmentContentTests
    {
        private const float GatePositionTolerance = 1f;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void TearDown()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void AuthoredCatalog_HasExactlyFiveValidThemes()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();

            Assert.That(definitions.Length, Is.EqualTo(EnvironmentThemeRules.RequiredThemeCount));
            Assert.That(EnvironmentCatalogRules.ValidateCatalog(definitions), Is.Empty);
        }

        [Test]
        public void AuthoredCatalog_PassesThemeDistinctness()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();

            Assert.That(EnvironmentDistinctnessRules.ValidateThemeSeparation(definitions), Is.Empty);
        }

        [Test]
        public void AuthoredLayouts_PassRaceGateRules()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                Assert.That(
                    EnvironmentRaceRules.Validate(definitions[i].TrackLayout),
                    Is.Empty,
                    definitions[i].EnvironmentId + " gate rules");
            }
        }

        [Test]
        public void AuthoredLayouts_HaveDistinctRoadWidthsAndLengths()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var left = 0; left < definitions.Length; left++)
            {
                for (var right = left + 1; right < definitions.Length; right++)
                {
                    var widthDelta = Mathf.Abs(
                        definitions[left].TrackLayout.RoadWidth -
                        definitions[right].TrackLayout.RoadWidth);
                    var lengthDelta = Mathf.Abs(
                        definitions[left].TrackLayout.TotalLength -
                        definitions[right].TrackLayout.TotalLength);

                    Assert.That(
                        widthDelta,
                        Is.GreaterThanOrEqualTo(EnvironmentDistinctnessRules.MinimumRoadWidthDelta),
                        definitions[left].EnvironmentId + " vs " + definitions[right].EnvironmentId);
                    Assert.That(
                        lengthDelta,
                        Is.GreaterThanOrEqualTo(EnvironmentDistinctnessRules.MinimumLengthDelta),
                        definitions[left].EnvironmentId + " vs " + definitions[right].EnvironmentId);
                }
            }
        }

        [Test]
        public void AuthoredDefinitions_HaveGeneratedMaterialsMeshesAndSkyboxes()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var id = definition.EnvironmentId;

                Assert.That(
                    EnvironmentMaterialKit.LoadMaterialSet(id),
                    Is.Not.Null,
                    id + " material set");
                Assert.That(
                    UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
                        EnvironmentLightingBuilder.GetSkyboxPath(definition)),
                    Is.Not.Null,
                    id + " skybox");

                var folder = EnvironmentMaterialKit.GetEnvironmentFolder(id);
                Assert.That(File.Exists(folder + "/" + id + "_Road.asset"), Is.True, id + " road mesh");
                Assert.That(File.Exists(folder + "/" + id + "_Barrier.asset"), Is.True, id + " barrier mesh");
                Assert.That(File.Exists(folder + "/" + id + "_EdgeLines.asset"), Is.True, id + " edge mesh");
                Assert.That(File.Exists(folder + "/" + id + "_Skirt.asset"), Is.True, id + " skirt mesh");
            }
        }

        [Test]
        public void GeneratedScenes_ExistForEveryDefinition()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                var path = EnvironmentSceneBuilder.GetScenePath(definitions[i]);
                Assert.That(
                    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(path),
                    Is.Not.Null,
                    path);
            }
        }

        [Test]
        public void GeneratedScenes_PassFailClosedAudit()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            var paths = new string[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                paths[i] = EnvironmentSceneBuilder.GetScenePath(definitions[i]);
            }

            for (var i = 0; i < paths.Length; i++)
            {
                var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                SceneManager.SetActiveScene(scene);
                var definition = UnityEditor.AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(
                    EnvironmentCatalogBuilder.EnvironmentFolder + "/" +
                    definitions[i].Theme + "/" + definitions[i].EnvironmentId + "/" +
                    definitions[i].EnvironmentId + "_Definition.asset");

                Assert.That(definition, Is.Not.Null, paths[i] + " definition");
                var report = EnvironmentSceneBuilder.AuditScene(scene, definition);

                Assert.That(report.MissingScriptCount, Is.Zero, paths[i] + " missing scripts");
                Assert.That(report.MissingReferenceCount, Is.Zero, paths[i] + " missing references");
                Assert.That(report.CheckpointCount, Is.EqualTo(EnvironmentRaceRules.CheckpointCount));
                Assert.That(report.CameraCount, Is.EqualTo(1), paths[i] + " cameras");
                Assert.That(report.DirectionalLightCount, Is.EqualTo(1), paths[i] + " directional lights");
                Assert.That(report.MaterialCount, Is.EqualTo(7), paths[i] + " environment materials");
                Assert.That(report.SpawnerCount, Is.EqualTo(1), paths[i] + " spawner");
                Assert.That(report.HudCount, Is.EqualTo(1), paths[i] + " HUD");
                Assert.That(report.AudioControllerCount, Is.EqualTo(4), paths[i] + " audio");
                Assert.That(report.VfxCount, Is.EqualTo(1), paths[i] + " VFX");
                Assert.That(report.LandmarkCount, Is.GreaterThan(0), paths[i] + " landmarks");
            }
        }

        [Test]
        public void CoastalScene_HasWater_AndOtherThemesDoNot()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var scene = EditorSceneManager.OpenScene(
                    EnvironmentSceneBuilder.GetScenePath(definition),
                    OpenSceneMode.Single);
                SceneManager.SetActiveScene(scene);
                var reloaded = UnityEditor.AssetDatabase.LoadAssetAtPath<EnvironmentDefinition>(
                    EnvironmentCatalogBuilder.EnvironmentFolder + "/" + definition.Theme + "/" +
                    definition.EnvironmentId + "/" + definition.EnvironmentId + "_Definition.asset");
                var report = EnvironmentSceneBuilder.AuditScene(scene, reloaded);

                if (definition.Theme == EnvironmentTheme.CoastalCircuit)
                {
                    Assert.That(report.WaterCount, Is.EqualTo(2), "Coastal water planes");
                }
                else
                {
                    Assert.That(report.WaterCount, Is.Zero, definition.EnvironmentId + " water");
                }
            }
        }

        [Test]
        public void NeonCityScene_HasEmissiveLaneMarkers()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            EnvironmentDefinition neon = null;
            for (var i = 0; i < definitions.Length; i++)
            {
                if (definitions[i].Theme == EnvironmentTheme.NeonCity)
                {
                    neon = definitions[i];
                    break;
                }
            }

            Assert.That(neon, Is.Not.Null);
            var scene = EditorSceneManager.OpenScene(
                EnvironmentSceneBuilder.GetScenePath(neon),
                OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var markerRoot = Find(scene, "Track/Neon Lane Markers");
            Assert.That(markerRoot, Is.Not.Null);
            Assert.That(markerRoot.childCount, Is.GreaterThan(20));
            for (var i = 0; i < markerRoot.childCount; i++)
            {
                var renderer = markerRoot.GetChild(i).GetComponent<MeshRenderer>();
                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.sharedMaterial, Is.Not.Null);
            }
        }

        [Test]
        public void GeneratedScenes_PlaceOrderedGatesOnTheCenterline()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            var paths = new string[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                paths[i] = EnvironmentSceneBuilder.GetScenePath(definitions[i]);
            }

            for (var i = 0; i < paths.Length; i++)
            {
                var definition = definitions[i];
                var scene = EditorSceneManager.OpenScene(paths[i], OpenSceneMode.Single);
                SceneManager.SetActiveScene(scene);

                var layout = definition.TrackLayout;
                var centerline = layout.BuildCenterline(layout.SampleSpacing, EnvironmentLayoutVariant.Signature);
                var gateRoot = Find(scene, "Race Gates");
                Assert.That(gateRoot, Is.Not.Null, definition.EnvironmentId + " gates");

                for (var checkpointIndex = 0; checkpointIndex < EnvironmentRaceRules.CheckpointCount; checkpointIndex++)
                {
                    var expected = layout.SampleAtDistance(
                        centerline,
                        EnvironmentRaceRules.GetCheckpointDistance(layout, checkpointIndex),
                        out _);
                    var gate = gateRoot.Find("Checkpoint " + checkpointIndex);
                    Assert.That(gate, Is.Not.Null, definition.EnvironmentId + " checkpoint " + checkpointIndex);
                    Assert.That(
                        Vector3.Distance(gate.position, expected),
                        Is.LessThan(GatePositionTolerance),
                        definition.EnvironmentId + " checkpoint " + checkpointIndex + " position");
                }

                var expectedFinish = layout.SampleAtDistance(
                    centerline,
                    EnvironmentRaceRules.GetFinishDistance(layout),
                    out _);
                var finish = gateRoot.Find("Finish");
                Assert.That(finish, Is.Not.Null, definition.EnvironmentId + " finish");
                Assert.That(
                    Vector3.Distance(finish.position, expectedFinish),
                    Is.LessThan(GatePositionTolerance),
                    definition.EnvironmentId + " finish position");
            }
        }

        private static Transform Find(Scene scene, string path)
        {
            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, path, StringComparison.Ordinal))
                {
                    return roots[i].transform;
                }

                var child = roots[i].transform.Find(path);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
