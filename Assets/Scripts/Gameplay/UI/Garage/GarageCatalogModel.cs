using System;
using System.Collections.Generic;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure selection state for the Garage catalog (T-8.9): browse, wrap, select, confirm.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Construction projects the roster into a fixed array once. Every navigation call mutates an
    /// index and returns a bool; there are no LINQ queries, closures, string constructions, or
    /// event raises after construction, so repeated browsing is allocation-free.
    /// </para>
    /// <para>
    /// Progression, unlocks, ownership, and save state are deliberately absent: this is the
    /// Stage-4 Garage foundation only (Phase 11 owns progression).
    /// </para>
    /// </remarks>
    public sealed class GarageCatalogModel
    {
        private readonly GarageVehicleSummary[] _vehicles;

        /// <summary>Builds the model from a validated roster.</summary>
        /// <param name="definitions">The ten owner-approved vehicle definitions.</param>
        public GarageCatalogModel(IReadOnlyList<VehicleDefinition> definitions)
        {
            if (definitions == null)
            {
                throw new ArgumentNullException(nameof(definitions));
            }

            if (definitions.Count == 0)
            {
                throw new ArgumentException("Garage catalog requires at least one definition.", nameof(definitions));
            }

            _vehicles = new GarageVehicleSummary[definitions.Count];
            for (var i = 0; i < definitions.Count; i++)
            {
                var definition = definitions[i];
                if (definition == null)
                {
                    throw new ArgumentException("Garage catalog contains a null definition.", nameof(definitions));
                }

                _vehicles[i] = GarageVehicleSummary.FromDefinition(definition);
            }

            SelectedIndex = 0;
            ConfirmedIndex = -1;
        }

        /// <summary>Number of vehicles in the catalog.</summary>
        public int Count => _vehicles.Length;

        /// <summary>Currently highlighted index.</summary>
        public int SelectedIndex { get; private set; }

        /// <summary>Last confirmed index, or -1 before the first confirmation.</summary>
        public int ConfirmedIndex { get; private set; }

        /// <summary>The currently highlighted vehicle.</summary>
        public GarageVehicleSummary Current => _vehicles[SelectedIndex];

        /// <summary>Returns one vehicle by index; the caller must stay in range.</summary>
        public GarageVehicleSummary this[int index] => _vehicles[index];

        /// <summary>Moves to the next vehicle, wrapping at the end.</summary>
        /// <returns>True when the selection changed.</returns>
        public bool Next()
        {
            return Select((SelectedIndex + 1) % _vehicles.Length);
        }

        /// <summary>Moves to the previous vehicle, wrapping at the start.</summary>
        /// <returns>True when the selection changed.</returns>
        public bool Previous()
        {
            var next = SelectedIndex - 1;
            if (next < 0)
            {
                next = _vehicles.Length - 1;
            }

            return Select(next);
        }

        /// <summary>Selects an explicit index.</summary>
        /// <returns>True when the selection changed; false for the current index or an invalid one.</returns>
        public bool Select(int index)
        {
            if ((uint)index >= (uint)_vehicles.Length || index == SelectedIndex)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }

        /// <summary>Confirms the current selection for the future race/garage handoff.</summary>
        /// <returns>The confirmed index.</returns>
        public int Confirm()
        {
            ConfirmedIndex = SelectedIndex;
            return ConfirmedIndex;
        }
    }
}
