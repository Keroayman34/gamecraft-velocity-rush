#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>Result of building one environment's track and dressing.</summary>
    public readonly struct EnvironmentTrackResult
    {
        public EnvironmentTrackResult(
            Vector3[] centerline,
            float length,
            int decorationCount,
            Mesh roadMesh,
            Mesh barrierMesh,
            Mesh edgeLineMesh)
        {
            Centerline = centerline;
            Length = length;
            DecorationCount = decorationCount;
            RoadMesh = roadMesh;
            BarrierMesh = barrierMesh;
            EdgeLineMesh = edgeLineMesh;
        }

        /// <summary>Sampled centerline used for spawning and decorations.</summary>
        public Vector3[] Centerline { get; }

        /// <summary>Authored centerline length in meters.</summary>
        public float Length { get; }

        /// <summary>Number of deterministic decoration roots created.</summary>
        public int DecorationCount { get; }

        /// <summary>Saved road mesh.</summary>
        public Mesh RoadMesh { get; }

        /// <summary>Saved barrier mesh.</summary>
        public Mesh BarrierMesh { get; }

        /// <summary>Saved edge-line mesh.</summary>
        public Mesh EdgeLineMesh { get; }
    }

    /// <summary>
    /// Shared environment kit: per-theme road, edge lines, barriers, ground, and static
    /// decoration (T-9.3, D-054/D-055).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every decoration is an engine primitive with its collider removed, so the kit can never
    /// change vehicle handling. Grip and hazards are explicitly presentation-only (D-057).
    /// </para>
    /// <para>
    /// Static flags intentionally exclude <c>ContributeGI</c>: Phase 9 is realtime-only (D-056), so
    /// the generated track has no baked-GI dependency or lightmap UV2 requirement.
    /// </para>
    /// </remarks>
    public static class EnvironmentKit
    {
        /// <summary>Driving/track layer used by the slice and prototype scenes.</summary>
        public const int TrackLayer = 8;

        /// <summary>Edge-line ribbon width in meters.</summary>
        public const float EdgeLineWidth = 0.18f;

        /// <summary>Edge-line inset from the road edge in meters.</summary>
        public const float EdgeLineInset = 0.35f;

        /// <summary>
        /// Builds the road, barriers, edge lines, ground, and deterministic theme dressing under
        /// <paramref name="environmentRoot"/>.
        /// </summary>
        public static EnvironmentTrackResult BuildTrack(
            Transform environmentRoot,
            EnvironmentDefinition definition,
            EnvironmentMaterialSet materials,
            string meshFolder,
            EnvironmentLayoutVariant variant)
        {
            if (environmentRoot == null)
            {
                throw new ArgumentNullException(nameof(environmentRoot));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            if (!definition.TryValidate(out var definitionError))
            {
                throw new InvalidOperationException("Environment definition is invalid: " + definitionError);
            }

            if (string.IsNullOrEmpty(meshFolder))
            {
                throw new ArgumentException("Mesh folder must not be empty.", nameof(meshFolder));
            }

            EnvironmentMaterialKit.EnsureFolder(meshFolder);

            var layout = definition.TrackLayout;
            var centerline = layout.BuildCenterline(layout.SampleSpacing, variant);
            var id = definition.EnvironmentId;

            var roadMesh = SaveMesh(
                BuildRoadMesh(centerline, layout.RoadWidth, id + "_Road"),
                meshFolder + "/" + id + "_Road.asset");
            var barrierMesh = SaveMesh(
                BuildBarrierMesh(centerline, layout.RoadWidth, layout.BarrierHeight, layout.BarrierThickness),
                meshFolder + "/" + id + "_Barrier.asset");
            var edgeLineMesh = SaveMesh(
                BuildEdgeLineMesh(centerline, layout.RoadWidth),
                meshFolder + "/" + id + "_EdgeLines.asset");

            var trackRoot = new GameObject("Track").transform;
            trackRoot.SetParent(environmentRoot, false);

            var road = new GameObject("Road");
            road.layer = TrackLayer;
            MarkStatic(road);
            road.transform.SetParent(trackRoot, false);
            road.AddComponent<MeshFilter>().sharedMesh = roadMesh;
            road.AddComponent<MeshRenderer>().sharedMaterial = materials.RoadSurface;
            road.AddComponent<MeshCollider>().sharedMesh = roadMesh;

            var barriers = new GameObject("Barriers");
            barriers.layer = TrackLayer;
            MarkStatic(barriers);
            barriers.transform.SetParent(trackRoot, false);
            barriers.AddComponent<MeshFilter>().sharedMesh = barrierMesh;
            barriers.AddComponent<MeshRenderer>().sharedMaterial = materials.Barrier;
            barriers.AddComponent<MeshCollider>().sharedMesh = barrierMesh;

            var edgeLines = new GameObject("Track Edge Lines");
            edgeLines.layer = TrackLayer;
            MarkStatic(edgeLines);
            edgeLines.transform.SetParent(trackRoot, false);
            edgeLines.AddComponent<MeshFilter>().sharedMesh = edgeLineMesh;
            edgeLines.AddComponent<MeshRenderer>().sharedMaterial = materials.RoadEdge;

            CreateGround(trackRoot, centerline, materials.GroundPrimary);
            var decorationCount = Decorate(trackRoot, definition, centerline, materials);

            return new EnvironmentTrackResult(
                centerline,
                layout.TotalLength,
                decorationCount,
                roadMesh,
                barrierMesh,
                edgeLineMesh);
        }

        /// <summary>
        /// Adds deterministic, non-colliding theme dressing. The same definition always produces
        /// the same placement because the seed is derived only from the stable environment id.
        /// </summary>
        public static int Decorate(
            Transform trackRoot,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            if (trackRoot == null)
            {
                throw new ArgumentNullException(nameof(trackRoot));
            }

            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (centerline == null || centerline.Length < 2)
            {
                throw new ArgumentException("Centerline must contain at least two points.", nameof(centerline));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            var root = new GameObject("Decor").transform;
            root.SetParent(trackRoot, false);

            var layout = definition.TrackLayout;
            var halfWidth = layout.RoadWidth * 0.5f;
            var random = new System.Random(StableHash(definition.EnvironmentId));
            var stride = Mathf.Max(4, centerline.Length / 52);
            var count = 0;

            for (var i = stride; i < centerline.Length - stride; i += stride)
            {
                var forward = Flatten(TangentAt(centerline, i));
                var right = Right(forward);
                var side = random.Next(0, 2) == 0 ? -1 : 1;
                var offset = halfWidth + 6f + ((float)random.NextDouble() * 20f);
                var position = centerline[i] + (right * (side * offset));
                position.y = centerline[i].y;
                var yaw = ((float)random.NextDouble() * 360f);
                var rotation = Quaternion.Euler(0f, yaw, 0f);

                switch (definition.Theme)
                {
                    case EnvironmentTheme.DesertCircuit:
                        DecorateDesert(root, position, rotation, materials, random);
                        break;
                    case EnvironmentTheme.CoastalCircuit:
                        DecorateCoastal(root, position, rotation, materials, random);
                        break;
                    case EnvironmentTheme.MountainPass:
                        DecorateMountain(root, position, rotation, materials, random);
                        break;
                    case EnvironmentTheme.NeonCity:
                        DecorateNeonCity(root, position, rotation, materials, random);
                        break;
                    case EnvironmentTheme.WorldArena:
                        DecorateWorldArena(root, position, rotation, materials, random);
                        break;
                }

                count++;
            }

            return count;
        }

        private static void DecorateDesert(
            Transform root,
            Vector3 position,
            Quaternion rotation,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var rockCount = random.Next(1, 3);
            for (var i = 0; i < rockCount; i++)
            {
                var scale = new Vector3(
                    1.4f + ((float)random.NextDouble() * 3.2f),
                    0.7f + ((float)random.NextDouble() * 1.6f),
                    1.4f + ((float)random.NextDouble() * 3.2f));
                CreatePrimitive(
                    root,
                    "Desert Rock",
                    PrimitiveType.Sphere,
                    position + new Vector3(i * 2.2f, scale.y * 0.45f, 0f),
                    rotation,
                    scale,
                    materials.GroundSecondary);
            }

            if (random.Next(0, 2) == 0)
            {
                var cactus = new GameObject("Desert Cactus");
                cactus.transform.SetParent(root, false);
                cactus.transform.SetPositionAndRotation(position, rotation);
                AddPrimitive(
                    cactus.transform,
                    "Trunk",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 1.7f, 0f),
                    Quaternion.identity,
                    new Vector3(0.38f, 1.7f, 0.38f),
                    materials.GroundSecondary);
                AddPrimitive(
                    cactus.transform,
                    "Arm",
                    PrimitiveType.Cylinder,
                    new Vector3(0.55f, 2.1f, 0f),
                    Quaternion.Euler(0f, 0f, 90f),
                    new Vector3(0.22f, 0.65f, 0.22f),
                    materials.GroundSecondary);
            }
        }

        private static void DecorateCoastal(
            Transform root,
            Vector3 position,
            Quaternion rotation,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var palm = new GameObject("Coastal Palm");
            palm.transform.SetParent(root, false);
            palm.transform.SetPositionAndRotation(position, rotation);

            AddPrimitive(
                palm.transform,
                "Trunk",
                PrimitiveType.Cylinder,
                new Vector3(0f, 2.4f, 0f),
                Quaternion.identity,
                new Vector3(0.3f, 2.4f, 0.3f),
                materials.GroundSecondary);

            var frondCount = 4 + random.Next(0, 2);
            for (var i = 0; i < frondCount; i++)
            {
                var angle = (360f / frondCount) * i;
                AddPrimitive(
                    palm.transform,
                    "Frond",
                    PrimitiveType.Sphere,
                    new Vector3(0f, 4.75f, 0f),
                    Quaternion.Euler(12f, angle, 0f),
                    new Vector3(2.4f, 0.22f, 0.75f),
                    materials.Accent);
            }

            CreatePrimitive(
                root,
                "Coastal Rock",
                PrimitiveType.Sphere,
                position + new Vector3(2.4f, 0.45f, 1.2f),
                rotation,
                new Vector3(1.8f, 0.9f, 1.5f),
                materials.GroundSecondary);
        }

        private static void DecorateMountain(
            Transform root,
            Vector3 position,
            Quaternion rotation,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var pine = new GameObject("Mountain Pine");
            pine.transform.SetParent(root, false);
            pine.transform.SetPositionAndRotation(position, rotation);

            AddPrimitive(
                pine.transform,
                "Trunk",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.1f, 0f),
                Quaternion.identity,
                new Vector3(0.28f, 1.1f, 0.28f),
                materials.GroundSecondary);
            AddPrimitive(
                pine.transform,
                "Canopy Lower",
                PrimitiveType.Cylinder,
                new Vector3(0f, 2.6f, 0f),
                Quaternion.identity,
                new Vector3(1.9f, 1.5f, 1.9f),
                materials.Accent);
            AddPrimitive(
                pine.transform,
                "Canopy Middle",
                PrimitiveType.Cylinder,
                new Vector3(0f, 4.3f, 0f),
                Quaternion.identity,
                new Vector3(1.4f, 1.2f, 1.4f),
                materials.Accent);
            AddPrimitive(
                pine.transform,
                "Canopy Upper",
                PrimitiveType.Cylinder,
                new Vector3(0f, 5.8f, 0f),
                Quaternion.identity,
                new Vector3(0.9f, 0.9f, 0.9f),
                materials.Accent);

            CreatePrimitive(
                root,
                "Mountain Boulder",
                PrimitiveType.Sphere,
                position + new Vector3(-2.8f, 0.6f, 1.1f),
                rotation,
                new Vector3(2.2f, 1.2f, 2.0f),
                materials.GroundSecondary);

            if (random.Next(0, 2) == 0)
            {
                CreatePrimitive(
                    root,
                    "Snow Cap",
                    PrimitiveType.Sphere,
                    position + new Vector3(0f, 6.7f, 0f),
                    rotation,
                    new Vector3(0.8f, 0.25f, 0.8f),
                    materials.GroundPrimary);
            }
        }

        private static void DecorateNeonCity(
            Transform root,
            Vector3 position,
            Quaternion rotation,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var width = 5f + ((float)random.NextDouble() * 8f);
            var depth = 5f + ((float)random.NextDouble() * 8f);
            var height = 12f + ((float)random.NextDouble() * 30f);
            CreatePrimitive(
                root,
                "Neon Tower",
                PrimitiveType.Cube,
                position + new Vector3(0f, height * 0.5f, 0f),
                rotation,
                new Vector3(width, height, depth),
                materials.Barrier);

            var stripHeight = Mathf.Max(1f, height * 0.12f);
            CreatePrimitive(
                root,
                "Neon Sign",
                PrimitiveType.Cube,
                position + new Vector3(0f, height * 0.7f, 0f) +
                (rotation * new Vector3(0f, 0f, (depth * 0.5f) + 0.05f)),
                rotation,
                new Vector3(width * 0.7f, stripHeight, 0.08f),
                materials.Emissive);

            var lightPosition = position + (rotation * new Vector3((width * 0.5f) + 2.2f, 0f, 0f));
            AddPrimitive(
                root,
                "Light Pole",
                PrimitiveType.Cylinder,
                lightPosition + new Vector3(0f, 3.2f, 0f),
                Quaternion.identity,
                new Vector3(0.18f, 3.2f, 0.18f),
                materials.Accent);
            CreatePrimitive(
                root,
                "Light Head",
                PrimitiveType.Sphere,
                lightPosition + new Vector3(0f, 6.5f, 0f),
                Quaternion.identity,
                new Vector3(0.45f, 0.45f, 0.45f),
                materials.Emissive);
        }

        private static void DecorateWorldArena(
            Transform root,
            Vector3 position,
            Quaternion rotation,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var width = 14f + ((float)random.NextDouble() * 10f);
            CreatePrimitive(
                root,
                "Grandstand",
                PrimitiveType.Cube,
                position + new Vector3(0f, 2.4f, 0f),
                rotation,
                new Vector3(width, 4.8f, 7f),
                materials.Barrier);

            CreatePrimitive(
                root,
                "Grandstand Accent",
                PrimitiveType.Cube,
                position + new Vector3(0f, 5.2f, 0f),
                rotation,
                new Vector3(width, 0.8f, 7.2f),
                materials.Accent);

            var towerPosition = position + (rotation * new Vector3((width * 0.5f) + 3.5f, 0f, 0f));
            AddPrimitive(
                root,
                "Floodlight Tower",
                PrimitiveType.Cylinder,
                towerPosition + new Vector3(0f, 9f, 0f),
                Quaternion.identity,
                new Vector3(0.45f, 9f, 0.45f),
                materials.Barrier);
            CreatePrimitive(
                root,
                "Floodlight Head",
                PrimitiveType.Cube,
                towerPosition + new Vector3(0f, 18.2f, 0f),
                rotation,
                new Vector3(5.5f, 1.2f, 0.4f),
                materials.Emissive);
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

            var center = new Vector3((min.x + max.x) * 0.5f, -0.45f, (min.z + max.z) * 0.5f);
            var extent = Mathf.Max(max.x - min.x, max.z - min.z) + 320f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.layer = 0;
            MarkStatic(ground);
            ground.transform.SetParent(parent, false);
            ground.transform.position = center;
            ground.transform.localScale = new Vector3(extent / 10f, 1f, extent / 10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(ground.GetComponent<Collider>());
        }

        private static void CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.layer = 0;
            MarkStatic(go);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void AddPrimitive(
            Transform parent,
            string name,
            PrimitiveType primitive,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            var go = GameObject.CreatePrimitive(primitive);
            go.name = name;
            go.layer = 0;
            MarkStatic(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static void MarkStatic(GameObject target)
        {
            GameObjectUtility.SetStaticEditorFlags(
                target,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
        }

        private static Mesh BuildRoadMesh(Vector3[] points, float roadWidth, string meshName)
        {
            var vertices = new Vector3[points.Length * 2];
            var uvs = new Vector2[points.Length * 2];
            var triangles = new int[(points.Length - 1) * 6];
            var halfWidth = roadWidth * 0.5f;
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

            var mesh = new Mesh { name = meshName };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildBarrierMesh(
            Vector3[] points,
            float roadWidth,
            float barrierHeight,
            float barrierThickness)
        {
            var vertices = new List<Vector3>(points.Length * 16);
            var uvs = new List<Vector2>(points.Length * 16);
            var triangles = new List<int>(points.Length * 24);
            var halfWidth = roadWidth * 0.5f;
            var halfThickness = barrierThickness * 0.5f;
            var offset = halfWidth + halfThickness + 0.02f;
            var height = Vector3.up * barrierHeight;

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

                    AddQuad(vertices, uvs, triangles, bottom0Inner, top0Inner, top1Inner, bottom1Inner);
                    AddQuad(vertices, uvs, triangles, bottom0Outer, bottom1Outer, top1Outer, top0Outer);
                    AddQuad(vertices, uvs, triangles, top0Inner, top0Outer, top1Outer, top1Inner);
                    AddQuad(vertices, uvs, triangles, bottom0Inner, bottom1Inner, bottom1Outer, bottom0Outer);
                }
            }

            var mesh = new Mesh { name = "EnvironmentBarrier" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildEdgeLineMesh(Vector3[] points, float roadWidth)
        {
            var vertices = new List<Vector3>(points.Length * 4);
            var uvs = new List<Vector2>(points.Length * 4);
            var triangles = new List<int>((points.Length - 1) * 12);
            var offset = (roadWidth * 0.5f) - EdgeLineInset;
            var halfLine = EdgeLineWidth * 0.5f;
            var lift = Vector3.up * 0.03f;

            for (var side = -1; side <= 1; side += 2)
            {
                var start = vertices.Count;
                for (var i = 0; i < points.Length; i++)
                {
                    var forward = TangentAt(points, i);
                    var right = Right(forward);
                    var center = points[i] + (right * (side * offset)) + lift;
                    vertices.Add(center - (right * halfLine));
                    vertices.Add(center + (right * halfLine));
                    uvs.Add(new Vector2(0f, i));
                    uvs.Add(new Vector2(1f, i));
                }

                for (var i = 0; i < points.Length - 1; i++)
                {
                    var baseIndex = start + (i * 2);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            var mesh = new Mesh { name = "EnvironmentEdgeLines" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddQuad(
            List<Vector3> vertices,
            List<Vector2> uvs,
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
            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(0f, 1f));
            uvs.Add(new Vector2(1f, 1f));
            uvs.Add(new Vector2(1f, 0f));
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

        private static Mesh SaveMesh(Mesh mesh, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static int StableHash(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }

                return (int)(hash & 0x7FFFFFFF);
            }
        }
    }
}
#endif
