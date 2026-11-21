using System;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay.Traffic;

namespace VelocityRush.Gameplay.AI
{
    /// <summary>
    /// Pure static traffic avoidance logic for AI rival lane weaving (Phase 12 Stage 3; TDD §60).
    /// Detects active traffic in the rival's current lane and calculates open adjacent lane lateral offsets (0 B/frame).
    /// </summary>
    public static class RivalTrafficAvoidance
    {
        private const float DefaultLookahead = 32f;
        private const float DefaultSafetyMargin = 8f;

        /// <summary>
        /// Evaluates whether the current lane is obstructed ahead and selects an open adjacent lane offset.
        /// </summary>
        /// <param name="currentLateralBias">Rival current lateral offset in meters.</param>
        /// <param name="rivalDistance">Rival track distance in meters.</param>
        /// <param name="trafficManager">Active traffic manager coordinating pooled traffic.</param>
        /// <param name="laneConfig">4-lane highway configuration.</param>
        /// <param name="lookaheadMeters">Distance ahead to scan for traffic.</param>
        /// <returns>Target lateral offset in meters.</returns>
        public static float EvaluateTargetLateralBias(
            float currentLateralBias,
            float rivalDistance,
            TrafficLaneManager trafficManager,
            TrafficLaneConfig laneConfig,
            float lookaheadMeters = DefaultLookahead)
        {
            if (trafficManager == null || trafficManager.Pool == null || laneConfig.laneCount <= 0)
            {
                return currentLateralBias;
            }

            int currentLane = laneConfig.GetNearestLaneIndex(currentLateralBias);
            var activeVehicles = trafficManager.Pool.ActiveVehicles;
            int activeCount = activeVehicles.Length;

            bool laneBlocked = false;
            for (int i = 0; i < activeCount; i++)
            {
                var traffic = activeVehicles[i];
                if (traffic == null || !traffic.IsActive)
                {
                    continue;
                }

                if (traffic.LaneIndex == currentLane)
                {
                    float gap = traffic.TrackDistance - rivalDistance;
                    if (gap > -DefaultSafetyMargin && gap < lookaheadMeters)
                    {
                        laneBlocked = true;
                        break;
                    }
                }
            }

            if (!laneBlocked)
            {
                // Current lane is clear; stay in current lane lateral offset
                return laneConfig.GetLaneLateralOffset(currentLane);
            }

            // Current lane is blocked; evaluate adjacent lanes (prefer inner lane, then outer)
            int leftLane = currentLane - 1;
            int rightLane = currentLane + 1;

            bool leftClear = leftLane >= 0 && IsLaneClear(leftLane, rivalDistance, activeVehicles, lookaheadMeters);
            bool rightClear = rightLane < laneConfig.laneCount && IsLaneClear(rightLane, rivalDistance, activeVehicles, lookaheadMeters);

            if (leftClear && rightClear)
            {
                // Both clear: shift towards center (lane 1 or 2)
                int preferred = currentLane > 1 ? leftLane : rightLane;
                return laneConfig.GetLaneLateralOffset(preferred);
            }
            if (leftClear)
            {
                return laneConfig.GetLaneLateralOffset(leftLane);
            }
            if (rightClear)
            {
                return laneConfig.GetLaneLateralOffset(rightLane);
            }

            // All adjacent blocked; maintain current lane
            return currentLateralBias;
        }

        private static bool IsLaneClear(
            int laneIndex,
            float rivalDistance,
            ReadOnlySpan<TrafficVehicle> vehicles,
            float lookahead)
        {
            int count = vehicles.Length;
            for (int i = 0; i < count; i++)
            {
                var traffic = vehicles[i];
                if (traffic == null || !traffic.IsActive)
                {
                    continue;
                }

                if (traffic.LaneIndex == laneIndex)
                {
                    float gap = traffic.TrackDistance - rivalDistance;
                    if (gap > -DefaultSafetyMargin && gap < lookahead)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
