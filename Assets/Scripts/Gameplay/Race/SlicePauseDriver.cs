using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Scene adapter that maps the pure pause state owned by <see cref="RaceController"/> onto the
    /// engine globals the slice needs frozen: <see cref="Time.timeScale"/> and
    /// <see cref="AudioListener.pause"/> (T-7.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Pause judgement stays in <see cref="RaceRules"/>; this component only applies an already
    /// accepted transition. It therefore contains no gameplay rules and no allocation, and it can
    /// be omitted (or replaced) without changing race behaviour.
    /// </para>
    /// <para>
    /// The driver restores an unpaused time scale when it is disabled or destroyed only if it was
    /// the component that applied the pause, so leaving play mode never strands
    /// <c>Time.timeScale</c> at zero.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class SlicePauseDriver : MonoBehaviour
    {
        [SerializeField] private RaceController race;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("Time scale applied while the attempt is paused. Zero freezes the simulation.")]
        private float pausedTimeScale;

        private bool _appliedPause;

        private void OnEnable()
        {
            if (race == null)
            {
                return;
            }

            race.PausedChanged += HandlePausedChanged;
            Apply(race.IsPaused);
        }

        private void OnDisable()
        {
            if (race != null)
            {
                race.PausedChanged -= HandlePausedChanged;
            }

            Restore();
        }

        private void HandlePausedChanged(bool paused)
        {
            Apply(paused);
        }

        private void Apply(bool paused)
        {
            if (paused)
            {
                Time.timeScale = pausedTimeScale;
                AudioListener.pause = true;
                _appliedPause = true;
                return;
            }

            Restore();
        }

        private void Restore()
        {
            if (!_appliedPause)
            {
                return;
            }

            _appliedPause = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
