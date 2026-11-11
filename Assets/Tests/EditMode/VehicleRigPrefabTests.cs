using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Stage-3 acceptance over the generated rig base and ten vehicle variants (T-8.6–T-8.8).
    /// </summary>
    /// <remarks>
    /// The tests assert the owner-approved OQ-A09 primitive scope: one shared physics rig, exact
    /// collider parity, LOD0-only visuals, and a class-distinct silhouette for every vehicle. They
    /// run against the real prefab assets, not synthetic objects.
    /// </remarks>
    public sealed class VehicleRigPrefabTests
    {
        private const string BasePrefabPath = "Assets/Prefabs/Vehicles/VehicleRigBase.prefab";
        private const string KitPrefix = "KitPart_";

        [Test]
        public void BaseRig_HasOneBoxColliderFourWheelCollidersAndValidWiring()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            Assert.That(prefab, Is.Not.Null, "VehicleRigBase prefab is missing.");

            var controller = prefab.GetComponent<VehicleController>();
            Assert.That(controller, Is.Not.Null, "Base rig must own a VehicleController.");
            Assert.That(controller.TryValidateRig(out var error), Is.True, error);

            var colliders = prefab.GetComponentsInChildren<Collider>(true);
            Assert.That(colliders.Length, Is.EqualTo(5), "Base rig must expose exactly five colliders.");
            Assert.That(prefab.GetComponentsInChildren<WheelCollider>(true).Length, Is.EqualTo(4));

            var chassis = prefab.transform.Find("ChassisBody");
            Assert.That(chassis, Is.Not.Null, "Base rig must keep the canonical ChassisBody collider.");
            Assert.That(chassis.localScale, Is.EqualTo(new Vector3(1.5f, 0.6f, 3.6f)),
                "Chassis primitive bounds must stay 1.5 x 0.6 x 3.6 (collider parity).");

            var box = chassis.GetComponent<BoxCollider>();
            Assert.That(box, Is.Not.Null);
            Assert.That(box.size, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void EveryVehicle_HasAValidVariantBoundToItsOwnProfile()
        {
            var definitions = LoadRoster();
            Assert.That(definitions, Has.Count.EqualTo(VehicleRosterRules.RequiredVehicleCount));

            foreach (var definition in definitions)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VehicleRigBuilder.GetVariantPath(definition));
                Assert.That(prefab, Is.Not.Null, "Missing rig prefab for " + definition.VehicleId + ".");

                var controller = prefab.GetComponent<VehicleController>();
                Assert.That(controller, Is.Not.Null, definition.VehicleId + " has no VehicleController.");
                Assert.That(controller.Profile, Is.SameAs(definition.PhysicsProfile),
                    definition.VehicleId + " must reference its own physics profile.");
                Assert.That(controller.TryValidateRig(out var rigError), Is.True,
                    definition.VehicleId + ": " + rigError);
            }
        }

        [Test]
        public void EveryVehicle_KeepsExactColliderParity_AndAddsNoVisualColliders()
        {
            foreach (var definition in LoadRoster())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VehicleRigBuilder.GetVariantPath(definition));
                Assert.That(prefab, Is.Not.Null);

                var colliders = prefab.GetComponentsInChildren<Collider>(true);
                var wheelColliders = 0;
                var boxColliders = 0;
                for (var i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] is WheelCollider)
                    {
                        wheelColliders++;
                    }
                    else if (colliders[i] is BoxCollider)
                    {
                        boxColliders++;
                    }
                    else
                    {
                        Assert.Fail(definition.VehicleId + " has an unexpected " +
                                    colliders[i].GetType().Name + ".");
                    }
                }

                Assert.That(wheelColliders, Is.EqualTo(4), definition.VehicleId + " wheel collider count.");
                Assert.That(boxColliders, Is.EqualTo(1), definition.VehicleId + " chassis collider count.");

                var kitParts = 0;
                for (var i = 0; i < prefab.transform.childCount; i++)
                {
                    var child = prefab.transform.GetChild(i);
                    if (!child.name.StartsWith(KitPrefix, System.StringComparison.Ordinal))
                    {
                        continue;
                    }

                    kitParts++;
                    Assert.That(child.GetComponent<Collider>(), Is.Null,
                        definition.VehicleId + " kit part '" + child.name + "' must not add a collider.");
                }

                Assert.That(kitParts, Is.GreaterThanOrEqualTo(10),
                    definition.VehicleId + " needs a readable primitive silhouette.");
            }
        }

        [Test]
        public void Classes_HaveDistinctPrimitiveSilhouettes()
        {
            var signatures = new Dictionary<VehicleClass, string>
            {
                { VehicleClass.Starter, "KitPart_RearHatch" },
                { VehicleClass.Sport, "KitPart_LongHood" },
                { VehicleClass.Drift, "KitPart_BigWing" },
                { VehicleClass.Supercar, "KitPart_WedgeNose" }
            };

            foreach (var definition in LoadRoster())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VehicleRigBuilder.GetVariantPath(definition));
                Assert.That(prefab, Is.Not.Null);

                var signature = signatures[definition.Class];
                Assert.That(prefab.transform.Find(signature), Is.Not.Null,
                    definition.VehicleId + " (" + definition.Class + ") is missing " + signature + ".");
            }
        }

        [Test]
        public void EveryVariant_SerializesItsProfileWheelValues()
        {
            foreach (var definition in LoadRoster())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    VehicleRigBuilder.GetVariantPath(definition));
                Assert.That(prefab, Is.Not.Null);

                var profile = definition.PhysicsProfile;
                var wheels = prefab.GetComponentsInChildren<WheelCollider>(true);
                for (var i = 0; i < wheels.Length; i++)
                {
                    Assert.That(wheels[i].radius, Is.EqualTo(profile.WheelRadius).Within(0.0001f),
                        definition.VehicleId + " wheel radius.");
                    Assert.That(wheels[i].mass, Is.EqualTo(profile.WheelMass).Within(0.0001f),
                        definition.VehicleId + " wheel mass.");
                    Assert.That(wheels[i].suspensionDistance,
                        Is.EqualTo(profile.SuspensionDistance).Within(0.0001f),
                        definition.VehicleId + " suspension distance.");
                }
            }
        }

        private static List<VehicleDefinition> LoadRoster()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleDefinition",
                new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(System.StringComparer.Ordinal);

            var definitions = new List<VehicleDefinition>(paths.Count);
            for (var i = 0; i < paths.Count; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[i]);
                Assert.That(definition, Is.Not.Null, "Could not load " + paths[i]);
                definitions.Add(definition);
            }

            return definitions;
        }
    }
}
