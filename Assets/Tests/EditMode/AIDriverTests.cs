using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using VelocityRush.Data;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-2 coverage for the kinematic AI driver, input adapter, visual rig, bounded
    /// rubber-banding, and soft push (T-10.2.1–T-10.2.6).
    /// </summary>
    public sealed class AIDriverTests
    {
        private const string ProductionRigPath =
            "Assets/Prefabs/Vehicles/Sport/sport-velocity-gt_Rig.prefab";

        private EnvironmentTestAssets _assets;
        private readonly List<GameObject> _objects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _assets = new EnvironmentTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            _assets.Dispose();
        }

        [Test]
        public void Initialize_NullPath_Throws()
        {
            var driver = CreateDriverObject();
            Assert.Throws<ArgumentNullException>(() => driver.Initialize(null));
        }

        [Test]
        public void Initialize_ClampsLateralBiasInsideTheRoad()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f, 1000f);

            Assert.That(driver.LateralBias, Is.EqualTo(path.MaxLateralOffset).Within(0.001f));

            driver.Initialize(path, 0f, -1000f);
            Assert.That(driver.LateralBias, Is.EqualTo(-path.MaxLateralOffset).Within(0.001f));
        }

        [Test]
        public void Tick_AdvancesPathDistanceAndTravelledDistance()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f);

            for (var i = 0; i < 250; i++)
            {
                driver.Tick(0.02f);
            }

            Assert.That(driver.Speed, Is.GreaterThan(1f));
            Assert.That(driver.DistanceTravelled, Is.GreaterThan(1f));
            Assert.That(driver.PathDistance, Is.GreaterThanOrEqualTo(0f));
            Assert.That(driver.PathDistance, Is.LessThan(path.TotalLength));
        }

        [Test]
        public void Tick_CommandsStayNormalizedAndFinite()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f, path.MaxLateralOffset * 0.5f);

            for (var i = 0; i < 500; i++)
            {
                driver.Tick(0.02f);
                Assert.That(float.IsNaN(driver.Throttle) || float.IsInfinity(driver.Throttle), Is.False);
                Assert.That(driver.Throttle, Is.InRange(0f, 1f));
                Assert.That(driver.Brake, Is.InRange(0f, 1f));
                Assert.That(driver.Steering, Is.InRange(-1f, 1f));
                Assert.That(driver.Handbrake, Is.False);
            }
        }

        [Test]
        public void Tick_OnAStraight_SteersEssentiallyStraight()
        {
            var path = CreateStraightPath(200f);
            var driver = CreateDriverObject();
            driver.Initialize(path, 20f);

            for (var i = 0; i < 100; i++)
            {
                driver.Tick(0.02f);
            }

            Assert.That(Mathf.Abs(driver.Steering), Is.LessThan(0.02f),
                "A straight path must not produce meaningful steering.");
        }

        [Test]
        public void Tick_OnAnOpenPath_StopsAtTheEndpoint()
        {
            var path = CreateStraightPath(60f);
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f);

            for (var i = 0; i < 2000; i++)
            {
                driver.Tick(0.02f);
            }

            Assert.That(driver.PathDistance, Is.EqualTo(path.TotalLength).Within(0.01f));
            Assert.That(driver.Speed, Is.EqualTo(0f).Within(0.001f));
            Assert.That(driver.TargetSpeed, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Tick_OnAClosedLoop_WrapsAndAccumulatesTravelledDistance()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f);

            for (var i = 0; i < 4000; i++)
            {
                driver.Tick(0.02f);
            }

            Assert.That(driver.DistanceTravelled, Is.GreaterThan(path.TotalLength));
            Assert.That(driver.PathDistance, Is.LessThan(path.TotalLength));
        }

        [Test]
        public void RubberBand_WithoutAPlayer_IsNeutral()
        {
            var path = CreateStraightPath(200f);
            var driver = CreateDriverObject();
            driver.Initialize(path, 100f);

            driver.Tick(0.02f);

            Assert.That(driver.RubberBandScale, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(driver.PlayerPathDistance, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void RubberBand_PlayerFarAhead_ClampsAtUpperBound()
        {
            var path = CreateStraightPath(400f);
            var player = CreatePlayerObject(path.SampleAtDistance(250f).Position);
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f, 0f, player.transform);

            driver.Tick(0.02f);

            Assert.That(driver.PlayerPathDistance, Is.GreaterThan(200f));
            Assert.That(driver.RubberBandScale, Is.EqualTo(1.1f).Within(0.0001f));
        }

        [Test]
        public void RubberBand_PlayerFarBehind_ClampsAtLowerBound()
        {
            var path = CreateStraightPath(400f);
            var player = CreatePlayerObject(path.SampleAtDistance(0f).Position);
            var driver = CreateDriverObject();
            driver.Initialize(path, 250f, 0f, player.transform);

            driver.Tick(0.02f);

            Assert.That(driver.PlayerPathDistance, Is.LessThan(20f));
            Assert.That(driver.RubberBandScale, Is.EqualTo(0.9f).Within(0.0001f));
        }

        [Test]
        public void RubberBand_StaysBoundedAcrossEveryTick()
        {
            var path = CreateClosedLoopPath();
            var player = CreatePlayerObject(path.SampleAtDistance(0f).Position);
            var driver = CreateDriverObject();
            driver.Initialize(path, path.TotalLength * 0.5f, 0f, player.transform);

            for (var i = 0; i < 1500; i++)
            {
                driver.Tick(0.02f);
                Assert.That(driver.RubberBandScale, Is.InRange(0.9f, 1.1f));
            }
        }

        [Test]
        public void Tick_AfterWarmup_AllocatesZeroBytes()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f, 0.4f);

            for (var i = 0; i < 300; i++)
            {
                driver.Tick(0.02f);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                driver.Tick(0.02f);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "AI Tick must allocate 0 B; measured " + allocated + " B over 5000 ticks.");
        }

        [Test]
        public void AIVehicleInput_MirrorsTheDriverCommands()
        {
            var path = CreateClosedLoopPath();
            var driver = CreateDriverObject();
            driver.Initialize(path, 0f);
            for (var i = 0; i < 50; i++)
            {
                driver.Tick(0.02f);
            }

            var adapter = driver.gameObject.AddComponent<AIVehicleInput>();
            Assert.That(adapter.TryBind(driver), Is.True);

            Assert.That(adapter.Throttle, Is.EqualTo(driver.Throttle));
            Assert.That(adapter.Brake, Is.EqualTo(driver.Brake));
            Assert.That(adapter.Steering, Is.EqualTo(driver.Steering));
            Assert.That(adapter.Handbrake, Is.False);
        }

        [Test]
        public void AIVehicleInput_WithoutADriver_IsNeutral()
        {
            var go = new GameObject("AI Input Without Driver");
            _objects.Add(go);
            var adapter = go.AddComponent<AIVehicleInput>();

            Assert.That(adapter.Throttle, Is.EqualTo(0f));
            Assert.That(adapter.Brake, Is.EqualTo(0f));
            Assert.That(adapter.Steering, Is.EqualTo(0f));
            Assert.That(adapter.Handbrake, Is.False);
            Assert.That(adapter.TryBind(null), Is.False);
        }

        [Test]
        public void VisualRig_OnProductionPrefab_DisablesEveryRealtimeShadowCaster()
        {
            var rig = CreateProductionRig();
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            var renderers = rig.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));
            for (var i = 0; i < renderers.Length; i++)
            {
                Assert.That(
                    renderers[i].shadowCastingMode,
                    Is.EqualTo(ShadowCastingMode.Off),
                    renderers[i].name + " must not cast realtime shadows.");
                Assert.That(renderers[i].receiveShadows, Is.False);
            }
        }

        [Test]
        public void VisualRig_OnProductionPrefab_DisablesWheelsAndKinematizesTheBody()
        {
            var rig = CreateProductionRig();
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            var wheels = rig.GetComponentsInChildren<WheelCollider>(true);
            Assert.That(wheels.Length, Is.EqualTo(4));
            for (var i = 0; i < wheels.Length; i++)
            {
                Assert.That(wheels[i].enabled, Is.False, wheels[i].name + " must be disabled.");
            }

            var body = rig.GetComponent<Rigidbody>();
            Assert.That(body, Is.Not.Null);
            Assert.That(body.isKinematic, Is.True);
            Assert.That(body.useGravity, Is.False);

            var colliders = rig.GetComponentsInChildren<Collider>(true);
            for (var i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] is WheelCollider)
                {
                    continue;
                }

                Assert.That(colliders[i].isTrigger, Is.True,
                    colliders[i].name + " must be a query-only trigger.");
            }
        }

        [Test]
        public void VisualRig_OnProductionPrefab_CreatesExactlyOneBlobShadow()
        {
            var rig = CreateProductionRig();
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            Assert.That(visual.BlobShadow, Is.Not.Null);

            var count = 0;
            var children = rig.GetComponentsInChildren<Transform>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (children[i].name == "AI Blob Shadow")
                {
                    count++;
                }
            }

            Assert.That(count, Is.EqualTo(1), "AI rigs own exactly one blob shadow.");
        }

        [Test]
        public void VisualRig_Apply_SpinsWheelsAndSteersOnlyTheFrontAxle()
        {
            var rig = CreateProductionRig();
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            var frontLeft = rig.transform.Find("Wheel FL Visual");
            var rearLeft = rig.transform.Find("Wheel RL Visual");
            Assert.That(frontLeft, Is.Not.Null);
            Assert.That(rearLeft, Is.Not.Null);

            var rearBefore = rearLeft.localRotation;
            var frontBefore = frontLeft.localRotation;

            visual.Apply(1f, 20f, 1f, 4f, 2f, 0f);

            Assert.That(visual.WheelSpinDegrees, Is.GreaterThan(0f));
            Assert.That(frontLeft.localRotation, Is.Not.EqualTo(frontBefore));
            Assert.That(rearLeft.localRotation, Is.Not.EqualTo(rearBefore));

            var frontForward = frontLeft.localRotation * Vector3.forward;
            Assert.That(frontForward.sqrMagnitude, Is.GreaterThan(0.9f));
        }

        [Test]
        public void VisualRig_Apply_AfterWarmup_AllocatesZeroBytes()
        {
            var rig = CreateProductionRig();
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            for (var i = 0; i < 200; i++)
            {
                visual.Apply(0.016f, 20f, 0.4f, 5f, 1f, 0f);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                visual.Apply(0.016f, 20f, 0.4f, 5f, 1f, 0f);
            }

            var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "AIVisualRig.Apply must allocate 0 B; measured " + allocated + " B.");
        }

        private AIDriver CreateDriverObject()
        {
            var go = new GameObject("AI Driver Test");
            _objects.Add(go);
            return go.AddComponent<AIDriver>();
        }

        private GameObject CreatePlayerObject(Vector3 position)
        {
            var go = new GameObject("AI Player Target Test");
            go.transform.position = position;
            _objects.Add(go);
            return go;
        }

        private GameObject CreateProductionRig()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ProductionRigPath);
            Assert.That(prefab, Is.Not.Null, "Missing production rig prefab: " + ProductionRigPath);
            var rig = UnityEngine.Object.Instantiate(prefab);
            _objects.Add(rig);
            return rig;
        }

        private TrackPath CreateClosedLoopPath()
        {
            var layout = _assets.CreateLayout(
                new[]
                {
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1),
                    TrackSegment.Arc(90f, 20f, 1)
                },
                sampleSpacing: 1f,
                roadWidth: 10f,
                closedLoop: true);
            return TrackPath.Create(layout);
        }

        private TrackPath CreateStraightPath(float length)
        {
            var layout = _assets.CreateLayout(
                new[]
                {
                    TrackSegment.Straight(length * 0.5f),
                    TrackSegment.Straight(length * 0.5f)
                },
                sampleSpacing: 1f,
                roadWidth: 10f);
            return TrackPath.Create(layout);
        }
    }
}
