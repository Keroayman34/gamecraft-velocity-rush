using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class SaveModelTests
    {
        [Test]
        public void CreateDefault_ProducesValidModel()
        {
            var model = SaveModel.CreateDefault();
            Assert.IsTrue(model.TryValidate(out var error), error);
            Assert.AreEqual(SaveModel.CurrentSaveVersion, model.saveVersion);
            Assert.AreEqual(500, model.credits);
            Assert.AreEqual("starter-apex-v", model.selectedVehicleId);
            Assert.Contains("starter-apex-v", model.unlockedVehicleIds);
            Assert.Contains("c1-ignition-series", model.unlockedChampionshipIds);
            Assert.AreEqual("en", model.settings.language);
        }

        [Test]
        public void JsonRoundTrip_PreservesAllDataAndNestedArrays()
        {
            var original = SaveModel.CreateDefault();
            original.credits = 12500;
            original.selectedVehicleId = "sport-velocity-gt";
            original.unlockedVehicleIds = new[] { "starter-apex-v", "sport-velocity-gt" };
            original.unlockedChampionshipIds = new[] { "c1-ignition-series", "c2-apex-challenger" };
            original.settings = new PlayerSettingsDto
            {
                language = "ar",
                masterVolume = 0.8f,
                sfxVolume = 0.7f,
                engineVolume = 0.9f
            };
            original.vehicleUpgrades = new[]
            {
                new VehicleUpgradeEntry
                {
                    vehicleId = "starter-apex-v",
                    engineTier = 1,
                    tiresTier = 2,
                    brakesTier = 3,
                    suspensionTier = 1,
                    transmissionTier = 2,
                    aeroTier = 3
                },
                new VehicleUpgradeEntry
                {
                    vehicleId = "sport-velocity-gt",
                    engineTier = 3,
                    tiresTier = 3,
                    brakesTier = 3,
                    suspensionTier = 3,
                    transmissionTier = 3,
                    aeroTier = 3
                }
            };
            original.eventRecords = new[]
            {
                new EventProgressEntry
                {
                    eventId = "c1-ignition-series-round-1",
                    bestTimeSeconds = 48.25,
                    starsEarned = 3,
                    completed = true
                },
                new EventProgressEntry
                {
                    eventId = "c1-ignition-series-round-2",
                    bestTimeSeconds = 55.10,
                    starsEarned = 2,
                    completed = true
                }
            };

            Assert.IsTrue(original.TryValidate(out var validErr), validErr);

            string json = JsonUtility.ToJson(original, false);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var deserialized = JsonUtility.FromJson<SaveModel>(json);
            Assert.IsNotNull(deserialized);
            Assert.IsTrue(deserialized.TryValidate(out var deserErr), deserErr);

            Assert.AreEqual(original.credits, deserialized.credits);
            Assert.AreEqual(original.selectedVehicleId, deserialized.selectedVehicleId);
            CollectionAssert.AreEquivalent(original.unlockedVehicleIds, deserialized.unlockedVehicleIds);
            CollectionAssert.AreEquivalent(original.unlockedChampionshipIds, deserialized.unlockedChampionshipIds);
            Assert.AreEqual(original.settings.language, deserialized.settings.language);
            Assert.AreEqual(original.settings.masterVolume, deserialized.settings.masterVolume, 1e-4f);

            // Verify nested arrays survived intact
            Assert.AreEqual(2, deserialized.vehicleUpgrades.Length);
            Assert.AreEqual("sport-velocity-gt", deserialized.vehicleUpgrades[1].vehicleId);
            Assert.AreEqual(3, deserialized.vehicleUpgrades[1].engineTier);

            Assert.AreEqual(2, deserialized.eventRecords.Length);
            Assert.AreEqual("c1-ignition-series-round-1", deserialized.eventRecords[0].eventId);
            Assert.AreEqual(3, deserialized.eventRecords[0].starsEarned);
            Assert.AreEqual(48.25, deserialized.eventRecords[0].bestTimeSeconds, 1e-4);
        }

        [Test]
        public void NegativeCredits_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.credits = -50;
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("negative", error.ToLowerInvariant());
        }

        [Test]
        public void SelectedVehicleNotUnlocked_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.selectedVehicleId = "supercar-eclipse"; // Not in unlockedVehicleIds
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("not among unlocked", error.ToLowerInvariant());
        }

        [Test]
        public void DuplicateUnlockedVehicles_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.unlockedVehicleIds = new[] { "starter-apex-v", "starter-apex-v" };
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("duplicate", error.ToLowerInvariant());
        }

        [Test]
        public void InvalidUpgradeTier_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.vehicleUpgrades = new[]
            {
                new VehicleUpgradeEntry
                {
                    vehicleId = "starter-apex-v",
                    engineTier = 4 // Exceeds MaxTier 3
                }
            };
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("range", error.ToLowerInvariant());
        }

        [Test]
        public void InvalidStarsEarned_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.eventRecords = new[]
            {
                new EventProgressEntry
                {
                    eventId = "c1-round-1",
                    starsEarned = 5,
                    completed = true
                }
            };
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("starsearned", error.ToLowerInvariant());
        }

        [Test]
        public void StarsEarnedWithoutCompleted_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.eventRecords = new[]
            {
                new EventProgressEntry
                {
                    eventId = "c1-round-1",
                    starsEarned = 2,
                    completed = false // Invalid
                }
            };
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("not completed", error.ToLowerInvariant());
        }

        [Test]
        public void InvalidLanguage_FailsValidation()
        {
            var model = SaveModel.CreateDefault();
            model.settings.language = "fr"; // Only 'en' and 'ar' permitted
            Assert.IsFalse(model.TryValidate(out var error));
            StringAssert.Contains("language", error.ToLowerInvariant());
        }
    }
}
