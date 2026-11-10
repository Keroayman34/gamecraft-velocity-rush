#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Deterministic builder for the Phase-6 handling course scene (development prototype).
    /// </summary>
    /// <remarks>
    /// The scene is intentionally NOT added to <c>EditorBuildSettings</c>: the approved WebGL
    /// preflight still expects exactly Bootstrap → TestTrack. Phase-6 playtests open this scene
    /// directly in the editor. Running this tool never modifies TestTrack, build settings, or the
    /// existing prototype profiles.
    /// </remarks>
    public static class HandlingCourseBuilder
    {
        private const string ScenePath = "Assets/Scenes/HandlingCourse.unity";
        private const string GeneratedFolder = "Assets/Generated";
        private const string CourseFolder = GeneratedFolder + "/HandlingCourse";
        private const string RoadMeshPath = CourseFolder + "/HandlingCourseRoad.asset";
        private const string BarrierMeshPath = CourseFolder + "/HandlingCourseBarrier.asset";
        private const string RoadMaterialPath = CourseFolder + "/HandlingCourseRoad.mat";
        private const string BarrierMaterialPath = CourseFolder + "/HandlingCourseBarrier.mat";
        private const string VehiclePrefabPath = "Assets/Prefabs/PrototypeVehicle.prefab";
        private const string BaselineVehiclePath = "Assets/Data/Tuning/Baseline_Vehicle.asset";
        private const string BaselineCameraPath = "Assets/Data/Tuning/Baseline_Camera.asset";

        private const int TrackLayer = 8;

        /// <summary>Menu entry for a normal editor build.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Handling Course Scene", false, 20)]
        public static void BuildFromMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                if (!EditorUtility.DisplayDialog(
                        "Rebuild handling course?",
                        "This replaces " + ScenePath + " and its generated meshes/materials. " +
                        "TestTrack, the WebGL build settings, and the prototype profiles are not touched.",
                        "Rebuild",
                        "Cancel"))
                {
                    return;
                }
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Build(true);
        }

        /// <summary>Batch entry point used by the Phase-6 validation run.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build(true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError("[VelocityRush.HandlingCourse] Build failed: " + exception);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds and saves the handling-course scene.</summary>
        public static void Build(bool overwrite)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                throw new InvalidOperationException("HandlingCourse scene already exists: " + ScenePath);
            }

            EnsureFolder(GeneratedFolder);
            EnsureFolder(CourseFolder);

            var vehiclePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(VehiclePrefabPath);
            if (vehiclePrefab == null)
            {
                throw new InvalidOperationException("Missing vehicle prefab: " + VehiclePrefabPath);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var centerline = HandlingCourseLayout.BuildCenterline();

            var roadMesh = SaveMeshAsset(BuildRoadMesh(centerline), RoadMeshPath);
            var barrierMesh = SaveMeshAsset(BuildBarrierMesh(centerline), BarrierMeshPath);
            var roadMaterial = SaveMaterialAsset(CreateRoadMaterial(), RoadMaterialPath);
            var barrierMaterial = SaveMaterialAsset(CreateBarrierMaterial(), BarrierMaterialPath);

            // Presets must be (re)generated and loaded only after every generated asset has been
            // written: the DeleteAsset/CreateAsset import cycles below unload unreferenced assets,
            // which would leave earlier preset references as fake-null Unity objects.
            TuningPresetBuilder.EnsurePresets();
            var baselineVehicle = LoadRequired<VehiclePhysicsProfile>(BaselineVehiclePath, "tuning preset");
            var baselineCamera = LoadRequired<CameraProfile>(BaselineCameraPath, "tuning preset");

            var trackRoot = new GameObject("Track").transform;

            var road = new GameObject("Road");
            road.layer = TrackLayer;
            road.isStatic = true;
            road.transform.SetParent(trackRoot, false);
            road.AddComponent<MeshFilter>().sharedMesh = roadMesh;
            road.AddComponent<MeshRenderer>().sharedMaterial = roadMaterial;
            road.AddComponent<MeshCollider>().sharedMesh = roadMesh;

            var barriers = new GameObject("Barriers");
            barriers.layer = TrackLayer;
            barriers.isStatic = true;
            barriers.transform.SetParent(trackRoot, false);
            barriers.AddComponent<MeshFilter>().sharedMesh = barrierMesh;
            barriers.AddComponent<MeshRenderer>().sharedMaterial = barrierMaterial;
            barriers.AddComponent<MeshCollider>().sharedMesh = barrierMesh;

            CreateGround(trackRoot, centerline, roadMaterial);
            CreateBrakingMarkers(trackRoot, centerline, barrierMaterial);
            CreateLighting();

            var spawnPoint = HandlingCourseLayout.SampleAtDistance(centerline, 0f, out var spawnTangent);
            var spawnPosition = spawnPoint + (Vector3.up * 0.5f);
            var spawnForward = Flatten(spawnTangent);
            var spawnRotation = Quaternion.LookRotation(spawnForward, Vector3.up);

            var inputGo = new GameObject("PrototypeInput");
            var input = inputGo.AddComponent<LegacyKeyboardInputAdapter>();

            var vehicleGo = (GameObject)PrefabUtility.InstantiatePrefab(vehiclePrefab, scene);
            vehicleGo.name = "PrototypeVehicle";
            vehicleGo.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            var vehicle = vehicleGo.GetComponent<VehicleController>();
            SetObject(vehicle, "profile", baselineVehicle);
            SetObject(vehicle, "inputSource", input);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.07f, 0.08f, 0.11f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1500f;
            camera.fieldOfView = 60f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.SetPositionAndRotation(
                spawnPosition - (spawnForward * 7f) + (Vector3.up * 2.5f),
                spawnRotation);
            var cameraRig = cameraGo.AddComponent<VehicleCameraRig>();
            SetObject(cameraRig, "profile", baselineCamera);
            SetObject(cameraRig, "target", vehicleGo.transform);
            SetObject(cameraRig, "telemetrySource", vehicle);
            SetInt(cameraRig, "obstructionMask", 1 << TrackLayer);

            var raceGo = new GameObject("Race");
            var race = raceGo.AddComponent<RaceController>();
            SetFloat(race, "countdownSeconds", 2f);
            SetObject(race, "vehicle", vehicle);
            SetObject(race, "appInputSource", input);

            var gateRoot = new GameObject("Race Gates").transform;
            var checkpoints = new CheckpointTrigger[HandlingCourseLayout.CheckpointCount];
            for (var i = 0; i < checkpoints.Length; i++)
            {
                var gatePosition = HandlingCourseLayout.SampleAtDistance(
                    centerline,
                    HandlingCourseLayout.GetCheckpointDistance(i),
                    out var gateTangent);
                checkpoints[i] = CreateCheckpoint(gateRoot, gatePosition, gateTangent, i, race);
            }

            var finishPosition = HandlingCourseLayout.SampleAtDistance(
                centerline,
                HandlingCourseLayout.GetFinishDistance(),
                out var finishTangent);
            var finishTrigger = CreateFinish(gateRoot, finishPosition, finishTangent, race);

            var raceSerialized = new SerializedObject(race);
            var checkpointArray = raceSerialized.FindProperty("checkpoints");
            checkpointArray.arraySize = checkpoints.Length;
            for (var i = 0; i < checkpoints.Length; i++)
            {
                checkpointArray.GetArrayElementAtIndex(i).objectReferenceValue = checkpoints[i];
            }

            raceSerialized.FindProperty("finishTrigger").objectReferenceValue = finishTrigger;
            raceSerialized.ApplyModifiedPropertiesWithoutUndo();

            var overlayGo = new GameObject("TelemetryOverlay");
            var overlay = overlayGo.AddComponent<VehicleTelemetryOverlay>();
            SetObject(overlay, "telemetrySource", vehicle);

            var spawn = new GameObject("SpawnPoint");
            spawn.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException("Failed to save " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "[VelocityRush.HandlingCourse] Built " + ScenePath +
                " | length " + HandlingCourseLayout.TotalLength.ToString("0.0") + " m" +
                " | checkpoints " + checkpoints.Length +
                " | layer " + TrackLayer);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        private static void CreateGround(Transform parent, Vector3[] centerline, Material material)
        {
            var min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            var max = new Vector3(float.MinValue, 0f, float.MinValue);
            for (var i = 0; i < centerline.Length; i++)
            {
                min.x = Mathf.Min(min.x, centerline[i].x);
                min.z = Mathf.Min(min.z, centerline[i].z);
                max.x = Mathf.Max(max.x, centerline[i].x);
                max.z = Mathf.Max(max.z, centerline[i].z);
            }

            var center = new Vector3((min.x + max.x) * 0.5f, -0.35f, (min.z + max.z) * 0.5f);
            var extent = Mathf.Max(max.x - min.x, max.z - min.z) + 200f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.layer = TrackLayer;
            ground.transform.SetParent(parent, false);
            ground.transform.position = center;
            ground.transform.localScale = new Vector3(extent / 10f, 1f, extent / 10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void CreateBrakingMarkers(Transform parent, Vector3[] centerline, Material material)
        {
            var distances = new[] { 50f, 70f, 90f };
            for (var i = 0; i < distances.Length; i++)
            {
                var position = HandlingCourseLayout.SampleAtDistance(centerline, distances[i], out var tangent);
                var forward = Flatten(tangent);
                var right = Right(forward);
                for (var side = -1; side <= 1; side += 2)
                {
                    var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    marker.name = "Brake Marker " + distances[i].ToString("0") + "m";
                    marker.layer = TrackLayer;
                    marker.transform.SetParent(parent, false);
                    marker.transform.position =
                        position + (right * (side * ((HandlingCourseLayout.RoadWidth * 0.5f) - 0.45f))) +
                        (Vector3.up * 0.75f);
                    marker.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                    marker.transform.localScale = new Vector3(0.22f, 1.5f, 0.22f);
                    marker.GetComponent<MeshRenderer>().sharedMaterial = material;
                    Object.DestroyImmediate(marker.GetComponent<Collider>());
                }
            }
        }

        private static void CreateLighting()
        {
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = Color.white;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f);
        }

        private static CheckpointTrigger CreateCheckpoint(
            Transform parent,
            Vector3 position,
            Vector3 tangent,
            int index,
            RaceController race)
        {
            var gate = new GameObject("Checkpoint " + index);
            gate.layer = TrackLayer;
            gate.transform.SetParent(parent, false);
            gate.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Flatten(tangent), Vector3.up));
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(HandlingCourseLayout.RoadWidth + 2f, 4f, 2f);
            box.center = new Vector3(0f, 2f, 0f);
            var checkpoint = gate.AddComponent<CheckpointTrigger>();
            SetInt(checkpoint, "index", index);
            SetObject(checkpoint, "race", race);
            return checkpoint;
        }

        private static FinishTrigger CreateFinish(
            Transform parent,
            Vector3 position,
            Vector3 tangent,
            RaceController race)
        {
            var gate = new GameObject("Finish");
            gate.layer = TrackLayer;
            gate.transform.SetParent(parent, false);
            gate.transform.SetPositionAndRotation(position, Quaternion.LookRotation(Flatten(tangent), Vector3.up));
            var box = gate.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(HandlingCourseLayout.RoadWidth + 2f, 4f, 2f);
            box.center = new Vector3(0f, 2f, 0f);
            var finish = gate.AddComponent<FinishTrigger>();
            SetObject(finish, "race", race);
            return finish;
        }

        private static Mesh BuildRoadMesh(Vector3[] points)
        {
            var vertices = new Vector3[points.Length * 2];
            var uvs = new Vector2[points.Length * 2];
            var triangles = new int[(points.Length - 1) * 6];
            var halfWidth = HandlingCourseLayout.RoadWidth * 0.5f;
            var distance = 0f;

            for (var i = 0; i < points.Length; i++)
            {
                var forward = TangentAt(points, i);
                var right = Right(forward);
                vertices[(i * 2)] = points[i] - (right * halfWidth);
                vertices[(i * 2) + 1] = points[i] + (right * halfWidth);
                uvs[(i * 2)] = new Vector2(0f, distance);
                uvs[(i * 2) + 1] = new Vector2(1f, distance);

                if (i < points.Length - 1)
                {
                    distance += FlatDistance(points[i], points[i + 1]);
                    var triangle = i * 6;
                    var baseIndex = i * 2;
                    triangles[triangle] = baseIndex;
                    triangles[triangle + 1] = baseIndex + 2;
                    triangles[triangle + 2] = baseIndex + 1;
                    triangles[triangle + 3] = baseIndex + 1;
                    triangles[triangle + 4] = baseIndex + 2;
                    triangles[triangle + 5] = baseIndex + 3;
                }
            }

            var mesh = new Mesh { name = "HandlingCourseRoad" };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildBarrierMesh(Vector3[] points)
        {
            var vertices = new List<Vector3>(points.Length * 16);
            var triangles = new List<int>(points.Length * 24);
            var halfWidth = HandlingCourseLayout.RoadWidth * 0.5f;
            var halfThickness = HandlingCourseLayout.BarrierThickness * 0.5f;
            var offset = halfWidth + halfThickness + 0.02f;
            var height = Vector3.up * HandlingCourseLayout.BarrierHeight;

            for (var side = -1; side <= 1; side += 2)
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    var forward0 = TangentAt(points, i);
                    var forward1 = TangentAt(points, i + 1);
                    var right0 = Right(forward0);
                    var right1 = Right(forward1);

                    var center0 = points[i] + (right0 * (side * offset));
                    var center1 = points[i + 1] + (right1 * (side * offset));
                    var thickness0 = right0 * (side * halfThickness);
                    var thickness1 = right1 * (side * halfThickness);

                    var bottom0Inner = center0 - thickness0;
                    var bottom0Outer = center0 + thickness0;
                    var bottom1Inner = center1 - thickness1;
                    var bottom1Outer = center1 + thickness1;
                    var top0Inner = bottom0Inner + height;
                    var top0Outer = bottom0Outer + height;
                    var top1Inner = bottom1Inner + height;
                    var top1Outer = bottom1Outer + height;

                    AddQuad(vertices, triangles, bottom0Inner, top0Inner, top1Inner, bottom1Inner);
                    AddQuad(vertices, triangles, bottom0Outer, bottom1Outer, top1Outer, top0Outer);
                    AddQuad(vertices, triangles, top0Inner, top0Outer, top1Outer, top1Inner);
                    AddQuad(vertices, triangles, bottom0Inner, bottom1Inner, bottom1Outer, bottom0Outer);
                }
            }

            var mesh = new Mesh { name = "HandlingCourseBarrier" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<int> triangles,
            Vector3 a,
            Vector3 b,
            Vector3 c,
            Vector3 d)
        {
            var index = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
            triangles.Add(index);
            triangles.Add(index + 2);
            triangles.Add(index + 3);
        }

        private static Vector3 TangentAt(Vector3[] points, int index)
        {
            var previous = points[Mathf.Max(0, index - 1)];
            var next = points[Mathf.Min(points.Length - 1, index + 1)];
            return Flatten(next - previous);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            if (value.sqrMagnitude < 0.000001f)
            {
                return Vector3.forward;
            }

            return value.normalized;
        }

        private static Vector3 Right(Vector3 forward)
        {
            var right = Vector3.Cross(Vector3.up, forward);
            return right.sqrMagnitude < 0.000001f ? Vector3.right : right.normalized;
        }

        private static float FlatDistance(Vector3 a, Vector3 b)
        {
            var x = a.x - b.x;
            var z = a.z - b.z;
            return Mathf.Sqrt((x * x) + (z * z));
        }

        private static Mesh SaveMeshAsset(Mesh mesh, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material SaveMaterialAsset(Material material, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static Material CreateRoadMaterial()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            var material = new Material(shader) { name = "HandlingCourseRoad" };
            material.color = new Color(0.92f, 0.92f, 0.92f);
            return material;
        }

        private static Material CreateBarrierMaterial()
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            var material = new Material(shader) { name = "HandlingCourseBarrier" };
            material.color = new Color(0.72f, 0.75f, 0.80f);
            material.SetInt("_Cull", 0);
            return material;
        }

        private static void SetObject(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Loads a required asset and fails loudly instead of silently wiring a null reference.
        /// </summary>
        private static T LoadRequired<T>(string path, string description) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new InvalidOperationException("Missing " + description + ": " + path);
            }

            return asset;
        }

        private static void SetInt(Object target, string propertyName, int value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException(
                    "Missing serialized property '" + propertyName + "' on " + target.GetType().Name);
            }

            property.floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = path.Substring(0, path.LastIndexOf('/'));
            var name = path.Substring(path.LastIndexOf('/') + 1);
            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
