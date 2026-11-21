using System;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure C# state machine controlling nitro boost charge, hysteresis, and depletion (Phase 12 Stage 3; TDD §60).
    /// Zero allocations, pure struct/float math, 100% EditMode-testable.
    /// </summary>
    public sealed class NitroSystem
    {
        private readonly NitroConfig _config;
        private float _currentCharge;
        private bool _isBoosting;

        /// <summary>Current raw charge units [0..maxCharge].</summary>
        public float CurrentCharge => _currentCharge;

        /// <summary>Normalized nitro charge [0..1] for UI bar display.</summary>
        public float NormalizedCharge => _config.maxCharge > 0.001f ? Mathf.Clamp01(_currentCharge / _config.maxCharge) : 0f;

        /// <summary>True if nitro boost is actively applying acceleration and speed expansion.</summary>
        public bool IsBoosting => _isBoosting;

        /// <summary>True if current charge satisfies the activation threshold hysteresis.</summary>
        public bool CanActivate => _currentCharge >= _config.minActivationCharge;

        /// <summary>Active configuration parameters.</summary>
        public NitroConfig Config => _config;

        public NitroSystem(NitroConfig config = null, float startingCharge = 25f)
        {
            _config = config ?? NitroConfig.CreateDefault();
            if (!_config.TryValidate(out var reason))
            {
                Debug.LogWarning($"[NitroSystem] Invalid NitroConfig ({reason}). Reverting to default.");
                _config = NitroConfig.CreateDefault();
            }

            _currentCharge = Mathf.Clamp(startingCharge, 0f, _config.maxCharge);
            _isBoosting = false;
        }

        /// <summary>
        /// Ticks nitro state machine by delta seconds (0 B hot path).
        /// </summary>
        /// <param name="deltaSeconds">Frame delta time.</param>
        /// <param name="boostInputHeld">True if boost button (e.g., Shift / Space) is held.</param>
        /// <param name="forwardSpeed">Player forward speed in m/s.</param>
        /// <param name="isDrifting">True if vehicle lateral slip qualifies as an active drift.</param>
        public void Tick(float deltaSeconds, bool boostInputHeld, float forwardSpeed, bool isDrifting)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (_isBoosting)
            {
                // Once active, drain until input is released or charge is depleted
                if (boostInputHeld && _currentCharge > 0f)
                {
                    _currentCharge = Mathf.Max(0f, _currentCharge - (_config.drainRatePerSecond * deltaSeconds));
                    if (_currentCharge <= 0f)
                    {
                        _isBoosting = false;
                    }
                }
                else
                {
                    _isBoosting = false;
                }
            }
            else
            {
                // Hysteresis activation check
                if (boostInputHeld && CanActivate)
                {
                    _isBoosting = true;
                    _currentCharge = Mathf.Max(0f, _currentCharge - (_config.drainRatePerSecond * deltaSeconds));
                    if (_currentCharge <= 0f)
                    {
                        _isBoosting = false;
                    }
                }
                else
                {
                    // Passive regen while moving fast (speed > 5 m/s)
                    if (forwardSpeed > 5f && _config.passiveRegenPerSecond > 0f)
                    {
                        _currentCharge = Mathf.Min(_config.maxCharge, _currentCharge + (_config.passiveRegenPerSecond * deltaSeconds));
                    }

                    // Drift bonus charge accumulation
                    if (isDrifting && _config.driftChargePerSecond > 0f)
                    {
                        _currentCharge = Mathf.Min(_config.maxCharge, _currentCharge + (_config.driftChargePerSecond * deltaSeconds));
                    }
                }
            }
        }

        /// <summary>
        /// Instantly awards near-miss bonus charge, clamping at maxCharge.
        /// </summary>
        public void AddNearMissCharge()
        {
            _currentCharge = Mathf.Min(_config.maxCharge, _currentCharge + _config.nearMissChargeBonus);
        }

        /// <summary>
        /// Adds an arbitrary charge increment, clamping at [0..maxCharge].
        /// </summary>
        public void AddCharge(float amount)
        {
            _currentCharge = Mathf.Clamp(_currentCharge + amount, 0f, _config.maxCharge);
        }

        /// <summary>
        /// Resets nitro charge to specified level (default 25f) and cancels active boost.
        /// </summary>
        public void Reset(float startingCharge = 25f)
        {
            _currentCharge = Mathf.Clamp(startingCharge, 0f, _config.maxCharge);
            _isBoosting = false;
        }
    }
}
