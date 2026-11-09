namespace VelocityRush.Core
{
    /// <summary>
    /// High-level application intents exposed by the input adapter (TDD §37.1).
    /// Consumers translate these intents into their own commands; the input layer never
    /// mutates application state directly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Intents are frame-scoped edges: a property is <c>true</c> only on the frame the input
    /// was detected (not a held state). Consumer contract: one logical consumer reads them in
    /// <c>Update</c> after the adapter samples. They are a one-frame broadcast, not a
    /// consume-once queue, so a second reader in the same frame observes the same value; do not
    /// read them from <c>FixedUpdate</c>. Add consume-once semantics only if a second consumer
    /// genuinely appears.
    /// </para>
    /// <para>
    /// Escape is reserved for overlay-close (TDD §37.2/§37.6) and is not a pause binding.
    /// A quit intent is intentionally absent until an application quit/exit path exists in the
    /// architecture — the current bootstrap owns no quit flow, and adding an unused intent now
    /// would invent one.
    /// </para>
    /// </remarks>
    public interface IAppInput
    {
        /// <summary>Pause intent for this frame.</summary>
        bool PausePressed { get; }

        /// <summary>Retry intent for this frame.</summary>
        bool RetryPressed { get; }
    }
}
