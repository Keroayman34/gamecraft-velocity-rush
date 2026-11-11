using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Immutable, reference-only view of one roster entry for the Garage foundation (T-8.9).
    /// </summary>
    /// <remarks>
    /// The struct copies only value fields and existing string references from the definition, so
    /// building the catalog allocates once and navigation never allocates. Stats are presentation
    /// copies of the physics profile, never a second authored source (D-035).
    /// </remarks>
    public readonly struct GarageVehicleSummary
    {
        public GarageVehicleSummary(
            string vehicleId,
            string displayNameEn,
            string displayNameAr,
            VehicleClass vehicleClass,
            float mass,
            float motorTorque,
            float forwardGrip,
            float sidewaysGrip,
            float brakeBiasFront,
            float steeringAngle)
        {
            VehicleId = vehicleId;
            DisplayNameEn = displayNameEn;
            DisplayNameAr = displayNameAr;
            Class = vehicleClass;
            Mass = mass;
            MotorTorque = motorTorque;
            ForwardGrip = forwardGrip;
            SidewaysGrip = sidewaysGrip;
            BrakeBiasFront = brakeBiasFront;
            SteeringAngle = steeringAngle;
        }

        public string VehicleId { get; }
        public string DisplayNameEn { get; }
        public string DisplayNameAr { get; }
        public VehicleClass Class { get; }
        public float Mass { get; }
        public float MotorTorque { get; }
        public float ForwardGrip { get; }
        public float SidewaysGrip { get; }
        public float BrakeBiasFront { get; }
        public float SteeringAngle { get; }

        /// <summary>Projects a validated definition into a Garage summary.</summary>
        public static GarageVehicleSummary FromDefinition(VehicleDefinition definition)
        {
            var profile = definition.PhysicsProfile;
            return new GarageVehicleSummary(
                definition.VehicleId,
                definition.DisplayNameEn,
                definition.DisplayNameAr,
                definition.Class,
                profile.Mass,
                profile.MotorTorque,
                profile.ForwardFrictionStiffness,
                profile.SidewaysFrictionStiffness,
                profile.BrakeBiasFront,
                profile.SteeringAngle);
        }
    }
}
