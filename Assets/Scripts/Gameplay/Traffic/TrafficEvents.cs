using UnityEngine;

namespace VelocityRush.Gameplay.Traffic
{
    /// <summary>
    /// Zero-allocation event payload for traffic collision occurrences (Phase 12 Stage 1).
    /// </summary>
    public readonly struct TrafficCollisionEvent
    {
        public int VehicleId { get; }
        public int LaneIndex { get; }
        public float TrafficSpeed { get; }
        public float PlayerSpeed { get; }
        public Vector3 ContactPoint { get; }

        public TrafficCollisionEvent(
            int vehicleId,
            int laneIndex,
            float trafficSpeed,
            float playerSpeed,
            Vector3 contactPoint)
        {
            VehicleId = vehicleId;
            LaneIndex = laneIndex;
            TrafficSpeed = trafficSpeed;
            PlayerSpeed = playerSpeed;
            ContactPoint = contactPoint;
        }
    }

    /// <summary>
    /// Zero-allocation event payload for close near-miss overtakes with traffic vehicles (Phase 12 Stage 1).
    /// </summary>
    public readonly struct NearMissEvent
    {
        public int VehicleId { get; }
        public float PassSpeed { get; }
        public float Clearance { get; }
        public int ComboCount { get; }

        public NearMissEvent(
            int vehicleId,
            float passSpeed,
            float clearance,
            int comboCount)
        {
            VehicleId = vehicleId;
            PassSpeed = passSpeed;
            Clearance = clearance;
            ComboCount = comboCount;
        }
    }

    public delegate void TrafficCollisionHandler(in TrafficCollisionEvent evt);
    public delegate void NearMissHandler(in NearMissEvent evt);
}
