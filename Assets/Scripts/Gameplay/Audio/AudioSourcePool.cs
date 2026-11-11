using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Fixed-size pool of pre-created <see cref="AudioSource"/> children used for one-shot slice
    /// sounds (impact, UI, countdown) so playback never instantiates an object at runtime
    /// (T-7.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Play"/> is allocation-free: it assigns an existing clip/volume/pitch to an idle
    /// source, or steals the next source in round-robin order when every source is busy. Sources
    /// are created once during owner <c>Awake</c>.
    /// </para>
    /// <para>
    /// 3D pools follow the owner transform because the sources are parented to it; 2D pools are
    /// used for UI stings that must not attenuate with camera distance.
    /// </para>
    /// </remarks>
    public sealed class AudioSourcePool
    {
        private readonly AudioSource[] _sources;
        private int _next;

        /// <summary>Creates the pool under <paramref name="parent"/>.</summary>
        /// <param name="parent">Owner transform; the pool appears as a child object.</param>
        /// <param name="poolName">Child object name, for scene readability.</param>
        /// <param name="size">Number of sources; at least one.</param>
        /// <param name="spatial">True for 3D positional playback, false for 2D UI playback.</param>
        /// <param name="minDistance">Distance at which 3D playback stops growing louder.</param>
        /// <param name="maxDistance">Distance at which 3D playback becomes inaudible.</param>
        /// <param name="ignoreListenerPause">
        /// True for UI stings that must still be heard on the frame the game pauses.
        /// </param>
        public AudioSourcePool(
            Transform parent,
            string poolName,
            int size,
            bool spatial,
            float minDistance = 2f,
            float maxDistance = 60f,
            bool ignoreListenerPause = false)
        {
            var count = size < 1 ? 1 : size;
            _sources = new AudioSource[count];

            var poolGo = new GameObject(poolName);
            poolGo.transform.SetParent(parent, false);

            for (var i = 0; i < count; i++)
            {
                var sourceGo = new GameObject("Source " + i);
                sourceGo.transform.SetParent(poolGo.transform, false);

                var source = sourceGo.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = spatial ? 1f : 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = Mathf.Max(0.01f, minDistance);
                source.maxDistance = Mathf.Max(source.minDistance, maxDistance);
                source.dopplerLevel = 0f;
                source.ignoreListenerPause = ignoreListenerPause;
                _sources[i] = source;
            }
        }

        /// <summary>Number of sources in the pool.</summary>
        public int Count => _sources.Length;

        /// <summary>
        /// Plays a one-shot on the first idle source (or round-robin steals one). No-op for a null
        /// clip. Never allocates.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="volume">Linear volume 0..1.</param>
        /// <param name="pitch">Playback pitch; values are clamped to a safe audible range.</param>
        public void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null)
            {
                return;
            }

            var source = FindIdleSource();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.05f, 3f);
            source.Play();
        }

        /// <summary>Stops every source in the pool.</summary>
        public void StopAll()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                _sources[i].Stop();
            }
        }

        private AudioSource FindIdleSource()
        {
            for (var i = 0; i < _sources.Length; i++)
            {
                if (!_sources[i].isPlaying)
                {
                    return _sources[i];
                }
            }

            var stolen = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            return stolen;
        }
    }
}
