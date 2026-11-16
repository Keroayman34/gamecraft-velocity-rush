using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Unlock policy governing event and championship availability (G6/D-061).
    /// </summary>
    /// <remarks>
    /// Approved M8 policy: sequential completion only. Championship C1 must be fully
    /// completed before C2 unlocks; C2 before C3. Within a championship, event N requires
    /// completion of event N-1. No star/position gates, no points thresholds, and no
    /// additional currency requirements for M8.
    /// </remarks>
    public enum EventUnlockPolicy
    {
        /// <summary>Always available (championship 1, event 1).</summary>
        AlwaysAvailable = 0,

        /// <summary>Requires the previous event in the same championship to be completed.</summary>
        RequiresPreviousEvent = 1,

        /// <summary>Requires the entire previous championship to be completed.</summary>
        RequiresPreviousChampionship = 2
    }

    /// <summary>
    /// Pure metadata and validation rules for <see cref="EventUnlockPolicy"/>.
    /// </summary>
    public static class EventUnlockPolicyRules
    {
        private static readonly EventUnlockPolicy[] ApprovedPolicies =
        {
            EventUnlockPolicy.AlwaysAvailable,
            EventUnlockPolicy.RequiresPreviousEvent,
            EventUnlockPolicy.RequiresPreviousChampionship
        };

        /// <summary>All approved unlock policies for M8.</summary>
        public static IReadOnlyList<EventUnlockPolicy> Approved => ApprovedPolicies;

        /// <summary>True when the value is an approved M8 unlock policy.</summary>
        public static bool IsApproved(EventUnlockPolicy policy)
        {
            return policy >= EventUnlockPolicy.AlwaysAvailable && policy <= EventUnlockPolicy.RequiresPreviousChampionship;
        }

        /// <summary>Validates that the policy is approved for M8.</summary>
        public static bool TryValidate(EventUnlockPolicy policy, out string error)
        {
            if (!IsApproved(policy))
            {
                error = $"EventUnlockPolicy '{policy}' (value {(int)policy}) is not approved for M8.";
                return false;
            }
            error = null;
            return true;
        }
    }
}