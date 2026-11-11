#if UNITY_EDITOR
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Adds the Sport-class hero presentation to an instantiated prototype vehicle entirely in the
    /// slice scene, without editing the shared prefab or any physics value (T-7.7).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The prefab's primitive rig stays the single source of physics: chassis collider, four
    /// WheelColliders, and every serialized profile value are untouched. This kit only re-skins the
    /// existing chassis/wheel renderers and adds child visuals (cabin, splitter, wing, side pods,
    /// lamps, wheel hubs). Added objects carry no colliders, so collider parity is exact.
    /// </para>
    /// <para>
    /// Because all additions are scene objects under the prefab instance root, the shared
    /// <c>PrototypeVehicle.prefab</c> asset and its Phase-5/6 tests are unaffected.
    /// </para>
    /// </remarks>
    public static class SliceHeroVehicleKit
    {
        private const string ChassisRendererName = "ChassisBody";
        private const string WheelMeshPrefix = "Wheel ";

        /// <summary>Applies hero materials and body additions to an instantiated vehicle.</summary>
        /// <param name="vehicleRoot">Instantiated prototype vehicle root.</param>
        /// <param name="materials">Shared slice material set.</param>
        public static void Apply(GameObject vehicleRoot, SliceMaterialSet materials)
        {
            if (vehicleRoot == null)
            {
                throw new ArgumentNullException(nameof(vehicleRoot));
            }

            if (materials == null)
            {
                throw new ArgumentNullException(nameof(materials));
            }

            ReskinChassis(vehicleRoot.transform, materials);
            ReskinWheels(vehicleRoot.transform, materials);
            AddBodyKit(vehicleRoot.transform, materials);
        }

        private static void ReskinChassis(Transform root, SliceMaterialSet materials)
        {
            var chassis = FindDescendant(root, ChassisRendererName);
            if (chassis != null)
            {
                var renderer = chassis.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = materials.HeroPaint;
                }
            }
        }

        private static void ReskinWheels(Transform root, SliceMaterialSet materials)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (!child.name.StartsWith(WheelMeshPrefix, StringComparison.Ordinal) ||
                    !child.name.EndsWith("Mesh", StringComparison.Ordinal))
                {
                    continue;
                }

                var renderer = child.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial = materials.Rubber;
                }

                AddWheelHub(child, materials.Chrome);
            }
        }

        private static void AddWheelHub(Transform wheel, Material material)
        {
            var hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Hero Hub";
            hub.transform.SetParent(wheel, false);
            hub.transform.localPosition = Vector3.zero;
            hub.transform.localRotation = Quaternion.identity;
            hub.transform.localScale = new Vector3(0.44f, 0.135f, 0.44f);
            hub.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(hub.GetComponent<Collider>());

            var hubCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hubCap.name = "Hero Hub Cap";
            hubCap.transform.SetParent(wheel, false);
            hubCap.transform.localPosition = Vector3.zero;
            hubCap.transform.localRotation = Quaternion.identity;
            hubCap.transform.localScale = new Vector3(0.2f, 0.145f, 0.2f);
            hubCap.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(hubCap.GetComponent<Collider>());
        }

        private static void AddBodyKit(Transform root, SliceMaterialSet materials)
        {
            // Chassis box is 1.5 x 0.6 x 3.6 centered at the root (x, y, z half extents
            // 0.75 / 0.30 / 1.80). Every part below stays inside or slightly proud of that volume.
            CreateBox(root, "Hero Cabin", new Vector3(0f, 0.36f, -0.18f), new Vector3(1.06f, 0.44f, 1.5f), materials.HeroDark);
            CreateBox(root, "Hero Cabin Glass", new Vector3(0f, 0.44f, 0.44f), new Vector3(0.94f, 0.26f, 0.5f), materials.Chrome);
            CreateBox(root, "Hero Splitter", new Vector3(0f, -0.3f, 1.83f), new Vector3(1.72f, 0.06f, 0.3f), materials.HeroDark);
            CreateBox(root, "Hero Diffuser", new Vector3(0f, -0.3f, -1.82f), new Vector3(1.6f, 0.08f, 0.28f), materials.HeroDark);
            CreateBox(root, "Hero Side Pod L", new Vector3(-0.8f, -0.04f, -0.1f), new Vector3(0.12f, 0.42f, 1.55f), materials.HeroPaint);
            CreateBox(root, "Hero Side Pod R", new Vector3(0.8f, -0.04f, -0.1f), new Vector3(0.12f, 0.42f, 1.55f), materials.HeroPaint);
            CreateBox(root, "Hero Wing Support L", new Vector3(-0.56f, 0.5f, -1.7f), new Vector3(0.08f, 0.46f, 0.14f), materials.HeroDark);
            CreateBox(root, "Hero Wing Support R", new Vector3(0.56f, 0.5f, -1.7f), new Vector3(0.08f, 0.46f, 0.14f), materials.HeroDark);
            CreateBox(root, "Hero Rear Wing", new Vector3(0f, 0.74f, -1.72f), new Vector3(1.62f, 0.07f, 0.36f), materials.HeroDark);
            CreateBox(root, "Hero Roll Hoop", new Vector3(0f, 0.58f, -0.62f), new Vector3(0.92f, 0.08f, 0.08f), materials.Chrome);

            CreateBox(root, "Hero Headlight L", new Vector3(-0.5f, -0.02f, 1.845f), new Vector3(0.3f, 0.13f, 0.05f), materials.Headlight);
            CreateBox(root, "Hero Headlight R", new Vector3(0.5f, -0.02f, 1.845f), new Vector3(0.3f, 0.13f, 0.05f), materials.Headlight);
            CreateBox(root, "Hero Tail Light", new Vector3(0f, 0.06f, -1.845f), new Vector3(1.16f, 0.13f, 0.05f), materials.TailLight);
        }

        private static void CreateBox(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(box.GetComponent<Collider>());
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            for (var i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDescendant(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
#endif
