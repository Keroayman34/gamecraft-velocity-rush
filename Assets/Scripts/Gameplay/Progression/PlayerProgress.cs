using System;
using System.Collections.Generic;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure value snapshot of one event's player record.
    /// </summary>
    public readonly struct EventProgressRecord
    {
        public string EventId { get; }
        public double BestTimeSeconds { get; }
        public int StarsEarned { get; }
        public bool Completed { get; }

        public EventProgressRecord(string eventId, double bestTimeSeconds, int starsEarned, bool completed)
        {
            EventId = eventId;
            BestTimeSeconds = bestTimeSeconds;
            StarsEarned = starsEarned;
            Completed = completed;
        }
    }

    /// <summary>
    /// Aggregate Root and sole mutator of runtime progression state (TDD §34.3, D-067, D-068).
    /// </summary>
    /// <remarks>
    /// Hydrated from <see cref="SaveModel"/> on startup, maintains O(1) in-memory lookups,
    /// and exports cleanly back to <see cref="SaveModel"/> for persistence.
    /// </remarks>
    public sealed class PlayerProgress
    {
        private int _credits;
        private string _selectedVehicleId;
        private readonly HashSet<string> _unlockedVehicleIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _unlockedChampionshipIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, VehicleUpgradeState> _vehicleUpgrades = new Dictionary<string, VehicleUpgradeState>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, EventProgressRecord> _eventRecords = new Dictionary<string, EventProgressRecord>(StringComparer.OrdinalIgnoreCase);
        private PlayerSettingsDto _settings = new PlayerSettingsDto();

        /// <summary>Fired whenever progression, credits, upgrades, or unlocks change.</summary>
        public event Action OnChanged;

        /// <summary>Current available credit balance.</summary>
        public int Credits => _credits;

        /// <summary>Currently active vehicle id for racing and garage.</summary>
        public string SelectedVehicleId => _selectedVehicleId;

        /// <summary>Read-only snapshot of all currently unlocked vehicle ids.</summary>
        public IReadOnlyCollection<string> UnlockedVehicles => _unlockedVehicleIds;

        /// <summary>Read-only snapshot of all currently unlocked championship ids.</summary>
        public IReadOnlyCollection<string> UnlockedChampionships => _unlockedChampionshipIds;

        /// <summary>Current player settings.</summary>
        public PlayerSettingsDto Settings => _settings;

        public PlayerProgress()
        {
            HydrateFrom(SaveModel.CreateDefault());
        }

        public PlayerProgress(SaveModel saveModel)
        {
            HydrateFrom(saveModel ?? SaveModel.CreateDefault());
        }

        /// <summary>
        /// Hydrates in-memory lookup structures from a valid <see cref="SaveModel"/>.
        /// </summary>
        public void HydrateFrom(SaveModel model)
        {
            if (model == null)
            {
                model = SaveModel.CreateDefault();
            }

            _credits = Math.Max(0, model.credits);
            _selectedVehicleId = string.IsNullOrWhiteSpace(model.selectedVehicleId)
                ? SaveModel.DefaultVehicleId
                : model.selectedVehicleId;

            _unlockedVehicleIds.Clear();
            if (model.unlockedVehicleIds != null)
            {
                for (int i = 0; i < model.unlockedVehicleIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(model.unlockedVehicleIds[i]))
                    {
                        _unlockedVehicleIds.Add(model.unlockedVehicleIds[i]);
                    }
                }
            }
            if (_unlockedVehicleIds.Count == 0)
            {
                _unlockedVehicleIds.Add(SaveModel.DefaultVehicleId);
            }

            _unlockedChampionshipIds.Clear();
            if (model.unlockedChampionshipIds != null)
            {
                for (int i = 0; i < model.unlockedChampionshipIds.Length; i++)
                {
                    if (!string.IsNullOrWhiteSpace(model.unlockedChampionshipIds[i]))
                    {
                        _unlockedChampionshipIds.Add(model.unlockedChampionshipIds[i]);
                    }
                }
            }
            if (_unlockedChampionshipIds.Count == 0)
            {
                _unlockedChampionshipIds.Add(SaveModel.DefaultChampionshipId);
            }

            _vehicleUpgrades.Clear();
            if (model.vehicleUpgrades != null)
            {
                for (int i = 0; i < model.vehicleUpgrades.Length; i++)
                {
                    var entry = model.vehicleUpgrades[i];
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.vehicleId))
                    {
                        _vehicleUpgrades[entry.vehicleId] = new VehicleUpgradeState(
                            entry.engineTier,
                            entry.tiresTier,
                            entry.brakesTier,
                            entry.suspensionTier,
                            entry.transmissionTier,
                            entry.aeroTier);
                    }
                }
            }

            _eventRecords.Clear();
            if (model.eventRecords != null)
            {
                for (int i = 0; i < model.eventRecords.Length; i++)
                {
                    var entry = model.eventRecords[i];
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.eventId))
                    {
                        _eventRecords[entry.eventId] = new EventProgressRecord(
                            entry.eventId,
                            entry.bestTimeSeconds,
                            entry.starsEarned,
                            entry.completed);
                    }
                }
            }

            _settings = model.settings ?? new PlayerSettingsDto();

            OnChanged?.Invoke();
        }

        /// <summary>
        /// Exports the current live state into a new, validated <see cref="SaveModel"/> DTO.
        /// </summary>
        public SaveModel ExportSaveModel()
        {
            var model = new SaveModel
            {
                saveVersion = SaveModel.CurrentSaveVersion,
                credits = _credits,
                selectedVehicleId = _selectedVehicleId,
                unlockedVehicleIds = new string[_unlockedVehicleIds.Count],
                unlockedChampionshipIds = new string[_unlockedChampionshipIds.Count],
                settings = new PlayerSettingsDto
                {
                    language = _settings.language,
                    masterVolume = _settings.masterVolume,
                    sfxVolume = _settings.sfxVolume,
                    engineVolume = _settings.engineVolume
                }
            };

            _unlockedVehicleIds.CopyTo(model.unlockedVehicleIds);
            _unlockedChampionshipIds.CopyTo(model.unlockedChampionshipIds);

            var upgradeEntries = new List<VehicleUpgradeEntry>(_vehicleUpgrades.Count);
            foreach (var kvp in _vehicleUpgrades)
            {
                upgradeEntries.Add(new VehicleUpgradeEntry
                {
                    vehicleId = kvp.Key,
                    engineTier = kvp.Value.EngineTier,
                    tiresTier = kvp.Value.TiresTier,
                    brakesTier = kvp.Value.BrakesTier,
                    suspensionTier = kvp.Value.SuspensionTier,
                    transmissionTier = kvp.Value.TransmissionTier,
                    aeroTier = kvp.Value.AeroTier
                });
            }
            model.vehicleUpgrades = upgradeEntries.ToArray();

            var eventEntries = new List<EventProgressEntry>(_eventRecords.Count);
            foreach (var kvp in _eventRecords)
            {
                eventEntries.Add(new EventProgressEntry
                {
                    eventId = kvp.Value.EventId,
                    bestTimeSeconds = kvp.Value.BestTimeSeconds,
                    starsEarned = kvp.Value.StarsEarned,
                    completed = kvp.Value.Completed
                });
            }
            model.eventRecords = eventEntries.ToArray();

            return model;
        }

        #region Queries

        public bool IsVehicleUnlocked(string vehicleId)
        {
            return !string.IsNullOrWhiteSpace(vehicleId) && _unlockedVehicleIds.Contains(vehicleId);
        }

        public bool IsChampionshipUnlocked(string championshipId)
        {
            return !string.IsNullOrWhiteSpace(championshipId) && _unlockedChampionshipIds.Contains(championshipId);
        }

        public VehicleUpgradeState GetUpgradeState(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId))
            {
                return VehicleUpgradeState.Stock;
            }

            return _vehicleUpgrades.TryGetValue(vehicleId, out var state) ? state : VehicleUpgradeState.Stock;
        }

        public int GetUpgradeLevel(string vehicleId, UpgradeCategory category)
        {
            return GetUpgradeState(vehicleId).GetTier(category);
        }

        public bool TryGetEventRecord(string eventId, out EventProgressRecord record)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                record = default;
                return false;
            }

            return _eventRecords.TryGetValue(eventId, out record);
        }

        public int GetEventStars(string eventId)
        {
            return TryGetEventRecord(eventId, out var record) ? record.StarsEarned : 0;
        }

        public bool IsEventCompleted(string eventId)
        {
            return TryGetEventRecord(eventId, out var record) && record.Completed;
        }

        public double GetEventBestTime(string eventId)
        {
            return TryGetEventRecord(eventId, out var record) ? record.BestTimeSeconds : 0d;
        }

        #endregion

        #region Mutations

        public void AddCredits(int amount)
        {
            if (amount <= 0) return;
            checked
            {
                _credits += amount;
            }
            OnChanged?.Invoke();
        }

        public bool TryDeductCredits(int amount)
        {
            if (amount < 0) return false;
            if (_credits < amount) return false;

            _credits -= amount;
            OnChanged?.Invoke();
            return true;
        }

        public bool UnlockVehicle(string vehicleId)
        {
            if (string.IsNullOrWhiteSpace(vehicleId)) return false;
            if (_unlockedVehicleIds.Add(vehicleId))
            {
                if (!_vehicleUpgrades.ContainsKey(vehicleId))
                {
                    _vehicleUpgrades[vehicleId] = VehicleUpgradeState.Stock;
                }
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool SetSelectedVehicle(string vehicleId)
        {
            if (!IsVehicleUnlocked(vehicleId)) return false;
            if (string.Equals(_selectedVehicleId, vehicleId, StringComparison.OrdinalIgnoreCase)) return true;

            _selectedVehicleId = vehicleId;
            OnChanged?.Invoke();
            return true;
        }

        public bool UnlockChampionship(string championshipId)
        {
            if (string.IsNullOrWhiteSpace(championshipId)) return false;
            if (_unlockedChampionshipIds.Add(championshipId))
            {
                OnChanged?.Invoke();
                return true;
            }
            return false;
        }

        public bool TryApplyUpgrade(string vehicleId, UpgradeCategory category, int newTier)
        {
            if (!IsVehicleUnlocked(vehicleId)) return false;
            if (newTier < 0 || newTier > VehicleUpgradeState.MaxTier) return false;

            var current = GetUpgradeState(vehicleId);
            var updated = current.WithTier(category, newTier);
            if (current == updated) return false;

            _vehicleUpgrades[vehicleId] = updated;
            OnChanged?.Invoke();
            return true;
        }

        public bool RecordEventResult(
            string eventId,
            int stars,
            double elapsedSeconds,
            bool completed,
            out bool isNewBestTime,
            out int newStarsEarned)
        {
            isNewBestTime = false;
            newStarsEarned = 0;

            if (string.IsNullOrWhiteSpace(eventId)) return false;

            int clampedStars = Math.Max(0, Math.Min(EventProgressEntry.MaxStars, stars));
            double validTime = (double.IsNaN(elapsedSeconds) || elapsedSeconds < 0d) ? 0d : elapsedSeconds;

            if (_eventRecords.TryGetValue(eventId, out var existing))
            {
                int priorStars = existing.StarsEarned;
                int combinedStars = Math.Max(priorStars, clampedStars);
                newStarsEarned = Math.Max(0, combinedStars - priorStars);

                double bestTime = existing.BestTimeSeconds;
                if (completed && validTime > 0d)
                {
                    if (bestTime <= 0d || validTime < bestTime)
                    {
                        bestTime = validTime;
                        isNewBestTime = true;
                    }
                }

                bool nowCompleted = existing.Completed || completed;
                _eventRecords[eventId] = new EventProgressRecord(eventId, bestTime, combinedStars, nowCompleted);
            }
            else
            {
                newStarsEarned = completed ? clampedStars : 0;
                double bestTime = (completed && validTime > 0d) ? validTime : 0d;
                isNewBestTime = completed && bestTime > 0d;

                _eventRecords[eventId] = new EventProgressRecord(eventId, bestTime, clampedStars, completed);
            }

            OnChanged?.Invoke();
            return true;
        }

        #endregion
    }
}
