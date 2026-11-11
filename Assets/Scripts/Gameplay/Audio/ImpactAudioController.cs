using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Turns physical chassis collisions into pooled impact one-shots and optional spark VFX
    /// (T-7.11). Attach to the vehicle root that owns the <see cref="Rigidbody"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Audio and VFX are driven from the collision's relative velocity, so a wall tap is quiet and
    /// a high-speed barrier hit is loud and sparking. A short cooldown prevents a single physical
    /// contact from stacking many one-shots.
    /// </para>
    /// <para>
    /// This component is presentation-only: it adds no collider, changes no physics material, and
    /// never writes to the Rigidbody. Removing it leaves handling identical.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ImpactAudioController : MonoBehaviour
    {
        private const int PoolSize = 3;

        [Header("Scene wiring")]
        [SerializeField] private AudioClip impactClip;
        [SerializeField] private VehicleVfxController sparkTarget;

        [Header("Response (placeholder — slice presentation, not final mix)")]
        [SerializeField, Min(0f)] private float minImpactSpeed = 2f;
        [SerializeField, Min(0.01f)] private float maxImpactSpeed = 16f;
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.2f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.85f;
        [SerializeField, Min(0f)] private float cooldownSeconds = 0.2f;

        private AudioSourcePool _pool;
        private float _nextAllowedTime;
        private int _impactCount;

        private void Awake()
        {
            _pool = new AudioSourcePool(transform, "ImpactPool", PoolSize, spatial: true, minDistance: 3f, maxDistance: 70f);
        }

        /// <summary>The authored impact clip, used when the runtime spawner transfers presentation.</summary>
        public AudioClip ImpactClip => impactClip;

        /// <summary>
        /// Configures a runtime-added impact controller on a spawned vehicle (T-8.11).
        /// </summary>
        /// <param name="clip">One-shot impact clip.</param>
        /// <param name="vfx">Spark receiver on the same spawned root; may be null.</param>
        public void Configure(AudioClip clip, VehicleVfxController vfx)
        {
            impactClip = clip;
            sparkTarget = vfx;
            _impactCount = 0;
            _nextAllowedTime = 0f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null)
            {
                return;
            }

            var impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed < minImpactSpeed || Time.time < _nextAllowedTime)
            {
                return;
            }

            _nextAllowedTime = Time.time + cooldownSeconds;

            var span = Mathf.Max(0.01f, maxImpactSpeed - minImpactSpeed);
            var amount = Mathf.Clamp01((impactSpeed - minImpactSpeed) / span);

            // Deterministic small pitch variation: repeated hits stay distinguishable without a
            // random source on the hot path.
            var pitch = 0.94f + ((_impactCount % 4) * 0.035f);
            _impactCount++;

            _pool.Play(impactClip, Mathf.Lerp(minVolume, maxVolume, amount), pitch);

            if (sparkTarget != null)
            {
                sparkTarget.NotifyImpact(impactSpeed);
            }
        }
    }
}
