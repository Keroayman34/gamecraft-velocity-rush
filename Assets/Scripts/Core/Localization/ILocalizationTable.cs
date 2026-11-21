namespace VelocityRush.Core
{
    /// <summary>
    /// Read-only string table abstraction for localization lookups (D-013).
    /// Implemented by the Data layer's concrete table, keeping Core dependency-free.
    /// </summary>
    public interface ILocalizationTable
    {
        /// <summary>
        /// Returns the localized string for the given key and language,
        /// or null if the key is not found.
        /// </summary>
        string Get(string key, string language);

        /// <summary>Returns the total number of keys in the table.</summary>
        int KeyCount { get; }

        /// <summary>Returns true if the key exists in both "en" and "ar".</summary>
        bool HasKey(string key);
    }
}
