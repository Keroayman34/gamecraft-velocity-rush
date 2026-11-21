using System;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Gameplay.Traffic
{
    /// <summary>
    /// Lightweight kinematic traffic vehicle operating in a pre-warmed object pool (Phase 12 Stage 1; TDD §58).
    /// Advances along an assigned highway lane on the track path with zero per-frame heap allocations.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class TrafficVehicle : MonoBehaviour
    {
        [SerializeField] private BoxCollider boxCollider;
        [SerializeField] private Rigidbody body;

        private TrafficLaneManager _manager;

        public int VehicleId { get; private set; }
        public int LaneIndex { get; private set; }
        public float TrackDistance { get; private set; }
        public float Speed { get; private set; }
        public bool IsActive { get; private set; }
        public bool HasCollidedWithPlayer { get; set; }
        public bool HasNearMissTriggered { get; set; }

        public float Length => boxCollider != null ? boxCollider.size.z : 4.2f;
        public float Width => boxCollider != null ? boxCollider.size.x : 1.8f;
        public float Height => boxCollider != null ? boxCollider.size.y : 1.4f;

        private void Awake()
        {
            EnsureComponents();
        }

        public void EnsureComponents()
        {
            if (boxCollider == null)
            {
                boxCollider = GetComponent<BoxCollider>();
                if (boxCollider == null)
                {
                    boxCollider = gameObject.AddComponent<BoxCollider>();
                }
            }

            boxCollider.size = new Vector3(1.8f, 1.4f, 4.2f);
            boxCollider.center = new Vector3(0f, 0.7f, 0f);

            if (body == null)
            {
                body = GetComponent<Rigidbody>();
                if (body == null)
                {
                    body = gameObject.AddComponent<Rigidbody>();
                }
            }

            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Initialize(int vehicleId, TrafficLaneManager manager)
        {
            VehicleId = vehicleId;
            _manager = manager;
            EnsureComponents();
            Deactivate();
        }

        public void Spawn(int laneIndex, float trackDistance, float speed, Vector3 initialPosition, Quaternion initialRotation)
        {
            LaneIndex = laneIndex;
            TrackDistance = trackDistance;
            Speed = speed;
            IsActive = true;
            HasCollidedWithPlayer = false;
            HasNearMissTriggered = false;

            transform.position = initialPosition;
            transform.rotation = initialRotation;
            gameObject.SetActive(true);
        }

        public void Advance(float deltaSeconds, TrackPath path, float lateralOffset)
        {
            if (!IsActive || path == null)
            {
                return;
            }

            TrackDistance += Speed * deltaSeconds;

            var sample = path.SampleAtDistance(TrackDistance, lateralOffset);
            transform.position = sample.Position;
            if (sample.Tangent.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(sample.Tangent, Vector3.up);
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            HasCollidedWithPlayer = false;
            HasNearMissTriggered = false;
            gameObject.SetActive(false);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsActive || _manager == null)
            {
                return;
            }

            var player = collision.gameObject.GetComponentInParent<VehicleController>();
            if (player != null)
            {
                Vector3 contactPoint = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                _manager.HandleTrafficCollision(this, player, contactPoint);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive || _manager == null)
            {
                return;
            }

            var player = other.GetComponentInParent<VehicleController>();
            if (player != null)
            {
                _manager.HandleTrafficCollision(this, player, other.ClosestPoint(transform.position));
            }
        }
    }
}
