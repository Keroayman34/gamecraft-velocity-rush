using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Drives the looped engine sound from polled vehicle telemetry: speed becomes a simulated
    /// RPM value, which becomes loop pitch and volume (T-7.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read-only with respect to the simulation: the controller never touches the vehicle, the
    /// input, or the mixer asset. It only writes <see cref="AudioSource.pitch"/> and
    /// <see cref="AudioSource.volume"/> on its own source, so audio can be removed without
    /// changing gameplay.
    /// </para>
    /// <para>
    /// Response is smoothed with <see cref="Time.unscaledDeltaTime"/> so a pause (zero time scale)
    /// cannot leave the engine stuck mid-transition. Allocation-free per frame.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EngineAudioController : MonoBehaviour
    {
        [Header("Scene wiring")]
        [SerializeField] private MonoBehaviour telemetrySource;
        [SerializeField] private AudioSource source;

        [Header("Response (placeholder — slice presentation, not final mix)")]
        [SerializeField, Min(1f)] private float maxSpeed = 55f;
        [SerializeField, Min(1)] private int gearCount = EngineAudioMath.DefaultGearCount;
        [SerializeField, Min(0.01f)] private float minPitch = 0.75f;
        [SerializeField, Min(0.01f)] private float maxPitch = 1.9f;
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.18f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.85f;
        [SerializeField, Min(0f)] private float responsePerSecond = 9f;

        private IVehicleTelemetry _telemetry;
        private float _pitch;
        private float _volume;

        private void Awake()
        {
            _telemetry = telemetrySource as IVehicleTelemetry;

            if (source == null)
            {
                source = GetComponent<AudioSource>();
            }

            _pitch = EngineAudioMath.Pitch(EngineAudioMath.IdleRpmFraction, minPitch, maxPitch);
            _volume = EngineAudioMath.Volume(EngineAudioMath.IdleRpmFraction, minVolume, maxVolume);

            if (source == null)
            {
                return;
            }

            source.loop = true;
            source.playOnAwake = false;
            source.pitch = _pitch;
            source.volume = _volume;
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

            var speed = _telemetry != null ? _telemetry.Telemetry.Speed : 0f;
            var rpm = EngineAudioMath.Rpm01(speed, maxSpeed, gearCount);
            var targetPitch = EngineAudioMath.Pitch(rpm, minPitch, maxPitch);
            var targetVolume = EngineAudioMath.Volume(rpm, minVolume, maxVolume);

            var step = responsePerSecond * Mathf.Max(0f, Time.unscaledDeltaTime);
            _pitch = Mathf.MoveTowards(_pitch, targetPitch, step);
            _volume = Mathf.MoveTowards(_volume, targetVolume, step);

            source.pitch = _pitch;
            source.volume = _volume;
        }
    }
}
