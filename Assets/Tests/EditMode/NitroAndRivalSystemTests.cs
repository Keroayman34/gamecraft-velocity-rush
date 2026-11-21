using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;
using VelocityRush.Gameplay.AI;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Unit tests for Nitro Boost, Rubber-Banding, Camera Juice formulas, and 0 B allocation proof (Phase 12 Stage 3; TDD §60).
    /// </summary>
    [TestFixture]
    public sealed class NitroAndRivalSystemTests
    {
        [Test]
        public void NitroConfig_Validation_RejectsInvalidAndAcceptsDefault()
        {
            var defaultConfig = NitroConfig.CreateDefault();
            Assert.That(defaultConfig.TryValidate(out var validReason), Is.True, validReason);

            var invalidConfig = new NitroConfig
            {
                maxCharge = 5f, // Below minimum 10
                minActivationCharge = 20f
            };
            Assert.That(invalidConfig.TryValidate(out var reason1), Is.False);
            Assert.That(reason1, Does.Contain("maxCharge"));

            var invalidHysteresis = new NitroConfig
            {
                maxCharge = 100f,
                minActivationCharge = 120f // Exceeds maxCharge
            };
            Assert.That(invalidHysteresis.TryValidate(out var reason2), Is.False);
            Assert.That(reason2, Does.Contain("minActivationCharge"));

            var invalidTorque = new NitroConfig
            {
                boostTorqueMultiplier = 0.5f // Less than 1.0
            };
            Assert.That(invalidTorque.TryValidate(out var reason3), Is.False);
            Assert.That(reason3, Does.Contain("boostTorqueMultiplier"));
        }

        [Test]
        public void NitroSystem_HysteresisAndDrain_BehavesDeterministically()
        {
            var config = NitroConfig.CreateDefault();
            config.maxCharge = 100f;
            config.minActivationCharge = 20f;
            config.drainRatePerSecond = 20f;

            var nitro = new NitroSystem(config, startingCharge: 15f);

            // 1. Starting below minActivationCharge (15 < 20): cannot activate
            Assert.That(nitro.CanActivate, Is.False);
            nitro.Tick(deltaSeconds: 0.1f, boostInputHeld: true, forwardSpeed: 0f, isDrifting: false);
            Assert.That(nitro.IsBoosting, Is.False, "Nitro should not activate when below minActivationCharge threshold.");

            // 2. Add charge to 25f (25 >= 20): now can activate
            nitro.AddCharge(10f);
            Assert.That(nitro.CanActivate, Is.True);
            Assert.That(nitro.CurrentCharge, Is.EqualTo(25f));

            // 3. Activate nitro: drains at 20 units/sec for 0.5 sec -> drain 10 units -> 15f remaining
            nitro.Tick(deltaSeconds: 0.5f, boostInputHeld: true, forwardSpeed: 20f, isDrifting: false);
            Assert.That(nitro.IsBoosting, Is.True, "Nitro should be actively boosting.");
            Assert.That(nitro.CurrentCharge, Is.EqualTo(15f).Within(0.01f));

            // 4. Hysteresis rule: continues boosting below minActivationCharge as long as input is held and charge > 0
            Assert.That(nitro.CurrentCharge, Is.LessThan(config.minActivationCharge));
            nitro.Tick(deltaSeconds: 0.5f, boostInputHeld: true, forwardSpeed: 20f, isDrifting: false);
            Assert.That(nitro.IsBoosting, Is.True, "Nitro should remain boosting even when charge dips below minActivationCharge.");
            Assert.That(nitro.CurrentCharge, Is.EqualTo(5f).Within(0.01f));

            // 5. Complete drain: drops to 0 and deactivates
            nitro.Tick(deltaSeconds: 0.5f, boostInputHeld: true, forwardSpeed: 20f, isDrifting: false);
            Assert.That(nitro.IsBoosting, Is.False, "Nitro must deactivate once charge reaches 0.");
            Assert.That(nitro.CurrentCharge, Is.EqualTo(0f));

            // 6. Reset brings back to initial level
            nitro.Reset(30f);
            Assert.That(nitro.CurrentCharge, Is.EqualTo(30f));
            Assert.That(nitro.IsBoosting, Is.False);
        }

        [Test]
        public void NitroSystem_NearMissAndDrift_ChargesAndClampsAtMax()
        {
            var config = NitroConfig.CreateDefault();
            config.maxCharge = 100f;
            config.nearMissChargeBonus = 25f;
            config.driftChargePerSecond = 10f;
            config.passiveRegenPerSecond = 2f;

            var nitro = new NitroSystem(config, startingCharge: 50f);

            // Near miss adds instant bonus
            nitro.AddNearMissCharge();
            Assert.That(nitro.CurrentCharge, Is.EqualTo(75f));

            // Drift accumulates charge over 1 second (speed = 10 m/s, drift = true)
            nitro.Tick(deltaSeconds: 1.0f, boostInputHeld: false, forwardSpeed: 10f, isDrifting: true);
            // Passive regen (2) + drift (10) = +12
            Assert.That(nitro.CurrentCharge, Is.EqualTo(87f).Within(0.01f));

            // Multiple near misses clamp at maxCharge (100)
            nitro.AddNearMissCharge();
            Assert.That(nitro.CurrentCharge, Is.EqualTo(100f), "Charge must never exceed maxCharge.");
        }

        [Test]
        public void RubberBandCalculator_DeadZoneCatchUpAndSlowDown_StayWithinStrictClamps()
        {
            var config = RubberBandConfig.CreateDefault();
            config.catchUpStartGap = 25f;
            config.maxCatchUpBoost = 1.12f;
            config.slowDownStartGap = 35f;
            config.maxSlowDownFactor = 0.92f;
            config.deadZone = 15f;
            config.minMultiplier = 0.85f;
            config.maxMultiplier = 1.18f;

            float playerDist = 200f;

            // 1. Inside dead zone (±15m): multiplier is strictly 1.0f
            float multCloseAhead = RubberBandCalculator.CalculateMultiplier(playerDist + 10f, playerDist, 1.0f, config);
            Assert.That(multCloseAhead, Is.EqualTo(1.0f).Within(0.001f));

            float multCloseBehind = RubberBandCalculator.CalculateMultiplier(playerDist - 10f, playerDist, 1.0f, config);
            Assert.That(multCloseBehind, Is.EqualTo(1.0f).Within(0.001f));

            // 2. Trailing behind player (> 25m behind): catch-up boost scales towards 1.12
            float multFarBehind = RubberBandCalculator.CalculateMultiplier(playerDist - 65f, playerDist, 1.0f, config);
            Assert.That(multFarBehind, Is.EqualTo(config.maxCatchUpBoost).Within(0.01f));

            // 3. Leading far ahead of player (> 35m ahead): slow down scales towards 0.92
            float multFarAhead = RubberBandCalculator.CalculateMultiplier(playerDist + 75f, playerDist, 1.0f, config);
            Assert.That(multFarAhead, Is.EqualTo(config.maxSlowDownFactor).Within(0.01f));

            // 4. Extreme gaps never breach absolute bounds [0.85, 1.18]
            float extremeBehind = RubberBandCalculator.CalculateMultiplier(playerDist - 1000f, playerDist, 1.0f, config);
            Assert.That(extremeBehind, Is.LessThanOrEqualTo(config.maxMultiplier));

            float extremeAhead = RubberBandCalculator.CalculateMultiplier(playerDist + 1000f, playerDist, 1.0f, config);
            Assert.That(extremeAhead, Is.GreaterThanOrEqualTo(config.minMultiplier));
        }

        [Test]
        public void HudStringFormatter_RivalGap_FormatsSignedMetersZeroAlloc()
        {
            var buffer = new char[HudStringFormatter.RivalGapBufferSize];

            // Positive gap (rival ahead)
            int lenPos = HudStringFormatter.WriteRivalGap(buffer, 24.3f);
            string posStr = new string(buffer, 0, lenPos);
            Assert.That(posStr, Is.EqualTo("+24m"));

            // Negative gap (rival behind)
            int lenNeg = HudStringFormatter.WriteRivalGap(buffer, -18.7f);
            string negStr = new string(buffer, 0, lenNeg);
            Assert.That(negStr, Is.EqualTo("-19m"));

            // Zero gap
            int lenZero = HudStringFormatter.WriteRivalGap(buffer, 0.0f);
            string zeroStr = new string(buffer, 0, lenZero);
            Assert.That(zeroStr, Is.EqualTo("+0m"));
        }

        [Test]
        public void NitroAndRubberBand_ZeroSteadyStateAllocation_Proof()
        {
            var nitroConfig = NitroConfig.CreateDefault();
            var nitro = new NitroSystem(nitroConfig, 50f);
            var rbConfig = RubberBandConfig.CreateDefault();
            var buffer = new char[HudStringFormatter.RivalGapBufferSize];

            // Warm up JIT
            for (int i = 0; i < 20; i++)
            {
                nitro.Tick(0.016f, true, 20f, false);
                RubberBandCalculator.CalculateMultiplier(100f, 80f, 1.0f, rbConfig);
                HudStringFormatter.WriteRivalGap(buffer, 25f);
            }

            // Steady-state measurement
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            long allocBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int tick = 0; tick < 200; tick++)
            {
                nitro.Tick(0.016f, true, 22f, false);
                float mult = RubberBandCalculator.CalculateMultiplier(120f + tick, 100f + tick, 1.0f, rbConfig);
                int len = HudStringFormatter.WriteRivalGap(buffer, 20f - tick);
                Assert.That(mult, Is.GreaterThan(0f));
                Assert.That(len, Is.GreaterThan(0));
            }

            long allocAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAlloc = allocAfter - allocBefore;

            Assert.That(totalAlloc, Is.EqualTo(0), $"Nitro, RubberBand, and RivalGap formatting hot path must allocate exactly 0 B. Allocated: {totalAlloc} B.");
        }

        [Test]
        public void LocalizationTable_BilingualParity_ContainsAllPhase12Stage3Keys()
        {
            var table = new LocalizationTable();
            string[] requiredKeys = { "hud.nitro", "hud.rival", "hud.boost_active" };

            foreach (var key in requiredKeys)
            {
                Assert.That(table.HasKey(key), Is.True, $"Missing key in table: {key}");
                string enVal = table.Get(key, "en");
                string arVal = table.Get(key, "ar");
                Assert.That(string.IsNullOrEmpty(enVal), Is.False, $"Empty EN translation for {key}");
                Assert.That(string.IsNullOrEmpty(arVal), Is.False, $"Empty AR translation for {key}");
            }
        }
    }
}
