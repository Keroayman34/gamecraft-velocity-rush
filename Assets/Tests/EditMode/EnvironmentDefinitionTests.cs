using System;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Schema and identity tests for <see cref="EnvironmentDefinition"/> (T-9.1).</summary>
    public sealed class EnvironmentDefinitionTests
    {
        [Test]
        public void ValidDefinition_Passes()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();

            Assert.That(definition.TryValidate(out var error), Is.True, error);
            Assert.That(definition.EnvironmentId, Is.EqualTo("desert-test"));
            Assert.That(definition.ProductionOrder, Is.EqualTo(1));
            Assert.That(definition.Theme, Is.EqualTo(EnvironmentTheme.DesertCircuit));
        }

        [Test]
        public void UnsupportedSchemaVersion_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();
            EnvironmentTestAssets.SetSerializedInt(definition, "schemaVersion", 99);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("SchemaVersion", error);
        }

        [Test]
        public void EmptyId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: string.Empty);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("EnvironmentId must not be empty", error);
        }

        [Test]
        public void UppercaseId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: "Desert-test");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("lowercase ASCII", error);
        }

        [Test]
        public void IdWithoutThemePrefix_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: "coastal-test");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("must start with 'desert-'", error);
        }

        [Test]
        public void ConsecutiveHyphenId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: "desert--test");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("consecutive", error);
        }

        [Test]
        public void TrailingHyphenId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: "desert-test-");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("trailing", error);
        }

        [Test]
        public void OverlongId_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(environmentId: new string('a', 49));

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("at most", error);
        }

        [Test]
        public void MissingEnglishName_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(displayNameEn: string.Empty);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("DisplayNameEn", error);
        }

        [Test]
        public void MissingArabicName_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(displayNameAr: "   ");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("DisplayNameAr", error);
        }

        [Test]
        public void NameWithControlCharacter_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(displayNameEn: "Test\nEnvironment");

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("control characters", error);
        }

        [Test]
        public void UndefinedTheme_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(theme: (EnvironmentTheme)99);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("not defined", error);
        }

        [Test]
        public void WrongProductionOrder_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(productionOrder: 3);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("ProductionOrder must be 1", error);
        }

        [Test]
        public void MissingLightingReference_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(missingLighting: true);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("LightingProfile reference is missing", error);
        }

        [Test]
        public void MissingLayoutReference_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition(missingLayout: true);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("TrackLayout reference is missing", error);
        }

        [Test]
        public void InvalidLayoutPropagates_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var layout = assets.CreateLayout(Array.Empty<TrackSegment>());
            var definition = assets.CreateDefinition(trackLayout: layout);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("TrackLayout is invalid", error);
        }

        [Test]
        public void InvalidLightingPropagates_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var lighting = assets.CreateLighting(fogStartDistance: 500f, fogEndDistance: 100f);
            var definition = assets.CreateDefinition(lightingProfile: lighting);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("LightingProfile is invalid", error);
        }

        [Test]
        public void LowContrastPalette_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var surface = new Color(0.2f, 0.2f, 0.2f, 1f);
            var palette = EnvironmentPalette.Create(
                surface,
                surface,
                new Color(0.5f, 0.4f, 0.3f, 1f),
                new Color(0.3f, 0.3f, 0.3f, 1f),
                new Color(0.7f, 0.7f, 0.7f, 1f),
                new Color(0.8f, 0.4f, 0.1f, 1f),
                new Color(2f, 1f, 0.5f, 1f));
            var definition = assets.CreateDefinition(palette: palette);

            Assert.That(definition.TryValidate(out var error), Is.False);
            StringAssert.Contains("RoadEdge must contrast", error);
        }

        [Test]
        public void FiveThemes_HaveStableProductionOrder()
        {
            var themes = EnvironmentThemeRules.Themes;
            Assert.That(themes.Count, Is.EqualTo(5));
            for (var i = 0; i < themes.Count; i++)
            {
                Assert.That(EnvironmentThemeRules.GetProductionOrder(themes[i]), Is.EqualTo(i + 1));
                Assert.That(EnvironmentThemeRules.IsDefined(themes[i]), Is.True);
                Assert.That(EnvironmentThemeRules.GetStableFragment(themes[i]), Is.Not.Empty);
            }
        }

        [Test]
        public void SceneContext_WithValidDefinition_Passes()
        {
            using var assets = new EnvironmentTestAssets();
            var definition = assets.CreateDefinition();
            var context = assets.CreateSceneContext(definition);

            Assert.That(context.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void SceneContext_WithoutDefinition_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var context = assets.CreateSceneContext(null);

            Assert.That(context.TryValidate(out var error), Is.False);
            StringAssert.Contains("Definition reference is missing", error);
        }
    }
}
