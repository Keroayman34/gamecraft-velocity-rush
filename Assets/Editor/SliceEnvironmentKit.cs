#if UNITY_EDITOR
using System;
using UnityEngine;
using VelocityRush.Gameplay;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Builds the Desert Circuit production dressing around the generated handling-course road
    /// (T-7.6): edge lines, start/checkpoint/finish gantries, roadside rocks, and distant dunes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All geometry is engine primitives or generated meshes, all materials come from
    /// <see cref="SliceMaterialSet"/>, and every decoration is non-colliding so handling physics is
    /// unchanged. Decor is marked static so it participates in batching and baked GI.
    /// </para>
    /// <para>
    /// Layout is deterministic from a fixed seed. Decoration avoids the driving layer so the chase
    /// camera's obstruction probe never treats a roadside rock as a wall.
    /// </para>
    /// </remarks>
    public static class SliceEnvironmentKit
    {
        private const int DecorLayer = 0;
        private const int DecorSeed = 72031;

        /// <summary>Adds all environment dressing under <paramref name="trackRoot"/>.</summary>
        /// <param name="trackRoot">Scene root that owns the track objects.</param>
        /// <param name="centerline">Generated track centerline.</param>
        /// <param name="materials">Shared slice material set.</param>
        public static void Decorate(Transform trackRoot, Vector3[] centerline, SliceMaterialSet materials)
        {
            if (trackRoot == null)
            {
                throw new ArgumentNullException(nameof(trackRoot));
            }

            if (centerline == null || centerline.Length < 2)
            {
                throw new ArgumentException("Centerline must contain at least two points.", nameof(centerline));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            CreateEdgeLines(trackRoot, centerline, materials.TrackLine);
            CreateStartLine(trackRoot, centerline, materials.Hazard);
            CreateStartGantry(trackRoot, centerline, materials);
            CreateCheckpointGantries(trackRoot, centerline, materials);
            CreateFinishGantry(trackRoot, centerline, materials);
            CreateRoadsideRocks(trackRoot, centerline, materials);
            CreateDunes(trackRoot, centerline, materials);
        }

        private static void CreateEdgeLines(Transform parent, Vector3[] centerline, Material material)
        {
            var mesh = BuildEdgeLineMesh(centerline);
            var go = new GameObject("Track Edge Lines");
            go.layer = DecorLayer;
            MarkStatic(go);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            SaveMesh(mesh, "HandlingCourseEdgeLines");
        }

        private static void CreateStartLine(Transform parent, Vector3[] centerline, Material material)
        {
            var position = HandlingCourseLayout.SampleAtDistance(centerline, 0f, out var tangent);
            var forward = Flatten(tangent);
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Start Line";
            box.layer = DecorLayer;
            MarkStatic(box);
            box.transform.SetParent(parent, false);
            box.transform.SetPositionAndRotation(
                position + (Vector3.up * 0.03f),
                Quaternion.LookRotation(forward, Vector3.up));
            box.transform.localScale = new Vector3(HandlingCourseLayout.RoadWidth, 0.04f, 0.9f);
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        private static void CreateStartGantry(Transform parent, Vector3[] centerline, SliceMaterialSet materials)
        {
            var position = HandlingCourseLayout.SampleAtDistance(centerline, 0f, out var tangent);
            CreateGantry(parent, "Start Gantry", position, tangent, materials, bannerTextMaterial: materials.Hazard);
        }

        private static void CreateCheckpointGantries(Transform parent, Vector3[] centerline, SliceMaterialSet materials)
        {
            for (var i = 0; i < HandlingCourseLayout.CheckpointCount; i++)
            {
                var position = HandlingCourseLayout.SampleAtDistance(
                    centerline,
                    HandlingCourseLayout.GetCheckpointDistance(i),
                    out var tangent);
                CreateGantry(parent, "Checkpoint Gantry " + i, position, tangent, materials, materials.Metal);
            }
        }

        private static void CreateFinishGantry(Transform parent, Vector3[] centerline, SliceMaterialSet materials)
        {
            var position = HandlingCourseLayout.SampleAtDistance(
                centerline,
                HandlingCourseLayout.GetFinishDistance(),
                out var tangent);
            CreateGantry(parent, "Finish Gantry", position, tangent, materials, materials.TrackLine);
        }

        private static void CreateGantry(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 tangent,
            SliceMaterialSet materials,
            Material bannerTextMaterial)
        {
            var forward = Flatten(tangent);
            var rotation = Quaternion.LookRotation(forward, Vector3.up);
            var halfWidth = HandlingCourseLayout.RoadWidth * 0.5f;
            var postOffset = halfWidth + 1.3f;

            var root = new GameObject(name);
            root.layer = DecorLayer;
            MarkStatic(root);
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(position, rotation);

            for (var side = -1; side <= 1; side += 2)
            {
                CreateBox(
                    root.transform,
                    "Post",
                    new Vector3(side * postOffset, 2.6f, 0f),
                    Quaternion.identity,
                    new Vector3(0.32f, 5.2f, 0.32f),
                    materials.Metal);
            }

            CreateBox(
                root.transform,
                "Beam",
                new Vector3(0f, 5.05f, 0f),
                Quaternion.identity,
                new Vector3((postOffset * 2f) + 0.32f, 0.34f, 0.34f),
                materials.Metal);

            CreateBox(
                root.transform,
                "Banner",
                new Vector3(0f, 4.45f, 0f),
                Quaternion.identity,
                new Vector3((postOffset * 2f) - 0.3f, 0.75f, 0.1f),
                bannerTextMaterial);

            // Two warm bulbs under the beam read as event lighting without real-time lights.
            for (var side = -1; side <= 1; side += 2)
            {
                CreateBox(
                    root.transform,
                    "Lamp",
                    new Vector3(side * (halfWidth * 0.55f), 4.86f, 0.14f),
                    Quaternion.identity,
                    new Vector3(0.22f, 0.1f, 0.18f),
                    materials.Headlight);
            }
        }

        private static void CreateRoadsideRocks(Transform parent, Vector3[] centerline, SliceMaterialSet materials)
        {
            var random = new System.Random(DecorSeed);
            var root = new GameObject("Roadside Rocks");
            root.layer = DecorLayer;
            MarkStatic(root);
            root.transform.SetParent(parent, false);

            const int count = 26;
            for (var i = 0; i < count; i++)
            {
                var distance = HandlingCourseLayout.TotalLength * (0.03f + (0.94f * (float)random.NextDouble()));
                var position = HandlingCourseLayout.SampleAtDistance(centerline, distance, out var tangent);
                var forward = Flatten(tangent);
                var right = Right(forward);
                var side = random.NextDouble() < 0.5 ? -1f : 1f;
                var lateral = (HandlingCourseLayout.RoadWidth * 0.5f) + 12f + ((float)random.NextDouble() * 48f);

                var rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rock.name = "Rock " + i;
                rock.layer = DecorLayer;
                MarkStatic(rock);
                rock.transform.SetParent(root.transform, false);
                rock.transform.position = position + (right * (side * lateral)) + (Vector3.up * -0.25f);
                rock.transform.rotation = Quaternion.Euler(
                    (float)random.NextDouble() * 12f,
                    (float)random.NextDouble() * 360f,
                    (float)random.NextDouble() * 12f);
                rock.transform.localScale = new Vector3(
                    2.2f + ((float)random.NextDouble() * 3.4f),
                    1.1f + ((float)random.NextDouble() * 1.8f),
                    2.2f + ((float)random.NextDouble() * 3.4f));
                rock.GetComponent<MeshRenderer>().sharedMaterial = materials.Rock;
                UnityEngine.Object.DestroyImmediate(rock.GetComponent<Collider>());
            }
        }

        private static void CreateDunes(Transform parent, Vector3[] centerline, SliceMaterialSet materials)
        {
            var random = new System.Random(DecorSeed + 1);
            var root = new GameObject("Distant Dunes");
            root.layer = DecorLayer;
            MarkStatic(root);
            root.transform.SetParent(parent, false);

            const int count = 8;
            for (var i = 0; i < count; i++)
            {
                var distance = HandlingCourseLayout.TotalLength * (0.05f + (0.9f * (float)random.NextDouble()));
                var position = HandlingCourseLayout.SampleAtDistance(centerline, distance, out var tangent);
                var forward = Flatten(tangent);
                var right = Right(forward);
                var side = random.NextDouble() < 0.5 ? -1f : 1f;
                var lateral = 150f + ((float)random.NextDouble() * 120f);

                var dune = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                dune.name = "Dune " + i;
                dune.layer = DecorLayer;
                MarkStatic(dune);
                dune.transform.SetParent(root.transform, false);
                dune.transform.position = position + (right * (side * lateral)) + (Vector3.down * 7f);
                dune.transform.rotation = Quaternion.Euler(0f, (float)random.NextDouble() * 360f, 0f);
                dune.transform.localScale = new Vector3(
                    70f + ((float)random.NextDouble() * 70f),
                    16f + ((float)random.NextDouble() * 14f),
                    55f + ((float)random.NextDouble() * 55f));
                dune.GetComponent<MeshRenderer>().sharedMaterial = materials.Rock;
                UnityEngine.Object.DestroyImmediate(dune.GetComponent<Collider>());
            }
        }

        private static GameObject CreateBox(
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.layer = DecorLayer;
            MarkStatic(box);
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = localRotation;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());
            return box;
        }

        private static Mesh BuildEdgeLineMesh(Vector3[] points)
        {
            var vertices = new Vector3[(points.Length - 1) * 8];
            var triangles = new int[(points.Length - 1) * 12];
            var halfWidth = HandlingCourseLayout.RoadWidth * 0.5f;
            var yOffset = 0.012f;

            for (var i = 0; i < points.Length - 1; i++)
            {
                var forward0 = Flatten(points[i + 1] - points[i]);
                var forward1 = forward0;
                var right0 = Right(forward0);
                var right1 = Right(forward1);

                for (var side = -1; side <= 1; side += 2)
                {
                    var inner0 = points[i] + (right0 * (side * (halfWidth - 0.55f))) + (Vector3.up * yOffset);
                    var outer0 = points[i] + (right0 * (side * (halfWidth - 0.18f))) + (Vector3.up * yOffset);
                    var inner1 = points[i + 1] + (right1 * (side * (halfWidth - 0.55f))) + (Vector3.up * yOffset);
                    var outer1 = points[i + 1] + (right1 * (side * (halfWidth - 0.18f))) + (Vector3.up * yOffset);

                    var index = (i * 8) + ((side < 0 ? 0 : 4));
                    vertices[index] = inner0;
                    vertices[index + 1] = outer0;
                    vertices[index + 2] = inner1;
                    vertices[index + 3] = outer1;

                    var triangle = (i * 12) + ((side < 0 ? 0 : 6));
                    triangles[triangle] = index;
                    triangles[triangle + 1] = index + 2;
                    triangles[triangle + 2] = index + 1;
                    triangles[triangle + 3] = index + 1;
                    triangles[triangle + 4] = index + 2;
                    triangles[triangle + 5] = index + 3;
                }
            }

            var mesh = new Mesh { name = "HandlingCourseEdgeLines" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SaveMesh(Mesh mesh, string name)
        {
            var path = SliceTextureKit.GeneratedFolder + "/" + name + ".asset";
            UnityEditor.Unwrapping.GenerateSecondaryUVSet(mesh);

            if (UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
            }

            UnityEditor.AssetDatabase.CreateAsset(mesh, path);
        }

        private static void MarkStatic(GameObject target)
        {
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(
                target,
                UnityEditor.StaticEditorFlags.ContributeGI |
                UnityEditor.StaticEditorFlags.BatchingStatic |
                UnityEditor.StaticEditorFlags.OccluderStatic |
                UnityEditor.StaticEditorFlags.OccludeeStatic);
        }

        private static Vector3 Flatten(Vector3 value)
        {
            value.y = 0f;
            return value.sqrMagnitude < 0.000001f ? Vector3.forward : value.normalized;
        }

        private static Vector3 Right(Vector3 forward)
        {
            var right = Vector3.Cross(Vector3.up, forward);
            return right.sqrMagnitude < 0.000001f ? Vector3.right : right.normalized;
        }
    }
}
#endif
