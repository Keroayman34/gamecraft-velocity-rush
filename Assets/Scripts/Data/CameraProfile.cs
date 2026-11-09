using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Temporary tuning data for the T5.3 chase-camera prototype (TDD §38.4; D-035 template).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every value here is an <b>engineering placeholder</b> for the first third-person camera.
    /// None of it is final, tuned, or approved: camera perspective is OQ-01/OQ-A06, camera feel is
    /// gated by the D-023 playtest, and the presentation design in TDD §38 deliberately invents no
    /// numbers. Do not cite these values as design targets.
    /// </para>
    /// <para>
    /// The asset is a read-only definition template (D-035): runtime code must never mutate it.
    /// <see cref="TryValidate"/> is the single validation seam used by the rig and tests.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Camera Profile", fileName = "CameraProfile")]
    public sealed class CameraProfile : ScriptableObject
    {
        private const string LogPrefix = "[VelocityRush.CameraProfile] ";

        [Header("Follow (placeholder — not final tuning)")]
        [SerializeField, Min(0.01f)] private float followDistance = 7f;
        [SerializeField, Min(0f)] private float followHeight = 2.5f;
        [SerializeField, Min(0f)] private float positionSmoothing = 0.18f;
        [SerializeField, Min(0f)] private float rotationSmoothing = 0.12f;
        [SerializeField, Min(0f)] private float lookAheadPerSpeed = 0.15f;

        [Header("Field of view (placeholder — not final tuning)")]
        [SerializeField, Range(1f, 179f)] private float fieldOfViewAtRest = 60f;
        [SerializeField, Range(1f, 179f)] private float fieldOfViewAtTopSpeed = 72f;
        [SerializeField, Min(0.01f)] private float fieldOfViewSpeedReference = 60f;

        [Header("Collision (placeholder — not final tuning)")]
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.3f;
        [SerializeField, Min(0f)] private float collisionPadding = 0.2f;
        [SerializeField, Min(0.01f)] private float minimumDistance = 2f;

        /// <summary>Unobstructed distance behind the follow pivot, in meters. Placeholder.</summary>
        public float FollowDistance => followDistance;

        /// <summary>Height of the follow pivot above the target origin, in meters. Placeholder.</summary>
        public float FollowHeight => followHeight;

        /// <summary>SmoothDamp smooth time for camera position, in seconds. Placeholder.</summary>
        public float PositionSmoothing => positionSmoothing;

        /// <summary>Critically damped rotation spring time, in seconds. Placeholder.</summary>
        public float RotationSmoothing => rotationSmoothing;

        /// <summary>Look-ahead offset per meter/second of speed, in seconds. Placeholder.</summary>
        public float LookAheadPerSpeed => lookAheadPerSpeed;

        /// <summary>Field of view in degrees when stationary. Placeholder.</summary>
        public float FieldOfViewAtRest => fieldOfViewAtRest;

        /// <summary>Field of view in degrees at or above the speed reference. Placeholder.</summary>
        public float FieldOfViewAtTopSpeed => fieldOfViewAtTopSpeed;

        /// <summary>Speed in m/s at which the FOV ramp reaches <see cref="FieldOfViewAtTopSpeed"/>.</summary>
        public float FieldOfViewSpeedReference => fieldOfViewSpeedReference;

        /// <summary>Sphere-cast probe radius, in meters. Placeholder.</summary>
        public float CollisionRadius => collisionRadius;

        /// <summary>Safety margin kept between the probe and geometry, in meters. Placeholder.</summary>
        public float CollisionPadding => collisionPadding;

        /// <summary>Closest the camera may be pulled toward the pivot, in meters. Placeholder.</summary>
        public float MinimumDistance => minimumDistance;

        /// <summary>
        /// Checks that every serialized value is finite and inside its physically meaningful range.
        /// Returns false with a field-specific description on the first problem found.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (!IsPositive(followDistance))
            {
                return Fail("FollowDistance", "must be finite and greater than 0", followDistance, out error);
            }

            if (!IsFinite(followHeight) || followHeight < 0f)
            {
                return Fail("FollowHeight", "must be finite and at least 0", followHeight, out error);
            }

            if (!IsFinite(positionSmoothing) || positionSmoothing < 0f)
            {
                return Fail("PositionSmoothing", "must be finite and at least 0", positionSmoothing, out error);
            }

            if (!IsFinite(rotationSmoothing) || rotationSmoothing < 0f)
            {
                return Fail("RotationSmoothing", "must be finite and at least 0", rotationSmoothing, out error);
            }

            if (!IsFinite(lookAheadPerSpeed) || lookAheadPerSpeed < 0f)
            {
                return Fail("LookAheadPerSpeed", "must be finite and at least 0", lookAheadPerSpeed, out error);
            }

            if (!IsFinite(fieldOfViewAtRest) || fieldOfViewAtRest <= 1f || fieldOfViewAtRest >= 179f)
            {
                return Fail("FieldOfViewAtRest", "must be finite and inside (1, 179) degrees", fieldOfViewAtRest, out error);
            }

            if (!IsFinite(fieldOfViewAtTopSpeed) || fieldOfViewAtTopSpeed <= 1f || fieldOfViewAtTopSpeed >= 179f)
            {
                return Fail("FieldOfViewAtTopSpeed", "must be finite and inside (1, 179) degrees", fieldOfViewAtTopSpeed, out error);
            }

            if (fieldOfViewAtTopSpeed < fieldOfViewAtRest)
            {
                return Fail("FieldOfViewAtTopSpeed", "must be at least FieldOfViewAtRest", fieldOfViewAtTopSpeed, out error);
            }

            if (!IsPositive(fieldOfViewSpeedReference))
            {
                return Fail("FieldOfViewSpeedReference", "must be finite and greater than 0", fieldOfViewSpeedReference, out error);
            }

            if (!IsPositive(collisionRadius))
            {
                return Fail("CollisionRadius", "must be finite and greater than 0", collisionRadius, out error);
            }

            if (!IsFinite(collisionPadding) || collisionPadding < 0f)
            {
                return Fail("CollisionPadding", "must be finite and at least 0", collisionPadding, out error);
            }

            if (!IsPositive(minimumDistance))
            {
                return Fail("MinimumDistance", "must be finite and greater than 0", minimumDistance, out error);
            }

            if (minimumDistance > followDistance)
            {
                return Fail("MinimumDistance", "must not exceed FollowDistance", minimumDistance, out error);
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            // Authoring-time guard: surface invalid values in the inspector immediately. The rig
            // still validates at Awake because assets can change after import.
            if (!TryValidate(out var error))
            {
                Debug.LogWarning(LogPrefix + error, this);
            }
        }

        private static bool Fail(string field, string requirement, float value, out string error)
        {
            error = "CameraProfile: " + field + " " + requirement + " (was " + value + ").";
            return false;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static bool IsPositive(float value)
        {
            return IsFinite(value) && value > 0f;
        }
    }
}
