#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Phase-10 Stage-0 per-vehicle render-cost probe (T-10.0.3).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads the ten committed vehicle rig prefabs and records real structural cost: renderer
    /// count, submesh-based draw calls, triangles, vertices, unique materials, and shadow casters.
    /// It then derives how many additional cars fit inside the Phase-9 worst-case headroom
    /// (373/500 draw calls, 95,314/180,000 visible triangles) so the approved 5-car default and
    /// 7-car cap can be checked against evidence instead of assumption.
    /// </para>
    /// <para>
    /// The probe is editor-only, writes its JSON report outside the repository, and never
    /// modifies a prefab, scene, asset, or project setting.
    /// </para>
    /// </remarks>
    public static class VehicleCostProbe
    {
        private const string LogPrefix = "[VehicleCostProbe] ";
        private const int DrawCallCeiling = 500;
        private const int TriangleCeiling = 180000;
        private const int BaselineDrawCalls = 373;
        private const int BaselineVisibleTriangles = 95314;
        private const int DefaultAiCount = 4;
        private const int CapAiCount = 6;

        /// <summary>Menu entry point.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Probe Vehicle Render Cost")]
        public static void ProbeFromMenu()
        {
            var report = BuildReport();
            LogSummary(report);
        }

        /// <summary>Batch entry point. Exits 1 when the approved default grid does not fit.</summary>
        public static void ProbeFromCommandLine()
        {
            try
            {
                var report = BuildReport();
                var path = ResolveOutputPath();
                WriteReport(path, report);
                LogSummary(report);
                Debug.Log(LogPrefix + "Report: " + path);
                EditorApplication.Exit(report.defaultGridFits ? 0 : 1);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "Probe failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds the report without writing it.</summary>
        public static VehicleCostReport BuildReport()
        {
            var definitions = LoadRoster();
            var entries = new List<VehicleCostEntry>(definitions.Length);
            var worstDrawCalls = 0;
            var worstTriangles = 0;

            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                var path = VehicleRigBuilder.GetVariantPath(definition);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    throw new InvalidOperationException("Missing rig prefab: " + path);
                }

                var entry = Measure(definition, path, prefab);
                entries.Add(entry);
                worstDrawCalls = Mathf.Max(worstDrawCalls, entry.drawCalls);
                worstTriangles = Mathf.Max(worstTriangles, entry.triangles);
            }

            var drawHeadroom = DrawCallCeiling - BaselineDrawCalls;
            var triangleHeadroom = TriangleCeiling - BaselineVisibleTriangles;
            var maxAiByDrawCalls = worstDrawCalls > 0 ? drawHeadroom / worstDrawCalls : 0;
            var maxAiByTriangles = worstTriangles > 0 ? triangleHeadroom / worstTriangles : 0;
            var maxAi = Mathf.Min(maxAiByDrawCalls, maxAiByTriangles);

            return new VehicleCostReport
            {
                generatedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                evidenceClass = "real-prefab-structural",
                vehicleCount = entries.Count,
                baselineDrawCalls = BaselineDrawCalls,
                baselineVisibleTriangles = BaselineVisibleTriangles,
                drawCallCeiling = DrawCallCeiling,
                triangleCeiling = TriangleCeiling,
                drawCallHeadroom = drawHeadroom,
                triangleHeadroom = triangleHeadroom,
                worstDrawCalls = worstDrawCalls,
                worstTriangles = worstTriangles,
                maxAiByDrawCalls = maxAiByDrawCalls,
                maxAiByTriangles = maxAiByTriangles,
                maxSupportedAi = maxAi,
                approvedDefaultAi = DefaultAiCount,
                approvedCapAi = CapAiCount,
                defaultGridFits = maxAi >= DefaultAiCount,
                capFits = maxAi >= CapAiCount,
                vehicles = entries.ToArray()
            };
        }

        private static VehicleCostEntry Measure(
            VehicleDefinition definition,
            string path,
            GameObject prefab)
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var materials = new HashSet<Material>();
            var drawCalls = 0;
            var triangles = 0;
            var vertices = 0;
            var shadowCasters = 0;

            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off)
                {
                    shadowCasters++;
                }

                var mesh = GetSharedMesh(renderer);
                if (mesh == null)
                {
                    drawCalls++;
                    continue;
                }

                drawCalls += Mathf.Max(1, mesh.subMeshCount);
                vertices += mesh.vertexCount;
                for (var subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
                {
                    triangles += (int)(mesh.GetIndexCount(subMesh) / 3);
                }

                var shared = renderer.sharedMaterials;
                for (var materialIndex = 0; materialIndex < shared.Length; materialIndex++)
                {
                    if (shared[materialIndex] != null)
                    {
                        materials.Add(shared[materialIndex]);
                    }
                }
            }

            return new VehicleCostEntry
            {
                vehicleId = definition.VehicleId,
                classId = definition.Class.ToString(),
                prefabPath = path,
                renderers = renderers.Length,
                drawCalls = drawCalls,
                triangles = triangles,
                vertices = vertices,
                uniqueMaterials = materials.Count,
                shadowCasters = shadowCasters
            };
        }

        private static Mesh GetSharedMesh(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            var filter = renderer.GetComponent<MeshFilter>();
            return filter != null ? filter.sharedMesh : null;
        }

        private static VehicleDefinition[] LoadRoster()
        {
            var guids = AssetDatabase.FindAssets(
                "t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);

            var definitions = new VehicleDefinition[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                definitions[i] = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[i]);
                if (definitions[i] == null)
                {
                    throw new InvalidOperationException(
                        "Missing vehicle definition: " + paths[i]);
                }
            }

            if (definitions.Length == 0)
            {
                throw new InvalidOperationException(
                    "No vehicle definitions under " + VehicleRosterBuilder.RosterFolder + ".");
            }

            return definitions;
        }

        private static string ResolveOutputPath()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-vehicleCostReport", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.GetFullPath(args[i + 1]);
                }
            }

            return Path.Combine(
                Path.GetTempPath(),
                "vr-vehicle-cost-" +
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json");
        }

        private static void WriteReport(string path, VehicleCostReport report)
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
        }

        private static void LogSummary(VehicleCostReport report)
        {
            Debug.Log(LogPrefix + "vehicles=" + report.vehicleCount +
                      " | worstDrawCalls=" + report.worstDrawCalls +
                      " | worstTriangles=" + report.worstTriangles +
                      " | headroomDrawCalls=" + report.drawCallHeadroom +
                      " | headroomTriangles=" + report.triangleHeadroom +
                      " | maxAiByDrawCalls=" + report.maxAiByDrawCalls +
                      " | maxAiByTriangles=" + report.maxAiByTriangles +
                      " | maxSupportedAi=" + report.maxSupportedAi +
                      " | defaultGrid(4)=" + (report.defaultGridFits ? "PASS" : "FAIL") +
                      " | cap(6)=" + (report.capFits ? "PASS" : "FAIL"));

            for (var i = 0; i < report.vehicles.Length; i++)
            {
                var vehicle = report.vehicles[i];
                Debug.Log(LogPrefix + vehicle.vehicleId +
                          " (" + vehicle.classId + ")" +
                          " renderers=" + vehicle.renderers +
                          " drawCalls=" + vehicle.drawCalls +
                          " triangles=" + vehicle.triangles +
                          " vertices=" + vehicle.vertices +
                          " materials=" + vehicle.uniqueMaterials +
                          " shadowCasters=" + vehicle.shadowCasters);
            }
        }

        /// <summary>Serializable per-vehicle measurement.</summary>
        [Serializable]
        public sealed class VehicleCostEntry
        {
            public string vehicleId;
            public string classId;
            public string prefabPath;
            public int renderers;
            public int drawCalls;
            public int triangles;
            public int vertices;
            public int uniqueMaterials;
            public int shadowCasters;
        }

        /// <summary>Serializable probe report.</summary>
        [Serializable]
        public sealed class VehicleCostReport
        {
            public string generatedAtUtc;
            public string evidenceClass;
            public int vehicleCount;
            public int baselineDrawCalls;
            public int baselineVisibleTriangles;
            public int drawCallCeiling;
            public int triangleCeiling;
            public int drawCallHeadroom;
            public int triangleHeadroom;
            public int worstDrawCalls;
            public int worstTriangles;
            public int maxAiByDrawCalls;
            public int maxAiByTriangles;
            public int maxSupportedAi;
            public int approvedDefaultAi;
            public int approvedCapAi;
            public bool defaultGridFits;
            public bool capFits;
            public VehicleCostEntry[] vehicles;
        }
    }
}
#endif
