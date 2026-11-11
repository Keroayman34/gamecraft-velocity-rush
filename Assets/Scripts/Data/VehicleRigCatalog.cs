using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-authored mapping from a roster <see cref="VehicleDefinition"/> to its generated rig
    /// prefab (T-8.11).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The catalog holds references only, mirroring <see cref="VehicleRosterCatalog"/>: identity
    /// stays in the definition and handling stays in its physics profile. Runtime code resolves a
    /// selected vehicle id to a prefab without a Resources load, addressables package, or a new
    /// loading-strategy decision (D-035 remains open on that point).
    /// </para>
    /// <para>
    /// This is a debug-flow seam. Phase 11's GameFlow may replace it with an approved streaming
    /// strategy; the validation contract (one entry per roster definition, unique ids, non-null
    /// prefab) stays the same.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleRigCatalog : MonoBehaviour
    {
        [Tooltip("One entry per roster vehicle: definition plus its generated rig prefab.")]
        [SerializeField] private VehicleRigEntry[] entries = Array.Empty<VehicleRigEntry>();

        /// <summary>Number of definition-to-prefab entries.</summary>
        public int Count => Entries.Length;

        /// <summary>Read-only entry view; never null.</summary>
        public VehicleRigEntry[] Entries => entries ?? Array.Empty<VehicleRigEntry>();

        /// <summary>
        /// Resolves a stable vehicle id to its definition and generated rig prefab.
        /// </summary>
        /// <param name="vehicleId">Stable <c>VehicleDefinition.VehicleId</c>.</param>
        /// <param name="definition">Resolved definition when found.</param>
        /// <param name="rigPrefab">Resolved rig prefab when found.</param>
        /// <returns>True when a complete entry matched.</returns>
        public bool TryGetRig(string vehicleId, out VehicleDefinition definition, out GameObject rigPrefab)
        {
            definition = null;
            rigPrefab = null;
            if (string.IsNullOrEmpty(vehicleId))
            {
                return false;
            }

            var entriesView = Entries;
            for (var i = 0; i < entriesView.Length; i++)
            {
                var entry = entriesView[i];
                if (entry == null || entry.Definition == null || entry.RigPrefab == null)
                {
                    continue;
                }

                if (!string.Equals(entry.Definition.VehicleId, vehicleId, StringComparison.Ordinal))
                {
                    continue;
                }

                definition = entry.Definition;
                rigPrefab = entry.RigPrefab;
                return true;
            }

            return false;
        }

        /// <summary>Validates every entry and rejects duplicate vehicle ids.</summary>
        public bool TryValidate(out string error)
        {
            var entriesView = Entries;
            if (entriesView.Length == 0)
            {
                error = "VehicleRigCatalog has no entries.";
                return false;
            }

            for (var i = 0; i < entriesView.Length; i++)
            {
                var entry = entriesView[i];
                if (entry == null)
                {
                    error = "VehicleRigCatalog entry at slot " + i + " is missing.";
                    return false;
                }

                if (entry.Definition == null)
                {
                    error = "VehicleRigCatalog entry at slot " + i + " has no definition.";
                    return false;
                }

                if (!entry.Definition.TryValidate(out var definitionError))
                {
                    error = "VehicleRigCatalog entry at slot " + i + " is invalid: " + definitionError;
                    return false;
                }

                if (entry.RigPrefab == null)
                {
                    error = "VehicleRigCatalog entry '" + entry.Definition.VehicleId + "' has no rig prefab.";
                    return false;
                }

                for (var other = 0; other < i; other++)
                {
                    var candidate = entriesView[other];
                    if (candidate != null && candidate.Definition != null &&
                        string.Equals(
                            candidate.Definition.VehicleId,
                            entry.Definition.VehicleId,
                            StringComparison.Ordinal))
                    {
                        error = "VehicleRigCatalog contains duplicate vehicle id '" +
                                entry.Definition.VehicleId + "'.";
                        return false;
                    }
                }
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam for the deterministic scene builders. Runtime code treats the
        /// catalog as read-only and resolves it from the scene.
        /// </summary>
        /// <param name="definitions">Definitions in roster order.</param>
        /// <param name="rigPrefabs">Matching rig prefabs in the same order.</param>
        public void ConfigureForAuthoring(VehicleDefinition[] definitions, GameObject[] rigPrefabs)
        {
            if (definitions == null || rigPrefabs == null || definitions.Length != rigPrefabs.Length)
            {
                throw new ArgumentException("Definitions and rig prefabs must be non-null and the same length.");
            }

            var configured = new VehicleRigEntry[definitions.Length];
            for (var i = 0; i < definitions.Length; i++)
            {
                configured[i] = new VehicleRigEntry(definitions[i], rigPrefabs[i]);
            }

            entries = configured;
        }
#endif

        /// <summary>One definition-to-prefab pair.</summary>
        [Serializable]
        public sealed class VehicleRigEntry
        {
            [SerializeField] private VehicleDefinition definition;
            [SerializeField] private GameObject rigPrefab;

            /// <summary>Roster definition for this entry.</summary>
            public VehicleDefinition Definition => definition;

            /// <summary>Generated rig prefab for this entry.</summary>
            public GameObject RigPrefab => rigPrefab;

            /// <summary>Builds an entry; used by the editor authoring seam.</summary>
            public VehicleRigEntry(VehicleDefinition definition, GameObject rigPrefab)
            {
                this.definition = definition;
                this.rigPrefab = rigPrefab;
            }
        }
    }
}
