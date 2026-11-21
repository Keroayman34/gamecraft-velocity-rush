namespace VelocityRush.Core
{
    /// <summary>
    /// Abstract persistence store for serialized payload strings (TDD §39.1, D-036, D-067).
    /// </summary>
    /// <remarks>
    /// Completely decoupled from Unity scene/engine types. Storage providers implement
    /// fail-safe persistence (PlayerPrefs, InMemory mock, and future Phase 13 Bridge store).
    /// </remarks>
    public interface ISaveStore
    {
        /// <summary>Returns true if a saved payload exists in this store.</summary>
        bool Exists();

        /// <summary>
        /// Attempts to load the serialized payload string from storage.
        /// </summary>
        /// <param name="json">The loaded JSON string, or null on failure/absence.</param>
        /// <param name="error">Diagnostic message on failure, or null on success.</param>
        /// <returns>True if successfully loaded; false otherwise.</returns>
        bool TryLoad(out string json, out string error);

        /// <summary>
        /// Attempts to save the serialized payload string to storage.
        /// </summary>
        /// <param name="json">The non-null JSON string to persist.</param>
        /// <param name="error">Diagnostic message on failure, or null on success.</param>
        /// <returns>True if successfully saved; false otherwise.</returns>
        bool TrySave(string json, out string error);

        /// <summary>
        /// Attempts to delete any existing saved payload from storage.
        /// </summary>
        /// <param name="error">Diagnostic message on failure, or null on success.</param>
        /// <returns>True if successfully deleted or already absent; false on failure.</returns>
        bool TryDelete(out string error);
    }
}
