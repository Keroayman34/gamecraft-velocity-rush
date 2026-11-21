using System;
using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Data
{
    /// <summary>
    /// Fail-safe WebGL and desktop persistence store backed by Unity PlayerPrefs (TDD §39, D-067).
    /// </summary>
    public sealed class PlayerPrefsJsonSaveStore : ISaveStore
    {
        public const string DefaultStorageKey = "VelocityRush_SaveData_v1";

        private readonly string _storageKey;

        public PlayerPrefsJsonSaveStore(string storageKey = DefaultStorageKey)
        {
            _storageKey = string.IsNullOrWhiteSpace(storageKey) ? DefaultStorageKey : storageKey;
        }

        public string StorageKey => _storageKey;

        public bool Exists()
        {
            try
            {
                return PlayerPrefs.HasKey(_storageKey);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool TryLoad(out string json, out string error)
        {
            try
            {
                if (!PlayerPrefs.HasKey(_storageKey))
                {
                    json = null;
                    error = null; // Clean absence
                    return false;
                }

                json = PlayerPrefs.GetString(_storageKey, null);
                if (string.IsNullOrEmpty(json))
                {
                    json = null;
                    error = "PlayerPrefs returned empty or null string.";
                    return false;
                }

                error = null;
                return true;
            }
            catch (Exception ex)
            {
                json = null;
                error = $"PlayerPrefs load error: {ex.Message}";
                return false;
            }
        }

        public bool TrySave(string json, out string error)
        {
            if (string.IsNullOrEmpty(json))
            {
                error = "Cannot save a null or empty JSON string.";
                return false;
            }

            try
            {
                PlayerPrefs.SetString(_storageKey, json);
                PlayerPrefs.Save();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"PlayerPrefs save error (possible quota exceeded): {ex.Message}";
                return false;
            }
        }

        public bool TryDelete(out string error)
        {
            try
            {
                if (PlayerPrefs.HasKey(_storageKey))
                {
                    PlayerPrefs.DeleteKey(_storageKey);
                    PlayerPrefs.Save();
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = $"PlayerPrefs delete error: {ex.Message}";
                return false;
            }
        }
    }
}
