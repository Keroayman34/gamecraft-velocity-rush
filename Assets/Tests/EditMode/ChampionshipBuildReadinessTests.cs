using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VelocityRush.Data;
using VelocityRush.EditorTools;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Pure decision tests for the Phase-10 championship build readiness gate (T-10.7.4).
    /// </summary>
    [TestFixture]
    public sealed class ChampionshipBuildReadinessTests
    {
        private static readonly string[] BaseScenes =
        {
            "Assets/Scenes/Bootstrap.unity",
            "Assets/Scenes/Garage.unity"
        };

        private static string[] AllEventScenes()
        {
            var paths = new List<string>();
            for (var i = 0; i < EnvironmentThemeRules.Themes.Count; i++)
            {
                var theme = EnvironmentThemeRules.Themes[i];
                paths.Add(ChampionshipBuildReadinessTool.GetEventScenePath(theme, EnvironmentLayoutVariant.Signature));
                paths.Add(ChampionshipBuildReadinessTool.GetEventScenePath(theme, EnvironmentLayoutVariant.Reverse));
            }

            return paths.ToArray();
        }

        private static string[] Profile()
        {
            return BaseScenes.Concat(AllEventScenes()).ToArray();
        }

        [Test]
        public void AllTenEventScenesExistAndAreInProfile_IsReady()
        {
            var profile = Profile();
            var result = ChampionshipBuildReadinessTool.Evaluate(
                profile,
                _ => true,
                profile,
                AllEventScenes());

            Assert.IsTrue(result.IsReady, string.Join("\n", result.Issues));
            Assert.AreEqual(profile.Length, result.RequiredSceneCount);
            Assert.AreEqual(ChampionshipContentPolicy.RequiredConfigurationCount, result.ExpectedEventSceneCount);
            Assert.AreEqual(5, result.ReverseSceneCount);
            Assert.IsTrue(result.AllScenesInBuildSettings);
        }

        [Test]
        public void MissingEventScene_FailsClosed()
        {
            var expected = AllEventScenes();
            var missing = expected[0];
            var result = ChampionshipBuildReadinessTool.Evaluate(
                Profile(),
                path => !string.Equals(path, missing, StringComparison.Ordinal),
                Profile(),
                expected);

            Assert.IsFalse(result.IsReady);
            Assert.IsTrue(result.Issues.Any(issue => issue.Contains(missing)));
        }

        [Test]
        public void EventSceneMissingFromProfile_FailsClosed()
        {
            var expected = AllEventScenes();
            var omitted = expected[3];
            var profile = Profile().Where(path => path != omitted).ToArray();
            var result = ChampionshipBuildReadinessTool.Evaluate(
                profile,
                _ => true,
                profile,
                expected);

            Assert.IsFalse(result.IsReady);
            Assert.IsTrue(result.Issues.Any(issue => issue.Contains("not listed") && issue.Contains(omitted)));
        }

        [Test]
        public void MissingReverseScene_FailsClosed()
        {
            var expected = AllEventScenes()
                .Where(path => !path.EndsWith("-reverse.unity", StringComparison.Ordinal))
                .ToArray();
            var result = ChampionshipBuildReadinessTool.Evaluate(
                Profile(),
                _ => true,
                Profile(),
                expected);

            Assert.IsFalse(result.IsReady);
            Assert.IsTrue(result.Issues.Any(issue => issue.Contains("Expected exactly 10")));
            Assert.AreEqual(0, result.ReverseSceneCount);
        }

        [Test]
        public void BuildSettingsMismatch_WarnsWithoutFailing()
        {
            var result = ChampionshipBuildReadinessTool.Evaluate(
                Profile(),
                _ => true,
                BaseScenes,
                AllEventScenes());

            Assert.IsTrue(result.IsReady, string.Join("\n", result.Issues));
            Assert.IsFalse(result.AllScenesInBuildSettings);
            Assert.IsTrue(result.Warnings.Any(warning => warning.Contains("EditorBuildSettings")));
        }

        [Test]
        public void ExpectedScenePaths_CoverAllTenConfigurations()
        {
            var paths = AllEventScenes();
            Assert.AreEqual(ChampionshipContentPolicy.RequiredConfigurationCount, paths.Length);
            Assert.AreEqual(paths.Length, paths.Distinct(StringComparer.Ordinal).Count());

            for (var i = 0; i < EnvironmentThemeRules.Themes.Count; i++)
            {
                var theme = EnvironmentThemeRules.Themes[i];
                var fragment = EnvironmentThemeRules.GetStableFragment(theme);
                Assert.IsTrue(paths.Contains("Assets/Scenes/Environments/" + fragment + "-signature.unity"));
                Assert.IsTrue(paths.Contains("Assets/Scenes/Environments/" + fragment + "-reverse.unity"));
            }
        }

        [Test]
        public void UndefinedTheme_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ChampionshipBuildReadinessTool.GetEventScenePath((EnvironmentTheme)99, EnvironmentLayoutVariant.Signature));
        }
    }
}
