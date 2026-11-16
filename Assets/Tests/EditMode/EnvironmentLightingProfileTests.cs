using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Realtime lighting-profile validation and determinism tests (T-9.4).</summary>
    public sealed class EnvironmentLightingProfileTests
    {
        [Test]
        public void DefaultProfile_Passes()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting();

            Assert.That(profile.TryValidate(out var error), Is.True, error);
        }

        [Test]
        public void UnsupportedSchemaVersion_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting();
            EnvironmentTestAssets.SetSerializedInt(profile, "schemaVersion", 99);

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("SchemaVersion", error);
        }

        [Test]
        public void RealtimeOnlyContract_DisablesBakedLighting()
        {
            Assert.That(EnvironmentLightingProfile.SupportsBakedLighting, Is.False);
        }

        [Test]
        public void BuildSettings_IsDeterministic()
        {
            using var assets = new EnvironmentTestAssets();
            var first = assets.CreateLighting();
            var second = assets.CreateLighting();

            var a = first.BuildSettings();
            var b = first.BuildSettings();
            var c = second.BuildSettings();

            AssertSettingsEqual(a, b);
            AssertSettingsEqual(a, c);
        }

        [Test]
        public void BuildSettings_MapsAuthoredValues()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(
                skyTint: new Color(0.1f, 0.2f, 0.3f, 1f),
                groundColor: new Color(0.4f, 0.3f, 0.2f, 1f),
                atmosphereThickness: 2f,
                sunSize: 0.1f,
                sunSizeConvergence: 0.4f,
                exposure: 2f,
                ambientIntensity: 1.5f,
                directionalColor: new Color(1f, 0.9f, 0.8f, 1f),
                directionalIntensity: 3f,
                directionalEuler: new Vector3(30f, 40f, 5f),
                shadowsEnabled: false,
                shadowStrength: 0.2f,
                fogEnabled: false,
                fogColor: new Color(0.5f, 0.6f, 0.7f, 1f),
                fogStartDistance: 10f,
                fogEndDistance: 20f);

            var settings = profile.BuildSettings();

            Assert.That(settings.SkyTint, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 1f)));
            Assert.That(settings.GroundColor, Is.EqualTo(new Color(0.4f, 0.3f, 0.2f, 1f)));
            Assert.That(settings.AtmosphereThickness, Is.EqualTo(2f));
            Assert.That(settings.SunSize, Is.EqualTo(0.1f));
            Assert.That(settings.SunSizeConvergence, Is.EqualTo(0.4f));
            Assert.That(settings.Exposure, Is.EqualTo(2f));
            Assert.That(settings.AmbientIntensity, Is.EqualTo(1.5f));
            Assert.That(settings.DirectionalColor, Is.EqualTo(new Color(1f, 0.9f, 0.8f, 1f)));
            Assert.That(settings.DirectionalIntensity, Is.EqualTo(3f));
            Assert.That(settings.DirectionalEuler, Is.EqualTo(new Vector3(30f, 40f, 5f)));
            Assert.That(settings.ShadowsEnabled, Is.False);
            Assert.That(settings.ShadowStrength, Is.EqualTo(0.2f));
            Assert.That(settings.FogEnabled, Is.False);
            Assert.That(settings.FogColor, Is.EqualTo(new Color(0.5f, 0.6f, 0.7f, 1f)));
            Assert.That(settings.FogStartDistance, Is.EqualTo(10f));
            Assert.That(settings.FogEndDistance, Is.EqualTo(20f));
        }

        [Test]
        public void NonFiniteSkyTint_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(skyTint: new Color(float.NaN, 0.2f, 0.3f, 1f));

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("SkyTint", error);
        }

        [Test]
        public void NegativeColor_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(groundColor: new Color(-0.1f, 0.2f, 0.3f, 1f));

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("GroundColor", error);
        }

        [Test]
        public void AtmosphereOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var low = assets.CreateLighting(atmosphereThickness: 0.05f);
            var high = assets.CreateLighting(atmosphereThickness: 6f);

            StringAssert.Contains("AtmosphereThickness", FirstError(low));
            StringAssert.Contains("AtmosphereThickness", FirstError(high));
        }

        [Test]
        public void SunSizeOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(sunSize: 1.2f);

            StringAssert.Contains("SunSize", FirstError(profile));
        }

        [Test]
        public void SunConvergenceOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(sunSizeConvergence: 1.2f);

            StringAssert.Contains("SunSizeConvergence", FirstError(profile));
        }

        [Test]
        public void ExposureOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(exposure: 0.05f);

            StringAssert.Contains("Exposure", FirstError(profile));
        }

        [Test]
        public void AmbientIntensityOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(ambientIntensity: 3.5f);

            StringAssert.Contains("AmbientIntensity", FirstError(profile));
        }

        [Test]
        public void DirectionalIntensityOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(directionalIntensity: 9f);

            StringAssert.Contains("DirectionalIntensity", FirstError(profile));
        }

        [Test]
        public void ShadowStrengthOutOfRange_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(shadowStrength: 1.5f);

            StringAssert.Contains("ShadowStrength", FirstError(profile));
        }

        [Test]
        public void NonFiniteDirectionalEuler_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(
                directionalEuler: new Vector3(float.NaN, 0f, 0f));

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("DirectionalEuler", error);
        }

        [Test]
        public void NegativeFogStart_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(fogStartDistance: -1f, fogEndDistance: 100f);

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("FogStartDistance", error);
        }

        [Test]
        public void FogEndNotAfterStart_Fails()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(fogStartDistance: 500f, fogEndDistance: 100f);

            Assert.That(profile.TryValidate(out var error), Is.False);
            StringAssert.Contains("FogEndDistance", error);
        }

        [Test]
        public void DisabledFog_WithValidDistances_Passes()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(fogEnabled: false);

            Assert.That(profile.TryValidate(out var error), Is.True, error);
            Assert.That(profile.BuildSettings().FogEnabled, Is.False);
        }

        [Test]
        public void TryValidate_ReturnsFirstIssue()
        {
            using var assets = new EnvironmentTestAssets();
            var profile = assets.CreateLighting(
                skyTint: new Color(float.PositiveInfinity, 0f, 0f, 1f),
                atmosphereThickness: 0.01f);

            Assert.That(profile.TryValidate(out var error), Is.False);
            Assert.That(error, Is.Not.Null.And.Not.Empty);
        }

        private static string FirstError(EnvironmentLightingProfile profile)
        {
            Assert.That(profile.TryValidate(out var error), Is.False);
            return error;
        }

        private static void AssertSettingsEqual(
            EnvironmentLightingSettings left,
            EnvironmentLightingSettings right)
        {
            Assert.That(left.SkyTint, Is.EqualTo(right.SkyTint));
            Assert.That(left.GroundColor, Is.EqualTo(right.GroundColor));
            Assert.That(left.AtmosphereThickness, Is.EqualTo(right.AtmosphereThickness));
            Assert.That(left.SunSize, Is.EqualTo(right.SunSize));
            Assert.That(left.SunSizeConvergence, Is.EqualTo(right.SunSizeConvergence));
            Assert.That(left.Exposure, Is.EqualTo(right.Exposure));
            Assert.That(left.AmbientIntensity, Is.EqualTo(right.AmbientIntensity));
            Assert.That(left.DirectionalColor, Is.EqualTo(right.DirectionalColor));
            Assert.That(left.DirectionalIntensity, Is.EqualTo(right.DirectionalIntensity));
            Assert.That(left.DirectionalEuler, Is.EqualTo(right.DirectionalEuler));
            Assert.That(left.ShadowsEnabled, Is.EqualTo(right.ShadowsEnabled));
            Assert.That(left.ShadowStrength, Is.EqualTo(right.ShadowStrength));
            Assert.That(left.FogEnabled, Is.EqualTo(right.FogEnabled));
            Assert.That(left.FogColor, Is.EqualTo(right.FogColor));
            Assert.That(left.FogStartDistance, Is.EqualTo(right.FogStartDistance));
            Assert.That(left.FogEndDistance, Is.EqualTo(right.FogEndDistance));
        }
    }
}
