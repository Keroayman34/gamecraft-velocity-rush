using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Phase-10B Stage-4 championship profiling evidence (T-10.9.2/T-10.9.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tool measures all <b>ten</b> committed environment/configuration scenes (five themes ×
    /// signature/reverse). Each measurement is the real scene structure plus the four-AI runtime
    /// envelope: the committed <c>AIOpponent</c> prefab is instantiated four times on the real
    /// starting grid so renderer, material, triangle, and frustum-culled visible-triangle counts
    /// include the AI field that <see cref="RaceEventRuntime"/> creates at launch.
    /// </para>
    /// <para>
    /// Draw calls are the unbatched main-pass renderer count plus one runtime blob shadow per AI
    /// car. Every AI realtime shadow caster is disabled (D-059/D-060), so no shadow pass is
    /// modelled. Heap, frame time, and the 10-cycle WASM drift are <b>simulated</b> from the real
    /// Phase-7/8 Chromium/ANGLE baseline, exactly as the Phase-9 and M8a matrices do. The PlayMode
    /// suite independently proves the ten-cycle launch/unload lifecycle. Real browser capture
    /// remains OQ-P01 and is never claimed here.
    /// </para>
    /// <para>
    /// The four-AI field is a conservative bound for every configuration, including the three
    /// Time-Trial-only configurations that ship no opponents. The authored event counts per
    /// configuration are reported alongside the bound.
    /// </para>
    /// </remarks>
    public static class Phase10ChampionshipsProfilingEvidence
    {
        private const string LogPrefix = "[VelocityRush.Phase10ChampsProfile] ";
        private const string AiPrefabPath = "Assets/Prefabs/Events/AIOpponent.prefab";
        private const string EventsFolder = "Assets/Data/Events";
        private const string OutputArgument = "-vrPhase10ChampionshipsEvidence";
        private const string OutputEnvironmentVariable = "VR_PHASE10_CHAMPIONSHIPS_EVIDENCE";
        private const long SimulatedRuntimeOverheadBytes = 8L * 1024L * 1024L;
        private const int ProfiledAiCount = ChampionshipContentPolicy.CircuitRaceAiCount;
        private const int CycleCount = Phase9ProfilingEvidence.CycleCount;

        private static readonly ConfigurationSpec[] Configurations =
        {
            new ConfigurationSpec("desert-signature", EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature),
            new ConfigurationSpec("desert-reverse", EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse),
            new ConfigurationSpec("coastal-signature", EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature),
            new ConfigurationSpec("coastal-reverse", EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Reverse),
            new ConfigurationSpec("mountain-pass-signature", EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Signature),
            new ConfigurationSpec("mountain-pass-reverse", EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Reverse),
            new ConfigurationSpec("neon-city-signature", EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature),
            new ConfigurationSpec("neon-city-reverse", EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Reverse),
            new ConfigurationSpec("world-arena-signature", EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Signature),
            new ConfigurationSpec("world-arena-reverse", EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Reverse)
        };

        /// <summary>Batch entry point. Exits 1 when any ceiling or cycle check fails.</summary>
        public static void WriteFromCommandLine()
        {
            try
            {
                var path = ResolveOutputPath();
                var report = BuildReport();
                WriteReport(path, report);
                LogMatrix(report, path);
                EditorApplication.Exit(report.allPassed ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Profiling evidence failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds the ten-configuration report without writing it.</summary>
        public static Phase10ChampionshipsReport BuildReport()
        {
            if (!File.Exists(AiPrefabPath))
            {
                throw new InvalidOperationException("Missing AI opponent prefab: " + AiPrefabPath);
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AiPrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Could not load AI opponent prefab: " + AiPrefabPath);
            }

            var eventCounts = CountAuthoredEvents();
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();

            var configurations = new List<Phase10ConfigurationProfile>(Configurations.Length);
            for (var i = 0; i < Configurations.Length; i++)
            {
                configurations.Add(MeasureConfiguration(Configurations[i], definitions, prefab, eventCounts));
            }

            var report = new Phase10ChampionshipsReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                evidenceClass = "simulated-browser-run",
                configurationCount = Configurations.Length,
                profiledAiCount = ProfiledAiCount,
                wasmBaselineBytes = Phase9ProfilingEvidence.BaselineWasmHeapBytes,
                drawCallCeiling = Phase9ProfilingEvidence.DrawCallCeiling,
                triangleCeiling = Phase9ProfilingEvidence.TriangleCeiling,
                dataMegabyteCeiling = Phase9ProfilingEvidence.DataMegabyteCeiling,
                materialCeiling = Phase9ProfilingEvidence.MaterialCeiling,
                cycleCount = CycleCount,
                configurations = configurations.ToArray()
            };

            long totalSerializedBytes = 0L;
            for (var i = 0; i < report.configurations.Length; i++)
            {
                totalSerializedBytes += report.configurations[i].serializedBytes;
            }

            report.totalSerializedBytes = totalSerializedBytes;

            var cycles = new List<Phase10ChampionshipsCycleProfile>(CycleCount);
            for (var cycle = 1; cycle <= CycleCount; cycle++)
            {
                var sequence = new StringBuilder();
                var peakHeap = Phase9ProfilingEvidence.BaselineWasmHeapBytes;
                var peakDrawCalls = 0;
                var peakTriangles = 0L;
                var frameTotal = 0d;
                for (var i = 0; i < report.configurations.Length; i++)
                {
                    var configuration = report.configurations[i];
                    if (i > 0)
                    {
                        sequence.Append(" -> ");
                    }

                    sequence.Append(configuration.configurationId);
                    peakHeap = Math.Max(peakHeap, configuration.simulatedPeakHeapBytes);
                    peakDrawCalls = Math.Max(peakDrawCalls, configuration.drawCalls);
                    peakTriangles = Math.Max(peakTriangles, configuration.visibleTriangles);
                    frameTotal += configuration.simulatedMeanFrameMs;
                }

                cycles.Add(new Phase10ChampionshipsCycleProfile
                {
                    cycle = cycle,
                    sequence = sequence.ToString(),
                    postReturnHeapBytes = Phase9ProfilingEvidence.BaselineWasmHeapBytes,
                    cumulativeDriftBytes = 0L,
                    peakHeapBytes = peakHeap,
                    peakDrawCalls = peakDrawCalls,
                    peakTriangles = peakTriangles,
                    meanFrameMs = Math.Round(frameTotal / report.configurations.Length, 3),
                    payloadBytes = totalSerializedBytes
                });
            }

            report.cycles = cycles.ToArray();

            var allPassed = true;
            long maxDrawCalls = 0L;
            long maxTriangles = 0L;
            var maxData = 0d;
            var maxMaterials = 0;
            var maxFrame = 0d;
            for (var i = 0; i < report.configurations.Length; i++)
            {
                var configuration = report.configurations[i];
                configuration.drawCallsPass =
                    configuration.drawCalls <= Phase9ProfilingEvidence.DrawCallCeiling;
                configuration.trianglesPass =
                    configuration.visibleTriangles <= Phase9ProfilingEvidence.TriangleCeiling;
                configuration.dataPass =
                    configuration.dataMegabytes <= Phase9ProfilingEvidence.DataMegabyteCeiling;
                configuration.materialsPass =
                    configuration.environmentMaterials <= Phase9ProfilingEvidence.MaterialCeiling;
                configuration.passed = configuration.drawCallsPass && configuration.trianglesPass &&
                                       configuration.dataPass && configuration.materialsPass;
                allPassed &= configuration.passed;

                maxDrawCalls = Math.Max(maxDrawCalls, configuration.drawCalls);
                maxTriangles = Math.Max(maxTriangles, configuration.visibleTriangles);
                maxData = Math.Max(maxData, configuration.dataMegabytes);
                maxMaterials = Math.Max(maxMaterials, configuration.environmentMaterials);
                maxFrame = Math.Max(maxFrame, configuration.simulatedMeanFrameMs);
            }

            report.maxDrawCalls = maxDrawCalls;
            report.maxTriangles = maxTriangles;
            report.maxDataMegabytes = Math.Round(maxData, 3);
            report.maxMaterials = maxMaterials;
            report.maxMeanFrameMs = maxFrame;

            var cycleDriftPassed = true;
            for (var i = 0; i < report.cycles.Length; i++)
            {
                cycleDriftPassed &= report.cycles[i].cumulativeDriftBytes == 0L;
            }

            report.cycleDriftPassed = cycleDriftPassed;
            report.aggregateDrawCallsPass = maxDrawCalls <= Phase9ProfilingEvidence.DrawCallCeiling;
            report.aggregateTrianglesPass = maxTriangles <= Phase9ProfilingEvidence.TriangleCeiling;
            report.perConfigurationDataPass = maxData <= Phase9ProfilingEvidence.DataMegabyteCeiling;
            report.perConfigurationMaterialsPass = maxMaterials <= Phase9ProfilingEvidence.MaterialCeiling;
            report.allPassed = allPassed && cycleDriftPassed;
            report.notes =
                "Structural values are real measurements of all ten committed environment scenes " +
                "with the committed AIOpponent prefab instantiated four times on the real starting " +
                "grid. Draw calls are the unbatched main-pass renderer bound plus one runtime blob " +
                "shadow per AI car; AI realtime shadow casters are disabled. The four-AI field is a " +
                "conservative bound for every configuration, including Time-Trial-only " +
                "configurations. Heap, frame time, and drift are simulated from the real Phase-7/8 " +
                "Chromium/ANGLE baseline (137,887,744 B heap, 394 draw calls, 115,798 triangles, " +
                "16.95 ms). PlayMode proves ten clean launch/unload cycles. Real browser capture " +
                "remains OQ-P01.";
            return report;
        }

        private static Phase10ConfigurationProfile MeasureConfiguration(
            ConfigurationSpec spec,
            EnvironmentDefinition[] definitions,
            GameObject prefab,
            Dictionary<int, EventCounts> eventCounts)
        {
            var definition = FindDefinition(definitions, spec.Theme);
            if (definition == null)
            {
                throw new InvalidOperationException("No environment definition for theme " + spec.Theme + ".");
            }

            var scenePath = EnvironmentSceneBuilder.GetScenePath(definition, spec.Variant);
            if (!File.Exists(scenePath))
            {
                throw new InvalidOperationException("Missing configuration scene: " + scenePath);
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var context = FindSingle<EnvironmentSceneContext>();
            if (context == null || context.Definition == null || context.Definition.TrackLayout == null)
            {
                throw new InvalidOperationException(scenePath + " has no valid EnvironmentSceneContext.");
            }

            var layout = context.Definition.TrackLayout;
            var roots = scene.GetRootGameObjects();
            var sceneRenderers = new List<Renderer>(256);
            var environmentMaterials = new HashSet<Material>();
            var allMaterials = new HashSet<Material>();
            for (var r = 0; r < roots.Length; r++)
            {
                var found = roots[r].GetComponentsInChildren<Renderer>(true);
                for (var i = 0; i < found.Length; i++)
                {
                    sceneRenderers.Add(found[i]);
                    var shared = found[i].sharedMaterials;
                    for (var m = 0; m < shared.Length; m++)
                    {
                        if (shared[m] == null)
                        {
                            continue;
                        }

                        allMaterials.Add(shared[m]);
                        if (string.Equals(roots[r].name, "Environment", StringComparison.Ordinal))
                        {
                            environmentMaterials.Add(shared[m]);
                        }
                    }
                }
            }

            var aiInstances = new List<GameObject>(ProfiledAiCount);
            var aiRenderers = new List<Renderer>(ProfiledAiCount * 24);
            var gridPlacementValid = true;
            try
            {
                PlaceAiField(scene, layout, spec.Variant, prefab, aiInstances, out gridPlacementValid);
                for (var i = 0; i < aiInstances.Count; i++)
                {
                    var found = aiInstances[i].GetComponentsInChildren<Renderer>(true);
                    for (var r = 0; r < found.Length; r++)
                    {
                        aiRenderers.Add(found[r]);
                        var shared = found[r].sharedMaterials;
                        for (var m = 0; m < shared.Length; m++)
                        {
                            if (shared[m] != null)
                            {
                                allMaterials.Add(shared[m]);
                            }
                        }
                    }
                }

                var renderers = new List<Renderer>(sceneRenderers.Count + aiRenderers.Count);
                renderers.AddRange(sceneRenderers);
                renderers.AddRange(aiRenderers);

                var rendererArray = renderers.ToArray();
                var rendererTriangles = new long[rendererArray.Length];
                long sceneTriangles = 0L;
                long aiTriangles = 0L;
                long vertices = 0L;
                for (var i = 0; i < rendererArray.Length; i++)
                {
                    var meshFilter = rendererArray[i].GetComponent<MeshFilter>();
                    var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                    if (mesh == null)
                    {
                        continue;
                    }

                    var triangles = mesh.triangles.Length / 3L;
                    rendererTriangles[i] = triangles;
                    sceneTriangles += triangles;
                    vertices += mesh.vertexCount;
                    if (i >= sceneRenderers.Count)
                    {
                        aiTriangles += triangles;
                    }
                }

                Phase9ProfilingEvidence.MeasureVisibleGeometry(
                    layout,
                    spec.Variant,
                    rendererArray,
                    rendererTriangles,
                    out var visibleTriangles,
                    out var visibleRenderers);

                var serializedBytes = MeasureSerializedBytes(definition, scenePath);
                var dataMegabytes = serializedBytes / (1024d * 1024d);

                // One blob shadow per AI car is created at runtime by AIVisualRig. AI realtime
                // shadow casters are disabled, so no shadow pass is added.
                var drawCalls = rendererArray.Length + aiInstances.Count;
                var frameMs = 16.95d * Math.Max(
                    1d,
                    Math.Max(
                        drawCalls / (double)Phase9ProfilingEvidence.BaselineDrawCalls,
                        visibleTriangles / (double)Phase9ProfilingEvidence.BaselineTriangles));

                var counts = eventCounts[ConfigurationKey(spec.Theme, spec.Variant)];
                return new Phase10ConfigurationProfile
                {
                    configurationId = spec.ConfigurationId,
                    theme = spec.Theme.ToString(),
                    variant = spec.Variant.ToString(),
                    scenePath = scenePath,
                    sceneRendererCount = sceneRenderers.Count,
                    aiRendererCount = aiRenderers.Count,
                    rendererCount = rendererArray.Length,
                    aiCount = aiInstances.Count,
                    profiledAiCount = ProfiledAiCount,
                    gridPlacementValid = gridPlacementValid,
                    uniqueMaterials = allMaterials.Count,
                    environmentMaterials = environmentMaterials.Count,
                    sceneTriangles = sceneTriangles,
                    aiTriangles = aiTriangles,
                    visibleTriangles = visibleTriangles,
                    visibleRenderers = visibleRenderers,
                    vertices = vertices,
                    serializedBytes = serializedBytes,
                    dataMegabytes = Math.Round(dataMegabytes, 3),
                    drawCalls = drawCalls,
                    simulatedMeanFrameMs = Math.Round(frameMs, 3),
                    simulatedPeakHeapBytes = Phase9ProfilingEvidence.BaselineWasmHeapBytes +
                                             serializedBytes + SimulatedRuntimeOverheadBytes,
                    postUnloadHeapBytes = Phase9ProfilingEvidence.BaselineWasmHeapBytes,
                    driftBytes = 0L,
                    authoredEventCount = counts.Total,
                    authoredCircuitRaceCount = counts.CircuitRace,
                    authoredTimeTrialCount = counts.TimeTrial,
                    shippedWorstCaseAiCount = counts.CircuitRace > 0 ? ProfiledAiCount : 0,
                    drawCallsPass = drawCalls <= Phase9ProfilingEvidence.DrawCallCeiling,
                    trianglesPass = visibleTriangles <= Phase9ProfilingEvidence.TriangleCeiling,
                    dataPass = dataMegabytes <= Phase9ProfilingEvidence.DataMegabyteCeiling,
                    materialsPass = environmentMaterials.Count <= Phase9ProfilingEvidence.MaterialCeiling
                };
            }
            finally
            {
                for (var i = 0; i < aiInstances.Count; i++)
                {
                    if (aiInstances[i] != null)
                    {
                        Object.DestroyImmediate(aiInstances[i]);
                    }
                }
            }
        }

        private static void PlaceAiField(
            Scene scene,
            TrackLayout layout,
            EnvironmentLayoutVariant variant,
            GameObject prefab,
            List<GameObject> aiInstances,
            out bool gridPlacementValid)
        {
            gridPlacementValid = true;
            var path = TrackPath.Create(layout, variant);
            var slots = new RaceGridSlot[1 + ProfiledAiCount];
            var placed = RaceGridLayout.TryCreate(
                path,
                EventLaunchPlanBuilder.DefaultStartLineDistance,
                1 + ProfiledAiCount,
                RaceGridSettings.Default,
                slots,
                out _,
                out var error);

            if (!placed)
            {
                gridPlacementValid = false;
                Debug.LogWarning(
                    LogPrefix + "Grid placement unavailable (" + error + "); using a deterministic " +
                    "start-line cluster for the AI measurement bound.");
            }

            for (var i = 0; i < ProfiledAiCount; i++)
            {
                var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
                if (instance == null)
                {
                    throw new InvalidOperationException("Could not instantiate AI opponent " + i + ".");
                }

                instance.name = "Profiled AI Opponent " + i;
                if (placed)
                {
                    var slot = slots[i + 1];
                    instance.transform.SetPositionAndRotation(
                        slot.Position + (Vector3.up * 0.5f),
                        slot.Rotation);
                }
                else
                {
                    var distance = EventLaunchPlanBuilder.DefaultStartLineDistance - 3f - ((i / 2) * 6f);
                    var lateral = (i % 2 == 0 ? -1.3f : 1.3f);
                    var sample = path.SampleAtDistance(distance, lateral);
                    var rotation = sample.Tangent.sqrMagnitude > 0.000001f
                        ? Quaternion.LookRotation(sample.Tangent, Vector3.up)
                        : Quaternion.identity;
                    instance.transform.SetPositionAndRotation(
                        sample.Position + (Vector3.up * 0.5f),
                        rotation);
                }

                aiInstances.Add(instance);
            }
        }

        private static long MeasureSerializedBytes(EnvironmentDefinition definition, string scenePath)
        {
            long bytes = 0L;
            bytes += MeasureFolder("Assets/Generated/Environments/" + definition.EnvironmentId);
            bytes += MeasureFolder(FindDataFolder(definition.EnvironmentId));
            bytes += new FileInfo(scenePath).Length;
            bytes += new FileInfo(AiPrefabPath).Length;
            return bytes;
        }

        private static long MeasureFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                return 0L;
            }

            long bytes = 0L;
            foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                bytes += new FileInfo(file).Length;
            }

            return bytes;
        }

        private static string FindDataFolder(string environmentId)
        {
            var root = EnvironmentCatalogBuilder.EnvironmentFolder;
            if (!Directory.Exists(root))
            {
                return root;
            }

            foreach (var folder in Directory.EnumerateDirectories(root, environmentId, SearchOption.AllDirectories))
            {
                return folder;
            }

            return root;
        }

        private static EnvironmentDefinition FindDefinition(
            EnvironmentDefinition[] definitions,
            EnvironmentTheme theme)
        {
            for (var i = 0; i < definitions.Length; i++)
            {
                if (definitions[i] != null && definitions[i].Theme == theme)
                {
                    return definitions[i];
                }
            }

            return null;
        }

        private static Dictionary<int, EventCounts> CountAuthoredEvents()
        {
            var counts = new Dictionary<int, EventCounts>(Configurations.Length);
            for (var i = 0; i < Configurations.Length; i++)
            {
                counts[ConfigurationKey(Configurations[i].Theme, Configurations[i].Variant)] =
                    new EventCounts();
            }

            var guids = AssetDatabase.FindAssets("t:RaceEventDefinition", new[] { EventsFolder });
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var definition = AssetDatabase.LoadAssetAtPath<RaceEventDefinition>(path);
                if (definition == null)
                {
                    continue;
                }

                var key = ConfigurationKey(definition.EnvironmentTheme, definition.LayoutVariant);
                if (!counts.TryGetValue(key, out var entry))
                {
                    entry = new EventCounts();
                    counts[key] = entry;
                }

                entry.Total++;
                if (definition.EventType == RaceEventType.CircuitRace)
                {
                    entry.CircuitRace++;
                }
                else if (definition.EventType == RaceEventType.TimeTrial)
                {
                    entry.TimeTrial++;
                }
            }

            return counts;
        }

        private static int ConfigurationKey(EnvironmentTheme theme, EnvironmentLayoutVariant variant)
        {
            return ((int)theme * 2) + (variant == EnvironmentLayoutVariant.Reverse ? 1 : 0);
        }

        private static T FindSingle<T>() where T : Component
        {
            var found = Object.FindObjectsByType<T>(FindObjectsInactive.Include);
            return found.Length > 0 ? found[0] : null;
        }

        private static string ResolveOutputPath()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], OutputArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(arguments[i + 1]);
                }
            }

            var fromEnvironment = Environment.GetEnvironmentVariable(OutputEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return Path.GetFullPath(fromEnvironment);
            }

            return Path.Combine(
                Path.GetTempPath(),
                "vr-phase10-championships-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json");
        }

        private static void WriteReport(string path, Phase10ChampionshipsReport report)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        }

        private static void LogMatrix(Phase10ChampionshipsReport report, string path)
        {
            Debug.Log(LogPrefix + "Evidence class: " + report.evidenceClass +
                      " | configurations=" + report.configurationCount +
                      " | profiledAI=" + report.profiledAiCount +
                      " | baselineWasmHeap=" + report.wasmBaselineBytes + " B");
            for (var i = 0; i < report.configurations.Length; i++)
            {
                var configuration = report.configurations[i];
                Debug.Log(
                    LogPrefix + configuration.configurationId +
                    " | variant=" + configuration.variant +
                    " | events=" + configuration.authoredEventCount +
                    " (" + configuration.authoredCircuitRaceCount + " circuit / " +
                    configuration.authoredTimeTrialCount + " time trial)" +
                    " | profiledAI=" + configuration.aiCount +
                    " | shippedWorstCaseAI=" + configuration.shippedWorstCaseAiCount +
                    " | gridPlacement=" + configuration.gridPlacementValid +
                    " | sceneRenderers=" + configuration.sceneRendererCount +
                    " | aiRenderers=" + configuration.aiRendererCount +
                    " | drawCalls=" + configuration.drawCalls +
                    " | materials=" + configuration.environmentMaterials +
                    " (total " + configuration.uniqueMaterials + ")" +
                    " | sceneTriangles=" + configuration.sceneTriangles +
                    " | aiTriangles=" + configuration.aiTriangles +
                    " | visibleTriangles=" + configuration.visibleTriangles +
                    " | visibleRenderers=" + configuration.visibleRenderers +
                    " | serialized=" + configuration.serializedBytes + " B" +
                    " (" + configuration.dataMegabytes.ToString("0.00", CultureInfo.InvariantCulture) + " MB)" +
                    " | simulatedFrame=" + configuration.simulatedMeanFrameMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms" +
                    " | pass=" + configuration.passed);
            }

            for (var i = 0; i < report.cycles.Length; i++)
            {
                var cycle = report.cycles[i];
                Debug.Log(
                    LogPrefix + "cycle " + cycle.cycle +
                    " | postReturnHeap=" + cycle.postReturnHeapBytes + " B" +
                    " | drift=" + cycle.cumulativeDriftBytes + " B" +
                    " | peakDrawCalls=" + cycle.peakDrawCalls +
                    " | peakTriangles=" + cycle.peakTriangles +
                    " | meanFrame=" + cycle.meanFrameMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms");
            }

            Debug.Log(
                LogPrefix + "SUMMARY | allPassed=" + report.allPassed +
                " | maxDrawCalls=" + report.maxDrawCalls + "/" + report.drawCallCeiling +
                " | maxTriangles=" + report.maxTriangles + "/" + report.triangleCeiling +
                " | maxData=" + report.maxDataMegabytes.ToString("0.00", CultureInfo.InvariantCulture) +
                " MB/" + report.dataMegabyteCeiling +
                " | maxMaterials=" + report.maxMaterials + "/" + report.materialCeiling +
                " | cycleDrift=" + (report.cycleDriftPassed ? "0 B" : "NON-ZERO") +
                " | report=" + path);
        }

        private readonly struct ConfigurationSpec
        {
            public ConfigurationSpec(
                string configurationId,
                EnvironmentTheme theme,
                EnvironmentLayoutVariant variant)
            {
                ConfigurationId = configurationId;
                Theme = theme;
                Variant = variant;
            }

            public string ConfigurationId { get; }
            public EnvironmentTheme Theme { get; }
            public EnvironmentLayoutVariant Variant { get; }
        }

        private sealed class EventCounts
        {
            public int Total;
            public int CircuitRace;
            public int TimeTrial;
        }
    }

    [Serializable]
    public sealed class Phase10ChampionshipsReport
    {
        public string generatedAtUtc;
        public string evidenceClass;
        public int configurationCount;
        public int profiledAiCount;
        public long wasmBaselineBytes;
        public int drawCallCeiling;
        public long triangleCeiling;
        public double dataMegabyteCeiling;
        public int materialCeiling;
        public int cycleCount;
        public long totalSerializedBytes;
        public long maxDrawCalls;
        public long maxTriangles;
        public double maxDataMegabytes;
        public int maxMaterials;
        public double maxMeanFrameMs;
        public bool aggregateDrawCallsPass;
        public bool aggregateTrianglesPass;
        public bool perConfigurationDataPass;
        public bool perConfigurationMaterialsPass;
        public bool cycleDriftPassed;
        public bool allPassed;
        public string notes;
        public Phase10ConfigurationProfile[] configurations;
        public Phase10ChampionshipsCycleProfile[] cycles;
    }

    [Serializable]
    public sealed class Phase10ConfigurationProfile
    {
        public string configurationId;
        public string theme;
        public string variant;
        public string scenePath;
        public int sceneRendererCount;
        public int aiRendererCount;
        public int rendererCount;
        public int aiCount;
        public int profiledAiCount;
        public bool gridPlacementValid;
        public int uniqueMaterials;
        public int environmentMaterials;
        public long sceneTriangles;
        public long aiTriangles;
        public long visibleTriangles;
        public int visibleRenderers;
        public long vertices;
        public long serializedBytes;
        public double dataMegabytes;
        public int drawCalls;
        public double simulatedMeanFrameMs;
        public long simulatedPeakHeapBytes;
        public long postUnloadHeapBytes;
        public long driftBytes;
        public int authoredEventCount;
        public int authoredCircuitRaceCount;
        public int authoredTimeTrialCount;
        public int shippedWorstCaseAiCount;
        public bool drawCallsPass;
        public bool trianglesPass;
        public bool dataPass;
        public bool materialsPass;
        public bool passed;
    }

    [Serializable]
    public sealed class Phase10ChampionshipsCycleProfile
    {
        public int cycle;
        public string sequence;
        public long postReturnHeapBytes;
        public long cumulativeDriftBytes;
        public long peakHeapBytes;
        public int peakDrawCalls;
        public long peakTriangles;
        public double meanFrameMs;
        public long payloadBytes;
    }
}
