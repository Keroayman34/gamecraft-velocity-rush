#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.EditorTools
{
    /// <summary>
    /// Phase-8 Stage-2 roster generator (T-8.3/T-8.4). Expands the owner-approved OQ-14
    /// identities and OQ-06 handling briefs into ten <see cref="VehicleDefinition"/> assets and
    /// ten <see cref="VehiclePhysicsProfile"/> assets.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two prototypes are the baselines: <c>CandidateA_Grip</c> for Starter/Sport/Supercar
    /// and <c>CandidateB_Loose</c> for Drift. Each generated profile clones the relevant baseline
    /// and overrides the serialized fields listed in its <see cref="VehicleSpec"/>; values are
    /// engineering derivations of the owner's qualitative briefs, not independently invented
    /// identities. Balance remains OQ-06 and is validated by owner playtest in T-8.5.
    /// </para>
    /// <para>
    /// Drivetrain layout is not modelled by <see cref="VehiclePhysicsProfile"/>; the "FWD-feel"
    /// and "AWD-feel" briefs are approximated through mass, brake bias, centre of mass, grip,
    /// torque, and steering response. A true layout field is a future schema decision.
    /// </para>
    /// </remarks>
    public static class VehicleRosterStage2Builder
    {
        private const string LogPrefix = "[VelocityRush.Roster] ";
        private const string MenuPath = "Tools/GameCraft Studio/Velocity Rush/Generate Phase 8 Vehicle Roster";
        private const string GripBaselinePath = "Assets/Data/Tuning/CandidateA_Grip_Vehicle.asset";
        private const string LooseBaselinePath = "Assets/Data/Tuning/CandidateB_Loose_Vehicle.asset";

        private const float SharedLinearDamping = 0.02f;
        private const float SharedWheelRadius = 0.35f;
        private const float SharedWheelMass = 20f;
        private const float SharedSuspensionDistance = 0.2f;
        private const float SharedSuspensionTargetPosition = 0.5f;
        private const float SharedCurveEndTime = 60f;

        private static readonly VehicleSpec[] Specs =
        {
            // Starter — forgiving, lower mass, moderate torque.
            new VehicleSpec
            {
                Id = "starter-apex-v",
                DisplayNameEn = "Apex-V",
                DisplayNameAr = "أبيكس-في",
                Class = VehicleClass.Starter,
                BaselinePath = GripBaselinePath,
                Mass = 1050f,
                MotorTorque = 850f,
                BrakeTorque = 2600f,
                HandbrakeTorque = 3200f,
                SteeringAngle = 33f,
                BrakeBiasFront = 0.55f,
                ForwardFriction = 1.85f,
                SidewaysFriction = 1.90f,
                SuspensionSpring = 30000f,
                SuspensionDamper = 4000f,
                AngularDamping = 0.06f,
                CenterOfMassY = -0.30f,
                CenterOfMassZ = 0.05f,
                EngineTorqueAt60 = 0.55f,
                SteeringSensitivityAt60 = 0.68f
            },
            new VehicleSpec
            {
                Id = "starter-nova-hatch",
                DisplayNameEn = "Nova Hatch",
                DisplayNameAr = "نوفا هاتش",
                Class = VehicleClass.Starter,
                BaselinePath = GripBaselinePath,
                Mass = 1100f,
                MotorTorque = 900f,
                BrakeTorque = 2700f,
                HandbrakeTorque = 3000f,
                SteeringAngle = 32f,
                BrakeBiasFront = 0.62f,
                ForwardFriction = 1.80f,
                SidewaysFriction = 1.85f,
                SuspensionSpring = 32000f,
                SuspensionDamper = 4200f,
                AngularDamping = 0.06f,
                CenterOfMassY = -0.32f,
                CenterOfMassZ = 0.05f,
                EngineTorqueAt60 = 0.55f,
                SteeringSensitivityAt60 = 0.66f
            },

            // Sport — well-rounded, higher top speeds.
            new VehicleSpec
            {
                Id = "sport-velocity-gt",
                DisplayNameEn = "Velocity GT",
                DisplayNameAr = "فيلوسيتي جي تي",
                Class = VehicleClass.Sport,
                BaselinePath = GripBaselinePath,
                Mass = 1200f,
                MotorTorque = 1000f,
                BrakeTorque = 2500f,
                HandbrakeTorque = 3600f,
                SteeringAngle = 31f,
                BrakeBiasFront = 0.55f,
                ForwardFriction = 1.90f,
                SidewaysFriction = 1.95f,
                SuspensionSpring = 35000f,
                SuspensionDamper = 4500f,
                AngularDamping = 0.05f,
                CenterOfMassY = -0.28f,
                CenterOfMassZ = 0.05f,
                EngineTorqueAt60 = 0.55f,
                SteeringSensitivityAt60 = 0.62f
            },
            new VehicleSpec
            {
                Id = "sport-aero-coupe",
                DisplayNameEn = "Aero Coupe",
                DisplayNameAr = "إيرو كوبيه",
                Class = VehicleClass.Sport,
                BaselinePath = GripBaselinePath,
                Mass = 1300f,
                MotorTorque = 1100f,
                BrakeTorque = 2700f,
                HandbrakeTorque = 3400f,
                SteeringAngle = 30f,
                BrakeBiasFront = 0.54f,
                ForwardFriction = 1.88f,
                SidewaysFriction = 1.93f,
                SuspensionSpring = 38000f,
                SuspensionDamper = 4800f,
                AngularDamping = 0.05f,
                CenterOfMassY = -0.26f,
                CenterOfMassZ = 0.05f,
                EngineTorqueAt60 = 0.62f,
                SteeringSensitivityAt60 = 0.58f
            },
            new VehicleSpec
            {
                Id = "sport-pulse-r",
                DisplayNameEn = "Pulse R",
                DisplayNameAr = "نبض آر",
                Class = VehicleClass.Sport,
                BaselinePath = GripBaselinePath,
                Mass = 1120f,
                MotorTorque = 1150f,
                BrakeTorque = 2600f,
                HandbrakeTorque = 3600f,
                SteeringAngle = 33f,
                BrakeBiasFront = 0.56f,
                ForwardFriction = 1.92f,
                SidewaysFriction = 1.97f,
                SuspensionSpring = 34000f,
                SuspensionDamper = 4400f,
                AngularDamping = 0.05f,
                CenterOfMassY = -0.27f,
                CenterOfMassZ = 0.04f,
                EngineTorqueAt60 = 0.58f,
                SteeringSensitivityAt60 = 0.66f
            },

            // Drift — tail-happy, loose rear.
            new VehicleSpec
            {
                Id = "drift-sidewinder",
                DisplayNameEn = "Sidewinder",
                DisplayNameAr = "سايدوايندر",
                Class = VehicleClass.Drift,
                BaselinePath = LooseBaselinePath,
                Mass = 1200f,
                MotorTorque = 1100f,
                BrakeTorque = 2500f,
                HandbrakeTorque = 5200f,
                SteeringAngle = 33f,
                BrakeBiasFront = 0.40f,
                ForwardFriction = 1.35f,
                SidewaysFriction = 1.10f,
                SuspensionSpring = 35000f,
                SuspensionDamper = 4500f,
                AngularDamping = 0.04f,
                CenterOfMassY = -0.22f,
                CenterOfMassZ = 0.08f,
                EngineTorqueAt60 = 0.55f,
                SteeringSensitivityAt60 = 0.52f
            },
            new VehicleSpec
            {
                Id = "drift-kuma-dx",
                DisplayNameEn = "Kuma DX",
                DisplayNameAr = "كوما دي إكس",
                Class = VehicleClass.Drift,
                BaselinePath = LooseBaselinePath,
                Mass = 1350f,
                MotorTorque = 1250f,
                BrakeTorque = 2600f,
                HandbrakeTorque = 5600f,
                SteeringAngle = 34f,
                BrakeBiasFront = 0.38f,
                ForwardFriction = 1.40f,
                SidewaysFriction = 0.95f,
                SuspensionSpring = 33000f,
                SuspensionDamper = 4300f,
                AngularDamping = 0.03f,
                CenterOfMassY = -0.20f,
                CenterOfMassZ = 0.10f,
                EngineTorqueAt60 = 0.58f,
                SteeringSensitivityAt60 = 0.50f
            },

            // Supercar — extreme grip, massive torque, stiff suspension.
            new VehicleSpec
            {
                Id = "supercar-vortex-v10",
                DisplayNameEn = "Vortex V10",
                DisplayNameAr = "فورتكس في 10",
                Class = VehicleClass.Supercar,
                BaselinePath = GripBaselinePath,
                Mass = 1150f,
                MotorTorque = 1500f,
                BrakeTorque = 3200f,
                HandbrakeTorque = 4200f,
                SteeringAngle = 29f,
                BrakeBiasFront = 0.58f,
                ForwardFriction = 2.15f,
                SidewaysFriction = 2.25f,
                SuspensionSpring = 45000f,
                SuspensionDamper = 5500f,
                AngularDamping = 0.06f,
                CenterOfMassY = -0.32f,
                CenterOfMassZ = 0.02f,
                EngineTorqueAt60 = 0.68f,
                SteeringSensitivityAt60 = 0.55f
            },
            new VehicleSpec
            {
                Id = "supercar-eclipse",
                DisplayNameEn = "Eclipse",
                DisplayNameAr = "إكليبس",
                Class = VehicleClass.Supercar,
                BaselinePath = GripBaselinePath,
                Mass = 1180f,
                MotorTorque = 1450f,
                BrakeTorque = 3600f,
                HandbrakeTorque = 4000f,
                SteeringAngle = 28f,
                BrakeBiasFront = 0.60f,
                ForwardFriction = 2.20f,
                SidewaysFriction = 2.30f,
                SuspensionSpring = 50000f,
                SuspensionDamper = 6000f,
                AngularDamping = 0.06f,
                CenterOfMassY = -0.33f,
                CenterOfMassZ = 0.02f,
                EngineTorqueAt60 = 0.66f,
                SteeringSensitivityAt60 = 0.52f
            },
            new VehicleSpec
            {
                Id = "supercar-nebula-x",
                DisplayNameEn = "Nebula X",
                DisplayNameAr = "نيبولا إكس",
                Class = VehicleClass.Supercar,
                BaselinePath = GripBaselinePath,
                Mass = 1280f,
                MotorTorque = 1550f,
                BrakeTorque = 3300f,
                HandbrakeTorque = 4200f,
                SteeringAngle = 30f,
                BrakeBiasFront = 0.57f,
                ForwardFriction = 2.10f,
                SidewaysFriction = 2.20f,
                SuspensionSpring = 46000f,
                SuspensionDamper = 5600f,
                AngularDamping = 0.06f,
                CenterOfMassY = -0.30f,
                CenterOfMassZ = 0.03f,
                EngineTorqueAt60 = 0.72f,
                SteeringSensitivityAt60 = 0.42f
            }
        };

        /// <summary>Menu entry with an explicit overwrite confirmation.</summary>
        [MenuItem(MenuPath, false, 23)]
        public static void GenerateFromMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Generate Phase 8 vehicle roster?",
                    "This creates or replaces 10 VehicleDefinition assets and 10 VehiclePhysicsProfile assets under " +
                    VehicleRosterBuilder.RosterFolder + ". Manual tuning edits to those generated assets are lost.",
                    "Generate",
                    "Cancel"))
            {
                return;
            }

            BuildRoster(true);
        }

        /// <summary>Batch entry point; regenerates deterministically and exits non-zero on failure.</summary>
        public static void GenerateFromCommandLine()
        {
            try
            {
                BuildRoster(true);
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + "FAIL " + exception.Message);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Creates all profiles first, then the definitions that reference them.</summary>
        public static void BuildRoster(bool overwrite)
        {
            for (var i = 0; i < Specs.Length; i++)
            {
                CreateProfile(Specs[i], overwrite);
            }

            for (var i = 0; i < Specs.Length; i++)
            {
                var spec = Specs[i];
                var profilePath = GetProfilePath(spec);
                var profile = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(profilePath);
                if (profile == null)
                {
                    throw new InvalidOperationException("Generated profile is missing: " + profilePath);
                }

                VehicleRosterBuilder.CreateDefinition(
                    spec.Id,
                    spec.DisplayNameEn,
                    spec.DisplayNameAr,
                    spec.Class,
                    profile,
                    overwrite);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(LogPrefix + "Generated " + Specs.Length + " vehicle profiles and " +
                      Specs.Length + " definitions under " + VehicleRosterBuilder.RosterFolder + ".");
        }

        private static VehiclePhysicsProfile CreateProfile(VehicleSpec spec, bool overwrite)
        {
            var source = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(spec.BaselinePath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing baseline profile: " + spec.BaselinePath);
            }

            var profile = Object.Instantiate(source);
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("mass").floatValue = spec.Mass;
            serialized.FindProperty("centerOfMassOffset").vector3Value =
                new Vector3(0f, spec.CenterOfMassY, spec.CenterOfMassZ);
            serialized.FindProperty("linearDamping").floatValue = SharedLinearDamping;
            serialized.FindProperty("angularDamping").floatValue = spec.AngularDamping;
            serialized.FindProperty("wheelRadius").floatValue = SharedWheelRadius;
            serialized.FindProperty("wheelMass").floatValue = SharedWheelMass;
            serialized.FindProperty("suspensionDistance").floatValue = SharedSuspensionDistance;
            serialized.FindProperty("suspensionSpring").floatValue = spec.SuspensionSpring;
            serialized.FindProperty("suspensionDamper").floatValue = spec.SuspensionDamper;
            serialized.FindProperty("suspensionTargetPosition").floatValue = SharedSuspensionTargetPosition;
            serialized.FindProperty("forwardFrictionStiffness").floatValue = spec.ForwardFriction;
            serialized.FindProperty("sidewaysFrictionStiffness").floatValue = spec.SidewaysFriction;
            serialized.FindProperty("motorTorque").floatValue = spec.MotorTorque;
            serialized.FindProperty("brakeTorque").floatValue = spec.BrakeTorque;
            serialized.FindProperty("handbrakeTorque").floatValue = spec.HandbrakeTorque;
            serialized.FindProperty("steeringAngle").floatValue = spec.SteeringAngle;
            serialized.FindProperty("brakeBiasFront").floatValue = spec.BrakeBiasFront;
            serialized.FindProperty("engineTorqueCurve").animationCurveValue =
                AnimationCurve.Linear(0f, 1f, SharedCurveEndTime, spec.EngineTorqueAt60);
            serialized.FindProperty("steeringSpeedSensitivity").animationCurveValue =
                AnimationCurve.Linear(0f, 1f, SharedCurveEndTime, spec.SteeringSensitivityAt60);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            if (!profile.TryValidate(out var validationError))
            {
                Object.DestroyImmediate(profile);
                throw new InvalidOperationException("Generated profile '" + spec.Id + "' is invalid: " +
                                                    validationError);
            }

            var folder = GetVehicleFolder(spec);
            var path = GetProfilePath(spec);
            var existing = AssetDatabase.LoadAssetAtPath<VehiclePhysicsProfile>(path);
            if (existing != null && !overwrite)
            {
                Object.DestroyImmediate(profile);
                throw new InvalidOperationException(
                    "A vehicle profile already exists at " + path + "; pass overwrite to replace it.");
            }

            VehicleRosterBuilder.EnsureFolder(folder);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }

            profile.name = spec.Id + "_Physics";
            AssetDatabase.CreateAsset(profile, path);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static string GetVehicleFolder(VehicleSpec spec)
        {
            return VehicleRosterBuilder.RosterFolder + "/" +
                   VehicleRosterRules.GetClassFolderName(spec.Class) + "/" + spec.Id;
        }

        private static string GetProfilePath(VehicleSpec spec)
        {
            return GetVehicleFolder(spec) + "/" + spec.Id + "_Physics.asset";
        }

        private sealed class VehicleSpec
        {
            public string Id;
            public string DisplayNameEn;
            public string DisplayNameAr;
            public VehicleClass Class;
            public string BaselinePath;
            public float Mass;
            public float MotorTorque;
            public float BrakeTorque;
            public float HandbrakeTorque;
            public float SteeringAngle;
            public float BrakeBiasFront;
            public float ForwardFriction;
            public float SidewaysFriction;
            public float SuspensionSpring;
            public float SuspensionDamper;
            public float AngularDamping;
            public float CenterOfMassY;
            public float CenterOfMassZ;
            public float EngineTorqueAt60;
            public float SteeringSensitivityAt60;
        }
    }
}
#endif
