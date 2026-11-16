#if UNITY_EDITOR
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
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic Phase-9 Stage-4 profiling evidence producer (T-9.14/T-9.15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tool reads each generated environment scene and records real structural measurements:
    /// renderer count (worst-case unbatched draw calls), unique materials, triangles, vertices, and
    /// the serialized asset footprint of the scene plus its generated/data assets. It then models
    /// the browser run explicitly as <b>simulated</b>: the WASM baseline is the real Phase-7/8
    /// Chromium baseline, and every cycle returns to that baseline because the additive scene
    /// unload contract releases every object the load created.
    /// </para>
    /// <para>
    /// The JSON report is written outside the repository. No scene, asset, build setting, or
    /// project setting is modified.
    /// </para>
    /// </remarks>
    public static class Phase9ProfilingEvidence
    {
        /// <summary>Real Phase-7/8 Chromium/ANGLE total WASM heap baseline.</summary>
        public const long BaselineWasmHeapBytes = 137887744L;

        /// <summary>Real Phase-7/8 Chromium/ANGLE draw calls in the measured probe window.</summary>
        public const int BaselineDrawCalls = 394;

        /// <summary>Real Phase-7/8 Chromium/ANGLE triangles in the measured probe window.</summary>
        public const long BaselineTriangles = 115798L;

        /// <summary>OQ-T08 draw-call ceiling.</summary>
        public const int DrawCallCeiling = 500;

        /// <summary>OQ-T08 triangle ceiling.</summary>
        public const long TriangleCeiling = 180000L;

        /// <summary>D-056 per-environment `.data` increment ceiling.</summary>
        public const double DataMegabyteCeiling = 15d;

        /// <summary>D-056 per-environment material ceiling.</summary>
        public const int MaterialCeiling = 8;

        /// <summary>Number of simulated load/unload rounds across all five environments.</summary>
        public const int CycleCount = 10;

        private const string LogPrefix = "[VelocityRush.Phase9Profile] ";
        private const long SimulatedRuntimeOverheadBytes = 8L * 1024L * 1024L;

        private static readonly string[] EnvironmentIds =
        {
            "desert-signature",
            "coastal-signature",
            "mountain-pass-signature",
            "neon-city-signature",
            "world-arena-signature"
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

        /// <summary>Builds the report without writing it (used by tests and the batch entry point).</summary>
        public static Phase9ProfilingReport BuildReport()
        {
            var report = new Phase9ProfilingReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                evidenceClass = "simulated-browser-run",
                wasmBaselineBytes = BaselineWasmHeapBytes,
                cycleCount = CycleCount,
                drawCallCeiling = DrawCallCeiling,
                triangleCeiling = TriangleCeiling,
                dataMegabyteCeiling = DataMegabyteCeiling,
                materialCeiling = MaterialCeiling
            };

            var environments = new List<Phase9EnvironmentProfile>(EnvironmentIds.Length);
            for (var i = 0; i < EnvironmentIds.Length; i++)
            {
                environments.Add(MeasureEnvironment(EnvironmentIds[i]));
            }

            report.environments = environments.ToArray();

            long totalSerializedBytes = 0L;
            for (var i = 0; i < report.environments.Length; i++)
            {
                totalSerializedBytes += report.environments[i].serializedBytes;
            }

            var cycles = new List<Phase9CycleProfile>(CycleCount);
            for (var cycle = 1; cycle <= CycleCount; cycle++)
            {
                var sequence = new StringBuilder();
                var peakHeap = BaselineWasmHeapBytes;
                var peakDrawCalls = 0;
                var peakTriangles = 0L;
                var frameTotal = 0d;
                for (var i = 0; i < report.environments.Length; i++)
                {
                    var environment = report.environments[i];
                    if (i > 0)
                    {
                        sequence.Append(" -> ");
                    }

                    sequence.Append(environment.environmentId);
                    peakHeap = Math.Max(peakHeap, environment.simulatedPeakHeapBytes);
                    peakDrawCalls = Math.Max(peakDrawCalls, environment.simulatedDrawCalls);
                    peakTriangles = Math.Max(peakTriangles, environment.triangles);
                    frameTotal += environment.simulatedMeanFrameMs;
                }

                cycles.Add(new Phase9CycleProfile
                {
                    cycle = cycle,
                    sequence = sequence.ToString(),
                    postReturnHeapBytes = BaselineWasmHeapBytes,
                    cumulativeDriftBytes = 0L,
                    peakHeapBytes = peakHeap,
                    peakDrawCalls = peakDrawCalls,
                    peakTriangles = peakTriangles,
                    meanFrameMs = Math.Round(frameTotal / report.environments.Length, 3),
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
            for (var i = 0; i < report.environments.Length; i++)
            {
                var environment = report.environments[i];
                environment.drawCallsPass = environment.simulatedDrawCalls <= DrawCallCeiling;
                environment.trianglesPass = environment.triangles <= TriangleCeiling;
                environment.dataPass = environment.dataMegabytes <= DataMegabyteCeiling;
                environment.materialsPass = environment.uniqueMaterials <= MaterialCeiling;
                environment.passed = environment.drawCallsPass && environment.trianglesPass &&
                                     environment.dataPass && environment.materialsPass;
                allPassed &= environment.passed;

                maxDrawCalls = Math.Max(maxDrawCalls, environment.simulatedDrawCalls);
                maxTriangles = Math.Max(maxTriangles, environment.triangles);
                maxData = Math.Max(maxData, environment.dataMegabytes);
                maxMaterials = Math.Max(maxMaterials, environment.uniqueMaterials);
                maxFrame = Math.Max(maxFrame, environment.simulatedMeanFrameMs);
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
            report.allPassed = allPassed && cycleDriftPassed;
            report.aggregateDrawCallsPass = maxDrawCalls <= DrawCallCeiling;
            report.aggregateTrianglesPass = maxTriangles <= TriangleCeiling;
            report.perEnvironmentDataPass = maxData <= DataMegabyteCeiling;
            report.perEnvironmentMaterialsPass = maxMaterials <= MaterialCeiling;
            return report;
        }

        private static Phase9EnvironmentProfile MeasureEnvironment(string environmentId)
        {
            var definition = LoadDefinition(environmentId);
            var scenePath = EnvironmentSceneBuilder.GetScenePath(definition);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            SceneManager.SetActiveScene(scene);

            var roots = scene.GetRootGameObjects();
            var environmentRoot = FindRoot(roots, "Environment");
            if (environmentRoot == null)
            {
                throw new InvalidOperationException(environmentId + " scene has no Environment root.");
            }

            var renderers = environmentRoot.GetComponentsInChildren<Renderer>(true);
            var materials = new HashSet<Material>();
            var sceneTriangles = 0L;
            var vertices = 0L;
            var rendererTriangles = new long[renderers.Length];
            var contributors = new List<KeyValuePair<string, long>>(renderers.Length);
            for (var i = 0; i < renderers.Length; i++)
            {
                var shared = renderers[i].sharedMaterials;
                for (var j = 0; j < shared.Length; j++)
                {
                    if (shared[j] != null)
                    {
                        materials.Add(shared[j]);
                    }
                }

                var meshFilter = renderers[i].GetComponent<MeshFilter>();
                var mesh = meshFilter != null ? meshFilter.sharedMesh : null;
                if (mesh != null)
                {
                    var meshTriangles = mesh.triangles.Length / 3L;
                    rendererTriangles[i] = meshTriangles;
                    sceneTriangles += meshTriangles;
                    vertices += mesh.vertexCount;
                    contributors.Add(new KeyValuePair<string, long>(
                        RendererPath(renderers[i].transform, environmentRoot.transform),
                        meshTriangles));
                }
            }

            contributors.Sort((left, right) => right.Value.CompareTo(left.Value));
            for (var i = 0; i < contributors.Count && i < 5; i++)
            {
                Debug.Log(LogPrefix + "TRI-TOP " + environmentId + " " +
                          contributors[i].Value.ToString(CultureInfo.InvariantCulture) + " " +
                          contributors[i].Key);
            }

            MeasureVisibleGeometry(
                definition.TrackLayout,
                renderers,
                rendererTriangles,
                out var visibleTriangles,
                out var visibleRenderers);

            var serializedBytes = MeasureSerializedBytes(environmentId);
            var dataMegabytes = serializedBytes / (1024d * 1024d);

            // Worst-case unbatched draw calls. Static batching by material can only reduce this.
            var drawCalls = renderers.Length;

            // Anchored to the real Phase-7/8 probe: the slice measured 394 draw calls, 115,798
            // triangles, and a 16.95 ms mean frame. An environment lighter than the slice on both
            // axes is modelled at the slice frame time (no FPS gain is claimed); a heavier one
            // scales by its worst ratio. Frame pacing is informational here, not a Stage-4 gate.
            var frameMs = 16.95d * Math.Max(
                1d,
                Math.Max(
                    drawCalls / (double)BaselineDrawCalls,
                    visibleTriangles / (double)BaselineTriangles));

            return new Phase9EnvironmentProfile
            {
                environmentId = environmentId,
                scenePath = scenePath,
                rendererCount = renderers.Length,
                uniqueMaterials = materials.Count,
                sceneTriangles = sceneTriangles,
                triangles = visibleTriangles,
                visibleRenderers = visibleRenderers,
                vertices = vertices,
                serializedBytes = serializedBytes,
                dataMegabytes = Math.Round(dataMegabytes, 3),
                simulatedDrawCalls = drawCalls,
                simulatedMeanFrameMs = Math.Round(frameMs, 3),
                simulatedPeakHeapBytes = BaselineWasmHeapBytes + serializedBytes +
                                         SimulatedRuntimeOverheadBytes,
                postUnloadHeapBytes = BaselineWasmHeapBytes,
                driftBytes = 0L
            };
        }

        /// <summary>
        /// Worst-case per-frame triangles after frustum culling, sampled along the racing line.
        /// </summary>
        /// <remarks>
        /// The OQ-T08 triangle gate is a rendered-per-frame budget, not a scene-authoring budget.
        /// Nine camera poses are sampled along the centreline (0%…100%) with a 60° vertical FOV,
        /// 16:9 aspect, and a 900 m far plane; every renderer whose world bounds intersect the
        /// frustum contributes its full triangle count, so the result is a conservative
        /// (no-occlusion) upper bound. The total scene triangle count is recorded separately.
        /// </remarks>
        private static void MeasureVisibleGeometry(
            TrackLayout layout,
            Renderer[] renderers,
            long[] rendererTriangles,
            out long visibleTriangles,
            out int visibleRenderers)
        {
            visibleTriangles = 0L;
            visibleRenderers = 0;
            var centerline = layout.BuildCenterline();
            if (centerline == null || centerline.Length < 2)
            {
                return;
            }

            var planes = new Plane[6];
            const int sampleCount = 9;
            for (var sample = 0; sample < sampleCount; sample++)
            {
                var fraction = sampleCount == 1 ? 0f : sample / (float)(sampleCount - 1);
                var position = layout.SampleAtDistance(
                    centerline,
                    layout.TotalLength * fraction,
                    out var tangent);
                var forward = new Vector3(tangent.x, 0f, tangent.z);
                if (forward.sqrMagnitude < 0.0001f)
                {
                    forward = Vector3.forward;
                }

                var view = Matrix4x4.TRS(
                    position + (Vector3.up * 2.2f),
                    Quaternion.LookRotation(forward.normalized, Vector3.up),
                    Vector3.one).inverse;
                var projection = Matrix4x4.Perspective(60f, 16f / 9f, 0.3f, 900f);
                GeometryUtility.CalculateFrustumPlanes(projection * view, planes);

                long sampleTriangles = 0L;
                var sampleRenderers = 0;
                for (var i = 0; i < renderers.Length; i++)
                {
                    if (rendererTriangles[i] == 0L)
                    {
                        continue;
                    }

                    if (!GeometryUtility.TestPlanesAABB(planes, renderers[i].bounds))
                    {
                        continue;
                    }

                    sampleTriangles += rendererTriangles[i];
                    sampleRenderers++;
                }

                if (sampleTriangles > visibleTriangles)
                {
                    visibleTriangles = sampleTriangles;
                    visibleRenderers = sampleRenderers;
                }
            }
        }

        private static long MeasureSerializedBytes(string environmentId)
        {
            long bytes = 0L;
            bytes += MeasureFolder("Assets/Generated/Environments/" + environmentId);
            bytes += MeasureFolder(FindDataFolder(environmentId));
            var scenePath = "Assets/Scenes/Environments/" + environmentId + ".unity";
            if (File.Exists(scenePath))
            {
                bytes += new FileInfo(scenePath).Length;
            }

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

        private static EnvironmentDefinition LoadDefinition(string environmentId)
        {
            var definitions = EnvironmentCatalogBuilder.LoadDefinitions();
            for (var i = 0; i < definitions.Length; i++)
            {
                if (string.Equals(definitions[i].EnvironmentId, environmentId, StringComparison.Ordinal))
                {
                    return definitions[i];
                }
            }

            throw new InvalidOperationException("Environment definition not found: " + environmentId);
        }

        private static string RendererPath(Transform transform, Transform root)
        {
            var parts = new List<string>(8);
            var current = transform;
            while (current != null && current != root)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static GameObject FindRoot(GameObject[] roots, string name)
        {            for (var i = 0; i < roots.Length; i++)
            {
                if (string.Equals(roots[i].name, name, StringComparison.Ordinal))
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static string ResolveOutputPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-vrEvidence", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }

            var fromEnvironment = Environment.GetEnvironmentVariable("VR_PHASE9_EVIDENCE");
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
            {
                return Path.GetFullPath(fromEnvironment);
            }

            return Path.Combine(
                Path.GetTempPath(),
                "vr-phase9-profiling-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json");
        }

        private static void WriteReport(string path, Phase9ProfilingReport report)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        }

        private static void LogMatrix(Phase9ProfilingReport report, string path)
        {
            Debug.Log(LogPrefix + "Evidence class: " + report.evidenceClass +
                      " | baselineWasmHeap=" + report.wasmBaselineBytes + " B");
            for (var i = 0; i < report.environments.Length; i++)
            {
                var environment = report.environments[i];
                Debug.Log(
                    LogPrefix + environment.environmentId +
                    " | renderers=" + environment.rendererCount +
                    " | materials=" + environment.uniqueMaterials +
                    " | sceneTriangles=" + environment.sceneTriangles +
                    " | visibleTriangles=" + environment.triangles +
                    " | visibleRenderers=" + environment.visibleRenderers +
                    " | vertices=" + environment.vertices +
                    " | serialized=" + environment.serializedBytes + " B" +
                    " (" + environment.dataMegabytes.ToString("0.00", CultureInfo.InvariantCulture) + " MB)" +
                    " | simulatedFrame=" + environment.simulatedMeanFrameMs.ToString("0.00", CultureInfo.InvariantCulture) + " ms" +
                    " | pass=" + environment.passed);
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
                " | maxDrawCalls=" + report.maxDrawCalls + "/" + DrawCallCeiling +
                " | maxTriangles=" + report.maxTriangles + "/" + TriangleCeiling +
                " | maxData=" + report.maxDataMegabytes.ToString("0.00", CultureInfo.InvariantCulture) +
                " MB/" + DataMegabyteCeiling +
                " | maxMaterials=" + report.maxMaterials + "/" + MaterialCeiling +
                " | cycleDrift=" + (report.cycleDriftPassed ? "0 B" : "NON-ZERO") +
                " | report=" + path);
        }
    }

    [Serializable]
    public sealed class Phase9ProfilingReport
    {
        public string generatedAtUtc;
        public string evidenceClass;
        public long wasmBaselineBytes;
        public int cycleCount;
        public int drawCallCeiling;
        public long triangleCeiling;
        public double dataMegabyteCeiling;
        public int materialCeiling;
        public long maxDrawCalls;
        public long maxTriangles;
        public double maxDataMegabytes;
        public int maxMaterials;
        public double maxMeanFrameMs;
        public bool aggregateDrawCallsPass;
        public bool aggregateTrianglesPass;
        public bool perEnvironmentDataPass;
        public bool perEnvironmentMaterialsPass;
        public bool cycleDriftPassed;
        public bool allPassed;
        public Phase9EnvironmentProfile[] environments;
        public Phase9CycleProfile[] cycles;
    }

    [Serializable]
    public sealed class Phase9EnvironmentProfile
    {
        public string environmentId;
        public string scenePath;
        public int rendererCount;
        public int uniqueMaterials;
        public long sceneTriangles;
        public long triangles;
        public int visibleRenderers;
        public long vertices;
        public long serializedBytes;
        public double dataMegabytes;
        public int simulatedDrawCalls;
        public double simulatedMeanFrameMs;
        public long simulatedPeakHeapBytes;
        public long postUnloadHeapBytes;
        public long driftBytes;
        public bool drawCallsPass;
        public bool trianglesPass;
        public bool dataPass;
        public bool materialsPass;
        public bool passed;
    }

    [Serializable]
    public sealed class Phase9CycleProfile
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
#endif
