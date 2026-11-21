using System;
using System.Collections.Generic;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Versioned, serializable Data Transfer Object holding player persistence (TDD §39, D-067).
    /// </summary>
    /// <remarks>
    /// Designed strictly for Unity <see cref="JsonUtility"/>: no Dictionaries, no polymorphism,
    /// no polymorphic interfaces. Arrays of serializable DTO entries are mapped cleanly
    /// to/from in-memory lookup aggregates by domain services.
    /// </remarks>
    [Serializable]
    public sealed class SaveModel
    {
        /// <summary>Current schema version recognized by this build.</summary>
        public const int CurrentSaveVersion = 1;

        /// <summary>Default starting credits for a new profile.</summary>
        public const int DefaultStartingCredits = 500;

        /// <summary>Default vehicle id unlocked and selected on first run.</summary>
        public const string DefaultVehicleId = "starter-apex-v";

        /// <summary>Default root championship unlocked on first run.</summary>
        public const string DefaultChampionshipId = "c1-ignition-series";

        [SerializeField] public int saveVersion = CurrentSaveVersion;
        [SerializeField] public int credits = DefaultStartingCredits;
        [SerializeField] public string selectedVehicleId = DefaultVehicleId;
        [SerializeField] public string[] unlockedVehicleIds = new[] { DefaultVehicleId };
        [SerializeField] public string[] unlockedChampionshipIds = new[] { DefaultChampionshipId };
        [SerializeField] public PlayerSettingsDto settings = new PlayerSettingsDto();
        [SerializeField] public VehicleUpgradeEntry[] vehicleUpgrades = Array.Empty<VehicleUpgradeEntry>();
        [SerializeField] public EventProgressEntry[] eventRecords = Array.Empty<EventProgressEntry>();

        /// <summary>Creates a pristine, valid default save model for a new player.</summary>
        public static SaveModel CreateDefault()
        {
            return new SaveModel
            {
                saveVersion = CurrentSaveVersion,
                credits = DefaultStartingCredits,
                selectedVehicleId = DefaultVehicleId,
                unlockedVehicleIds = new[] { DefaultVehicleId },
                unlockedChampionshipIds = new[] { DefaultChampionshipId },
                settings = new PlayerSettingsDto
                {
                    language = "en",
                    masterVolume = 1.0f,
                    sfxVolume = 1.0f,
                    engineVolume = 1.0f
                },
                vehicleUpgrades = new[]
                {
                    new VehicleUpgradeEntry { vehicleId = DefaultVehicleId }
                },
                eventRecords = Array.Empty<EventProgressEntry>()
            };
        }

        /// <summary>
        /// Validates schema integrity, range limits, and non-empty IDs (fail-closed).
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (saveVersion != CurrentSaveVersion)
            {
                error = $"Unsupported saveVersion {saveVersion}; expected {CurrentSaveVersion}.";
                return false;
            }

            if (credits < 0)
            {
                error = $"Credits cannot be negative: {credits}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedVehicleId))
            {
                error = "SelectedVehicleId cannot be null or empty.";
                return false;
            }

            if (unlockedVehicleIds == null || unlockedVehicleIds.Length == 0)
            {
                error = "UnlockedVehicleIds must contain at least one vehicle.";
                return false;
            }

            bool selectedIsUnlocked = false;
            var seenVehicles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < unlockedVehicleIds.Length; i++)
            {
                var vId = unlockedVehicleIds[i];
                if (string.IsNullOrWhiteSpace(vId))
                {
                    error = $"UnlockedVehicleIds contains a null/empty entry at index {i}.";
                    return false;
                }
                if (!seenVehicles.Add(vId))
                {
                    error = $"Duplicate vehicle id in UnlockedVehicleIds: '{vId}'.";
                    return false;
                }
                if (string.Equals(vId, selectedVehicleId, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIsUnlocked = true;
                }
            }

            if (!selectedIsUnlocked)
            {
                error = $"SelectedVehicleId '{selectedVehicleId}' is not among unlocked vehicles.";
                return false;
            }

            if (unlockedChampionshipIds == null || unlockedChampionshipIds.Length == 0)
            {
                error = "UnlockedChampionshipIds must contain at least one championship.";
                return false;
            }

            var seenChampionships = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < unlockedChampionshipIds.Length; i++)
            {
                var cId = unlockedChampionshipIds[i];
                if (string.IsNullOrWhiteSpace(cId))
                {
                    error = $"UnlockedChampionshipIds contains a null/empty entry at index {i}.";
                    return false;
                }
                if (!seenChampionships.Add(cId))
                {
                    error = $"Duplicate championship id in UnlockedChampionshipIds: '{cId}'.";
                    return false;
                }
            }

            if (settings == null)
            {
                error = "Settings DTO cannot be null.";
                return false;
            }

            if (!settings.TryValidate(out var settingsError))
            {
                error = $"Settings validation failed: {settingsError}";
                return false;
            }

            if (vehicleUpgrades != null)
            {
                var seenUpgradeVehicles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < vehicleUpgrades.Length; i++)
                {
                    var entry = vehicleUpgrades[i];
                    if (entry == null)
                    {
                        error = $"VehicleUpgrades entry at index {i} is null.";
                        return false;
                    }
                    if (!entry.TryValidate(out var entryError))
                    {
                        error = $"VehicleUpgrades entry '{entry.vehicleId}' invalid: {entryError}";
                        return false;
                    }
                    if (!seenUpgradeVehicles.Add(entry.vehicleId))
                    {
                        error = $"Duplicate vehicle id in VehicleUpgrades: '{entry.vehicleId}'.";
                        return false;
                    }
                }
            }

            if (eventRecords != null)
            {
                var seenEvents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < eventRecords.Length; i++)
                {
                    var entry = eventRecords[i];
                    if (entry == null)
                    {
                        error = $"EventRecords entry at index {i} is null.";
                        return false;
                    }
                    if (!entry.TryValidate(out var entryError))
                    {
                        error = $"EventRecords entry '{entry.eventId}' invalid: {entryError}";
                        return false;
                    }
                    if (!seenEvents.Add(entry.eventId))
                    {
                        error = $"Duplicate event id in EventRecords: '{entry.eventId}'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Serializable entry storing upgrade tiers for one vehicle.
    /// </summary>
    [Serializable]
    public sealed class VehicleUpgradeEntry
    {
        public const int MaxTier = 3;

        [SerializeField] public string vehicleId = string.Empty;
        [SerializeField] public int engineTier;
        [SerializeField] public int tiresTier;
        [SerializeField] public int brakesTier;
        [SerializeField] public int suspensionTier;
        [SerializeField] public int transmissionTier;
        [SerializeField] public int aeroTier;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                error = "VehicleId cannot be null or empty.";
                return false;
            }

            if (engineTier < 0 || engineTier > MaxTier ||
                tiresTier < 0 || tiresTier > MaxTier ||
                brakesTier < 0 || brakesTier > MaxTier ||
                suspensionTier < 0 || suspensionTier > MaxTier ||
                transmissionTier < 0 || transmissionTier > MaxTier ||
                aeroTier < 0 || aeroTier > MaxTier)
            {
                error = $"Upgrade tiers must be in range [0..{MaxTier}]. (Engine: {engineTier}, Tires: {tiresTier}, Brakes: {brakesTier}, Suspension: {suspensionTier}, Transmission: {transmissionTier}, Aero: {aeroTier})";
                return false;
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Serializable entry storing completion record, stars, and best time for one event.
    /// </summary>
    [Serializable]
    public sealed class EventProgressEntry
    {
        public const int MaxStars = 3;

        [SerializeField] public string eventId = string.Empty;
        [SerializeField] public double bestTimeSeconds;
        [SerializeField] public int starsEarned;
        [SerializeField] public bool completed;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                error = "EventId cannot be null or empty.";
                return false;
            }

            if (starsEarned < 0 || starsEarned > MaxStars)
            {
                error = $"StarsEarned must be in range [0..{MaxStars}]; got {starsEarned}.";
                return false;
            }

            if (double.IsNaN(bestTimeSeconds) || double.IsInfinity(bestTimeSeconds) || bestTimeSeconds < 0d)
            {
                error = $"BestTimeSeconds must be a finite, non-negative number; got {bestTimeSeconds}.";
                return false;
            }

            if (starsEarned > 0 && !completed)
            {
                error = "An event cannot have earned stars while marked not completed.";
                return false;
            }

            error = null;
            return true;
        }
    }

    /// <summary>
    /// Serializable entry storing persisted player settings (language and volumes).
    /// </summary>
    [Serializable]
    public sealed class PlayerSettingsDto
    {
        [SerializeField] public string language = "en";
        [SerializeField, Range(0f, 1f)] public float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] public float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] public float engineVolume = 1f;

        public bool TryValidate(out string error)
        {
            if (string.IsNullOrWhiteSpace(language) ||
                (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(language, "ar", StringComparison.OrdinalIgnoreCase)))
            {
                error = $"Language must be either 'en' or 'ar'; got '{language}'.";
                return false;
            }

            if (float.IsNaN(masterVolume) || masterVolume < 0f || masterVolume > 1f ||
                float.IsNaN(sfxVolume) || sfxVolume < 0f || sfxVolume > 1f ||
                float.IsNaN(engineVolume) || engineVolume < 0f || engineVolume > 1f)
            {
                error = $"Volumes must be in range [0..1]. (Master: {masterVolume}, SFX: {sfxVolume}, Engine: {engineVolume})";
                return false;
            }

            error = null;
            return true;
        }
    }
}
