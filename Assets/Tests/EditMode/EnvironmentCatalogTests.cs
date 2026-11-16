using NUnit.Framework;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Catalog rules and resolution tests for the five environments (T-9.1).</summary>
    public sealed class EnvironmentCatalogTests
    {
        [Test]
        public void NullCatalog_Fails()
        {
            var issues = EnvironmentCatalogRules.ValidateCatalog(null);

            Assert.That(issues.Count, Is.GreaterThan(0));
            StringAssert.Contains("null", issues[0]);
        }

        [Test]
        public void EmptyCatalog_Fails()
        {
            var issues = EnvironmentCatalogRules.ValidateCatalog(
                System.Array.Empty<EnvironmentDefinition>());

            Assert.That(issues.Count, Is.GreaterThan(0));
            StringAssert.Contains("exactly 5", issues[0]);
        }

        [Test]
        public void CompleteFiveThemeCatalog_Passes()
        {
            using var assets = new EnvironmentTestAssets();
            var definitions = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);

            var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void MissingTheme_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var all = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);
            var four = new[] { all[0], all[1], all[2], all[3] };

            var issues = EnvironmentCatalogRules.ValidateCatalog(four);

            Assert.That(issues.Count, Is.GreaterThan(0));
            StringAssert.Contains("World Arena", string.Join("\n", issues));
        }

        [Test]
        public void DuplicateId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definitions = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);
            definitions[1] = assets.CreateDefinition(
                EnvironmentTheme.DesertCircuit,
                "desert-test",
                "Duplicate Id",
                "مكرر",
                1);

            var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);

            StringAssert.Contains("Duplicate EnvironmentId", string.Join("\n", issues));
        }

        [Test]
        public void DuplicateProductionOrder_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definitions = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);
            definitions[4] = assets.CreateDefinition(
                EnvironmentTheme.DesertCircuit,
                "desert-extra",
                "Extra Desert",
                "صحراء إضافية",
                1);

            var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);

            StringAssert.Contains("Duplicate ProductionOrder", string.Join("\n", issues));
        }

        [Test]
        public void NullEntry_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definitions = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);
            definitions[2] = null;

            var issues = EnvironmentCatalogRules.ValidateCatalog(definitions);

            StringAssert.Contains("entry 2 is null", string.Join("\n", issues));
        }

        [Test]
        public void Catalog_ReportsCountAndResolvesByIdAndTheme()
        {
            using var assets = new EnvironmentTestAssets();
            var definitions = EnvironmentTestAssets.CreateFiveThemeDefinitions(assets);
            var catalog = assets.CreateCatalog(definitions);

            Assert.That(catalog.Count, Is.EqualTo(5));
            Assert.That(catalog.FindById("neon-city-test"), Is.SameAs(definitions[3]));
            Assert.That(catalog.FindByTheme(EnvironmentTheme.WorldArena), Is.SameAs(definitions[4]));
            Assert.That(catalog.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void Catalog_UnknownIdAndTheme_ReturnNull()
        {
            using var assets = new EnvironmentTestAssets();
            var catalog = assets.CreateCatalog(
                EnvironmentTestAssets.CreateFiveThemeDefinitions(assets));

            Assert.That(catalog.FindById("missing-environment"), Is.Null);
            Assert.That(catalog.FindById(string.Empty), Is.Null);
            Assert.That(catalog.FindByTheme((EnvironmentTheme)99), Is.Null);
        }

        [Test]
        public void Catalog_Empty_TryValidateFails()
        {
            using var assets = new EnvironmentTestAssets();
            var catalog = assets.CreateCatalog(System.Array.Empty<EnvironmentDefinition>());

            Assert.That(catalog.TryValidate(out var error), Is.False);
            StringAssert.Contains("exactly 5", error);
        }

        [Test]
        public void RequiredEnvironmentCount_MatchesFiveThemes()
        {
            Assert.That(EnvironmentCatalogRules.RequiredEnvironmentCount, Is.EqualTo(5));
            Assert.That(EnvironmentCatalogRules.RequiredEnvironmentCount,
                Is.EqualTo(EnvironmentThemeRules.RequiredThemeCount));
        }
    }
}
