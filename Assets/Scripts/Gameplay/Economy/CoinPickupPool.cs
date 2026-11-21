using System;
using UnityEngine;
using VelocityRush.Data;
using Object = UnityEngine.Object;

namespace VelocityRush.Gameplay.Economy
{
    /// <summary>
    /// Zero-allocation pre-warmed object pool for collectible road coins (Phase 12 Stage 2; TDD §59).
    /// Pre-warms <see cref="CoinSpawnConfig.poolSize"/> instances during initialization with zero runtime instantiation.
    /// </summary>
    public sealed class CoinPickupPool
    {
        private CoinPickup[] _all;
        private CoinPickup[] _available;
        private CoinPickup[] _active;
        private int _capacity;
        private int _availableCount;
        private int _activeCount;
        private Transform _parent;

        public int Capacity => _capacity;
        public int AvailableCount => _availableCount;
        public int ActiveCount => _activeCount;
        public CoinPickup[] ActiveCoins => _active;

        public void Initialize(Transform parent, CoinSpawnConfig config, GameObject prefab = null)
        {
            _capacity = Mathf.Clamp(config.poolSize, 8, 64);
            _parent = parent;

            _all = new CoinPickup[_capacity];
            _available = new CoinPickup[_capacity];
            _active = new CoinPickup[_capacity];

            _availableCount = _capacity;
            _activeCount = 0;

            for (int i = 0; i < _capacity; i++)
            {
                CoinPickup coin;
                if (prefab != null)
                {
                    var instance = Object.Instantiate(prefab, parent);
                    instance.name = $"RoadCoin_{i:D2}";
                    coin = instance.GetComponent<CoinPickup>() ?? instance.AddComponent<CoinPickup>();
                }
                else
                {
                    var go = new GameObject($"RoadCoin_{i:D2}");
                    go.transform.SetParent(parent, false);
                    coin = go.AddComponent<CoinPickup>();
                    coin.EnsureProceduralVisuals();
                }

                coin.Deactivate();
                _all[i] = coin;
                _available[i] = coin;
            }
        }

        public bool TryRent(out CoinPickup coin)
        {
            if (_availableCount <= 0)
            {
                coin = null;
                return false;
            }

            _availableCount--;
            coin = _available[_availableCount];
            _available[_availableCount] = null;

            _active[_activeCount] = coin;
            _activeCount++;
            return true;
        }

        public void Return(CoinPickup coin)
        {
            if (coin == null || !coin.IsActive)
            {
                return;
            }

            coin.Deactivate();

            // Remove from active array (swap with last active)
            for (int i = 0; i < _activeCount; i++)
            {
                if (_active[i] == coin)
                {
                    _activeCount--;
                    _active[i] = _active[_activeCount];
                    _active[_activeCount] = null;
                    break;
                }
            }

            // Return to available stack
            _available[_availableCount] = coin;
            _availableCount++;
        }

        public void ResetAll()
        {
            if (_all == null) return;

            for (int i = 0; i < _activeCount; i++)
            {
                if (_active[i] != null)
                {
                    _active[i].Deactivate();
                }
                _active[i] = null;
            }

            _activeCount = 0;
            _availableCount = _capacity;

            for (int i = 0; i < _capacity; i++)
            {
                _available[i] = _all[i];
            }
        }

        public void Teardown()
        {
            if (_all == null) return;

            for (int i = 0; i < _capacity; i++)
            {
                if (_all[i] != null && _all[i].gameObject != null)
                {
                    Object.DestroyImmediate(_all[i].gameObject);
                }
                _all[i] = null;
            }

            _all = null;
            _available = null;
            _active = null;
            _capacity = 0;
            _availableCount = 0;
            _activeCount = 0;
        }
    }
}
