using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure, frame-scoped application-intent state (TDD §37.1). Converts this frame's key-down
    /// samples into the <see cref="IAppInput"/> intent surface without any Unity API, so
    /// pause/retry edge behavior is EditMode-testable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Sample"/> is called once per frame with already edge-triggered values (for
    /// example <c>Input.GetKeyDown</c> results), never with held-key states. The exposed intents
    /// remain readable until the next <see cref="Sample"/> or <see cref="Reset"/>; they are a
    /// one-frame broadcast, not a consume-once queue.
    /// </para>
    /// <para>
    /// Consumer contract: one logical consumer reads the intents in <c>Update</c> (GameFlow or the
    /// future application-state owner). Do not read them from <c>FixedUpdate</c> — physics can
    /// tick zero or multiple times per frame, which would drop or duplicate an intent. Add
    /// consume-once semantics only when a real second consumer exists.
    /// </para>
    /// </remarks>
    public sealed class AppInputState : IAppInput
    {
        /// <inheritdoc />
        public bool PausePressed { get; private set; }

        /// <inheritdoc />
        public bool RetryPressed { get; private set; }

        /// <summary>True when no intent is currently raised.</summary>
        public bool IsIdle => !PausePressed && !RetryPressed;

        /// <summary>
        /// Latches this frame's key-down samples. Pass edge-triggered values, not held-key states.
        /// </summary>
        /// <param name="pauseDown">True when the pause key went down this frame.</param>
        /// <param name="retryDown">True when the retry key went down this frame.</param>
        public void Sample(bool pauseDown, bool retryDown)
        {
            PausePressed = pauseDown;
            RetryPressed = retryDown;
        }

        /// <summary>Clears both intents deterministically.</summary>
        public void Reset()
        {
            PausePressed = false;
            RetryPressed = false;
        }
    }
}
