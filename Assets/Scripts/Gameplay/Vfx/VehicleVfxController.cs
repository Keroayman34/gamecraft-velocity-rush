using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Budgeted, telemetry-driven tyre smoke, ground dust, and impact sparks (T-7.9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Particle systems are authored with automatic emission disabled, so this controller is the
    /// single source of every particle. Emission is driven by slip, speed, and impact velocity
    /// through <see cref="VfxEmissionMath"/>, which caps each tick and smooths low rates with a
    /// fractional budget. Consequently the slice has no unbounded overdraw and no allocation on
    /// the emission path: <see cref="ParticleSystem.Emit(ParticleSystem.EmitParams, int)"/> takes a
    /// struct.
    /// </para>
    /// <para>
    /// Emission is presentation-only. It reads the existing telemetry interfaces and never writes
    /// to the vehicle or its colliders.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleVfxController : MonoBehaviour
    {
        [Header("Scene wiring")]
        [SerializeField] private MonoBehaviour telemetrySource;
        [SerializeField] private ParticleSystem smoke;
        [SerializeField] private ParticleSystem dust;
        [SerializeField] private ParticleSystem sparks;

        [Tooltip("Rear wheel anchor transforms used as smoke emission points. Empty = vehicle origin.")]
        [SerializeField] private Transform[] smokeAnchors;

        [Tooltip("Impact spark emission point. Empty = vehicle origin plus 0.35 m.")]
        [SerializeField] private Transform sparkOrigin;

        [Header("Budgets (particles per second — slice presentation, not final FX tuning)")]
        [SerializeField, Min(0f)] private float smokeRatePerSecond = 26f;
        [SerializeField, Min(0f)] private float dustRatePerSecond = 34f;
        [SerializeField, Min(0f)] private float smokeSlipThreshold = 1.4f;
        [SerializeField, Min(0.01f)] private float smokeMaxSlip = 6f;
        [SerializeField, Min(0f)] private float dustSpeedThreshold = 8f;
        [SerializeField, Min(0.01f)] private float dustMaxSpeed = 40f;

        [Header("Impact sparks (placeholder)")]
        [SerializeField, Min(0f)] private float sparkImpactThreshold = 3f;
        [SerializeField, Min(0.01f)] private float sparkMaxImpactSpeed = 16f;
        [SerializeField, Min(0)] private int maxSparksPerImpact = 14;
        [SerializeField, Min(0.01f)] private float sparkSpeed = 5.5f;

        private IVehicleTelemetry _telemetry;
        private IVehicleWheelTelemetry _wheelTelemetry;
        private float _smokeBudget;
        private float _dustBudget;
        private int _smokeAnchorIndex;
        private int _sparkCount;

        private void Awake()
        {
            _telemetry = telemetrySource as IVehicleTelemetry;
            _wheelTelemetry = telemetrySource as IVehicleWheelTelemetry;

            PlayIfPresent(smoke);
            PlayIfPresent(dust);
            PlayIfPresent(sparks);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            var telemetry = _telemetry != null ? _telemetry.Telemetry : default;
            if (!telemetry.IsGrounded)
            {
                // Do not bank budget while airborne: no burst of smoke on landing.
                _smokeBudget = 0f;
                _dustBudget = 0f;
                return;
            }

            var slip = CurrentSlip(telemetry.WheelSlip);
            var smokeIntensity = VfxEmissionMath.SlipIntensity01(slip, smokeSlipThreshold, smokeMaxSlip);
            if (smokeIntensity > 0f && smoke != null)
            {
                var count = VfxEmissionMath.ConsumeBudget(
                    ref _smokeBudget,
                    smokeRatePerSecond * smokeIntensity,
                    deltaTime);
                if (count > 0)
                {
                    Emit(smoke, NextSmokePosition(), count, 0.45f, 1.15f);
                }
            }

            var dustIntensity = VfxEmissionMath.SpeedIntensity01(telemetry.Speed, dustSpeedThreshold, dustMaxSpeed);
            if (dustIntensity > 0f && dust != null)
            {
                var count = VfxEmissionMath.ConsumeBudget(
                    ref _dustBudget,
                    dustRatePerSecond * dustIntensity,
                    deltaTime);
                if (count > 0)
                {
                    Emit(dust, transform.position + (Vector3.up * 0.1f), count, 0.9f, 1.8f);
                }
            }
        }

        /// <summary>
        /// Emits impact sparks for a collision at <paramref name="impactSpeed"/> (m/s). Called by
        /// <see cref="ImpactAudioController"/>; a no-op below the configured threshold.
        /// </summary>
        /// <param name="impactSpeed">Relative collision speed in m/s.</param>
        public void NotifyImpact(float impactSpeed)
        {
            if (sparks == null)
            {
                return;
            }

            var count = VfxEmissionMath.SparkCount(
                impactSpeed,
                sparkImpactThreshold,
                sparkMaxImpactSpeed,
                maxSparksPerImpact);
            if (count <= 0)
            {
                return;
            }

            var origin = sparkOrigin != null
                ? sparkOrigin.position
                : transform.position + (Vector3.up * 0.35f);
            EmitSparks(origin, count);
        }

        private void EmitSparks(Vector3 origin, int count)
        {
            for (var i = 0; i < count; i++)
            {
                // Deterministic golden-angle spread keeps repeated hits visually distinct without
                // a random-number allocation.
                var angle = (_sparkCount++ * 2.399963f) % (Mathf.PI * 2f);
                var direction = new Vector3(Mathf.Cos(angle), 0.55f, Mathf.Sin(angle));
                var emit = new ParticleSystem.EmitParams
                {
                    position = origin,
                    velocity = direction * sparkSpeed,
                    startLifetime = Mathf.Lerp(0.18f, 0.42f, (i + 1f) / count),
                    startSize = Mathf.Lerp(0.05f, 0.1f, (i % 3) / 2f)
                };
                sparks.Emit(emit, 1);
            }
        }

        private void Emit(ParticleSystem system, Vector3 position, int count, float minSize, float maxSize)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                startLifetime = Mathf.Lerp(0.9f, 1.6f, (count % 4) / 3f),
                startSize = Mathf.Lerp(minSize, maxSize, (count % 3) / 2f)
            };
            system.Emit(emit, count);
        }

        private Vector3 NextSmokePosition()
        {
            if (smokeAnchors == null || smokeAnchors.Length == 0)
            {
                return transform.position;
            }

            var anchor = smokeAnchors[_smokeAnchorIndex % smokeAnchors.Length];
            _smokeAnchorIndex++;
            return anchor != null ? anchor.position : transform.position;
        }

        private float CurrentSlip(float aggregateSlip)
        {
            if (_wheelTelemetry == null || _wheelTelemetry.WheelCount == 0)
            {
                return aggregateSlip;
            }

            var slip = 0f;
            for (var i = 0; i < _wheelTelemetry.WheelCount; i++)
            {
                var wheel = _wheelTelemetry.GetWheel(i);
                if (wheel.IsGrounded && wheel.Slip > slip)
                {
                    slip = wheel.Slip;
                }
            }

            return slip;
        }

        private static void PlayIfPresent(ParticleSystem system)
        {
            if (system != null && !system.isPlaying)
            {
                system.Play();
            }
        }
    }
}
