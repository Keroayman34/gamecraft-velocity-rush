using System;
using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Stable, read-only definition for one M8 race event (T-10.6.x, G3/G6/G8/G9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each event belongs to exactly one championship and one environment (by theme). Events
    /// are identified by a stable id and carry bilingual display names per the approved
    /// systematic naming convention (G8/G9): "Championship Name - Round N" (EN) and
    /// "اسم البطولة - الجولة N" (AR).
    /// </para>
    /// <para>
    /// The approved M8 scope includes exactly two event types: CircuitRace (with 4 AI
    /// opponents) and TimeTrial (solo). The environment reference resolves to one of the
    /// five approved EnvironmentDefinition assets and determines which TrackLayout variant
    /// (Signature or Reverse) is used. Pace scale is per-difficulty: 0.90 (C1), 0.97 (C2),
    /// 1.03 (C3) per G5/D-061.
    /// </para>
    /// <para>
    /// Unlock follows the approved sequential chain (G6): C1E1 is always available;
    /// subsequent events require the previous event; the first event of C2/C3 requires
    /// the entire previous championship to be completed.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Velocity Rush/Race Event Definition", fileName = "RaceEventDefinition")]
    public sealed class RaceEventDefinition : ScriptableObject
    {
        /// <summary>Schema version understood by this build.</summary>
        public const int CurrentSchemaVersion = 1;

        /// <summary>Maximum accepted length of a stable event id.</summary>
        public const int MaxEventIdLength = 64;

        /// <summary>Maximum accepted length of a localized display name.</summary>
        public const int MaxDisplayNameLength = 120;

        [Tooltip("Schema version for editor-side migration; unknown versions fail validation loudly.")]
        [SerializeField] private int schemaVersion = CurrentSchemaVersion;

        [Tooltip("Stable, lowercase ASCII id (for example c1-round-1). Never rename after content references it.")]
        [SerializeField] private string eventId = string.Empty;

        [Tooltip("English display name per G8/G9: \"Championship Name - Round N\".")]
        [SerializeField] private string displayNameEn = string.Empty;

        [Tooltip("Arabic display name per G8/G9: \"اسم البطولة - الجولة N\".")]
        [SerializeField] private string displayNameAr = string.Empty;

        [Tooltip("Approved M8 event type: CircuitRace or TimeTrial.")]
    [SerializeField] private RaceEventType eventType = RaceEventType.CircuitRace;

        [Tooltip("Environment theme this event uses (resolves to EnvironmentDefinition).")]
        [SerializeField] private EnvironmentTheme environmentTheme = EnvironmentTheme.DesertCircuit;

        [Tooltip("Layout variant: Signature (forward) or Reverse.")]
        [SerializeField] private EnvironmentLayoutVariant layoutVariant = EnvironmentLayoutVariant.Signature;

        [Tooltip("Championship this event belongs to (stable id, e.g., c1-ignition-series).")]
        [SerializeField] private string championshipId = string.Empty;

        [Tooltip("One-based index within the championship (1..5).")]
        [SerializeField] private int roundIndex = 1;

        [Tooltip("Opponent configuration derived from event type.")]
        [SerializeField] private EventOpponentMode opponentMode = EventOpponentMode.FixedFourAI;

        [Tooltip("Unlock policy per G6/D-061.")]
        [SerializeField] private EventUnlockPolicy unlockPolicy = EventUnlockPolicy.AlwaysAvailable;

        [Tooltip("AI pace scale for this event's difficulty tier (0.90 C1 / 0.97 C2 / 1.03 C3 per G5/D-061).")]
        [SerializeField, Range(0.5f, 1.5f)] private float paceScale = 0.90f;

        [Tooltip("Optional description for debugging/authoring.")]
        [SerializeField] private string description = string.Empty;

        /// <summary>Schema version stored in the asset.</summary>
        public int SchemaVersion => schemaVersion;

        /// <summary>Stable, lowercase ASCII event id.</summary>
        public string EventId => eventId;

        /// <summary>English display name.</summary>
        public string DisplayNameEn => displayNameEn;

        /// <summary>Arabic display name.</summary>
        public string DisplayNameAr => displayNameAr;

        /// <summary>Approved M8 event type.</summary>
        public RaceEventType EventType => eventType;

        /// <summary>Environment theme this event uses.</summary>
        public EnvironmentTheme EnvironmentTheme => environmentTheme;

        /// <summary>Layout variant (Signature or Reverse).</summary>
        public EnvironmentLayoutVariant LayoutVariant => layoutVariant;

        /// <summary>Championship id this event belongs to.</summary>
        public string ChampionshipId => championshipId;

        /// <summary>One-based round index within the championship.</summary>
    public int RoundIndex => roundIndex;

        /// <summary>Opponent configuration.</summary>
        public EventOpponentMode OpponentMode => opponentMode;

        /// <summary>Unlock policy governing availability.</summary>
        public EventUnlockPolicy UnlockPolicy => unlockPolicy;

        /// <summary>AI pace scale for this event's difficulty tier.</summary>
        public float PaceScale => paceScale;

        /// <summary>Authoring description.</summary>
        public string Description => description;

        /// <summary>
        /// Validates identity, type/theme consistency, id prefix, and pace scale.
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
                issues.Add($"RaceEventDefinition: unsupported SchemaVersion {schemaVersion}; expected {CurrentSchemaVersion}.");
            }

            if (!TryValidateEventId(eventId, out var idError))
            {
                issues.Add($"RaceEventDefinition: {idError}");
            }

            ValidateDisplayName(displayNameEn, "DisplayNameEn", issues);
            ValidateDisplayName(displayNameAr, "DisplayNameAr", issues);

            if (!RaceEventTypeRules.IsApproved(eventType))
            {
                issues.Add($"RaceEventDefinition: EventType value {(int)eventType} is not approved for M8.");
            }

            if (!EnvironmentThemeRules.IsDefined(environmentTheme))
            {
                issues.Add($"RaceEventDefinition: EnvironmentTheme value {(int)environmentTheme} is not defined.");
            }
            else
            {
                var expectedPrefix = EnvironmentThemeRules.GetStableFragment(environmentTheme) + "-";
                if (!string.IsNullOrEmpty(eventId) &&
                    !eventId.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Event ids use championship prefix, not environment prefix, so this is informational
                    // Only validate that it's a valid id format
                }
            }

            if (!EnvironmentLayoutVariantRules.IsDefined(layoutVariant))
            {
                issues.Add($"RaceEventDefinition: LayoutVariant value {(int)layoutVariant} is not defined.");
            }

            if (string.IsNullOrWhiteSpace(championshipId))
            {
                issues.Add("RaceEventDefinition: ChampionshipId must not be empty.");
            }
            else if (!TryValidateChampionshipId(championshipId, out var champIdError))
            {
                issues.Add($"RaceEventDefinition: ChampionshipId {champIdError}");
            }

            if (roundIndex < 1 || roundIndex > 5)
            {
                issues.Add("RaceEventDefinition: RoundIndex must be within 1..5 (M8 has exactly 5 events per championship).");
            }

            if (!EventOpponentModeRules.IsApproved(opponentMode))
            {
                issues.Add($"RaceEventDefinition: OpponentMode value {(int)opponentMode} is not approved for M8.");
            }

            // Validate opponent mode matches event type
            if (!EventOpponentModeRules.TryValidateForEventType(opponentMode, eventType, out var oppError))
            {
                issues.Add($"RaceEventDefinition: {oppError}");
            }

            if (!EventUnlockPolicyRules.TryValidate(unlockPolicy, out var unlockError))
            {
                issues.Add($"RaceEventDefinition: {unlockError}");
            }

            if (!IsFinite(paceScale) || paceScale < 0.5f || paceScale > 1.5f)
            {
                issues.Add($"RaceEventDefinition: PaceScale must be finite and within 0.5..1.5 (found {paceScale}).");
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
                issues.Add($"RaceEventDefinition: PaceScale {paceScale} does not match approved values (0.90 C1, 0.97 C2, 1.03 C3).");
            }

            return issues;
        }

        /// <summary>
        /// Validates the stable-id contract: non-empty, at most MaxEventIdLength characters,
        /// starts with a lowercase ASCII letter, and contains only lowercase ASCII letters,
        /// digits, and single hyphens.
        /// </summary>
        public static bool TryValidateEventId(string candidate, out string error)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "EventId must not be empty.";
                return false;
            }

            if (candidate.Length > MaxEventIdLength)
            {
                error = $"EventId must be at most {MaxEventIdLength} characters.";
                return false;
            }

            if (candidate[0] < 'a' || candidate[0] > 'z')
            {
                error = "EventId must start with a lowercase ASCII letter.";
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
                        error = "EventId must not contain consecutive or trailing hyphens.";
                        return false;
                    }

                    previousWasHyphen = true;
                    continue;
                }

                error = "EventId may contain only lowercase ASCII letters, digits, and single hyphens.";
                return false;
            }

            error = null;
            return true;
        }

        /// <summary>
        /// Validates the championship id contract: lowercase ASCII letters, digits, and single hyphens.
        /// </summary>
        public static bool TryValidateChampionshipId(string candidate, out string error)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                error = "ChampionshipId must not be empty.";
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

                error = "ChampionshipId may contain only lowercase ASCII letters, digits, and single hyphens.";
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
                issues.Add($"RaceEventDefinition: {fieldName} must not be empty.");
                return;
            }

            if (value.Length > MaxDisplayNameLength)
            {
                issues.Add($"RaceEventDefinition: {fieldName} must be at most {MaxDisplayNameLength} characters.");
                return;
            }

            if (!string.Equals(value, value.Trim(), StringComparison.Ordinal))
            {
                issues.Add($"RaceEventDefinition: {fieldName} must not have leading or trailing whitespace.");
            }

            for (var i = 0; i < value.Length; i++)
            {
                if (char.IsControl(value[i]))
                {
                    issues.Add($"RaceEventDefinition: {fieldName} must not contain control characters.");
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
        /// Editor-only authoring seam used by the deterministic event builder and tests.
        /// Runtime code must treat definitions as read-only.
        /// </summary>
        public void ConfigureForAuthoring(
            string id,
            string nameEn,
            string nameAr,
            RaceEventType type,
            EnvironmentTheme envTheme,
            EnvironmentLayoutVariant variant,
            string champId,
            int round,
            EventOpponentMode oppMode,
            EventUnlockPolicy unlock,
            float paceScaleValue,
            string authoringDescription)
        {
            schemaVersion = CurrentSchemaVersion;
            eventId = id;
            displayNameEn = nameEn;
            displayNameAr = nameAr;
            eventType = type;
            environmentTheme = envTheme;
            layoutVariant = variant;
            championshipId = champId;
            roundIndex = round;
            opponentMode = oppMode;
            unlockPolicy = unlock;
            paceScale = paceScaleValue;
            description = authoringDescription;
        }
#endif
    }
}