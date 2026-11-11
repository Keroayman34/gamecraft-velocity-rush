using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-authored reference set for the complete vehicle roster (T-8.3/T-8.5/T-8.9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The catalog holds references only. Identity lives in each <see cref="VehicleDefinition"/>
    /// and handling values live in its <see cref="VehiclePhysicsProfile"/>, so this component can
    /// never become a second source of truth. It exists so runtime systems (the balance harness
    /// and the Garage foundation) can resolve the roster without a Resources load or a new
    /// loading strategy decision (D-035 remains open on that point).
    /// </para>
    /// <para>
    /// Validation delegates to <see cref="VehicleRosterRules"/>, which enforces the 2/3/2/3
    /// distribution and every per-entry schema rule.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class VehicleRosterCatalog : MonoBehaviour
    {
        [Tooltip("The ten Phase-8 vehicle definitions in owner-approved roster order.")]
        [SerializeField] private VehicleDefinition[] vehicles = Array.Empty<VehicleDefinition>();

        /// <summary>Read-only roster view; never null.</summary>
        public VehicleDefinition[] Vehicles => vehicles ?? Array.Empty<VehicleDefinition>();

        /// <summary>Number of definitions in the catalog.</summary>
        public int Count => Vehicles.Length;

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam for the deterministic scene builders. Runtime code must
        /// treat the roster as read-only and resolve it from the scene.
        /// </summary>
        /// <param name="definitions">Definitions in owner-approved roster order.</param>
        public void ConfigureForAuthoring(VehicleDefinition[] definitions)
        {
            vehicles = definitions ?? Array.Empty<VehicleDefinition>();
        }
#endif

        /// <summary>Validates the complete roster, returning every issue at once.</summary>
        public bool TryValidate(out string error)
        {
            var issues = VehicleRosterRules.ValidateRoster(Vehicles);
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = string.Join("\n", issues);
            return false;
        }
    }
}
