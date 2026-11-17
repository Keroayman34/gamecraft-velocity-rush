using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.EditorTools;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-3 content proof (T-10.8.1–T-10.8.5): the authored catalog satisfies the approved
    /// matrix, every reverse scene is a derived copy that audits cleanly, and every event resolves
    /// to a launchable plan with the correct direction and opponent policy.
    /// </summary>
    /// <remarks>
    /// These tests read the committed Stage-3 assets. They never regenerate a signature scene and
    /// never save a scene, so D-064 stays enforceable: the derived reverse scenes are audited, not
    /// rebuilt from meshes.
    /// </remarks>
    public sealed class ChampionshipContentEditModeTests
    {
        private const string ReverseSuffix = "-reverse";

        private readonly List<GameObject> _created = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
        }

        [Test]
        public void AuthoredCatalog_PassesCompleteValidation()
        {
            var environments = LoadEnvironmentDefinitions();
            var championships = LoadAssets<ChampionshipDefinition>(
                ChampionshipContentBuilder.ChampionshipsFolder);
            var events = LoadAssets<RaceEventDefinition>(ChampionshipContentBuilder.EventsFolder);

            Assert.That(environments.Length, Is.EqualTo(5), "Expected five environment definitions.");
            Assert.That(championships.Length, Is.EqualTo(
                ChampionshipContentPolicy.RequiredChampionshipCount));
            Assert.That(events.Length, Is.EqualTo(ChampionshipContentPolicy.RequiredTotalEvents));

            var host = new GameObject("Championship Catalog Probe");
            _created.Add(host);
            var championshipCatalog = host.AddComponent<ChampionshipCatalog>();
            championshipCatalog.ConfigureForAuthoring(championships);
            var eventCatalog = host.AddComponent<EventCatalog>();
            eventCatalog.ConfigureForAuthoring(events);
            var environmentCatalog = host.AddComponent<EnvironmentCatalog>();
            environmentCatalog.ConfigureForAuthoring(environments);

            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(
                championshipCatalog,
                new List<RaceEventDefinition>(events),
                environmentCatalog);

            Assert.That(
                result.IsValid,
                Is.True,
                "Authored catalog is invalid:" + Environment.NewLine +
                string.Join(Environment.NewLine, result.Errors));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);

            var coverage = result.Coverage;
            Assert.That(coverage.ChampionshipCount, Is.EqualTo(3));
            Assert.That(coverage.TotalEventCount, Is.EqualTo(15));
            Assert.That(coverage.CircuitRaceCount, Is.EqualTo(9));
            Assert.That(coverage.TimeTrialCount, Is.EqualTo(6));
            Assert.That(coverage.UnlockChainValid, Is.True);
            Assert.That(coverage.HasCyclicUnlock, Is.False);
            Assert.That(coverage.AllEventsReachable, Is.True);

            Assert.That(coverage.EventsPerEnvironment.Length, Is.EqualTo(5));
            for (var i = 0; i < coverage.EventsPerEnvironment.Length; i++)
            {
                Assert.That(
                    coverage.EventsPerEnvironment[i],
                    Is.EqualTo(ChampionshipContentPolicy.RequiredEventsPerEnvironment),
                    "Environment " + i + " must own three events.");
            }

            Assert.That(
                coverage.EventsPerConfiguration.Length,
                Is.EqualTo(ChampionshipContentPolicy.RequiredConfigurationCount));
            for (var i = 0; i < coverage.EventsPerConfiguration.Length; i++)
            {
                Assert.That(
                    coverage.EventsPerConfiguration[i],
                    Is.GreaterThanOrEqualTo(1),
                    "Configuration " + i + " must be covered by at least one event.");
            }
        }

        [Test]
        public void ReverseScenePaths_MatchTheReadinessProfile()
        {
            var environments = LoadEnvironmentDefinitions();
            var profileScenes = VelocityRushWebGLBuilder.GetScenePaths(
                WebGLBuildProfile.Phase10Championships);

            for (var i = 0; i < environments.Length; i++)
            {
                var definition = environments[i];
                var signature = EnvironmentSceneBuilder.GetScenePath(
                    definition,
                    EnvironmentLayoutVariant.Signature);
                var reverse = EnvironmentSceneBuilder.GetScenePath(
                    definition,
                    EnvironmentLayoutVariant.Reverse);
                var expectedReverse = ChampionshipBuildReadinessTool.GetEventScenePath(
                    definition.Theme,
                    EnvironmentLayoutVariant.Reverse);

                Assert.That(reverse, Is.EqualTo(expectedReverse));
                Assert.That(reverse, Does.EndWith(ReverseSuffix + ".unity"));
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(signature),
                    Is.Not.Null,
                    "Signature scene is missing: " + signature);
                Assert.That(
                    AssetDatabase.LoadAssetAtPath<SceneAsset>(reverse),
                    Is.Not.Null,
                    "Reverse scene is missing: " + reverse);

                Assert.That(Array.IndexOf(profileScenes, signature), Is.GreaterThanOrEqualTo(0),
                    "The build profile must list " + signature);
                Assert.That(Array.IndexOf(profileScenes, reverse), Is.GreaterThanOrEqualTo(0),
                    "The build profile must list " + reverse);
            }
        }

        [Test]
        public void ReverseScenes_AuditCleanlyAndRunInGateOrder()
        {
            var environments = LoadEnvironmentDefinitions();
            for (var i = 0; i < environments.Length; i++)
            {
                var definition = environments[i];
                var path = EnvironmentSceneBuilder.GetScenePath(
                    definition,
                    EnvironmentLayoutVariant.Reverse);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var report = EnvironmentSceneBuilder.AuditScene(
                        scene,
                        definition,
                        EnvironmentLayoutVariant.Reverse);

                    Assert.That(report.MissingScriptCount, Is.EqualTo(0), path + ": missing script.");
                    Assert.That(report.MissingReferenceCount, Is.EqualTo(0), path + ": missing reference.");
                    Assert.That(report.CheckpointCount, Is.EqualTo(EnvironmentRaceRules.CheckpointCount));
                    Assert.That(report.CameraCount, Is.EqualTo(1));
                    Assert.That(report.DirectionalLightCount, Is.EqualTo(1));

                    AssertGateOrder(scene, definition, EnvironmentLayoutVariant.Reverse);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void SignatureScenes_StillAuditCleanly()
        {
            var environments = LoadEnvironmentDefinitions();
            for (var i = 0; i < environments.Length; i++)
            {
                var definition = environments[i];
                var path = EnvironmentSceneBuilder.GetScenePath(
                    definition,
                    EnvironmentLayoutVariant.Signature);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    var report = EnvironmentSceneBuilder.AuditScene(
                        scene,
                        definition,
                        EnvironmentLayoutVariant.Signature);
                    Assert.That(report.MissingScriptCount, Is.EqualTo(0), path + ": missing script.");
                    Assert.That(report.MissingReferenceCount, Is.EqualTo(0), path + ": missing reference.");
                    AssertGateOrder(scene, definition, EnvironmentLayoutVariant.Signature);
                }
                finally
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }

        [Test]
        public void EveryAuthoredEvent_ResolvesToALaunchablePlan()
        {
            var environments = LoadEnvironmentDefinitions();
            var championships = LoadAssets<ChampionshipDefinition>(
                ChampionshipContentBuilder.ChampionshipsFolder);
            var events = LoadAssets<RaceEventDefinition>(ChampionshipContentBuilder.EventsFolder);

            var host = new GameObject("Launch Plan Probe");
            _created.Add(host);
            var championshipCatalog = host.AddComponent<ChampionshipCatalog>();
            championshipCatalog.ConfigureForAuthoring(championships);
            var eventCatalog = host.AddComponent<EventCatalog>();
            eventCatalog.ConfigureForAuthoring(events);
            var environmentCatalog = host.AddComponent<EnvironmentCatalog>();
            environmentCatalog.ConfigureForAuthoring(environments);

            var launches = 0;
            for (var i = 0; i < events.Length; i++)
            {
                var definition = events[i];
                Assert.That(
                    EventLaunchPlanBuilder.TryBuild(
                        definition.EventId,
                        eventCatalog,
                        championshipCatalog,
                        environmentCatalog,
                        out var plan,
                        out var error),
                    Is.True,
                    definition.EventId + ": " + error);

                Assert.That(plan.EventDefinition, Is.SameAs(definition));
                Assert.That(plan.EventDefinition.LayoutVariant, Is.EqualTo(definition.LayoutVariant));
                Assert.That(plan.PaceScale, Is.EqualTo(definition.PaceScale).Within(0.0001f));

                var expectedOpponents = definition.EventType == RaceEventType.CircuitRace
                    ? ChampionshipContentPolicy.CircuitRaceAiCount
                    : 0;
                Assert.That(plan.OpponentCount, Is.EqualTo(expectedOpponents),
                    definition.EventId + ": wrong opponent count.");
                Assert.That(plan.HasOpponents, Is.EqualTo(expectedOpponents > 0));

                var scenePath = ChampionshipBuildReadinessTool.GetEventScenePath(
                    definition.EnvironmentTheme,
                    definition.LayoutVariant);
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath), Is.Not.Null,
                    definition.EventId + ": missing launch scene " + scenePath);

                var gates = EventLaunchPlanBuilder.BuildGateDistances(
                    plan.EnvironmentDefinition.TrackLayout);
                Assert.That(gates.Length, Is.EqualTo(EnvironmentRaceRules.CheckpointCount + 1));
                for (var gate = 1; gate < gates.Length; gate++)
                {
                    Assert.That(gates[gate], Is.GreaterThan(gates[gate - 1]),
                        definition.EventId + ": gate distances must be strictly increasing.");
                }

                launches++;
            }

            Assert.That(launches, Is.EqualTo(ChampionshipContentPolicy.RequiredTotalEvents));
        }

        private static void AssertGateOrder(
            Scene scene,
            EnvironmentDefinition definition,
            EnvironmentLayoutVariant variant)
        {
            var path = EventLaunchPlanBuilder.GenerateTrackPath(definition, variant);
            var checkpoints = new CheckpointTrigger[EnvironmentRaceRules.CheckpointCount];
            var finishes = new List<FinishTrigger>();

            var roots = scene.GetRootGameObjects();
            for (var i = 0; i < roots.Length; i++)
            {
                var found = roots[i].GetComponentsInChildren<CheckpointTrigger>(true);
                for (var foundIndex = 0; foundIndex < found.Length; foundIndex++)
                {
                    var index = found[foundIndex].Index;
                    Assert.That(index, Is.InRange(0, checkpoints.Length - 1));
                    checkpoints[index] = found[foundIndex];
                }

                finishes.AddRange(roots[i].GetComponentsInChildren<FinishTrigger>(true));
            }

            Assert.That(finishes.Count, Is.EqualTo(1));

            var previous = -1f;
            for (var index = 0; index < checkpoints.Length; index++)
            {
                Assert.That(checkpoints[index], Is.Not.Null,
                    definition.EnvironmentId + ": missing checkpoint " + index);
                var distance = path.ProjectDistance(checkpoints[index].transform.position);
                Assert.That(distance, Is.GreaterThan(previous),
                    definition.EnvironmentId + " (" + variant + "): checkpoint " + index +
                    " does not follow the previous gate.");
                previous = distance;
            }

            var finishDistance = path.ProjectDistance(finishes[0].transform.position);
            Assert.That(finishDistance, Is.GreaterThan(previous),
                definition.EnvironmentId + " (" + variant + "): finish does not follow the final checkpoint.");
        }

        private static EnvironmentDefinition[] LoadEnvironmentDefinitions()
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            Assert.That(definitions, Is.Not.Null);
            Assert.That(definitions.Length, Is.EqualTo(5));
            return definitions;
        }

        private static T[] LoadAssets<T>(string folder) where T : Object
        {
            var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);
            var assets = new T[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                assets[i] = AssetDatabase.LoadAssetAtPath<T>(paths[i]);
                Assert.That(assets[i], Is.Not.Null, "Could not load " + paths[i]);
            }

            return assets;
        }
    }
}
