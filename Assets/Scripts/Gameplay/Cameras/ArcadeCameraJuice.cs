using System;
using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Dynamic Camera Juice system: speed/nitro-scaled FOV and damped impulse shake (Phase 12 Stage 3; TDD §60).
    /// Executes in LateUpdate after VehicleCameraRig to apply camera punch and shake without GC allocation (0 B/frame).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(100)] // Runs after VehicleCameraRig
    public sealed class ArcadeCameraJuice : MonoBehaviour
    {
        [Header("FOV Bounds & Dynamic Kick")]
        [SerializeField, Range(50f, 75f)] private float minFov = 60f;
        [SerializeField, Range(70f, 95f)] private float maxFov = 84f;
        [SerializeField, Range(5f, 20f)] private float nitroFovKick = 11f;
        [SerializeField, Range(1f, 30f)] private float fovSmoothing = 10f;

        [Header("Impulse Shake Tuning")]
        [SerializeField, Range(5f, 50f)] private float shakeFrequency = 24f;

        private Camera _camera;
        private float _currentFov;
        private float _currentSpeed;
        private float _speedReference = 60f;
        private bool _isBoosting;

        private float _shakeIntensity;
        private float _shakeDuration;
        private float _shakeTimer;
        private Vector3 _appliedShakeOffset;

        public float CurrentFov => _currentFov;
        public Vector3 CurrentShakeOffset => _appliedShakeOffset;
        public bool IsShaking => _shakeTimer > 0f;
        public bool IsBoosting => _isBoosting;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _currentFov = _camera != null ? _camera.fieldOfView : minFov;
            _appliedShakeOffset = Vector3.zero;
        }

        /// <summary>
        /// Updates the current speed snapshot for dynamic speed-scaled FOV calculation.
        /// </summary>
        public void SetSpeed(float speed, float speedReference = 60f)
        {
            _currentSpeed = Mathf.Max(0f, speed);
            _speedReference = Mathf.Max(1f, speedReference);
        }

        /// <summary>
        /// Sets active nitro boost state to modulate dynamic FOV kick and micro-rumble.
        /// </summary>
        public void SetNitroActive(bool isBoosting)
        {
            if (!_isBoosting && isBoosting)
            {
                // Micro-rumble kick upon initial nitro ignition
                TriggerImpulse(0.12f, 0.25f);
            }
            _isBoosting = isBoosting;
        }

        /// <summary>
        /// Triggers a damped directional camera impulse shake (e.g. from traffic collision).
        /// </summary>
        /// <param name="intensity">Maximum shake translation displacement in meters.</param>
        /// <param name="duration">Duration of shake decay in seconds.</param>
        public void TriggerImpulse(float intensity, float duration)
        {
            if (duration <= 0f)
            {
                return;
            }

            _shakeIntensity = Mathf.Max(_shakeIntensity * (_shakeTimer / Mathf.Max(0.001f, _shakeDuration)), intensity);
            _shakeDuration = duration;
            _shakeTimer = duration;
        }

        /// <summary>
        /// Snaps current FOV immediately to the calculated target FOV without smoothing (e.g. for resets or tests).
        /// </summary>
        public void SnapFov()
        {
            if (_camera == null) return;
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / _speedReference);
            float baseTargetFov = Mathf.Lerp(minFov, minFov + 10f, normalizedSpeed);
            float boostKick = _isBoosting ? nitroFovKick : 0f;
            _currentFov = Mathf.Clamp(baseTargetFov + boostKick, minFov, maxFov);
            _camera.fieldOfView = _currentFov;
        }

        /// <summary>
        /// Resets shake offsets and restores base FOV immediately (e.g., on Fast Retry / Scene load).
        /// </summary>
        public void ResetJuice()
        {
            // Remove previous shake offset if any was applied
            if (_appliedShakeOffset.sqrMagnitude > 0.00001f)
            {
                transform.position -= _appliedShakeOffset;
            }

            _appliedShakeOffset = Vector3.zero;
            _shakeIntensity = 0f;
            _shakeDuration = 0f;
            _shakeTimer = 0f;
            _isBoosting = false;
            _currentSpeed = 0f;

            if (_camera != null)
            {
                _currentFov = minFov;
                _camera.fieldOfView = minFov;
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || Time.timeScale <= 0f)
            {
                // Paused: freeze camera shake and skip decay to prevent post-pause oscillations
                return;
            }

            float dt = Time.deltaTime;

            // 1. Dynamic FOV
            float normalizedSpeed = Mathf.Clamp01(_currentSpeed / _speedReference);
            float baseTargetFov = Mathf.Lerp(minFov, minFov + 10f, normalizedSpeed);
            float boostKick = _isBoosting ? nitroFovKick : 0f;
            float targetFov = Mathf.Clamp(baseTargetFov + boostKick, minFov, maxFov);

            _currentFov = Mathf.Lerp(_currentFov, targetFov, 1f - Mathf.Exp(-dt * fovSmoothing));
            _camera.fieldOfView = _currentFov;

            // 2. Damped Camera Shake
            // Remove previously applied offset before calculating new pose
            if (_appliedShakeOffset.sqrMagnitude > 0.000001f)
            {
                transform.position -= _appliedShakeOffset;
                _appliedShakeOffset = Vector3.zero;
            }

            if (_shakeTimer > 0f)
            {
                _shakeTimer -= dt;
                float decay = Mathf.Clamp01(_shakeTimer / Mathf.Max(0.001f, _shakeDuration));
                float t = (Time.time * shakeFrequency);

                float x = (Mathf.PerlinNoise(t, 0.5f) - 0.5f) * 2f * _shakeIntensity * decay;
                float y = (Mathf.PerlinNoise(0.5f, t) - 0.5f) * 2f * _shakeIntensity * decay;

                _appliedShakeOffset = (transform.right * x) + (transform.up * y);
                transform.position += _appliedShakeOffset;

                if (_shakeTimer <= 0f)
                {
                    _appliedShakeOffset = Vector3.zero;
                }
            }
        }
    }
}
