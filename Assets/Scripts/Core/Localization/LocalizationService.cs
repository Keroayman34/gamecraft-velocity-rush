using System;
using UnityEngine;

namespace VelocityRush.Core
{
    /// <summary>
    /// Lightweight, zero-third-party bilingual localization service (D-013, D-069).
    /// Supports "en" (LTR) and "ar" (RTL) with instant runtime switching and fail-closed
    /// missing-key fallback that returns the key ID with a diagnostic warning.
    /// </summary>
    /// <remarks>
    /// Static service with zero per-frame allocation. Language switching emits
    /// <see cref="LanguageChanged"/> for UI presenter rebinding. The string table is
    /// injected via <see cref="SetTable"/> to decouple Core from Data.
    /// </remarks>
    public static class LocalizationService
    {
        public const string LanguageEnglish = "en";
        public const string LanguageArabic = "ar";
        private const string LogPrefix = "[VelocityRush.L10n] ";

        private static string _currentLanguage = LanguageEnglish;
        private static ILocalizationTable _table;

        /// <summary>Current active language code ("en" or "ar").</summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>True when the current language is right-to-left (Arabic).</summary>
        public static bool IsRTL => string.Equals(_currentLanguage, LanguageArabic, StringComparison.OrdinalIgnoreCase);

        /// <summary>Raised once after every successful language switch.</summary>
        public static event Action<string> LanguageChanged;

        /// <summary>
        /// Injects the localization string table. Must be called before any Tr() lookups.
        /// </summary>
        public static void SetTable(ILocalizationTable table)
        {
            _table = table;
        }

        /// <summary>
        /// Switches the active language. Only "en" and "ar" are accepted (fail-closed).
        /// Emits <see cref="LanguageChanged"/> if the language actually changed.
        /// </summary>
        public static bool SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                Debug.LogWarning(LogPrefix + "SetLanguage rejected: null or empty language code.");
                return false;
            }

            var normalized = language.Trim().ToLowerInvariant();
            if (!string.Equals(normalized, LanguageEnglish, StringComparison.Ordinal) &&
                !string.Equals(normalized, LanguageArabic, StringComparison.Ordinal))
            {
                Debug.LogWarning(LogPrefix + "SetLanguage rejected: unsupported language '" + language + "'. Only 'en' and 'ar' are supported.");
                return false;
            }

            if (string.Equals(_currentLanguage, normalized, StringComparison.Ordinal))
            {
                return true; // Already set, no event needed
            }

            _currentLanguage = normalized;
            LanguageChanged?.Invoke(_currentLanguage);
            return true;
        }

        /// <summary>
        /// Translates a key to the current language. Returns the key ID and logs a warning
        /// if the key is missing (fail-closed, never throws, never returns null).
        /// </summary>
        public static string Tr(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }

            if (_table == null)
            {
                Debug.LogWarning(LogPrefix + "No localization table set. Returning key: " + key);
                return key;
            }

            var value = _table.Get(key, _currentLanguage);
            if (value != null)
            {
                return value;
            }

            Debug.LogWarning(LogPrefix + "Missing key '" + key + "' for language '" + _currentLanguage + "'. Returning key ID.");
            return key;
        }

        /// <summary>
        /// Resets the service to defaults (English, no table). Used for test isolation.
        /// </summary>
        public static void Reset()
        {
            _currentLanguage = LanguageEnglish;
            _table = null;
            LanguageChanged = null;
        }
    }
}
