using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Stable, read-only roster definition for one Commercial V1.0 vehicle (D-010/D-014;
    /// TDD §36.7; D-035 definition-template rules).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This asset owns <b>identity and references only</b>. Handling values live exclusively in
    /// the referenced <see cref="VehiclePhysicsProfile"/>, so the roster can never become a
    /// second source of truth for physics. Visual rig and upgrade bindings are added in later
    /// Phase-8 stages with an explicit <see cref="CurrentSchemaVersion"/> bump.
    /// </para>
    /// <para>
    /// Roster identities and statistics are owner-gated (OQ-14/OQ-06). This schema ships with
    /// no invented vehicle content; <see cref="TryValidate"/> is the single authoring/runtime
    /// validation seam.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Vehicle Definition", fileName = "VehicleDefinition")]
    public sealed class VehicleDefinition : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Maximum accepted length of a stable vehicle id.</summary>
        public const int MaxVehicleIdLength = 40;

        /// <summary>Maximum accepted length of a localized display name.</summary>
        public const int MaxDisplayNameLength = 80;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Tooltip("Stable, lowercase ASCII id (for example starter-01). Never rename after content references it.")]
        [SerializeField] private string vehicleId = string.Empty;

        [Tooltip("English display name (owner-approved OQ-14 identity).")]
        [SerializeField] private string displayNameEn = string.Empty;

        [Tooltip("Arabic display name (owner-approved OQ-14 identity).")]
        [SerializeField] private string displayNameAr = string.Empty;

        [Tooltip("Commercial roster class (D-014 planning distribution).")]
        [SerializeField] private VehicleClass vehicleClass = VehicleClass.Starter;

        [Tooltip("The only source of handling values for this vehicle (OQ-06).")]
        [SerializeField] private VehiclePhysicsProfile physicsProfile;

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Stable, lowercase ASCII roster id.</summary>
        public string VehicleId => vehicleId;

        /// <summary>English display name.</summary>
        public string DisplayNameEn => displayNameEn;

        /// <summary>Arabic display name.</summary>
        public string DisplayNameAr => displayNameAr;

        /// <summary>Commercial roster class.</summary>
        public VehicleClass Class => vehicleClass;

        /// <summary>Referenced handling profile; this asset never duplicates its values.</summary>
        public VehiclePhysicsProfile PhysicsProfile => physicsProfile;

        /// <summary>
        /// Checks schema version, stable id, both localized names, the class value, and the
        /// physics-profile reference. Returns the first problem with a field-specific message.
        /// </summary>
        public bool TryValidate(out string error)
        {
            if (schemaVersion != CurrentSchemaVersion)
            {
                error = "VehicleDefinition: unsupported SchemaVersion " + schemaVersion +
                        "; expected " + CurrentSchemaVersion + ".";
                return false;
            }

            if (!TryValidateVehicleId(vehicleId, out var idError))
            {
                error = "VehicleDefinition: " + idError;
                return false;
            }

            if (!TryValidateDisplayName(displayNameEn, "DisplayNameEn", out var englishError))
            {
                error = "VehicleDefinition: " + englishError;
                return false;
            }

            if (!TryValidateDisplayName(displayNameAr, "DisplayNameAr", out var arabicError))
            {
                error = "VehicleDefinition: " + arabicError;
                return false;
            }

            if (!IsDefinedClass(vehicleClass))
            {
                error = "VehicleDefinition: VehicleClass value " + (int)vehicleClass +
                        " is not a defined roster class.";
                return false;
            }

            if (physicsProfile == null)
            {
                error = "VehicleDefinition: PhysicsProfile reference is missing.";
                return false;
            }

            if (!physicsProfile.TryValidate(out var profileError))
            {
                error = "VehicleDefinition: PhysicsProfile is invalid: " + profileError;
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Validates the stable-id contract: non-empty, at most <see cref="MaxVehicleIdLength"/>
        /// characters, starts with a lowercase ASCII letter, and contains only lowercase ASCII
        /// letters, digits, and single hyphens (no leading/trailing/consecutive hyphen).
        /// </summary>
        public static bool TryValidateVehicleId(string candidate, out string error)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "VehicleId must not be empty.";
                return false;
            }

            if (candidate.Length > MaxVehicleIdLength)
            {
                error = "VehicleId must be at most " + MaxVehicleIdLength + " characters.";
                return false;
            }

            if (candidate[0] < 'a' || candidate[0] > 'z')
            {
                error = "VehicleId must start with a lowercase ASCII letter.";
                return false;
            }

            var previousWasHyphen = false;
            for (var i = 0; i < candidate.Length; i++)
            {
                var character = candidate[i];
                if ((character >= 'a' && character <= 'z') || (character >= '0' && character <= '9'))
                {
                    previousWasHyphen = false;
                    continue;
                }

                if (character == '-')
                {
                    if (previousWasHyphen || i == candidate.Length - 1)
                    {
                        error = "VehicleId must not contain consecutive or trailing hyphens.";
                        return false;
                    }

                    previousWasHyphen = true;
                    continue;
                }

                error = "VehicleId may contain only lowercase ASCII letters, digits, and single hyphens.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryValidateDisplayName(string value, string fieldName, out string error)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                error = fieldName + " must not be empty.";
                return false;
            }

            if (value.Length > MaxDisplayNameLength)
            {
                error = fieldName + " must be at most " + MaxDisplayNameLength + " characters.";
                return false;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                error = fieldName + " must not have leading or trailing whitespace.";
                return false;
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    error = fieldName + " must not contain control characters.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private static bool IsDefinedClass(VehicleClass value)
        {
            return value >= VehicleClass.Starter && value <= VehicleClass.Supercar;
        }
    }
}
