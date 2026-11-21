using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class UpgradeCalculatorTests
    {
        [Test]
        public void StockModifiers_AreIdentity()
        {
            var stock = VehicleUpgradeState.Stock;
            var modifiers = UpgradeCalculator.ComputeModifiers(stock);

            Assert.AreEqual(1.0f, modifiers.TorqueMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, modifiers.FrictionMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, modifiers.BrakeTorqueMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, modifiers.SuspensionMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, modifiers.TransmissionRetentionMultiplier, 1e-4f);
            Assert.AreEqual(1.0f, modifiers.DragMultiplier, 1e-4f);
        }

        [Test]
        public void MaxTierModifiers_MatchApprovedPercentages()
        {
            var maxState = new VehicleUpgradeState(3, 3, 3, 3, 3, 3);
            var modifiers = UpgradeCalculator.ComputeModifiers(maxState);

            // Engine: +3% per tier -> 1.09
            Assert.AreEqual(1.09f, modifiers.TorqueMultiplier, 1e-4f);
            // Tires: +3% per tier -> 1.09
            Assert.AreEqual(1.09f, modifiers.FrictionMultiplier, 1e-4f);
            // Brakes: +4% per tier -> 1.12
            Assert.AreEqual(1.12f, modifiers.BrakeTorqueMultiplier, 1e-4f);
            // Suspension: +3% per tier -> 1.09
            Assert.AreEqual(1.09f, modifiers.SuspensionMultiplier, 1e-4f);
            // Transmission: +4% per tier -> 1.12
            Assert.AreEqual(1.12f, modifiers.TransmissionRetentionMultiplier, 1e-4f);
            // Aero: -3% drag per tier -> 0.91
            Assert.AreEqual(0.91f, modifiers.DragMultiplier, 1e-4f);
        }

        [Test]
        public void UpgradeCosts_ScaleCorrectlyByClassAndTier()
        {
            // Starter: 250, 500, 750
            Assert.AreEqual(250, UpgradeCalculator.GetUpgradeCost(VehicleClass.Starter, 1));
            Assert.AreEqual(500, UpgradeCalculator.GetUpgradeCost(VehicleClass.Starter, 2));
            Assert.AreEqual(750, UpgradeCalculator.GetUpgradeCost(VehicleClass.Starter, 3));

            // Supercar: 1000, 2000, 3000
            Assert.AreEqual(1000, UpgradeCalculator.GetUpgradeCost(VehicleClass.Supercar, 1));
            Assert.AreEqual(2000, UpgradeCalculator.GetUpgradeCost(VehicleClass.Supercar, 2));
            Assert.AreEqual(3000, UpgradeCalculator.GetUpgradeCost(VehicleClass.Supercar, 3));

            // Tier 0 is free, tier > 3 is invalid
            Assert.AreEqual(0, UpgradeCalculator.GetUpgradeCost(VehicleClass.Sport, 0));
            Assert.AreEqual(-1, UpgradeCalculator.GetUpgradeCost(VehicleClass.Sport, 4));
        }

        [Test]
        public void ComputeModifiers_AllocatesZeroBytes()
        {
            var state = new VehicleUpgradeState(2, 1, 3, 0, 2, 1);

            // Warm-up
            for (int i = 0; i < 50; i++)
            {
                var dummy = UpgradeCalculator.ComputeModifiers(state);
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long before = GC.GetTotalMemory(true);

            for (int i = 0; i < 1000; i++)
            {
                var mod = UpgradeCalculator.ComputeModifiers(state);
            }

            long after = GC.GetTotalMemory(false);
            Assert.AreEqual(0, after - before, "ComputeModifiers must be a pure 0 B allocation path.");
        }
    }
}
