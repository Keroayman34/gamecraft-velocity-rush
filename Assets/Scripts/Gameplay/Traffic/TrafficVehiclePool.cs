using System;
using UnityEngine;

namespace VelocityRush.Gameplay.Traffic
{
    /// <summary>
    /// Pre-warmed object pool for traffic vehicles (Phase 12 Stage 1; TDD §58).
    /// Enforces zero runtime instantiation/destruction allocations during the active race loop.
    /// </summary>
    public sealed class TrafficVehiclePool
    {
        private readonly TrafficVehicle[] _available;
        private readonly TrafficVehicle[] _active;
        private readonly int _capacity;
        private int _availableCount;
        private int _activeCount;
        private Transform _parentContainer;

        public int Capacity => _capacity;
        public int AvailableCount => _availableCount;
        public int ActiveCount => _activeCount;
        public ReadOnlySpan<TrafficVehicle> ActiveVehicles => new ReadOnlySpan<TrafficVehicle>(_active, 0, _activeCount);

        public TrafficVehiclePool(int capacity)
        {
            _capacity = Mathf.Clamp(capacity, 4, 64);
            _available = new TrafficVehicle[_capacity];
            _active = new TrafficVehicle[_capacity];
            _availableCount = 0;
            _activeCount = 0;
        }

        /// <summary>
        /// Pre-warms the pool with traffic vehicles.
        /// </summary>
        public void Initialize(Transform parent, TrafficLaneManager manager, GameObject customPrefab = null)
        {
            _parentContainer = parent;
            _availableCount = 0;
            _activeCount = 0;

            for (int i = 0; i < _capacity; i++)
            {
                TrafficVehicle vehicle;
                if (customPrefab != null)
                {
                    var go = UnityEngine.Object.Instantiate(customPrefab, parent);
                    vehicle = go.GetComponent<TrafficVehicle>() ?? go.AddComponent<TrafficVehicle>();
                }
                else
                {
                    vehicle = CreateProceduralTrafficVehicle(i, parent);
                }

                vehicle.Initialize(i + 1, manager);
                _available[_availableCount++] = vehicle;
            }
        }

        /// <summary>
        /// Creates a lightweight procedural visual for a traffic vehicle if no prefab is assigned.
        /// </summary>
        private static TrafficVehicle CreateProceduralTrafficVehicle(int index, Transform parent)
        {
            var root = new GameObject($"TrafficVehicle_{index + 1:D2}");
            root.transform.SetParent(parent, false);

            var vehicle = root.AddComponent<TrafficVehicle>();
            vehicle.EnsureComponents();

            // Lightweight visual chassis
            var bodyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bodyObj.name = "Chassis_Visual";
            bodyObj.transform.SetParent(root.transform, false);
            bodyObj.transform.localPosition = new Vector3(0f, 0.7f, 0f);
            bodyObj.transform.localScale = new Vector3(1.8f, 1.4f, 4.2f);

            // Strip primitive collider to avoid double collision
            var primCollider = bodyObj.GetComponent<Collider>();
            if (primCollider != null)
            {
                UnityEngine.Object.DestroyImmediate(primCollider);
            }

            return vehicle;
        }

        /// <summary>
        /// Rents a vehicle from the pool. Returns null if all vehicles are currently active.
        /// Zero allocations.
        /// </summary>
        public TrafficVehicle Rent()
        {
            if (_availableCount <= 0)
            {
                return null;
            }

            var vehicle = _available[--_availableCount];
            _active[_activeCount++] = vehicle;
            return vehicle;
        }

        /// <summary>
        /// Returns a vehicle to the pool. Zero allocations.
        /// </summary>
        public void Return(TrafficVehicle vehicle)
        {
            if (vehicle == null)
            {
                return;
            }

            // Remove from active array
            for (int i = 0; i < _activeCount; i++)
            {
                if (_active[i] == vehicle)
                {
                    // Swap with last active element
                    _active[i] = _active[--_activeCount];
                    _active[_activeCount] = null;
                    break;
                }
            }

            vehicle.Deactivate();
            _available[_availableCount++] = vehicle;
        }

        /// <summary>
        /// Returns all active vehicles to the pool cleanly without allocations.
        /// </summary>
        public void ResetAll()
        {
            while (_activeCount > 0)
            {
                var vehicle = _active[--_activeCount];
                _active[_activeCount] = null;
                vehicle.Deactivate();
                _available[_availableCount++] = vehicle;
            }
        }

        /// <summary>
        /// Destroys all pooled instances on scene teardown.
        /// </summary>
        public void Teardown()
        {
            for (int i = 0; i < _capacity; i++)
            {
                if (_available[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_available[i].gameObject);
                    _available[i] = null;
                }
                if (_active[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_active[i].gameObject);
                    _active[i] = null;
                }
            }

            _availableCount = 0;
            _activeCount = 0;
        }
    }
}
