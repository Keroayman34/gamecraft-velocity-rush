using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Opponent mode for a race event (G3/D-061).
    /// </summary>
    /// <remarks>
    /// CircuitRace events use exactly 4 AI opponents (D-060 default grid of 5 cars).
    /// TimeTrial events have no opponents.
    /// </remarks>
    public enum EventOpponentMode
    {
        /// <summary>No opponents — solo time trial.</summary>
        None = 0,

        /// <summary>Fixed grid of 4 AI opponents (5 total including player).</summary>
        FixedFourAI = 1
    }

    /// <summary>
    /// Pure metadata and validation rules for <see cref="EventOpponentMode"/>.
    /// </summary>
    public static class EventOpponentModeRules
    {
        private static readonly EventOpponentMode[] ApprovedModes =
        {
            EventOpponentMode.None,
            EventOpponentMode.FixedFourAI
        };

        /// <summary>All approved opponent modes for M8.</summary>
        public static IReadOnlyList<EventOpponentMode> Approved => ApprovedModes;

        /// <summary>True when the value is an approved M8 opponent mode.</summary>
        public static bool IsApproved(EventOpponentMode mode)
        {
            return mode == EventOpponentMode.None || mode == EventOpponentMode.FixedFourAI;
        }

        /// <summary>Returns the approved mode for a given event type.</summary>
        public static EventOpponentMode ModeForEventType(RaceEventType eventType)
        {
            switch (eventType)
            {
                case RaceEventType.CircuitRace:
                    return EventOpponentMode.FixedFourAI;
                case RaceEventType.TimeTrial:
                    return EventOpponentMode.None;
                default:
                    return EventOpponentMode.None;
            }
        }

        /// <summary>Validates that the opponent mode matches the event type.</summary>
        public static bool TryValidateForEventType(
            EventOpponentMode mode,
            RaceEventType eventType,
            out string error)
        {
            var expected = ModeForEventType(eventType);
            if (mode != expected)
            {
                error = $"EventOpponentMode '{mode}' does not match {eventType} (expected '{expected}').";
                return false;
            }

            if (!IsApproved(mode))
            {
                error = $"EventOpponentMode '{mode}' is not approved for M8.";
                return false;
            }

            error = null;
            return true;
        }
    }
}