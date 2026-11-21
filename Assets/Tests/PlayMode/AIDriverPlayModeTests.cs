using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using VelocityRush.Data;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// PlayMode proof for Stage-2 kinematic AI: full-lap completion, 0 B/frame simulation,
    /// two-car separation, capped player nudge, and blob-shadow enforcement on a real rig.
    /// </summary>
    public sealed class AIDriverPlayModeTests
    {
        private const float Step = 0.02f;
        private const string ProductionRigPath =
            "Assets/Prefabs/Vehicles/Sport/sport-velocity-gt_Rig.prefab";

        private readonly List<GameObject> _objects = new List<GameObject>();
        private readonly List<TrackLayout> _layouts = new List<TrackLayout>();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (var i = 0; i < _objects.Count; i++)
            {
                if (_objects[i] != null)
                {
                    Object.Destroy(_objects[i]);
                }
            }

            for (var i = 0; i < _layouts.Count; i++)
            {
                if (_layouts[i] != null)
                {
                    Object.Destroy(_layouts[i]);
                }
            }

            _objects.Clear();
            _layouts.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator FullLap_CompletesWithoutStallingOrLeavingThePath()
        {
            var path = CreateClosedLoopPath();
            var car = CreateAiCar("AI Full Lap");
            var driver = car.AddComponent<AIDriver>();
            driver.Initialize(path, 0f);

            var minimumSpeedAfterLaunch = float.MaxValue;
            for (var i = 0; i < 4000; i++)
            {
                driver.Tick(Step);
                if (i > 200 && driver.Speed < minimumSpeedAfterLaunch)
                {
                    minimumSpeedAfterLaunch = driver.Speed;
                }
            }

            Assert.That(driver.DistanceTravelled, Is.GreaterThan(path.TotalLength),
                "The AI must complete at least one full lap.");
            Assert.That(driver.Speed, Is.GreaterThan(1f), "The AI must not stall.");
            Assert.That(minimumSpeedAfterLaunch, Is.GreaterThan(0.5f),
                "The AI must keep rolling after launch.");
            Assert.That(driver.Steering, Is.InRange(-1f, 1f));
            Assert.That(IsOnTheRoad(path, car.transform.position), Is.True,
                "The AI left the road corridor.");

            yield return null;
        }

        [UnityTest]
        public IEnumerator SimulatingThousandsOfSteps_AllocatesZeroBytes()
        {
            var path = CreateClosedLoopPath();
            var car = CreateAiCar("AI Allocation");
            var driver = car.AddComponent<AIDriver>();
            driver.Initialize(path, 0f, 0.4f);

            for (var i = 0; i < 400; i++)
            {
                driver.Tick(Step);
            }

            yield return null;

            var before = System.GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 5000; i++)
            {
                driver.Tick(Step);
            }

            var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.EqualTo(0L),
                "AI simulation must allocate 0 B/frame; measured " + allocated + " B over 5000 ticks.");
        }

        [UnityTest]
        public IEnumerator TwoCarsSpawnedTogether_SeparateAndDoNotOverlap()
        {
            var path = CreateClosedLoopPath();
            var first = CreateAiCar("AI Separation A");
            var second = CreateAiCar("AI Separation B");

            var firstDriver = first.AddComponent<AIDriver>();
            var secondDriver = second.AddComponent<AIDriver>();
            firstDriver.Initialize(path, 40f);
            secondDriver.Initialize(path, 40f);

            for (var i = 0; i < 150; i++)
            {
                firstDriver.Tick(Step);
                secondDriver.Tick(Step);
                // Emulate the engine's physics step: queries see the poses applied last step.
                Physics.SyncTransforms();
            }

            var separation = PlanarDistance(first.transform.position, second.transform.position);
            Assert.That(separation, Is.GreaterThan(2f),
                "Two AI cars spawned on the same point must separate; measured " + separation + " m.");

            // The push must stay bounded: neither car may be flung off its path.
            Assert.That(firstDriver.PushOffset.magnitude, Is.LessThanOrEqualTo(1.5001f));
            Assert.That(secondDriver.PushOffset.magnitude, Is.LessThanOrEqualTo(1.5001f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerNudge_IsSoftAndCapped()
        {
            var path = CreateClosedLoopPath();
            var car = CreateAiCar("AI Player Nudge");
            var driver = car.AddComponent<AIDriver>();
            var player = CreatePlayerBody(path.SampleAtDistance(40f).Position);
            driver.Initialize(path, 40f, 0f, player.transform);
            Physics.SyncTransforms();

            for (var i = 0; i < 40; i++)
            {
                driver.Tick(Step);
                Physics.SyncTransforms();
            }

            var speed = player.linearVelocity.magnitude;
            Assert.That(speed, Is.GreaterThan(0f), "The player must receive a soft nudge.");
            Assert.That(speed, Is.LessThanOrEqualTo(0.65f),
                "The player nudge must stay capped; measured " + speed + " m/s.");
            Assert.That(PlanarDistance(car.transform.position, player.transform.position), Is.GreaterThan(1.5f));

            yield return null;
        }

        [UnityTest]
        public IEnumerator ProductionRig_HasNoRealtimeShadowCastersAndOneBlobShadow()
        {
            // A production rig inherits the player VehicleController; instantiating it without a
            // bound input logs one authored error before AIVisualRig disables the component.
            LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(
                    @"\[VelocityRush\.Vehicle\] No input source assigned.*"));

            var prevIgnore = LogAssert.ignoreFailingMessages;
            LogAssert.ignoreFailingMessages = true;
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ProductionRigPath);
            LogAssert.ignoreFailingMessages = prevIgnore;
            Assert.That(prefab, Is.Not.Null, "Missing production rig prefab: " + ProductionRigPath);

            var rig = Object.Instantiate(prefab);
            _objects.Add(rig);
            var visual = rig.AddComponent<AIVisualRig>();
            visual.Bind();

            yield return null;

            var renderers = rig.GetComponentsInChildren<Renderer>(true);
            Assert.That(renderers.Length, Is.GreaterThan(0));
            for (var i = 0; i < renderers.Length; i++)
            {
                Assert.That(renderers[i].shadowCastingMode, Is.EqualTo(ShadowCastingMode.Off),
                    renderers[i].name + " casts realtime shadows.");
            }

            Assert.That(visual.BlobShadow, Is.Not.Null, "The AI rig must own a blob shadow.");

            var wheels = rig.GetComponentsInChildren<WheelCollider>(true);
            for (var i = 0; i < wheels.Length; i++)
            {
                Assert.That(wheels[i].enabled, Is.False);
            }
        }

        private GameObject CreateAiCar(string name)
        {
            var car = new GameObject(name);
            car.transform.position = Vector3.zero;
            var body = car.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var collider = car.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 1.2f, 3.6f);
            collider.center = new Vector3(0f, 0.7f, 0f);
            car.AddComponent<AIVisualRig>();
            _objects.Add(car);
            return car;
        }

        private Rigidbody CreatePlayerBody(Vector3 position)
        {
            var player = new GameObject("AI Player Probe");
            player.transform.position = position;
            var body = player.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.mass = 1200f;
            var collider = player.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 1.2f, 3.6f);
            collider.center = new Vector3(0f, 0.7f, 0f);
            // The AI query also sees the player's own collider; the AI's own body is filtered.
            _objects.Add(player);
            return body;
        }

        private TrackPath CreateClosedLoopPath()
        {
            var layout = ScriptableObject.CreateInstance<TrackLayout>();
            layout.ConfigureForAuthoring(
                12f,
                0.8f,
                0.3f,
                1f,
                true,
                new[]
                {
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1),
                    TrackSegment.Arc(90f, 25f, 1)
                },
                System.Array.Empty<TrackElevationKey>());
            _layouts.Add(layout);
            return TrackPath.Create(layout);
        }

        private static bool IsOnTheRoad(TrackPath path, Vector3 position)
        {
            var best = float.MaxValue;
            for (var i = 0; i < path.Count; i++)
            {
                var center = path.WaypointAt(i).Center;
                var distance = PlanarDistance(center, position);
                if (distance < best)
                {
                    best = distance;
                }
            }

            return best <= path.MaxLateralOffset + 1.6f;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            var dx = a.x - b.x;
            var dz = a.z - b.z;
            return Mathf.Sqrt((dx * dx) + (dz * dz));
        }
    }
}
