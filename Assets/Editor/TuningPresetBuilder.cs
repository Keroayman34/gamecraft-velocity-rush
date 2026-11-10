#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Generates the Phase-6 tuning preset assets. Values are explicit placeholders for owner
    /// playtesting, not approved handling decisions (OQ-02 remains open).
    /// </summary>
    public static class TuningPresetBuilder
    {
        private const string TuningFolder = "Assets/Data/Tuning";
        private const string VehicleSource = "Assets/Data/PrototypeVehicleProfile.asset";
        private const string CameraSource = "Assets/Data/PrototypeCameraProfile.asset";

        private const string BaselineVehiclePath = TuningFolder + "/Baseline_Vehicle.asset";
        private const string BaselineCameraPath = TuningFolder + "/Baseline_Camera.asset";
        private const string CandidateAVehiclePath = TuningFolder + "/CandidateA_Grip_Vehicle.asset";
        private const string CandidateBVehiclePath = TuningFolder + "/CandidateB_Loose_Vehicle.asset";

        /// <summary>Menu entry for an explicit preset rebuild.</summary>
        [MenuItem("Tools/GameCraft Studio/Velocity Rush/Build Tuning Presets", false, 21)]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Rebuild tuning presets?",
                    "This overwrites Baseline/Candidate vehicle and camera presets under Assets/Data/Tuning. " +
                    "Existing manual edits to those preset assets are lost.",
                    "Rebuild",
                    "Cancel"))
            {
                return;
            }

            EnsurePresets(true);
        }

        /// <summary>Creates any missing presets without touching existing ones.</summary>
        public static void EnsurePresets()
        {
            EnsurePresets(false);
        }

        /// <summary>Creates or replaces the tuning presets.</summary>
        public static void EnsurePresets(bool overwrite)
        {
            EnsureFolder(TuningFolder);

            CreateVehicleCopy(VehicleSource, BaselineVehiclePath, "Baseline_Vehicle", null, overwrite);
            CreateCameraCopy(CameraSource, BaselineCameraPath, "Baseline_Camera", overwrite);

            CreateVehicleCopy(
                VehicleSource,
                CandidateAVehiclePath,
                "CandidateA_Grip_Vehicle",
                serialized =>
                {
                    serialized.FindProperty("forwardFrictionStiffness").floatValue = 1.9f;
                    serialized.FindProperty("sidewaysFrictionStiffness").floatValue = 1.95f;
                    serialized.FindProperty("steeringAngle").floatValue = 31f;
                    serialized.FindProperty("brakeBiasFront").floatValue = 0.55f;
                    serialized.FindProperty("handbrakeTorque").floatValue = 3600f;
                    serialized.FindProperty("centerOfMassOffset").vector3Value = new Vector3(0f, -0.28f, 0.05f);
                    serialized.FindProperty("steeringSpeedSensitivity").animationCurveValue =
                        AnimationCurve.Linear(0f, 1f, 60f, 0.62f);
                },
                overwrite);

            CreateVehicleCopy(
                VehicleSource,
                CandidateBVehiclePath,
                "CandidateB_Loose_Vehicle",
                serialized =>
                {
                    serialized.FindProperty("forwardFrictionStiffness").floatValue = 1.35f;
                    serialized.FindProperty("sidewaysFrictionStiffness").floatValue = 1.1f;
                    serialized.FindProperty("steeringAngle").floatValue = 33f;
                    serialized.FindProperty("brakeBiasFront").floatValue = 0.4f;
                    serialized.FindProperty("handbrakeTorque").floatValue = 5200f;
                    serialized.FindProperty("motorTorque").floatValue = 1100f;
                    serialized.FindProperty("angularDamping").floatValue = 0.04f;
                    serialized.FindProperty("centerOfMassOffset").vector3Value = new Vector3(0f, -0.22f, 0.08f);
                    serialized.FindProperty("steeringSpeedSensitivity").animationCurveValue =
                        AnimationCurve.Linear(0f, 1f, 60f, 0.52f);
                },
                overwrite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[VelocityRush.Tuning] Tuning presets ready under " + TuningFolder + ".");
        }

        /// <summary>Batch entry point; always refreshes the presets.</summary>
        public static void BuildFromCommandLine()
        {
            EnsurePresets(true);
            EditorApplication.Exit(0);
        }

        private static void CreateVehicleCopy(
            string sourcePath,
            string targetPath,
            string assetName,
            Action<SerializedObject> configure,
            bool overwrite)
        {
            var source = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing vehicle profile source: " + sourcePath);
            }

            if (AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(targetPath) != null)
            {
                if (!overwrite)
                {
                    return;
                }

                AssetDatabase.DeleteAsset(targetPath);
            }

            var copy = Object.Instantiate(source);
            copy.name = assetName;
            var serialized = new SerializedObject(copy);
            configure?.Invoke(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(copy, targetPath);
            EditorUtility.SetDirty(copy);
        }

        private static void CreateCameraCopy(string sourcePath, string targetPath, string assetName, bool overwrite)
        {
            var source = AssetDatabase.LoadAssetAtPath<CameraProfile>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing camera profile source: " + sourcePath);
            }

            if (AssetDatabase.LoadAssetAtPath<CameraProfile>(targetPath) != null)
            {
                if (!overwrite)
                {
                    return;
                }

                AssetDatabase.DeleteAsset(targetPath);
            }

            var copy = Object.Instantiate(source);
            copy.name = assetName;
            AssetDatabase.CreateAsset(copy, targetPath);
            EditorUtility.SetDirty(copy);
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
