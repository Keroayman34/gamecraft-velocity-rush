using NUnit.Framework;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class NextEventResolverTests
    {
        [Test]
        public void TryResolveNextEvent_WithinSameChampionship_AdvancesRoundSequentially()
        {
            // C1 Rounds 1..4 advance to next round
            for (int r = 1; r < 5; r++)
            {
                string current = $"c1-ignition-series-round-{r}";
                bool resolved = NextEventResolver.TryResolveNextEvent(current, out string nextChamp, out string nextEvent);

                Assert.That(resolved, Is.True);
                Assert.That(nextChamp, Is.EqualTo("c1-ignition-series"));
                Assert.That(nextEvent, Is.EqualTo($"c1-ignition-series-round-{r + 1}"));
            }
        }

        [Test]
        public void TryResolveNextEvent_AtChampionshipBoundary_AdvancesToNextChampionshipRound1()
        {
            // C1 Round 5 -> C2 Round 1
            bool c1End = NextEventResolver.TryResolveNextEvent("c1-ignition-series-round-5", out string nextChamp1, out string nextEvent1);
            Assert.That(c1End, Is.True);
            Assert.That(nextChamp1, Is.EqualTo("c2-apex-challenger"));
            Assert.That(nextEvent1, Is.EqualTo("c2-apex-challenger-round-1"));

            // C2 Round 5 -> C3 Round 1
            bool c2End = NextEventResolver.TryResolveNextEvent("c2-apex-challenger-round-5", out string nextChamp2, out string nextEvent2);
            Assert.That(c2End, Is.True);
            Assert.That(nextChamp2, Is.EqualTo("c3-velocity-masters"));
            Assert.That(nextEvent2, Is.EqualTo("c3-velocity-masters-round-1"));
        }

        [Test]
        public void TryResolveNextEvent_AtGameCompletionBoundary_ReturnsFalse()
        {
            // C3 Round 5 is the final event
            bool c3End = NextEventResolver.TryResolveNextEvent("c3-velocity-masters-round-5", out string nextChamp, out string nextEvent);
            Assert.That(c3End, Is.False);
            Assert.That(nextChamp, Is.Null);
            Assert.That(nextEvent, Is.Null);
        }

        [Test]
        public void TryResolveNextEvent_InvalidOrNullId_ReturnsFalseFailClosed()
        {
            Assert.That(NextEventResolver.TryResolveNextEvent(null, out _, out _), Is.False);
            Assert.That(NextEventResolver.TryResolveNextEvent("", out _, out _), Is.False);
            Assert.That(NextEventResolver.TryResolveNextEvent("invalid-id", out _, out _), Is.False);
            Assert.That(NextEventResolver.TryResolveNextEvent("c1-ignition-series-round-99", out _, out _), Is.False);
        }

        [Test]
        public void GetEnvironmentSceneName_ProducesExpectedSceneNames()
        {
            Assert.That(NextEventResolver.GetEnvironmentSceneName(EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature),
                Is.EqualTo("desert-signature"));
            Assert.That(NextEventResolver.GetEnvironmentSceneName(EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse),
                Is.EqualTo("desert-reverse"));
            Assert.That(NextEventResolver.GetEnvironmentSceneName(EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature),
                Is.EqualTo("neon-city-signature"));
            Assert.That(NextEventResolver.GetEnvironmentSceneName(EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Reverse),
                Is.EqualTo("coastal-reverse"));
        }
    }
}
