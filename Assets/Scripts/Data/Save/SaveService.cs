using System;
using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Data
{
    /// <summary>
    /// Single-writer persistence coordinator managing serialization and storage lifecycle (TDD §39, D-067).
    /// </summary>
    public sealed class SaveService
    {
        private const string LogPrefix = "[SaveService] ";

        private readonly ISaveStore _store;
        private SaveModel _currentModel;
        private bool _isDirty;

        /// <summary>Gets the current in-memory save model.</summary>
        public SaveModel CurrentModel => _currentModel;

        /// <summary>True if in-memory state has uncommitted changes.</summary>
        public bool IsDirty => _isDirty;

        /// <summary>Creates a SaveService bound to the specified store.</summary>
        public SaveService(ISaveStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _currentModel = SaveModel.CreateDefault();
            _isDirty = false;
        }

        /// <summary>
        /// Loads the saved profile from storage. If absent, corrupted, or having an unsupported future
        /// schema version, falls back cleanly to <see cref="SaveModel.CreateDefault"/> (fail-closed).
        /// </summary>
        /// <returns>True if an existing valid save was loaded; false if defaults were established.</returns>
        public bool Load()
        {
            if (!_store.TryLoad(out var json, out var loadError))
            {
                if (loadError != null)
                {
                    Debug.LogWarning(LogPrefix + $"Storage read failed: {loadError}. Loading clean defaults.");
                }
                else
                {
                    Debug.Log(LogPrefix + "No existing save data found. Initializing fresh defaults.");
                }

                _currentModel = SaveModel.CreateDefault();
                _isDirty = true; // New profile needs to be saved
                return false;
            }

            SaveModel loadedModel;
            try
            {
                loadedModel = JsonUtility.FromJson<SaveModel>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning(LogPrefix + $"Failed to parse save JSON: {ex.Message}. Recovering with defaults.");
                _currentModel = SaveModel.CreateDefault();
                _isDirty = true;
                return false;
            }

            if (loadedModel == null)
            {
                Debug.LogWarning(LogPrefix + "Parsed save model is null. Recovering with defaults.");
                _currentModel = SaveModel.CreateDefault();
                _isDirty = true;
                return false;
            }

            // Schema version guard
            if (loadedModel.saveVersion > SaveModel.CurrentSaveVersion)
            {
                Debug.LogWarning(LogPrefix + $"Future save version {loadedModel.saveVersion} detected (supported: {SaveModel.CurrentSaveVersion}). Refusing forward migration and establishing defaults.");
                _currentModel = SaveModel.CreateDefault();
                _isDirty = true;
                return false;
            }

            // Schema validation guard
            if (!loadedModel.TryValidate(out var validationError))
            {
                Debug.LogWarning(LogPrefix + $"Save data validation failed: {validationError}. Recovering with defaults.");
                _currentModel = SaveModel.CreateDefault();
                _isDirty = true;
                return false;
            }

            _currentModel = loadedModel;
            _isDirty = false;
            Debug.Log(LogPrefix + $"Successfully loaded profile (credits: {_currentModel.credits}, vehicle: {_currentModel.selectedVehicleId}).");
            return true;
        }

        /// <summary>Marks that in-memory state has been modified and needs flushing.</summary>
        public void MarkDirty()
        {
            _isDirty = true;
        }

        /// <summary>
        /// Replaces the current save model and marks dirty.
        /// </summary>
        public void SetModel(SaveModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!model.TryValidate(out var error))
            {
                throw new ArgumentException($"Cannot set invalid SaveModel: {error}", nameof(model));
            }

            _currentModel = model;
            _isDirty = true;
        }

        /// <summary>
        /// Serializes and writes current state to storage if dirty.
        /// </summary>
        /// <returns>True if written successfully or clean; false on storage write failure.</returns>
        public bool Flush()
        {
            if (!_isDirty)
            {
                return true; // No work needed
            }

            return SaveNow();
        }

        /// <summary>
        /// Forcefully serializes and writes current state to storage regardless of dirty flag.
        /// </summary>
        public bool SaveNow()
        {
            if (_currentModel == null)
            {
                _currentModel = SaveModel.CreateDefault();
            }

            if (!_currentModel.TryValidate(out var validationError))
            {
                Debug.LogError(LogPrefix + $"Cannot save corrupted model: {validationError}. Aborting save.");
                return false;
            }

            string json;
            try
            {
                json = JsonUtility.ToJson(_currentModel, false);
            }
            catch (Exception ex)
            {
                Debug.LogError(LogPrefix + $"Failed to serialize SaveModel to JSON: {ex.Message}.");
                return false;
            }

            if (!_store.TrySave(json, out var saveError))
            {
                Debug.LogError(LogPrefix + $"Failed to commit save to store: {saveError}.");
                return false;
            }

            _isDirty = false;
            return true;
        }
    }
}
