using System;
using System.Collections.Generic;

namespace VelocityRush.Data
{
    /// <summary>
    /// Pure, deterministic registry mapping all 15 M8 championship events to calibrated
    /// highway difficulty, coin economy, and rival competition parameters (Phase 12 Stage 4; TDD §61).
    /// </summary>
    public static class HighwayDifficultyTable
    {
        public const string DefaultEventId = "c1-ignition-series-round-1";

        public static readonly string[] AllEventIds = new string[]
        {
            "c1-ignition-series-round-1",
            "c1-ignition-series-round-2",
            "c1-ignition-series-round-3",
            "c1-ignition-series-round-4",
            "c1-ignition-series-round-5",
            "c2-apex-challenger-round-1",
            "c2-apex-challenger-round-2",
            "c2-apex-challenger-round-3",
            "c2-apex-challenger-round-4",
            "c2-apex-challenger-round-5",
            "c3-velocity-masters-round-1",
            "c3-velocity-masters-round-2",
            "c3-velocity-masters-round-3",
            "c3-velocity-masters-round-4",
            "c3-velocity-masters-round-5"
        };

        private static readonly Dictionary<string, HighwayDifficultyProfile> Profiles = BuildProfiles();

        public static HighwayDifficultyProfile DefaultProfile => Profiles[DefaultEventId];

        /// <summary>
        /// Resolves the difficulty profile for the specified event id.
        /// Falls back safely to DefaultProfile (C1 Round 1) if eventId is null, empty, or unknown.
        /// Never throws or returns NaN/unvalidated values.
        /// </summary>
        public static HighwayDifficultyProfile Resolve(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return DefaultProfile;
            }

            var trimmed = eventId.Trim().ToLowerInvariant();
            if (Profiles.TryGetValue(trimmed, out var profile))
            {
                return profile;
            }

            return DefaultProfile;
        }

        /// <summary>
        /// Attempts to get the profile for an event id. Returns false and provides DefaultProfile if not registered.
        /// </summary>
        public static bool TryGetProfile(string eventId, out HighwayDifficultyProfile profile)
        {
            if (!string.IsNullOrWhiteSpace(eventId) && Profiles.TryGetValue(eventId.Trim().ToLowerInvariant(), out profile))
            {
                return true;
            }

            profile = DefaultProfile;
            return false;
        }

        private static Dictionary<string, HighwayDifficultyProfile> BuildProfiles()
        {
            var dict = new Dictionary<string, HighwayDifficultyProfile>(15, StringComparer.OrdinalIgnoreCase);

            // =========================================================================
            // Championship 1: Ignition Series (c1-ignition-series, Rounds 1-5)
            // Forgiving traffic, generous early upgrade coin funding, forgiving rivals.
            // =========================================================================
            dict["c1-ignition-series-round-1"] = new HighwayDifficultyProfile(
                "c1-ignition-series-round-1",
                trafficDensity: 0.30f,
                trafficMinSpeed: 10.0f,
                trafficMaxSpeed: 18.0f,
                coinArcSpacing: 35.0f,
                coinValue: 10,
                nearMissBonusCredits: 25,
                rivalBasePaceMultiplier: 0.88f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 28f,
                    maxCatchUpBoost = 1.08f,
                    slowDownStartGap = 30f,
                    maxSlowDownFactor = 0.95f,
                    deadZone = 15f,
                    minMultiplier = 0.88f,
                    maxMultiplier = 1.10f
                });

            dict["c1-ignition-series-round-2"] = new HighwayDifficultyProfile(
                "c1-ignition-series-round-2",
                trafficDensity: 0.33f,
                trafficMinSpeed: 10.5f,
                trafficMaxSpeed: 19.0f,
                coinArcSpacing: 37.0f,
                coinValue: 10,
                nearMissBonusCredits: 25,
                rivalBasePaceMultiplier: 0.895f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 27f,
                    maxCatchUpBoost = 1.09f,
                    slowDownStartGap = 31f,
                    maxSlowDownFactor = 0.94f,
                    deadZone = 15f,
                    minMultiplier = 0.88f,
                    maxMultiplier = 1.11f
                });

            dict["c1-ignition-series-round-3"] = new HighwayDifficultyProfile(
                "c1-ignition-series-round-3",
                trafficDensity: 0.37f,
                trafficMinSpeed: 11.0f,
                trafficMaxSpeed: 20.0f,
                coinArcSpacing: 40.0f,
                coinValue: 10,
                nearMissBonusCredits: 25,
                rivalBasePaceMultiplier: 0.91f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 26f,
                    maxCatchUpBoost = 1.10f,
                    slowDownStartGap = 32f,
                    maxSlowDownFactor = 0.94f,
                    deadZone = 15f,
                    minMultiplier = 0.87f,
                    maxMultiplier = 1.12f
                });

            dict["c1-ignition-series-round-4"] = new HighwayDifficultyProfile(
                "c1-ignition-series-round-4",
                trafficDensity: 0.41f,
                trafficMinSpeed: 11.5f,
                trafficMaxSpeed: 21.0f,
                coinArcSpacing: 42.0f,
                coinValue: 10,
                nearMissBonusCredits: 25,
                rivalBasePaceMultiplier: 0.925f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 25f,
                    maxCatchUpBoost = 1.10f,
                    slowDownStartGap = 33f,
                    maxSlowDownFactor = 0.93f,
                    deadZone = 15f,
                    minMultiplier = 0.87f,
                    maxMultiplier = 1.12f
                });

            dict["c1-ignition-series-round-5"] = new HighwayDifficultyProfile(
                "c1-ignition-series-round-5",
                trafficDensity: 0.45f,
                trafficMinSpeed: 12.0f,
                trafficMaxSpeed: 22.0f,
                coinArcSpacing: 45.0f,
                coinValue: 10,
                nearMissBonusCredits: 25,
                rivalBasePaceMultiplier: 0.94f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 25f,
                    maxCatchUpBoost = 1.11f,
                    slowDownStartGap = 34f,
                    maxSlowDownFactor = 0.93f,
                    deadZone = 15f,
                    minMultiplier = 0.86f,
                    maxMultiplier = 1.13f
                });

            // =========================================================================
            // Championship 2: Apex Challenger (c2-apex-challenger, Rounds 1-5)
            // Moderate traffic density, higher credit rewards, competitive rivals.
            // =========================================================================
            dict["c2-apex-challenger-round-1"] = new HighwayDifficultyProfile(
                "c2-apex-challenger-round-1",
                trafficDensity: 0.50f,
                trafficMinSpeed: 13.0f,
                trafficMaxSpeed: 23.0f,
                coinArcSpacing: 45.0f,
                coinValue: 15,
                nearMissBonusCredits: 35,
                rivalBasePaceMultiplier: 0.95f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 25f,
                    maxCatchUpBoost = 1.12f,
                    slowDownStartGap = 35f,
                    maxSlowDownFactor = 0.92f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.15f
                });

            dict["c2-apex-challenger-round-2"] = new HighwayDifficultyProfile(
                "c2-apex-challenger-round-2",
                trafficDensity: 0.53f,
                trafficMinSpeed: 13.5f,
                trafficMaxSpeed: 24.0f,
                coinArcSpacing: 47.0f,
                coinValue: 15,
                nearMissBonusCredits: 35,
                rivalBasePaceMultiplier: 0.962f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 24f,
                    maxCatchUpBoost = 1.12f,
                    slowDownStartGap = 35f,
                    maxSlowDownFactor = 0.92f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.15f
                });

            dict["c2-apex-challenger-round-3"] = new HighwayDifficultyProfile(
                "c2-apex-challenger-round-3",
                trafficDensity: 0.57f,
                trafficMinSpeed: 14.0f,
                trafficMaxSpeed: 25.0f,
                coinArcSpacing: 50.0f,
                coinValue: 15,
                nearMissBonusCredits: 35,
                rivalBasePaceMultiplier: 0.975f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 24f,
                    maxCatchUpBoost = 1.13f,
                    slowDownStartGap = 36f,
                    maxSlowDownFactor = 0.91f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.15f
                });

            dict["c2-apex-challenger-round-4"] = new HighwayDifficultyProfile(
                "c2-apex-challenger-round-4",
                trafficDensity: 0.61f,
                trafficMinSpeed: 14.5f,
                trafficMaxSpeed: 26.0f,
                coinArcSpacing: 52.0f,
                coinValue: 15,
                nearMissBonusCredits: 35,
                rivalBasePaceMultiplier: 0.988f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 23f,
                    maxCatchUpBoost = 1.13f,
                    slowDownStartGap = 36f,
                    maxSlowDownFactor = 0.91f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.16f
                });

            dict["c2-apex-challenger-round-5"] = new HighwayDifficultyProfile(
                "c2-apex-challenger-round-5",
                trafficDensity: 0.65f,
                trafficMinSpeed: 15.0f,
                trafficMaxSpeed: 27.0f,
                coinArcSpacing: 55.0f,
                coinValue: 15,
                nearMissBonusCredits: 35,
                rivalBasePaceMultiplier: 1.00f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 23f,
                    maxCatchUpBoost = 1.14f,
                    slowDownStartGap = 37f,
                    maxSlowDownFactor = 0.90f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.16f
                });

            // =========================================================================
            // Championship 3: Velocity Masters (c3-velocity-masters, Rounds 1-5)
            // Dense high-speed traffic, high stakes near-miss and coins, boss rivals.
            // =========================================================================
            dict["c3-velocity-masters-round-1"] = new HighwayDifficultyProfile(
                "c3-velocity-masters-round-1",
                trafficDensity: 0.68f,
                trafficMinSpeed: 16.0f,
                trafficMaxSpeed: 28.0f,
                coinArcSpacing: 55.0f,
                coinValue: 20,
                nearMissBonusCredits: 50,
                rivalBasePaceMultiplier: 1.01f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 22f,
                    maxCatchUpBoost = 1.15f,
                    slowDownStartGap = 38f,
                    maxSlowDownFactor = 0.90f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.18f
                });

            dict["c3-velocity-masters-round-2"] = new HighwayDifficultyProfile(
                "c3-velocity-masters-round-2",
                trafficDensity: 0.71f,
                trafficMinSpeed: 16.5f,
                trafficMaxSpeed: 29.0f,
                coinArcSpacing: 57.0f,
                coinValue: 20,
                nearMissBonusCredits: 50,
                rivalBasePaceMultiplier: 1.022f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 22f,
                    maxCatchUpBoost = 1.15f,
                    slowDownStartGap = 38f,
                    maxSlowDownFactor = 0.89f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.18f
                });

            dict["c3-velocity-masters-round-3"] = new HighwayDifficultyProfile(
                "c3-velocity-masters-round-3",
                trafficDensity: 0.75f,
                trafficMinSpeed: 17.0f,
                trafficMaxSpeed: 30.0f,
                coinArcSpacing: 60.0f,
                coinValue: 20,
                nearMissBonusCredits: 50,
                rivalBasePaceMultiplier: 1.035f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 21f,
                    maxCatchUpBoost = 1.16f,
                    slowDownStartGap = 39f,
                    maxSlowDownFactor = 0.89f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.18f
                });

            dict["c3-velocity-masters-round-4"] = new HighwayDifficultyProfile(
                "c3-velocity-masters-round-4",
                trafficDensity: 0.78f,
                trafficMinSpeed: 17.5f,
                trafficMaxSpeed: 31.0f,
                coinArcSpacing: 62.0f,
                coinValue: 20,
                nearMissBonusCredits: 50,
                rivalBasePaceMultiplier: 1.048f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 20f,
                    maxCatchUpBoost = 1.16f,
                    slowDownStartGap = 40f,
                    maxSlowDownFactor = 0.88f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.18f
                });

            dict["c3-velocity-masters-round-5"] = new HighwayDifficultyProfile(
                "c3-velocity-masters-round-5",
                trafficDensity: 0.82f,
                trafficMinSpeed: 18.0f,
                trafficMaxSpeed: 32.0f,
                coinArcSpacing: 65.0f,
                coinValue: 20,
                nearMissBonusCredits: 50,
                rivalBasePaceMultiplier: 1.06f,
                rubberBand: new RubberBandConfig
                {
                    catchUpStartGap = 20f,
                    maxCatchUpBoost = 1.17f,
                    slowDownStartGap = 40f,
                    maxSlowDownFactor = 0.88f,
                    deadZone = 15f,
                    minMultiplier = 0.85f,
                    maxMultiplier = 1.18f
                });

            return dict;
        }
    }
}
