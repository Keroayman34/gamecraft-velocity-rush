using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Drives the looped tyre-screech bed from per-wheel slip telemetry, staying silent under
    /// normal grip (T-7.11).
    /// </summary>
    /// <remarks>
    /// Prefers the additive <see cref="IVehicleWheelTelemetry"/> surface and falls back to the
    /// aggregate <see cref="VehicleTelemetry.WheelSlip"/> when a vehicle does not expose wheels.
    /// Only grounded wheels contribute, so a wheel spinning in the air cannot produce a screech.
    /// Allocation-free per frame.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TyreAudioController : MonoBehaviour
    {
        [Header("Scene wiring")]
        [SerializeField] private MonoBehaviour telemetrySource;
        [SerializeField] private AudioSource source;

        [Header("Response (placeholder — slice presentation, not final mix)")]
        [SerializeField, Min(0f)] private float slipThreshold = 1.2f;
        [SerializeField, Min(0.01f)] private float maxSlip = 6f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.5f;
        [SerializeField, Min(0.01f)] private float minPitch = 0.82f;
        [SerializeField, Min(0.01f)] private float maxPitch = 1.35f;
        [SerializeField, Min(0f)] private float responsePerSecond = 7f;

        private IVehicleTelemetry _telemetry;
        private IVehicleWheelTelemetry _wheelTelemetry;
        private float _intensity;

        private void Awake()
        {
            _telemetry = telemetrySource as IVehicleTelemetry;
            _wheelTelemetry = telemetrySource as IVehicleWheelTelemetry;

            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            if (source == null)
            {
                return;
            }

            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.pitch = TyreAudioMath.Pitch(0f, minPitch, maxPitch);
        }

        private void OnEnable()
        {
            if (source != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private void Update()
        {
            if (source == null)
            {
                return;
            }

            var slip = CurrentSlip();
            var target = TyreAudioMath.Intensity01(slip, slipThreshold, maxSlip);
            var step = responsePerSecond * Mathf.Max(0f, Time.unscaledDeltaTime);
            _intensity = Mathf.MoveTowards(_intensity, target, step);

            source.volume = TyreAudioMath.Volume(_intensity, maxVolume);
            source.pitch = TyreAudioMath.Pitch(_intensity, minPitch, maxPitch);
        }

        private float CurrentSlip()
        {
            if (_wheelTelemetry != null && _wheelTelemetry.WheelCount > 0)
            {
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

            return _telemetry != null ? _telemetry.Telemetry.WheelSlip : 0f;
        }
    }
}
