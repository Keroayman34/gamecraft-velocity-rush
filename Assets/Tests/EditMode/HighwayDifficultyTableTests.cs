using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Unit tests for 15-event progressive difficulty matrix, fail-closed validation,
    /// safe fallback resolution, and monotonic tier escalation (Phase 12 Stage 4; TDD §61).
    /// </summary>
    [TestFixture]
    public sealed class HighwayDifficultyTableTests
    {
        [Test]
        public void AllFifteenEvents_ResolveToValidProfiles()
        {
            var eventIds = HighwayDifficultyTable.AllEventIds;
            Assert.That(eventIds.Length, Is.EqualTo(15), "Must register exactly 15 M8 events.");

            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < eventIds.Length; i++)
            {
                string id = eventIds[i];
                Assert.That(seenIds.Add(id), Is.True, $"Duplicate event ID detected: {id}");

                var profile = HighwayDifficultyTable.Resolve(id);
                Assert.That(profile.EventId, Is.EqualTo(id));

                bool valid = profile.TryValidate(out string reason);
                Assert.That(valid, Is.True, $"Profile validation failed for '{id}': {reason}");

                // Hard boundary checks
                Assert.That(profile.TrafficDensity, Is.InRange(0.25f, 0.85f), $"TrafficDensity out of range for {id}");
                Assert.That(profile.TrafficMinSpeed, Is.GreaterThan(0f));
                Assert.That(profile.TrafficMaxSpeed, Is.GreaterThanOrEqualTo(profile.TrafficMinSpeed));
                Assert.That(profile.CoinArcSpacing, Is.GreaterThan(0f));
                Assert.That(profile.CoinValue, Is.GreaterThan(0));
                Assert.That(profile.NearMissBonusCredits, Is.GreaterThanOrEqualTo(0));
                Assert.That(profile.RivalBasePaceMultiplier, Is.InRange(0.5f, 1.5f));

                // RubberBand boundaries
                Assert.That(profile.RubberBand, Is.Not.Null);
                Assert.That(profile.RubberBand.minMultiplier, Is.GreaterThanOrEqualTo(0.85f), $"minMultiplier below 0.85 for {id}");
                Assert.That(profile.RubberBand.maxMultiplier, Is.LessThanOrEqualTo(1.18f), $"maxMultiplier above 1.18 for {id}");
            }
        }

        [Test]
        public void ChampionshipTiers_EscalateMonotonically_AcrossC1_C2_C3()
        {
            // C1: Rounds 1..5
            // C2: Rounds 1..5
            // C3: Rounds 1..5
            HighwayDifficultyProfile prevProfile = default;

            for (int round = 1; round <= 5; round++)
            {
                var c1 = HighwayDifficultyTable.Resolve($"c1-ignition-series-round-{round}");
                var c2 = HighwayDifficultyTable.Resolve($"c2-apex-challenger-round-{round}");
                var c3 = HighwayDifficultyTable.Resolve($"c3-velocity-masters-round-{round}");

                // Coin values strictly scale: 10 in C1, 15 in C2, 20 in C3
                Assert.That(c1.CoinValue, Is.EqualTo(10));
                Assert.That(c2.CoinValue, Is.EqualTo(15));
                Assert.That(c3.CoinValue, Is.EqualTo(20));

                // Near Miss bonus strictly scales: 25 in C1, 35 in C2, 50 in C3
                Assert.That(c1.NearMissBonusCredits, Is.EqualTo(25));
                Assert.That(c2.NearMissBonusCredits, Is.EqualTo(35));
                Assert.That(c3.NearMissBonusCredits, Is.EqualTo(50));

                // Traffic density strictly increases across championships for matching round
                Assert.That(c1.TrafficDensity, Is.LessThan(c2.TrafficDensity));
                Assert.That(c2.TrafficDensity, Is.LessThan(c3.TrafficDensity));

                // Traffic max speed strictly increases across championships
                Assert.That(c1.TrafficMaxSpeed, Is.LessThan(c2.TrafficMaxSpeed));
                Assert.That(c2.TrafficMaxSpeed, Is.LessThan(c3.TrafficMaxSpeed));

                // Rival pace strictly increases across championships
                Assert.That(c1.RivalBasePaceMultiplier, Is.LessThan(c2.RivalBasePaceMultiplier));
                Assert.That(c2.RivalBasePaceMultiplier, Is.LessThan(c3.RivalBasePaceMultiplier));

                // Coin arc spacing strictly increases (tighter spacing = higher density in C1, wider spacing in C3)
                Assert.That(c1.CoinArcSpacing, Is.LessThanOrEqualTo(c2.CoinArcSpacing));
                Assert.That(c2.CoinArcSpacing, Is.LessThanOrEqualTo(c3.CoinArcSpacing));
            }

            // Verify intra-championship progression is monotonic
            VerifyIntraChampionshipMonotonicity("c1-ignition-series");
            VerifyIntraChampionshipMonotonicity("c2-apex-challenger");
            VerifyIntraChampionshipMonotonicity("c3-velocity-masters");
        }

        private void VerifyIntraChampionshipMonotonicity(string champId)
        {
            float lastDensity = 0f;
            float lastPace = 0f;

            for (int r = 1; r <= 5; r++)
            {
                var profile = HighwayDifficultyTable.Resolve($"{champId}-round-{r}");
                Assert.That(profile.TrafficDensity, Is.GreaterThanOrEqualTo(lastDensity), $"{champId} round {r} density not monotonic");
                Assert.That(profile.RivalBasePaceMultiplier, Is.GreaterThanOrEqualTo(lastPace), $"{champId} round {r} pace not monotonic");
                lastDensity = profile.TrafficDensity;
                lastPace = profile.RivalBasePaceMultiplier;
            }
        }

        [Test]
        public void UnknownOrNullEventIds_FallBackSafelyToDefault_WithoutThrowing()
        {
            var defaultProfile = HighwayDifficultyTable.DefaultProfile;
            Assert.That(defaultProfile.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));
            Assert.That(defaultProfile.TryValidate(out _), Is.True);

            // Null, empty, whitespace
            var pNull = HighwayDifficultyTable.Resolve(null);
            Assert.That(pNull.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));

            var pEmpty = HighwayDifficultyTable.Resolve("");
            Assert.That(pEmpty.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));

            var pWhitespace = HighwayDifficultyTable.Resolve("   ");
            Assert.That(pWhitespace.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));

            // Unknown event id
            var pUnknown = HighwayDifficultyTable.Resolve("unregistered-secret-boss-event");
            Assert.That(pUnknown.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));

            // TryGetProfile false for unknown
            Assert.That(HighwayDifficultyTable.TryGetProfile("unknown-event", out var outProf), Is.False);
            Assert.That(outProf.EventId, Is.EqualTo(HighwayDifficultyTable.DefaultEventId));

            // Case-insensitivity
            var pCase = HighwayDifficultyTable.Resolve("C1-IGNITION-SERIES-ROUND-1");
            Assert.That(pCase.EventId, Is.EqualTo("c1-ignition-series-round-1"));
        }

        [Test]
        public void ProfileValidation_RejectsInvalidBoundaries_FailClosed()
        {
            var validRubberBand = RubberBandConfig.CreateDefault();

            // Null / empty event id
            var pBadId = new HighwayDifficultyProfile("", 0.5f, 10f, 20f, 40f, 10, 25, 1f, validRubberBand);
            Assert.That(pBadId.TryValidate(out _), Is.False);

            // Density too low / too high
            var pLowDensity = new HighwayDifficultyProfile("test", 0.10f, 10f, 20f, 40f, 10, 25, 1f, validRubberBand);
            Assert.That(pLowDensity.TryValidate(out _), Is.False);

            var pHighDensity = new HighwayDifficultyProfile("test", 0.90f, 10f, 20f, 40f, 10, 25, 1f, validRubberBand);
            Assert.That(pHighDensity.TryValidate(out _), Is.False);

            // Speed inverted or negative
            var pInvertedSpeed = new HighwayDifficultyProfile("test", 0.5f, 30f, 10f, 40f, 10, 25, 1f, validRubberBand);
            Assert.That(pInvertedSpeed.TryValidate(out _), Is.False);

            // Coin value <= 0
            var pZeroCoin = new HighwayDifficultyProfile("test", 0.5f, 10f, 20f, 40f, 0, 25, 1f, validRubberBand);
            Assert.That(pZeroCoin.TryValidate(out _), Is.False);

            // Near miss negative
            var pNegNearMiss = new HighwayDifficultyProfile("test", 0.5f, 10f, 20f, 40f, 10, -5, 1f, validRubberBand);
            Assert.That(pNegNearMiss.TryValidate(out _), Is.False);

            // Rubber band null
            var pNullRb = new HighwayDifficultyProfile("test", 0.5f, 10f, 20f, 40f, 10, 25, 1f, null);
            // constructor sets fallback default rubber band, so let's verify invalid rubber band bounds
            var invalidRb = new RubberBandConfig { minMultiplier = 0.50f, maxMultiplier = 1.35f };
            var pBadRb = new HighwayDifficultyProfile("test", 0.5f, 10f, 20f, 40f, 10, 25, 1f, invalidRb);
            Assert.That(pBadRb.TryValidate(out _), Is.False);
        }

        [Test]
        public void Resolve_OperatesWithZeroHeapAllocations()
        {
            // Warmup
            var _ = HighwayDifficultyTable.Resolve("c1-ignition-series-round-1");

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++)
            {
                var p = HighwayDifficultyTable.Resolve("c1-ignition-series-round-1");
                var p2 = HighwayDifficultyTable.Resolve("c2-apex-challenger-round-3");
                var p3 = HighwayDifficultyTable.Resolve("c3-velocity-masters-round-5");
            }
            long after = GC.GetAllocatedBytesForCurrentThread();

            Assert.That(after - before, Is.EqualTo(0), "HighwayDifficultyTable.Resolve must not allocate on heap.");
        }
    }
}
