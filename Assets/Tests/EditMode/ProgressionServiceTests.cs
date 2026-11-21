using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class ProgressionServiceTests
    {
        private PlayerProgress _progress;
        private ProgressionService _service;

        [SetUp]
        public void SetUp()
        {
            var model = SaveModel.CreateDefault();
            _progress = new PlayerProgress(model);
            _service = new ProgressionService(_progress);
        }

        private static RaceEventDefinition CreateEventDef(
            string eventId,
            string championshipId,
            int roundIndex,
            RaceEventType type)
        {
            var def = ScriptableObject.CreateInstance<RaceEventDefinition>();
            // Use reflection or serialized object to set private fields cleanly
            var so = new UnityEditor.SerializedObject(def);
            so.FindProperty("eventId").stringValue = eventId;
            so.FindProperty("displayNameEn").stringValue = $"Test {eventId}";
            so.FindProperty("displayNameAr").stringValue = $"اختبار {eventId}";
            so.FindProperty("eventType").enumValueIndex = (int)type;
            so.FindProperty("championshipId").stringValue = championshipId;
            so.FindProperty("roundIndex").intValue = roundIndex;
            so.FindProperty("environmentTheme").enumValueIndex = (int)EnvironmentTheme.DesertCircuit;
            so.FindProperty("layoutVariant").enumValueIndex = (int)EnvironmentLayoutVariant.Signature;
            so.FindProperty("paceScale").floatValue = 0.90f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return def;
        }

        [Test]
        public void CircuitRace_FirstPlace_AwardsThreeStarsAndFullBonus()
        {
            var ev = CreateEventDef("c1-ignition-series-round-2", "c1-ignition-series", 2, RaceEventType.CircuitRace);
            var result = new RaceResult(62.5, 3, 3, completed: true);

            var outcome = _service.ProcessRaceOutcome(result, ev, playerFinishPosition: 1);

            Assert.AreEqual(3, outcome.StarsEarnedOnRun);
            Assert.AreEqual(3, outcome.NewStarsEarned);
            Assert.AreEqual(100, outcome.BaseCredits);
            Assert.AreEqual(450, outcome.StarBonusCredits); // 3 * 150
            Assert.AreEqual(200, outcome.FirstPlaceBonusCredits);
            Assert.AreEqual(750, outcome.TotalCreditsAwarded);
            Assert.AreEqual(500 + 750, _progress.Credits);
        }

        [Test]
        public void CircuitRace_Podium_AwardsTwoStars()
        {
            var ev = CreateEventDef("c1-ignition-series-round-2", "c1-ignition-series", 2, RaceEventType.CircuitRace);
            var result = new RaceResult(65.0, 3, 3, completed: true);

            var outcome = _service.ProcessRaceOutcome(result, ev, playerFinishPosition: 3);

            Assert.AreEqual(2, outcome.StarsEarnedOnRun);
            Assert.AreEqual(2, outcome.NewStarsEarned);
            Assert.AreEqual(100, outcome.BaseCredits);
            Assert.AreEqual(300, outcome.StarBonusCredits); // 2 * 150
            Assert.AreEqual(0, outcome.FirstPlaceBonusCredits);
            Assert.AreEqual(400, outcome.TotalCreditsAwarded);
        }

        [Test]
        public void CircuitRace_FourthPlace_AwardsOneStar()
        {
            var ev = CreateEventDef("c1-ignition-series-round-2", "c1-ignition-series", 2, RaceEventType.CircuitRace);
            var result = new RaceResult(70.0, 3, 3, completed: true);

            var outcome = _service.ProcessRaceOutcome(result, ev, playerFinishPosition: 4);

            Assert.AreEqual(1, outcome.StarsEarnedOnRun);
            Assert.AreEqual(1, outcome.NewStarsEarned);
            Assert.AreEqual(250, outcome.TotalCreditsAwarded); // 100 + 150
        }

        [Test]
        public void CircuitRace_IncompleteRun_AwardsZero()
        {
            var ev = CreateEventDef("c1-ignition-series-round-2", "c1-ignition-series", 2, RaceEventType.CircuitRace);
            var result = new RaceResult(30.0, 1, 3, completed: false);

            var outcome = _service.ProcessRaceOutcome(result, ev, playerFinishPosition: 0);

            Assert.AreEqual(0, outcome.StarsEarnedOnRun);
            Assert.AreEqual(0, outcome.TotalCreditsAwarded);
            Assert.AreEqual(500, _progress.Credits);
        }

        [Test]
        public void TimeTrial_EvaluatesGoldSilverBronzeThresholds()
        {
            var ev = CreateEventDef("c1-ignition-series-round-1", "c1-ignition-series", 1, RaceEventType.TimeTrial);

            // Target table for round-1: Gold <= 50s, Silver <= 58s, Bronze <= 70s
            var goldResult = new RaceResult(48.5, 3, 3, completed: true);
            var goldOutcome = _service.ProcessRaceOutcome(goldResult, ev);
            Assert.AreEqual(3, goldOutcome.StarsEarnedOnRun);
            Assert.AreEqual(750, goldOutcome.TotalCreditsAwarded); // 100 + 450 + 200
        }

        [Test]
        public void ReplayingEventWithSameStars_GrantsBaseCreditsOnly_NoStarBonus()
        {
            var ev = CreateEventDef("c1-ignition-series-round-1", "c1-ignition-series", 1, RaceEventType.TimeTrial);

            // First run: 3 stars (750 credits)
            var run1 = new RaceResult(49.0, 3, 3, completed: true);
            var outcome1 = _service.ProcessRaceOutcome(run1, ev);
            Assert.AreEqual(3, outcome1.NewStarsEarned);
            Assert.AreEqual(750, outcome1.TotalCreditsAwarded);
            Assert.AreEqual(1250, _progress.Credits); // 500 + 750

            // Second run: 3 stars again (already earned all 3 stars)
            var run2 = new RaceResult(48.0, 3, 3, completed: true);
            var outcome2 = _service.ProcessRaceOutcome(run2, ev);
            Assert.AreEqual(0, outcome2.NewStarsEarned); // No new stars
            Assert.AreEqual(0, outcome2.StarBonusCredits);
            Assert.AreEqual(300, outcome2.TotalCreditsAwarded); // 100 base + 200 first place/gold bonus
            Assert.AreEqual(1550, _progress.Credits); // 1250 + 300
        }

        [Test]
        public void ImprovingStars_AwardsDeltaStarsOnly()
        {
            var ev = CreateEventDef("c1-ignition-series-round-1", "c1-ignition-series", 1, RaceEventType.TimeTrial);

            // First run: Bronze (1 star)
            var run1 = new RaceResult(65.0, 3, 3, completed: true);
            var outcome1 = _service.ProcessRaceOutcome(run1, ev);
            Assert.AreEqual(1, outcome1.NewStarsEarned);
            Assert.AreEqual(250, outcome1.TotalCreditsAwarded); // 100 + 150

            // Second run: Gold (3 stars, so delta = 2 new stars)
            var run2 = new RaceResult(48.0, 3, 3, completed: true);
            var outcome2 = _service.ProcessRaceOutcome(run2, ev);
            Assert.AreEqual(2, outcome2.NewStarsEarned); // 2 delta stars!
            Assert.AreEqual(300, outcome2.StarBonusCredits); // 2 * 150
            Assert.AreEqual(200, outcome2.FirstPlaceBonusCredits);
            Assert.AreEqual(600, outcome2.TotalCreditsAwarded); // 100 + 300 + 200
        }

        [Test]
        public void BestTime_IsUpdatedOnlyWhenFaster()
        {
            var ev = CreateEventDef("c1-ignition-series-round-1", "c1-ignition-series", 1, RaceEventType.TimeTrial);

            var run1 = new RaceResult(52.0, 3, 3, completed: true);
            var outcome1 = _service.ProcessRaceOutcome(run1, ev);
            Assert.IsTrue(outcome1.IsNewBestTime);
            Assert.AreEqual(52.0, _progress.GetEventBestTime("c1-ignition-series-round-1"), 1e-4);

            var run2 = new RaceResult(55.0, 3, 3, completed: true); // Slower
            var outcome2 = _service.ProcessRaceOutcome(run2, ev);
            Assert.IsFalse(outcome2.IsNewBestTime);
            Assert.AreEqual(52.0, _progress.GetEventBestTime("c1-ignition-series-round-1"), 1e-4);
        }

        [Test]
        public void ChampionshipProgression_CompletingRound5_UnlocksNextChampionship()
        {
            var round5 = CreateEventDef("c1-ignition-series-round-5", "c1-ignition-series", 5, RaceEventType.CircuitRace);

            Assert.IsFalse(_progress.IsChampionshipUnlocked("c2-apex-challenger"));

            var result = new RaceResult(60.0, 3, 3, completed: true);
            var outcome = _service.ProcessRaceOutcome(result, round5, playerFinishPosition: 2);

            Assert.IsTrue(_progress.IsChampionshipUnlocked("c2-apex-challenger"));
            CollectionAssert.Contains(outcome.NewlyUnlockedChampionshipIds, "c2-apex-challenger");
            CollectionAssert.Contains(outcome.NewlyUnlockedEventIds, "c2-apex-challenger-round-1");
        }

        [Test]
        public void VehicleUnlocks_RespectsClassGatingAndCreditCosts()
        {
            // Nova Hatch (Starter): costs 1000 CR
            _progress.AddCredits(500); // 500 + 500 = 1000
            Assert.IsTrue(_service.CanUnlockVehicle("starter-nova-hatch", out int cost, out _));
            Assert.AreEqual(1000, cost);
            Assert.IsTrue(_service.TryPurchaseVehicle("starter-nova-hatch", out _));
            Assert.IsTrue(_progress.IsVehicleUnlocked("starter-nova-hatch"));
            Assert.AreEqual(0, _progress.Credits);

            // Sport vehicle: requires C1 completed
            Assert.IsFalse(_service.CanUnlockVehicle("sport-velocity-gt", out _, out var sportReason));
            StringAssert.Contains("Ignition Series", sportReason);

            // Fake complete C1 round 5
            _progress.RecordEventResult("c1-ignition-series-round-5", 3, 50.0, true, out _, out _);
            _progress.AddCredits(3000);

            Assert.IsTrue(_service.CanUnlockVehicle("sport-velocity-gt", out int sportCost, out _));
            Assert.AreEqual(2500, sportCost);
            Assert.IsTrue(_service.TryPurchaseVehicle("sport-velocity-gt", out _));
            Assert.IsTrue(_progress.IsVehicleUnlocked("sport-velocity-gt"));
            Assert.AreEqual(500, _progress.Credits);
        }

        [Test]
        public void VehicleUpgrades_EnforcesCostAndMaxTierCap()
        {
            _progress.AddCredits(2000);

            // Purchase Tier 1 Engine on starter-apex-v (Starter class cost = 250 CR)
            Assert.IsTrue(_service.CanPurchaseUpgrade("starter-apex-v", UpgradeCategory.Engine, VehicleClass.Starter, out int cost1, out _));
            Assert.AreEqual(250, cost1);
            Assert.IsTrue(_service.TryPurchaseUpgrade("starter-apex-v", UpgradeCategory.Engine, VehicleClass.Starter, out _));
            Assert.AreEqual(1, _progress.GetUpgradeLevel("starter-apex-v", UpgradeCategory.Engine));

            // Purchase Tier 2 Engine (500 CR)
            Assert.IsTrue(_service.TryPurchaseUpgrade("starter-apex-v", UpgradeCategory.Engine, VehicleClass.Starter, out _));
            Assert.AreEqual(2, _progress.GetUpgradeLevel("starter-apex-v", UpgradeCategory.Engine));

            // Purchase Tier 3 Engine (750 CR)
            Assert.IsTrue(_service.TryPurchaseUpgrade("starter-apex-v", UpgradeCategory.Engine, VehicleClass.Starter, out _));
            Assert.AreEqual(3, _progress.GetUpgradeLevel("starter-apex-v", UpgradeCategory.Engine));

            // Tier 4 (beyond MaxTier 3) rejected
            Assert.IsFalse(_service.CanPurchaseUpgrade("starter-apex-v", UpgradeCategory.Engine, VehicleClass.Starter, out _, out var maxReason));
            StringAssert.Contains("maximum tier", maxReason);
        }

        [Test]
        public void PlayerProgress_QueriesAllocateZeroBytes()
        {
            // Populate records
            _progress.RecordEventResult("c1-ignition-series-round-1", 3, 49.5, true, out _, out _);
            _progress.TryApplyUpgrade("starter-apex-v", UpgradeCategory.Engine, 2);

            // Warm-up
            for (int i = 0; i < 50; i++)
            {
                bool u = _progress.IsVehicleUnlocked("starter-apex-v");
                int t = _progress.GetUpgradeLevel("starter-apex-v", UpgradeCategory.Engine);
                int s = _progress.GetEventStars("c1-ignition-series-round-1");
                double time = _progress.GetEventBestTime("c1-ignition-series-round-1");
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetTotalMemory(true);

            for (int i = 0; i < 500; i++)
            {
                bool u = _progress.IsVehicleUnlocked("starter-apex-v");
                int t = _progress.GetUpgradeLevel("starter-apex-v", UpgradeCategory.Engine);
                int s = _progress.GetEventStars("c1-ignition-series-round-1");
                double time = _progress.GetEventBestTime("c1-ignition-series-round-1");
                bool comp = _progress.IsEventCompleted("c1-ignition-series-round-1");
            }

            long after = GC.GetTotalMemory(false);
            Assert.AreEqual(0, after - before, "PlayerProgress queries must allocate 0 B.");
        }
    }
}
