using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class LocalizationServiceTests
    {
        private LocalizationTable _table;

        [SetUp]
        public void SetUp()
        {
            _table = new LocalizationTable();
            LocalizationService.SetTable(_table);
            LocalizationService.SetLanguage(LocalizationService.LanguageEnglish);
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationService.Reset();
        }

        [Test]
        public void Table_ContainsBothLanguages_ForEachKey()
        {
            Assert.That(_table.KeyCount, Is.GreaterThan(40));

            foreach (var key in _table.AllKeys)
            {
                var en = _table.Get(key, LocalizationService.LanguageEnglish);
                var ar = _table.Get(key, LocalizationService.LanguageArabic);

                Assert.That(en, Is.Not.Null.And.Not.Empty, $"Key '{key}' missing or empty in English.");
                Assert.That(ar, Is.Not.Null.And.Not.Empty, $"Key '{key}' missing or empty in Arabic.");
            }
        }

        [Test]
        public void SetLanguage_ValidCodes_SwitchesSuccessfullyAndEmitsEvent()
        {
            string receivedLang = null;
            LocalizationService.LanguageChanged += lang => receivedLang = lang;

            bool switchedToAr = LocalizationService.SetLanguage("ar");
            Assert.That(switchedToAr, Is.True);
            Assert.That(LocalizationService.CurrentLanguage, Is.EqualTo("ar"));
            Assert.That(LocalizationService.IsRTL, Is.True);
            Assert.That(receivedLang, Is.EqualTo("ar"));

            bool switchedToEn = LocalizationService.SetLanguage("en");
            Assert.That(switchedToEn, Is.True);
            Assert.That(LocalizationService.CurrentLanguage, Is.EqualTo("en"));
            Assert.That(LocalizationService.IsRTL, Is.False);
            Assert.That(receivedLang, Is.EqualTo("en"));
        }

        [Test]
        public void SetLanguage_InvalidCodes_RejectedFailClosed()
        {
            Assert.That(LocalizationService.SetLanguage(null), Is.False);
            Assert.That(LocalizationService.SetLanguage(""), Is.False);
            Assert.That(LocalizationService.SetLanguage("fr"), Is.False);
            Assert.That(LocalizationService.SetLanguage("de"), Is.False);
            Assert.That(LocalizationService.CurrentLanguage, Is.EqualTo("en"));
        }

        [Test]
        public void Tr_ExistingKey_ReturnsTranslatedString()
        {
            LocalizationService.SetLanguage("en");
            Assert.That(LocalizationService.Tr("menu.play"), Is.EqualTo("Play"));

            LocalizationService.SetLanguage("ar");
            Assert.That(LocalizationService.Tr("menu.play"), Is.EqualTo("العب"));
        }

        [Test]
        public void Tr_MissingKey_ReturnsKeyIdAndLogsWarning()
        {
            LogAssert.Expect(LogType.Warning, "[VelocityRush.L10n] Missing key 'nonexistent.key' for language 'en'. Returning key ID.");
            var result = LocalizationService.Tr("nonexistent.key");
            Assert.That(result, Is.EqualTo("nonexistent.key"));
        }

        [Test]
        public void Tr_NullOrEmptyKey_ReturnsEmptyStringWithoutThrowing()
        {
            Assert.That(LocalizationService.Tr(null), Is.EqualTo(string.Empty));
            Assert.That(LocalizationService.Tr(string.Empty), Is.EqualTo(string.Empty));
        }

        [Test]
        public void Tr_NoTableSet_ReturnsKeyIdAndLogsWarning()
        {
            LocalizationService.Reset();
            LogAssert.Expect(LogType.Warning, "[VelocityRush.L10n] No localization table set. Returning key: menu.play");
            var result = LocalizationService.Tr("menu.play");
            Assert.That(result, Is.EqualTo("menu.play"));
        }
    }
}
