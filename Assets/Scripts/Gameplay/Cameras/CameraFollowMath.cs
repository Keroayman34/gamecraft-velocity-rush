using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure, allocation-free helpers for the T5.3 chase camera (TDD §38.2/§38.3).
    /// </summary>
    /// <remarks>
    /// Kept free of scene/Unity object state so the smoothing, look-ahead, FOV ramp, rotational
    /// spring, and collision-distance rules are EditMode-testable. Feel and final magnitudes remain
    /// Phase-6 tuning surfaces (OQ-01/OQ-A06); nothing here is a final camera design.
    /// </remarks>
    public static class CameraFollowMath
    {
        private const float MinimumAngleDegrees = 0.0001f;
        private const float MinimumAngleRadians = 0.000001f;

        /// <summary>
        /// Frame-rate-independent exponential blend factor for a smoothing time in seconds.
        /// Returns 0 when time is paused and 1 when smoothing is disabled.
        /// </summary>
        public static float SmoothingFactor(float smoothingSeconds, float deltaTime)
        {
            if (!IsFinite(deltaTime) || deltaTime <= 0f)
            {
                return 0f;
            }

            if (!IsFinite(smoothingSeconds) || smoothingSeconds <= 0f)
            {
                return 1f;
            }

            return 1f - Mathf.Exp(-deltaTime / smoothingSeconds);
        }

        /// <summary>
        /// Speed-scaled look-ahead offset in meters. Negative, NaN, or infinite inputs map to 0.
        /// </summary>
        public static float LookAheadOffset(float speed, float lookAheadPerSpeed)
        {
            if (!IsFinite(speed) || speed <= 0f)
            {
                return 0f;
            }

            if (!IsFinite(lookAheadPerSpeed) || lookAheadPerSpeed <= 0f)
            {
                return 0f;
            }

            return speed * lookAheadPerSpeed;
        }

        /// <summary>
        /// Linear field-of-view ramp: rest FOV at standstill and top-speed FOV at or above
        /// <paramref name="speedReference"/>. Invalid speed inputs return the rest FOV.
        /// </summary>
        public static float FieldOfView(
            float speed,
            float fieldOfViewAtRest,
            float fieldOfViewAtTopSpeed,
            float speedReference)
        {
            var rest = IsFinite(fieldOfViewAtRest) ? fieldOfViewAtRest : 0f;

            if (!IsFinite(fieldOfViewAtTopSpeed)
                || !IsFinite(speed)
                || speed <= 0f
                || !IsFinite(speedReference)
                || speedReference <= 0f)
            {
                return rest;
            }

            var blend = speed >= speedReference ? 1f : speed / speedReference;
            return rest + ((fieldOfViewAtTopSpeed - rest) * blend);
        }

        /// <summary>
        /// Advances a quaternion toward <paramref name="target"/> with a critically damped angular
        /// spring and no overshoot. Paused time holds the current orientation; zero smoothing snaps.
        /// </summary>
        /// <param name="current">Current smoothed orientation.</param>
        /// <param name="target">Desired orientation.</param>
        /// <param name="angularVelocity">Persistent angular velocity state in world space (rad/s).</param>
        /// <param name="smoothingSeconds">Approximate time to reach the target.</param>
        /// <param name="deltaTime">Frame delta in seconds; non-positive freezes the spring.</param>
        public static Quaternion CriticallyDampedRotation(
            Quaternion current,
            Quaternion target,
            ref Vector3 angularVelocity,
            float smoothingSeconds,
            float deltaTime)
        {
            if (!IsFinite(deltaTime) || deltaTime <= 0f)
            {
                // Freeze on pause: hold both the orientation and its velocity so resuming does
                // not replay an oscillation that built up before the pause.
                return current;
            }

            if (!IsFinite(smoothingSeconds) || smoothingSeconds <= 0f)
            {
                angularVelocity = Vector3.zero;
                return target;
            }

            var error = target * Quaternion.Inverse(current);
            if (error.w < 0f)
            {
                // q and -q encode the same orientation; pick the shortest arc.
                error = new Quaternion(-error.x, -error.y, -error.z, -error.w);
            }

            error.ToAngleAxis(out var angleDegrees, out var axis);

            if (!IsFinite(angleDegrees) || angleDegrees < MinimumAngleDegrees)
            {
                angularVelocity = Vector3.zero;
                return current;
            }

            if (angleDegrees > 180f)
            {
                angleDegrees -= 360f;
            }

            var errorVector = axis * (angleDegrees * Mathf.Deg2Rad);
            var step = CriticallyDampedStep(errorVector, ref angularVelocity, smoothingSeconds, deltaTime);
            var stepMagnitude = step.magnitude;

            if (stepMagnitude < MinimumAngleRadians)
            {
                return current;
            }

            // The step is expressed in world space, so pre-multiply the current orientation.
            return Quaternion.AngleAxis(stepMagnitude * Mathf.Rad2Deg, step / stepMagnitude) * current;
        }

        /// <summary>
        /// One step of Unity's critically damped spring integration applied to an error vector.
        /// Kept explicit so the rotation spring is deterministic and testable in EditMode.
        /// </summary>
        private static Vector3 CriticallyDampedStep(
            Vector3 error,
            ref Vector3 velocity,
            float smoothingSeconds,
            float deltaTime)
        {
            var omega = 2f / smoothingSeconds;
            var x = omega * deltaTime;
            var damping = 1f / (1f + x + (0.48f * x * x) + (0.235f * x * x * x));
            var temp = (velocity - (omega * error)) * deltaTime;
            velocity = (velocity - (omega * temp)) * damping;
            return (error + temp) * damping;
        }

        /// <summary>
        /// Resolves the maximum camera distance allowed by a sphere-cast hit.
        /// </summary>
        /// <param name="desiredDistance">Unobstructed distance from pivot to desired camera position.</param>
        /// <param name="hitDistance">Sphere-cast hit distance, or a non-finite value when nothing was hit.</param>
        /// <param name="padding">Safety margin subtracted from the hit distance.</param>
        /// <param name="minimumDistance">Closest allowed camera distance.</param>
        /// <remarks>
        /// A hit always wins over the desired distance. The result never exceeds
        /// <paramref name="desiredDistance"/> so a close smoothed position cannot be pushed away.
        /// </remarks>
        public static float CollisionLimitedDistance(
            float desiredDistance,
            float hitDistance,
            float padding,
            float minimumDistance)
        {
            if (!IsFinite(desiredDistance) || desiredDistance <= 0f)
            {
                return 0f;
            }

            if (!IsFinite(hitDistance))
            {
                return desiredDistance;
            }

            var safePadding = IsFinite(padding) && padding > 0f ? padding : 0f;
            var safeMinimum = IsFinite(minimumDistance) && minimumDistance > 0f ? minimumDistance : 0f;
            var allowed = hitDistance - safePadding;

            if (allowed < safeMinimum)
            {
                allowed = safeMinimum;
            }

            if (allowed > desiredDistance)
            {
                allowed = desiredDistance;
            }

            return allowed < 0f ? 0f : allowed;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
