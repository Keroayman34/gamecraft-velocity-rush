using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Stable, read-only definition for one M8 championship (T-10.6.x, G1/G6/G7/G8/G9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three championships approved for M8 per G1/D-029:
    /// - C1: "Ignition Series" / "سلسلة الانطلاق" (Difficulty: Low, pace scale 0.90)
    /// - C2: "Apex Challenger" / "تحدي القمة" (Difficulty: Medium, pace scale 0.97)
    /// - C3: "Velocity Masters" / "أسياد السرعة" (Difficulty: High, pace scale 1.03)
    /// </para>
    /// <para>
    /// Each championship contains exactly 5 events (G2), utilizing all 5 environment themes
    /// across 10 configurations (5 signature + 5 reverse). Events follow systematic naming
    /// per G8/G9: "<Championship Name> - Round N" (EN) and "<اسم البطولة> - الجولة N" (AR).
    /// </para>
    /// <para>
    /// Unlock follows the approved sequential chain (G6/D-061): C1 is always available;
    /// C2 requires C1 completion; C3 requires C2 completion. No star/position gates for M8.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Championship Definition", fileName = "ChampionshipDefinition")]
    public sealed class ChampionshipDefinition : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Maximum accepted length of a stable championship id.</summary>
        public const int MaxChampionshipIdLength = 48;

        /// <summary>Maximum accepted length of a localized display name.</summary>
        public const int MaxDisplayNameLength = 80;

        /// <summary>Fixed number of events per championship for M8 (G2).</summary>
        public const int EventsPerChampionship = 5;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Tooltip("Stable, lowercase ASCII id (for example c1-ignition-series). Never rename after content references it.")]
        [SerializeField] private string championshipId = string.Empty;

        [Tooltip("English display name per G1.")]
        [SerializeField] private string displayNameEn = string.Empty;

        [Tooltip("Arabic display name per G1.")]
        [SerializeField] private string displayNameAr = string.Empty;

        [Tooltip("Difficulty tier name for this championship.")]
        [SerializeField] private string difficultyNameEn = string.Empty;

        [Tooltip("Arabic difficulty tier name.")]
        [SerializeField] private string difficultyNameAr = string.Empty;

        [Tooltip("Approved production order: C1=1, C2=2, C3=3.")]
        [SerializeField] private int productionOrder = 1;

        [Tooltip("AI pace scale for this championship (C1=0.90, C2=0.97, C3=1.03 per G5/D-061).")]
        [SerializeField, Range(0.5f, 1.5f)] private float paceScale = 0.90f;

        [Tooltip("Unlock policy governing championship availability (G6/D-061).")]
        [SerializeField] private EventUnlockPolicy unlockPolicy = EventUnlockPolicy.AlwaysAvailable;

        [Tooltip("Previous championship in the unlock chain. Null only for the single root championship.")]
        [SerializeField] private ChampionshipDefinition priorChampionship;

        [Tooltip("Optional description for debugging/authoring.")]
        [SerializeField] private string description = string.Empty;

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Stable, lowercase ASCII championship id.</summary>
        public string ChampionshipId => championshipId;

        /// <summary>English display name.</summary>
        public string DisplayNameEn => displayNameEn;

        /// <summary>Arabic display name.</summary>
        public string DisplayNameAr => displayNameAr;

        /// <summary>English difficulty name.</summary>
    public string DifficultyNameEn => difficultyNameEn;

        /// <summary>Arabic difficulty name.</summary>
        public string DifficultyNameAr => difficultyNameAr;

        /// <summary>Approved 1-based production order (C1=1, C2=2, C3=3).</summary>
        public int ProductionOrder => productionOrder;

        /// <summary>AI pace scale for this championship's difficulty tier.</summary>
        public float PaceScale => paceScale;

        /// <summary>Unlock policy governing availability.</summary>
        public EventUnlockPolicy UnlockPolicy => unlockPolicy;

        /// <summary>
        /// Previous championship in the unlock chain, or null for the single root (C1).
        /// </summary>
        public ChampionshipDefinition PriorChampionship => priorChampionship;

        /// <summary>Authoring description.</summary>
        public string Description => description;

        /// <summary>
        /// Validates identity, production order consistency, id format, and pace scale.
        /// Returns the first problem with a field-specific message.
        /// </summary>
        public bool TryValidate(out string error)
        {
            var issues = Validate();
            if (issues.Count == 0)
            {
                error = null;
                return true;
            }

            error = issues[0];
            return false;
        }

        /// <summary>Returns every definition problem at once (cold authoring/validation path).</summary>
        public System.Collections.Generic.IReadOnlyList<string> Validate()
        {
            var issues = new System.Collections.Generic.List<string>();

            if (schemaVersion != CurrentSchemaVersion)
            {
                issues.Add($"ChampionshipDefinition: unsupported SchemaVersion {schemaVersion}; expected {CurrentSchemaVersion}.");
            }

            if (!TryValidateChampionshipId(championshipId, out var idError))
            {
                issues.Add($"ChampionshipDefinition: {idError}");
            }

            ValidateDisplayName(displayNameEn, "DisplayNameEn", issues);
            ValidateDisplayName(displayNameAr, "DisplayNameAr", issues);

            ValidateDisplayName(difficultyNameEn, "DifficultyNameEn", issues);
            ValidateDisplayName(difficultyNameAr, "DifficultyNameAr", issues);

            if (productionOrder < 1 || productionOrder > 3)
            {
                issues.Add($"ChampionshipDefinition: ProductionOrder must be within 1..3 (M8 has exactly 3 championships).");
            }

            if (!IsFinite(paceScale) || paceScale < 0.5f || paceScale > 1.5f)
            {
                issues.Add($"ChampionshipDefinition: PaceScale must be finite and within 0.5..1.5 (found {paceScale}).");
            }

            // Approved pace scales per G5/D-061
            var approvedScales = new[] { 0.90f, 0.97f, 1.03f };
            var scaleMatches = false;
            foreach (var approvedScale in approvedScales)
            {
                if (Mathf.Abs(paceScale - approvedScale) < 0.001f)
                {
                    scaleMatches = true;
                    break;
                }
            }
            if (!scaleMatches)
            {
                issues.Add($"ChampionshipDefinition: PaceScale {paceScale} does not match approved values (0.90 C1, 0.97 C2, 1.03 C3).");
            }

            if (!EventUnlockPolicyRules.TryValidate(unlockPolicy, out var unlockError))
            {
                issues.Add($"ChampionshipDefinition: {unlockError}");
            }

            // Production order must match pace scale
            var expectedPaceScale = productionOrder switch
            {
                1 => 0.90f,
                2 => 0.97f,
                3 => 1.03f,
                _ => -1f
            };
            if (expectedPaceScale > 0f && Mathf.Abs(paceScale - expectedPaceScale) > 0.001f)
            {
                issues.Add($"ChampionshipDefinition: PaceScale must be {expectedPaceScale} for ProductionOrder {productionOrder} (found {paceScale}).");
            }

            return issues;
        }

        /// <summary>
        /// Validates the stable-id contract: non-empty, at most MaxChampionshipIdLength characters,
        /// starts with a lowercase ASCII letter, and contains only lowercase ASCII letters,
        /// digits, and single hyphens.
        /// </summary>
        public static bool TryValidateChampionshipId(string candidate, out string error)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "ChampionshipId must not be empty.";
                return false;
            }

            if (candidate.Length > MaxChampionshipIdLength)
            {
                error = $"ChampionshipId must be at most {MaxChampionshipIdLength} characters.";
                return false;
            }

            if (candidate[0] < 'a' || candidate[0] > 'z')
            {
                error = "ChampionshipId must start with a lowercase ASCII letter.";
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
                        error = "ChampionshipId must not contain consecutive or trailing hyphens.";
                        return false;
                    }

                    previousWasHyphen = true;
                    continue;
                }

                error = "ChampionshipId may contain only lowercase lowercase ASCII letters, digits, and single hyphens.";
                return false;
            }

            error = null;
            return true;
        }

        private static void ValidateDisplayName(
            string value,
            string fieldName,
            System.Collections.Generic.List<string> issues)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                issues.Add($"ChampionshipDefinition: {fieldName} must not be empty.");
                return;
            }

            if (value.Length > MaxDisplayNameLength)
            {
                issues.Add($"ChampionshipDefinition: {fieldName} must be at most {MaxDisplayNameLength} characters.");
                return;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                issues.Add($"ChampionshipDefinition: {fieldName} must not have leading or trailing whitespace.");
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    issues.Add($"ChampionshipDefinition: {fieldName} must not contain control characters.");
                    return;
                }
            }
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor-only authoring seam used by the deterministic championship builder and tests.
        /// Runtime code must treat definitions as read-only.
        /// </summary>
        public void ConfigureForAuthoring(
            string id,
            string nameEn,
            string nameAr,
            string diffNameEn,
            string diffNameAr,
            int order,
            float paceScaleValue,
            EventUnlockPolicy unlock,
            string authoringDescription,
            ChampionshipDefinition prior = null)
        {
            schemaVersion = CurrentSchemaVersion;
            championshipId = id;
            displayNameEn = nameEn;
            displayNameAr = nameAr;
            difficultyNameEn = diffNameEn;
            difficultyNameAr = diffNameAr;
            productionOrder = order;
            paceScale = paceScaleValue;
            unlockPolicy = unlock;
            priorChampionship = prior;
            description = authoringDescription;
        }
#endif
    }
}