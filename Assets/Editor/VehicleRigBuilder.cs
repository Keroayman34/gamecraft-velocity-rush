#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Builds the Phase-8 shared vehicle rig base and the ten data-driven primitive variants
    /// (T-8.6–T-8.8).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every variant is generated from one base prefab that already owns the validated physics
    /// rig: a <see cref="Rigidbody"/>, one chassis <see cref="BoxCollider"/>, four
    /// <see cref="WheelCollider"/>s, four visual wheel transforms, and the
    /// <see cref="VehicleController"/> wiring. The builder only changes the referenced
    /// <see cref="VehiclePhysicsProfile"/> plus serialized wheel parameters, and adds
    /// non-colliding primitive body parts. Collider parity is therefore exact by construction.
    /// </para>
    /// <para>
    /// Provenance is OQ-A05-approved internal-only: Unity primitives, the Built-in Standard
    /// shader, and flat procedural colors. No external mesh, texture, package, or per-vehicle
    /// script is introduced. LOD0 is the current bar (OQ-A09); LOD1 remains a later optimisation.
    /// </para>
    /// </remarks>
    public static class VehicleRigBuilder
    {
        private const string LogPrefix = "[VelocityRush.Rig] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Build Vehicle Rig Prefabs";
        private const string BasePrefabPath = "Assets/Prefabs/Vehicles/VehicleRigBase.prefab";
        private const string PrototypePrefabPath = "Assets/Prefabs/PrototypeVehicle.prefab";
        private const string MaterialFolder = "Assets/Materials/Vehicles";
        private const string PrefabFolder = "Assets/Prefabs/Vehicles";
        private const string KitPrefix = "KitPart_";

        /// <summary>Menu entry with an explicit overwrite confirmation.</summary>
        [MenuItem(MenuPath, false, 24)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Build Phase 8 vehicle rig prefabs?",
                    "This creates or replaces VehicleRigBase and the ten vehicle variant prefabs under " +
                    PrefabFolder + ". Manual prefab edits are lost.",
                    "Build",
                    "Cancel"))
            {
                return;
            }

            Build(true);
        }

        /// <summary>Batch entry point; exits non-zero on failure.</summary>
        public static void BuildFromCommandLine()
        {
            try
            {
                Build(true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "FAIL " + exception.Message);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Builds the base prefab and all ten variants.</summary>
        public static void Build(bool overwrite)
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);

            var materials = EnsureMaterials();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var basePrefab = EnsureBasePrefab(overwrite);
            var roster = LoadRosterDefinitions();
            if (roster.Length != VehicleRosterRules.RequiredVehicleCount)
            {
                throw new InvalidOperationException(
                    "Expected " + VehicleRosterRules.RequiredVehicleCount + " definitions, found " + roster.Length + ".");
            }

            for (var i = 0; i < roster.Length; i++)
            {
                BuildVariant(basePrefab, roster[i], materials, overwrite);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(LogPrefix + "Built base rig + " + roster.Length + " variants under " + PrefabFolder + ".");
        }

        private static GameObject EnsureBasePrefab(bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (existing != null && !overwrite)
            {
                return existing;
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(BasePrefabPath);
            }

            if (!AssetDatabase.CopyAsset(PrototypePrefabPath, BasePrefabPath))
            {
                throw new InvalidOperationException(
                    "Failed to copy " + PrototypePrefabPath + " to " + BasePrefabPath + ".");
            }

            AssetDatabase.ImportAsset(BasePrefabPath, ImportAssetOptions.ForceSynchronousImport);

            var basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BasePrefabPath);
            if (basePrefab == null)
            {
                throw new InvalidOperationException("Base rig prefab was not created: " + BasePrefabPath);
            }

            // The base keeps the approved CandidateA profile as a template default. Variants always
            // override it with their own definition's profile.
            basePrefab.name = "VehicleRigBase";
            return basePrefab;
        }

        private static void BuildVariant(
            GameObject basePrefab,
            VehicleDefinition definition,
            RigMaterials materials,
            bool overwrite)
        {
            if (definition == null)
            {
                throw new InvalidOperationException("Roster contains a null definition.");
            }

            if (!definition.TryValidate(out var definitionError))
            {
                throw new InvalidOperationException(
                    "Vehicle definition '" + definition.VehicleId + "' is invalid: " + definitionError);
            }

            var profile = definition.PhysicsProfile;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = definition.VehicleId;

            var controller = instance.GetComponent<VehicleController>();
            if (controller == null)
            {
                throw new InvalidOperationException("Base prefab has no VehicleController.");
            }

            SetObject(controller, "profile", profile);
            ConfigureWheels(instance, profile);
            ApplyClassKit(instance.transform, definition.Class, materials);

            if (!controller.TryValidateRig(out var rigError))
            {
                throw new InvalidOperationException(
                    "Generated rig '" + definition.VehicleId + "' is invalid: " + rigError);
            }

            var path = GetVariantPath(definition);
            EnsureFolder(GetVariantFolder(definition));

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !overwrite)
            {
                Object.DestroyImmediate(instance);
                throw new InvalidOperationException(
                    "Vehicle rig already exists at " + path + "; pass overwrite to replace it.");
            }

            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);

            Debug.Log(LogPrefix + "Created " + definition.VehicleId + " rig -> " + path);
        }

        private static void ConfigureWheels(GameObject instance, VehiclePhysicsProfile profile)
        {
            var wheels = instance.GetComponentsInChildren<WheelCollider>(true);
            if (wheels.Length != 4)
            {
                throw new InvalidOperationException(
                    "Base rig must expose exactly four WheelColliders; found " + wheels.Length + ".");
            }

            for (var i = 0; i < wheels.Length; i++)
            {
                var wheel = wheels[i];
                wheel.mass = profile.WheelMass;
                wheel.radius = profile.WheelRadius;
                wheel.suspensionDistance = profile.SuspensionDistance;

                var spring = wheel.suspensionSpring;
                spring.spring = profile.SuspensionSpring;
                spring.damper = profile.SuspensionDamper;
                spring.targetPosition = profile.SuspensionTargetPosition;
                wheel.suspensionSpring = spring;

                var forward = wheel.forwardFriction;
                forward.stiffness = profile.ForwardFrictionStiffness;
                wheel.forwardFriction = forward;

                var sideways = wheel.sidewaysFriction;
                sideways.stiffness = profile.SidewaysFrictionStiffness;
                wheel.sidewaysFriction = sideways;

                EditorUtility.SetDirty(wheel);
            }
        }

        private static void ApplyClassKit(Transform root, VehicleClass vehicleClass, RigMaterials materials)
        {
            RemoveExistingKit(root);

            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    BuildStarterKit(root, materials);
                    break;
                case VehicleClass.Sport:
                    BuildSportKit(root, materials);
                    break;
                case VehicleClass.Drift:
                    BuildDriftKit(root, materials);
                    break;
                case VehicleClass.Supercar:
                    BuildSupercarKit(root, materials);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(vehicleClass), vehicleClass, null);
            }
        }

        private static void BuildStarterKit(Transform root, RigMaterials m)
        {
            // Tall hatch cabin, short nose, upright rear: compact and forgiving.
            Box(root, "Cabin", new Vector3(0f, 0.36f, -0.25f), new Vector3(1.26f, 0.5f, 1.55f), m.Starter);
            Box(root, "CabinGlassFront", new Vector3(0f, 0.37f, 0.55f), new Vector3(1.1f, 0.3f, 0.06f), m.Glass);
            Box(root, "CabinGlassRear", new Vector3(0f, 0.37f, -1.04f), new Vector3(1.1f, 0.3f, 0.06f), m.Glass);
            Box(root, "Hood", new Vector3(0f, 0.09f, 1.3f), new Vector3(1.3f, 0.2f, 0.95f), m.Starter);
            Box(root, "RearHatch", new Vector3(0f, 0.16f, -1.52f), new Vector3(1.24f, 0.36f, 0.34f), m.Starter);
            Box(root, "FrontBumper", new Vector3(0f, -0.14f, 1.78f), new Vector3(1.44f, 0.16f, 0.16f), m.Trim);
            Box(root, "RearBumper", new Vector3(0f, -0.14f, -1.78f), new Vector3(1.4f, 0.16f, 0.16f), m.Trim);
            Box(root, "HeadlightL", new Vector3(-0.44f, 0.03f, 1.79f), new Vector3(0.28f, 0.11f, 0.04f), m.Headlight);
            Box(root, "HeadlightR", new Vector3(0.44f, 0.03f, 1.79f), new Vector3(0.28f, 0.11f, 0.04f), m.Headlight);
            Box(root, "TailLight", new Vector3(0f, 0.02f, -1.79f), new Vector3(1.02f, 0.11f, 0.04f), m.Tail);
        }

        private static void BuildSportKit(Transform root, RigMaterials m)
        {
            // Low coupe: long hood, fastback cabin, splitter and small wing.
            Box(root, "Cabin", new Vector3(0f, 0.26f, -0.15f), new Vector3(1.04f, 0.38f, 1.35f), m.Sport);
            Box(root, "CabinGlassFront", new Vector3(0f, 0.28f, 0.53f), new Vector3(0.92f, 0.26f, 0.06f), m.Glass);
            Box(root, "LongHood", new Vector3(0f, 0.06f, 1.22f), new Vector3(1.38f, 0.16f, 1.18f), m.Sport);
            Box(root, "Fastback", new Vector3(0f, 0.1f, -1.35f), new Vector3(1.12f, 0.22f, 0.72f), m.Sport);
            Box(root, "Splitter", new Vector3(0f, -0.25f, 1.83f), new Vector3(1.6f, 0.06f, 0.3f), m.Trim);
            Box(root, "Wing", new Vector3(0f, 0.5f, -1.6f), new Vector3(1.3f, 0.06f, 0.34f), m.Trim);
            Box(root, "WingSupportL", new Vector3(-0.45f, 0.37f, -1.6f), new Vector3(0.07f, 0.24f, 0.1f), m.Trim);
            Box(root, "WingSupportR", new Vector3(0.45f, 0.37f, -1.6f), new Vector3(0.07f, 0.24f, 0.1f), m.Trim);
            Box(root, "HeadlightL", new Vector3(-0.48f, 0f, 1.8f), new Vector3(0.3f, 0.1f, 0.04f), m.Headlight);
            Box(root, "HeadlightR", new Vector3(0.48f, 0f, 1.8f), new Vector3(0.3f, 0.1f, 0.04f), m.Headlight);
            Box(root, "TailLight", new Vector3(0f, 0.06f, -1.79f), new Vector3(1.06f, 0.1f, 0.04f), m.Tail);
        }

        private static void BuildDriftKit(Transform root, RigMaterials m)
        {
            // Wide fenders, tall rear wing, front lip: tail-happy silhouette.
            Box(root, "Cabin", new Vector3(0f, 0.29f, -0.12f), new Vector3(1.06f, 0.42f, 1.24f), m.Drift);
            Box(root, "CabinGlass", new Vector3(0f, 0.31f, 0.48f), new Vector3(0.94f, 0.28f, 0.06f), m.Glass);
            Box(root, "Hood", new Vector3(0f, 0.07f, 1.28f), new Vector3(1.36f, 0.16f, 1.0f), m.Drift);
            Box(root, "FenderL", new Vector3(-0.78f, 0.02f, -0.1f), new Vector3(0.16f, 0.26f, 1.7f), m.Drift);
            Box(root, "FenderR", new Vector3(0.78f, 0.02f, -0.1f), new Vector3(0.16f, 0.26f, 1.7f), m.Drift);
            Box(root, "FrontLip", new Vector3(0f, -0.24f, 1.82f), new Vector3(1.66f, 0.06f, 0.28f), m.Trim);
            Box(root, "BigWing", new Vector3(0f, 0.66f, -1.62f), new Vector3(1.66f, 0.07f, 0.42f), m.Trim);
            Box(root, "WingSupportL", new Vector3(-0.62f, 0.46f, -1.62f), new Vector3(0.08f, 0.38f, 0.12f), m.Trim);
            Box(root, "WingSupportR", new Vector3(0.62f, 0.46f, -1.62f), new Vector3(0.08f, 0.38f, 0.12f), m.Trim);
            Box(root, "HeadlightL", new Vector3(-0.46f, 0f, 1.8f), new Vector3(0.28f, 0.09f, 0.04f), m.Headlight);
            Box(root, "HeadlightR", new Vector3(0.46f, 0f, 1.8f), new Vector3(0.28f, 0.09f, 0.04f), m.Headlight);
            Box(root, "TailLight", new Vector3(0f, 0.08f, -1.79f), new Vector3(1.14f, 0.1f, 0.04f), m.Tail);
        }

        private static void BuildSupercarKit(Transform root, RigMaterials m)
        {
            // Very low wedge, side blades, large diffuser and wing: extreme-grip silhouette.
            Box(root, "Cabin", new Vector3(0f, 0.2f, -0.05f), new Vector3(0.94f, 0.3f, 1.2f), m.Supercar);
            Box(root, "CabinGlass", new Vector3(0f, 0.22f, 0.5f), new Vector3(0.84f, 0.2f, 0.06f), m.Glass);
            Box(root, "WedgeNose", new Vector3(0f, -0.02f, 1.35f), new Vector3(1.42f, 0.12f, 1.1f), m.Supercar);
            Box(root, "SideBladeL", new Vector3(-0.72f, -0.05f, -0.2f), new Vector3(0.14f, 0.2f, 1.35f), m.Trim);
            Box(root, "SideBladeR", new Vector3(0.72f, -0.05f, -0.2f), new Vector3(0.14f, 0.2f, 1.35f), m.Trim);
            Box(root, "FrontSplitter", new Vector3(0f, -0.26f, 1.84f), new Vector3(1.68f, 0.05f, 0.3f), m.Trim);
            Box(root, "Diffuser", new Vector3(0f, -0.24f, -1.82f), new Vector3(1.5f, 0.09f, 0.3f), m.Trim);
            Box(root, "RearWing", new Vector3(0f, 0.58f, -1.66f), new Vector3(1.56f, 0.06f, 0.38f), m.Trim);
            Box(root, "WingSupportL", new Vector3(-0.54f, 0.42f, -1.66f), new Vector3(0.07f, 0.3f, 0.1f), m.Trim);
            Box(root, "WingSupportR", new Vector3(0.54f, 0.42f, -1.66f), new Vector3(0.07f, 0.3f, 0.1f), m.Trim);
            Box(root, "HeadlightL", new Vector3(-0.46f, -0.02f, 1.86f), new Vector3(0.3f, 0.08f, 0.04f), m.Headlight);
            Box(root, "HeadlightR", new Vector3(0.46f, -0.02f, 1.86f), new Vector3(0.3f, 0.08f, 0.04f), m.Headlight);
            Box(root, "TailLight", new Vector3(0f, 0.04f, -1.8f), new Vector3(1.12f, 0.09f, 0.04f), m.Tail);
        }

        private static void RemoveExistingKit(Transform root)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name.StartsWith(KitPrefix, StringComparison.Ordinal))
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void Box(Transform parent, string role, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = KitPrefix + role;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;

            var collider = box.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static RigMaterials EnsureMaterials()
        {
            return new RigMaterials
            {
                Starter = EnsureMaterial("VehicleBodyStarter", new Color(0.86f, 0.66f, 0.18f), 0.2f, 0.35f),
                Sport = EnsureMaterial("VehicleBodySport", new Color(0.16f, 0.48f, 0.78f), 0.35f, 0.5f),
                Drift = EnsureMaterial("VehicleBodyDrift", new Color(0.72f, 0.14f, 0.2f), 0.25f, 0.42f),
                Supercar = EnsureMaterial("VehicleBodySupercar", new Color(0.82f, 0.84f, 0.88f), 0.55f, 0.62f),
                Trim = EnsureMaterial("VehicleTrim", new Color(0.08f, 0.09f, 0.1f), 0.2f, 0.28f),
                Glass = EnsureMaterial("VehicleGlass", new Color(0.06f, 0.1f, 0.14f), 0.7f, 0.86f),
                Headlight = EnsureMaterial("VehicleHeadlight", new Color(1f, 0.94f, 0.78f), 0.1f, 0.7f),
                Tail = EnsureMaterial("VehicleTailLight", new Color(0.8f, 0.06f, 0.06f), 0.1f, 0.6f)
            };
        }

        private static Material EnsureMaterial(string name, Color color, float metallic, float smoothness)
        {
            var path = MaterialFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("Built-in Standard shader is unavailable.");
                }

                material = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Glossiness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static VehicleDefinition[] LoadRosterDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:VehicleDefinition", new[] { VehicleRosterBuilder.RosterFolder });
            var paths = new List<string>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                paths.Add(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            paths.Sort(StringComparer.Ordinal);

            var definitions = new VehicleDefinition[paths.Count];
            for (var i = 0; i < paths.Count; i++)
            {
                var definition = AssetDatabase.LoadAssetAtPath<VehicleDefinition>(paths[i]);
                if (definition == null)
                {
                    throw new InvalidOperationException("Could not load vehicle definition: " + paths[i]);
                }

                definitions[i] = definition;
            }

            return definitions;
        }

        /// <summary>Project-relative prefab path for one vehicle.</summary>
        public static string GetVariantPath(VehicleDefinition definition)
        {
            return GetVariantFolder(definition) + "/" + definition.VehicleId + "_Rig.prefab";
        }

        private static string GetVariantFolder(VehicleDefinition definition)
        {
            return PrefabFolder + "/" + VehicleRosterRules.GetClassFolderName(definition.Class);
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

        private sealed class RigMaterials
        {
            public Material Starter;
            public Material Sport;
            public Material Drift;
            public Material Supercar;
            public Material Trim;
            public Material Glass;
            public Material Headlight;
            public Material Tail;
        }
    }
}
#endif
