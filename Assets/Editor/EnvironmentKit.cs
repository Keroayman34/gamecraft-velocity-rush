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
            int landmarkCount,
            int waterCount,
            Mesh roadMesh,
            Mesh barrierMesh,
            Mesh edgeLineMesh,
            Mesh skirtMesh)
        {
            Centerline = centerline;
            Length = length;
            DecorationCount = decorationCount;
            LandmarkCount = landmarkCount;
            WaterCount = waterCount;
            RoadMesh = roadMesh;
            BarrierMesh = barrierMesh;
            EdgeLineMesh = edgeLineMesh;
            SkirtMesh = skirtMesh;
        }

        /// <summary>Sampled centerline used for spawning and decorations.</summary>
        public Vector3[] Centerline { get; }

        /// <summary>Authored centerline length in meters.</summary>
        public float Length { get; }

        /// <summary>Number of deterministic decoration roots created.</summary>
        public int DecorationCount { get; }

        /// <summary>Number of theme landmark groups created.</summary>
        public int LandmarkCount { get; }

        /// <summary>Number of water presentation planes created (Coastal only).</summary>
        public int WaterCount { get; }

        /// <summary>Saved road mesh.</summary>
        public Mesh RoadMesh { get; }

        /// <summary>Saved barrier mesh.</summary>
        public Mesh BarrierMesh { get; }

        /// <summary>Saved edge-line mesh.</summary>
        public Mesh EdgeLineMesh { get; }

        /// <summary>Saved road skirt mesh (visual only; keeps elevated roads grounded).</summary>
        public Mesh SkirtMesh { get; }
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
            var skirtMesh = SaveMesh(
                BuildSkirtMesh(centerline, layout.RoadWidth, -0.45f),
                meshFolder + "/" + id + "_Skirt.asset");

            // Coastal palms use a purpose-built low-poly frond mesh. A Unity sphere frond is
            // 768 triangles, which alone pushed the coastal scene past the 180k render budget.
            Mesh frondMesh = null;
            if (definition.Theme == EnvironmentTheme.CoastalCircuit)
            {
                frondMesh = SaveMesh(
                    BuildPalmFrondMesh(),
                    meshFolder + "/" + id + "_PalmFrond.asset");
            }

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

            // The skirt is presentation-only: it hides the gap between an elevated road ribbon and
            // the flat ground plane and never adds a collider that could change handling (D-057).
            var skirt = new GameObject("Road Skirt");
            skirt.layer = 0;
            MarkStatic(skirt);
            skirt.transform.SetParent(trackRoot, false);
            skirt.AddComponent<MeshFilter>().sharedMesh = skirtMesh;
            skirt.AddComponent<MeshRenderer>().sharedMaterial = materials.GroundSecondary;

            CreateGround(trackRoot, centerline, materials.GroundPrimary);
            var decorationCount = Decorate(trackRoot, definition, centerline, materials, frondMesh);
            var landmarkCount = CreateLandmarks(trackRoot, definition, centerline, materials);
            var waterCount = definition.Theme == EnvironmentTheme.CoastalCircuit
                ? CreateWater(trackRoot, centerline, materials)
                : 0;
            if (definition.Theme == EnvironmentTheme.NeonCity)
            {
                CreateNeonLaneMarkers(trackRoot, centerline, layout, materials);
            }

            return new EnvironmentTrackResult(
                centerline,
                layout.TotalLength,
                decorationCount,
                landmarkCount,
                waterCount,
                roadMesh,
                barrierMesh,
                edgeLineMesh,
                skirtMesh);
        }

        /// <summary>
        /// Adds deterministic, non-colliding theme dressing. The same definition always produces
        /// the same placement because the seed is derived only from the stable environment id.
        /// </summary>
        public static int Decorate(
            Transform trackRoot,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials,
            Mesh palmFrondMesh = null)
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
                        DecorateCoastal(root, position, rotation, materials, random, palmFrondMesh);
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
            System.Random random,
            Mesh palmFrondMesh)
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
                if (palmFrondMesh != null)
                {
                    AddMeshPrimitive(
                        palm.transform,
                        "Frond",
                        palmFrondMesh,
                        new Vector3(0f, 4.75f, 0f),
                        Quaternion.Euler(12f, angle, 0f),
                        new Vector3(0.8f, 0.8f, 2.4f),
                        materials.Accent);
                }
                else
                {
                    AddPrimitive(
                        palm.transform,
                        "Frond",
                        PrimitiveType.Sphere,
                        new Vector3(0f, 4.75f, 0f),
                        Quaternion.Euler(12f, angle, 0f),
                        new Vector3(2.4f, 0.22f, 0.75f),
                        materials.Accent);
                }
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

        /// <summary>
        /// Builds the theme landmark language under <c>Track/Landmarks</c>. Every landmark is
        /// collider-free and offset outside the barrier line, so it can never occlude a gate or
        /// change vehicle handling (D-057).
        /// </summary>
        private static int CreateLandmarks(
            Transform parent,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            var root = new GameObject("Landmarks").transform;
            root.SetParent(parent, false);
            var random = new System.Random(StableHash(definition.EnvironmentId + "-landmarks"));
            switch (definition.Theme)
            {
                case EnvironmentTheme.DesertCircuit:
                    CreateDesertLandmarks(root, definition, centerline, materials);
                    break;
                case EnvironmentTheme.CoastalCircuit:
                    CreateCoastalLandmarks(root, definition, centerline, materials);
                    break;
                case EnvironmentTheme.MountainPass:
                    CreateMountainLandmarks(root, definition, centerline, materials);
                    break;
                case EnvironmentTheme.NeonCity:
                    CreateNeonLandmarks(root, definition, centerline, materials, random);
                    break;
                case EnvironmentTheme.WorldArena:
                    CreateArenaLandmarks(root, definition, centerline, materials);
                    break;
            }

            return root.childCount;
        }

        private static void CreateDesertLandmarks(
            Transform root,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            var fractions = new[] { 0.14f, 0.36f, 0.58f, 0.82f };
            for (var i = 0; i < fractions.Length; i++)
            {
                var side = i % 2 == 0 ? 1 : -1;
                var position = SampleTrack(definition, centerline, fractions[i], out var forward);
                var right = Right(forward);
                var offset = 42f + (i * 7f);
                var height = 12f + (i * 3.5f);
                var radius = 11f + (i * 1.8f);

                var mesa = new GameObject("Mesa " + i);
                mesa.transform.SetParent(root, false);
                mesa.transform.SetPositionAndRotation(
                    position + (right * (side * offset)),
                    Quaternion.Euler(0f, 16f * i, 0f));

                AddPrimitive(
                    mesa.transform,
                    "Mesa Skirt",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 0.5f, 0f),
                    Quaternion.identity,
                    new Vector3(radius * 2.6f, 0.5f, radius * 2.6f),
                    materials.GroundSecondary);
                AddPrimitive(
                    mesa.transform,
                    "Mesa Body",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, height * 0.5f, 0f),
                    Quaternion.identity,
                    new Vector3(radius * 2f, height * 0.5f, radius * 2f),
                    materials.GroundSecondary);
                AddPrimitive(
                    mesa.transform,
                    "Mesa Cap",
                    PrimitiveType.Cylinder,
                    new Vector3(0f, height + 0.35f, 0f),
                    Quaternion.identity,
                    new Vector3(radius * 1.6f, 0.35f, radius * 1.6f),
                    materials.Accent);
            }
        }

        private static void CreateCoastalLandmarks(
            Transform root,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            var position = SampleTrack(definition, centerline, 0.52f, out var forward);
            var right = Right(forward);
            var lighthouse = new GameObject("Lighthouse");
            lighthouse.transform.SetParent(root, false);
            lighthouse.transform.SetPositionAndRotation(
                position + (right * 36f),
                Quaternion.Euler(0f, 25f, 0f));

            AddPrimitive(
                lighthouse.transform,
                "Lighthouse Base",
                PrimitiveType.Cylinder,
                new Vector3(0f, 1.2f, 0f),
                Quaternion.identity,
                new Vector3(7.2f, 1.2f, 7.2f),
                materials.GroundPrimary);
            AddPrimitive(
                lighthouse.transform,
                "Lighthouse Tower",
                PrimitiveType.Cylinder,
                new Vector3(0f, 10.5f, 0f),
                Quaternion.identity,
                new Vector3(4f, 9.3f, 4f),
                materials.GroundPrimary);

            for (var i = 0; i < 3; i++)
            {
                AddPrimitive(
                    lighthouse.transform,
                    "Lighthouse Stripe " + i,
                    PrimitiveType.Cylinder,
                    new Vector3(0f, 5.5f + (i * 5f), 0f),
                    Quaternion.identity,
                    new Vector3(4.3f, 0.7f, 4.3f),
                    materials.Accent);
            }

            AddPrimitive(
                lighthouse.transform,
                "Lighthouse Gallery",
                PrimitiveType.Cylinder,
                new Vector3(0f, 19.7f, 0f),
                Quaternion.identity,
                new Vector3(5.3f, 0.35f, 5.3f),
                materials.Barrier);
            AddPrimitive(
                lighthouse.transform,
                "Lighthouse Lamp",
                PrimitiveType.Sphere,
                new Vector3(0f, 21.2f, 0f),
                Quaternion.identity,
                new Vector3(2.6f, 2.6f, 2.6f),
                materials.Emissive);

            var seaMark = new GameObject("Sea Markers");
            seaMark.transform.SetParent(root, false);
            for (var i = 0; i < 4; i++)
            {
                var markerPosition = SampleTrack(definition, centerline, 0.18f + (i * 0.2f), out var markerForward);
                var markerRight = Right(markerForward);
                AddPrimitive(
                    seaMark.transform,
                    "Sea Marker " + i,
                    PrimitiveType.Cylinder,
                    markerPosition + (markerRight * ((i % 2 == 0 ? 1 : -1) * 52f)) + new Vector3(0f, 0.9f, 0f),
                    Quaternion.identity,
                    new Vector3(1.1f, 0.9f, 1.1f),
                    materials.Emissive);
            }
        }

        private static void CreateMountainLandmarks(
            Transform root,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            var fractions = new[] { 0.1f, 0.24f, 0.4f, 0.56f, 0.72f, 0.88f };
            for (var i = 0; i < fractions.Length; i++)
            {
                var position = SampleTrack(definition, centerline, fractions[i], out var forward);
                var right = Right(forward);
                var rotation = Quaternion.LookRotation(forward, Vector3.up);
                for (var sideIndex = -1; sideIndex <= 1; sideIndex += 2)
                {
                    var height = 9f + ((i % 3) * 4.5f) + (sideIndex > 0 ? 2.5f : 0f);
                    var offset = 18f + (sideIndex > 0 ? 3f : 0f);
                    var cliff = new GameObject("Cliff " + i + (sideIndex > 0 ? " R" : " L"));
                    cliff.transform.SetParent(root, false);
                    cliff.transform.SetPositionAndRotation(
                        position + (right * (sideIndex * offset)),
                        rotation);
                    AddPrimitive(
                        cliff.transform,
                        "Cliff Wall",
                        PrimitiveType.Cube,
                        new Vector3(0f, height * 0.5f, 0f),
                        Quaternion.identity,
                        new Vector3(9f + (i * 1.2f), height, 7f),
                        materials.GroundSecondary);
                    AddPrimitive(
                        cliff.transform,
                        "Cliff Snow Cap",
                        PrimitiveType.Cube,
                        new Vector3(0f, height + 0.25f, 0f),
                        Quaternion.identity,
                        new Vector3(9.2f + (i * 1.2f), 0.5f, 7.2f),
                        materials.GroundPrimary);
                }
            }
        }

        private static void CreateNeonLandmarks(
            Transform root,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials,
            System.Random random)
        {
            var fractions = new[] { 0.1f, 0.22f, 0.35f, 0.5f, 0.65f, 0.78f, 0.9f };
            for (var i = 0; i < fractions.Length; i++)
            {
                var side = i % 2 == 0 ? -1 : 1;
                var position = SampleTrack(definition, centerline, fractions[i], out var forward);
                var right = Right(forward);
                var offset = 30f + ((float)random.NextDouble() * 16f);
                var width = 6f + ((float)random.NextDouble() * 5f);
                var depth = 6f + ((float)random.NextDouble() * 5f);
                var height = 28f + ((float)random.NextDouble() * 28f);
                var rotation = Quaternion.LookRotation(forward, Vector3.up);

                CreatePrimitive(
                    root,
                    "Neon Spire " + i,
                    PrimitiveType.Cube,
                    position + (right * (side * offset)) + new Vector3(0f, height * 0.5f, 0f),
                    Quaternion.Euler(0f, i * 11f, 0f),
                    new Vector3(width, height, depth),
                    materials.Barrier);
                CreatePrimitive(
                    root,
                    "Neon Spire Sign " + i,
                    PrimitiveType.Cube,
                    position + (right * (side * offset)) +
                    new Vector3(0f, height * 0.68f, 0f) -
                    (rotation * new Vector3(0f, 0f, (depth * 0.5f) + 0.08f)),
                    rotation,
                    new Vector3(width * 0.8f, height * 0.24f, 0.12f),
                    materials.Emissive);
                CreatePrimitive(
                    root,
                    "Neon Spire Beacon " + i,
                    PrimitiveType.Sphere,
                    position + (right * (side * offset)) + new Vector3(0f, height + 0.9f, 0f),
                    rotation,
                    new Vector3(1.4f, 1.4f, 1.4f),
                    materials.Emissive);
            }
        }

        private static void CreateArenaLandmarks(
            Transform root,
            EnvironmentDefinition definition,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
        {
            var layout = definition.TrackLayout;

            // The start arch frames the road without entering the drivable corridor: the legs sit
            // outside the barriers and the beam clears the vehicle by more than 6 m.
            var archPosition = SampleTrack(definition, centerline, 0.04f, out var archForward);
            var archRotation = Quaternion.LookRotation(archForward, Vector3.up);
            var legOffset = (layout.RoadWidth * 0.5f) + 4.5f;
            var arch = new GameObject("Arena Arch");
            arch.transform.SetParent(root, false);
            arch.transform.SetPositionAndRotation(archPosition, archRotation);
            for (var side = -1; side <= 1; side += 2)
            {
                AddPrimitive(
                    arch.transform,
                    "Arch Leg " + (side > 0 ? "R" : "L"),
                    PrimitiveType.Cube,
                    new Vector3(side * legOffset, 4.6f, 0f),
                    Quaternion.identity,
                    new Vector3(1.1f, 9.2f, 1.1f),
                    materials.Barrier);
            }

            AddPrimitive(
                arch.transform,
                "Arch Beam",
                PrimitiveType.Cube,
                new Vector3(0f, 9.4f, 0f),
                Quaternion.identity,
                new Vector3((legOffset * 2f) + 1.2f, 1.2f, 1.1f),
                materials.Barrier);
            AddPrimitive(
                arch.transform,
                "Arch Banner",
                PrimitiveType.Cube,
                new Vector3(0f, 8.5f, 0.5f),
                Quaternion.identity,
                new Vector3(layout.RoadWidth + 2f, 0.9f, 0.1f),
                materials.Emissive);

            var grandstandFractions = new[] { 0.22f, 0.5f, 0.78f };
            for (var i = 0; i < grandstandFractions.Length; i++)
            {
                var position = SampleTrack(definition, centerline, grandstandFractions[i], out var forward);
                var right = Right(forward);
                for (var side = -1; side <= 1; side += 2)
                {
                    var stand = new GameObject("Grandstand " + i + (side > 0 ? " R" : " L"));
                    stand.transform.SetParent(root, false);
                    stand.transform.SetPositionAndRotation(
                        position + (right * (side * 24f)),
                        Quaternion.LookRotation(forward, Vector3.up));
                    AddPrimitive(
                        stand.transform,
                        "Grandstand Body",
                        PrimitiveType.Cube,
                        new Vector3(0f, 3.2f, 0f),
                        Quaternion.identity,
                        new Vector3(28f, 6.4f, 12f),
                        materials.Barrier);
                    AddPrimitive(
                        stand.transform,
                        "Grandstand Roof",
                        PrimitiveType.Cube,
                        new Vector3(0f, 7.1f, 0f),
                        Quaternion.identity,
                        new Vector3(29f, 0.7f, 12.6f),
                        materials.Accent);
                    AddPrimitive(
                        stand.transform,
                        "Grandstand Lights",
                        PrimitiveType.Cube,
                        new Vector3(0f, 6.4f, -6.1f),
                        Quaternion.identity,
                        new Vector3(26f, 0.35f, 0.2f),
                        materials.Emissive);
                }
            }
        }

        private static int CreateWater(
            Transform parent,
            Vector3[] centerline,
            EnvironmentMaterialSet materials)
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

            var water = new GameObject("Water").transform;
            water.SetParent(parent, false);
            var centerZ = (min.z + max.z) * 0.5f;
            var length = (max.z - min.z) + 900f;
            var margin = (max.x - min.x) + 140f;

            for (var side = -1; side <= 1; side += 2)
            {
                var plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
                plane.name = side < 0 ? "Ocean West" : "Ocean East";
                plane.layer = 0;
                MarkStatic(plane);
                plane.transform.SetParent(water, false);
                plane.transform.position = new Vector3(
                    side < 0 ? min.x - margin - 240f : max.x + margin + 240f,
                    -0.32f,
                    centerZ);
                plane.transform.localScale = new Vector3(48f, 1f, length / 10f);
                plane.GetComponent<MeshRenderer>().sharedMaterial = materials.Accent;
                Object.DestroyImmediate(plane.GetComponent<Collider>());
            }

            return water.childCount;
        }

        private static int CreateNeonLaneMarkers(
            Transform parent,
            Vector3[] centerline,
            TrackLayout layout,
            EnvironmentMaterialSet materials)
        {
            var markers = new GameObject("Neon Lane Markers").transform;
            markers.SetParent(parent, false);
            var count = 0;
            var stride = 9f;
            var lift = Vector3.up * 0.045f;
            for (var distance = 6f; distance < layout.TotalLength - 6f; distance += stride)
            {
                var position = layout.SampleAtDistance(centerline, distance, out var tangent);
                var forward = Flatten(tangent);
                var rotation = Quaternion.LookRotation(forward, Vector3.up);
                AddPrimitive(
                    markers,
                    "Lane Dash " + count,
                    PrimitiveType.Cube,
                    position + lift,
                    rotation,
                    new Vector3(0.18f, 0.03f, 1.5f),
                    materials.Emissive);
                count++;
            }

            return count;
        }

        private static Vector3 SampleTrack(
            EnvironmentDefinition definition,
            Vector3[] centerline,
            float fraction,
            out Vector3 forward)
        {
            var distance = Mathf.Clamp01(fraction) * Mathf.Max(0f, definition.TrackLayout.TotalLength - 1f);
            var position = definition.TrackLayout.SampleAtDistance(centerline, distance, out var tangent);
            forward = Flatten(tangent);
            return position;
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
            ground.layer = TrackLayer;
            MarkStatic(ground);
            ground.transform.SetParent(parent, false);
            ground.transform.position = center;
            ground.transform.localScale = new Vector3(extent / 10f, 1f, extent / 10f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = material;

            // The ground keeps its MeshCollider on the track layer so a vehicle that leaves the
            // road still has a surface. It is terrain, not a grip/hazard feature (D-057).
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

        private static void AddMeshPrimitive(
            Transform parent,
            string name,
            Mesh mesh,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            var go = new GameObject(name);
            go.layer = 0;
            MarkStatic(go);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = localScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
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

        private static Mesh BuildSkirtMesh(Vector3[] points, float roadWidth, float bottomY)
        {
            var vertices = new List<Vector3>(points.Length * 4);
            var uvs = new List<Vector2>(points.Length * 4);
            var triangles = new List<int>((points.Length - 1) * 12);
            var halfWidth = roadWidth * 0.5f;

            for (var side = -1; side <= 1; side += 2)
            {
                var start = vertices.Count;
                for (var i = 0; i < points.Length; i++)
                {
                    var right = Right(TangentAt(points, i));
                    var edge = points[i] + (right * (side * halfWidth));
                    vertices.Add(edge);
                    vertices.Add(new Vector3(edge.x, Mathf.Min(bottomY, edge.y - 0.02f), edge.z));
                    uvs.Add(new Vector2(0f, i));
                    uvs.Add(new Vector2(1f, i));
                }

                for (var i = 0; i < points.Length - 1; i++)
                {
                    var baseIndex = start + (i * 2);
                    // Both windings are authored so the skirt reads from either side of the track
                    // with the Built-in Standard shader's back-face culling.
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex + 2);

                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 3);
                    triangles.Add(baseIndex + 1);
                    triangles.Add(baseIndex);
                    triangles.Add(baseIndex + 2);
                    triangles.Add(baseIndex + 3);
                }
            }

            var mesh = new Mesh { name = "EnvironmentRoadSkirt" };
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

        /// <summary>
        /// Builds a double-sided, drooping four-segment palm frond (16 triangles) along local +Z.
        /// Used instead of a 768-triangle sphere so five fronds per palm stay inside the triangle
        /// budget; the silhouette (tapered leaf with tip droop) is closer to a real frond.
        /// </summary>
        private static Mesh BuildPalmFrondMesh()
        {
            const int segments = 4;
            var vertices = new List<Vector3>((segments + 1) * 2);
            var uvs = new List<Vector2>((segments + 1) * 2);
            var triangles = new List<int>(segments * 12);
            for (var i = 0; i <= segments; i++)
            {
                var t = i / (float)segments;
                var halfWidth = Mathf.Lerp(0.16f, 0.02f, t);
                var y = -0.34f * t * t;
                var z = t;
                vertices.Add(new Vector3(-halfWidth, y, z));
                vertices.Add(new Vector3(halfWidth, y, z));
                uvs.Add(new Vector2(0f, t));
                uvs.Add(new Vector2(1f, t));
            }

            for (var i = 0; i < segments; i++)
            {
                var a = i * 2;
                var b = a + 1;
                var c = a + 2;
                var d = a + 3;
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(b);
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(d);
            }

            var mesh = new Mesh { name = "PalmFrond" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
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
