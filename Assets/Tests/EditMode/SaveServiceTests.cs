using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class SaveServiceTests
    {
        [Test]
        public void InitialState_IsCleanAndDefault()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store);

            Assert.IsNotNull(service.CurrentModel);
            Assert.IsFalse(service.IsDirty);
            Assert.AreEqual(SaveModel.DefaultStartingCredits, service.CurrentModel.credits);
        }

        [Test]
        public void Load_WhenNoDataExists_LoadsDefaultAndMarksDirty()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store);

            bool loaded = service.Load();
            Assert.IsFalse(loaded);
            Assert.IsNotNull(service.CurrentModel);
            Assert.IsTrue(service.IsDirty); // Fresh default profile flagged to save
        }

        [Test]
        public void SaveAndLoad_RoundTripsStateCleanly()
        {
            var store = new InMemorySaveStore();
            var service1 = new SaveService(store);

            service1.CurrentModel.credits = 3500;
            service1.MarkDirty();
            Assert.IsTrue(service1.Flush());
            Assert.IsFalse(service1.IsDirty);
            Assert.IsTrue(store.Exists());

            var service2 = new SaveService(store);
            Assert.IsTrue(service2.Load());
            Assert.IsFalse(service2.IsDirty);
            Assert.AreEqual(3500, service2.CurrentModel.credits);
        }

        [Test]
        public void CorruptedJson_RecoversWithCleanDefault()
        {
            var store = new InMemorySaveStore();
            store.SetRawPayload("{ this is not valid json! ### }");

            var service = new SaveService(store);
            bool result = service.Load();

            Assert.IsFalse(result);
            Assert.IsNotNull(service.CurrentModel);
            Assert.AreEqual(SaveModel.DefaultStartingCredits, service.CurrentModel.credits);
            Assert.IsTrue(service.IsDirty);
        }

        [Test]
        public void FutureSchemaVersion_RecoversWithCleanDefault()
        {
            var store = new InMemorySaveStore();
            string futureJson = "{\"saveVersion\": 99, \"credits\": 99999, \"selectedVehicleId\": \"supercar-x\"}";
            store.SetRawPayload(futureJson);

            var service = new SaveService(store);
            bool result = service.Load();

            Assert.IsFalse(result);
            Assert.AreEqual(SaveModel.CurrentSaveVersion, service.CurrentModel.saveVersion);
            Assert.AreEqual(SaveModel.DefaultStartingCredits, service.CurrentModel.credits);
        }

        [Test]
        public void Flush_WhenNotDirty_DoesNotPerformRedundantSave()
        {
            var store = new InMemorySaveStore();
            var service = new SaveService(store);

            Assert.IsFalse(service.IsDirty);
            Assert.IsTrue(service.Flush());
            Assert.IsFalse(store.Exists()); // Store was never written because not dirty
        }

        [Test]
        public void PlayerPrefsSaveStore_SafeReadWriteDelete()
        {
            string testKey = "VR_Test_Store_Key_" + System.Guid.NewGuid().ToString("N");
            var store = new PlayerPrefsJsonSaveStore(testKey);

            try
            {
                Assert.IsFalse(store.Exists());
                Assert.IsFalse(store.TryLoad(out var loaded, out _));

                string payload = "{\"test\": 123}";
                Assert.IsTrue(store.TrySave(payload, out var saveErr), saveErr);
                Assert.IsTrue(store.Exists());

                Assert.IsTrue(store.TryLoad(out var readJson, out var loadErr), loadErr);
                Assert.AreEqual(payload, readJson);

                Assert.IsTrue(store.TryDelete(out var delErr), delErr);
                Assert.IsFalse(store.Exists());
            }
            finally
            {
                store.TryDelete(out _);
            }
        }
    }
}
