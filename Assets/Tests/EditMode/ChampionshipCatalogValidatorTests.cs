using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Data;
using VelocityRush.EditorTools;
using Object = UnityEngine.Object;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// Table-driven defect tests for <see cref="ChampionshipCatalogValidator"/> (T-10.7.5).
    /// </summary>
    /// <remarks>
    /// Every case starts from the approved 15-event fixture and seeds exactly one structural,
    /// coverage, schema, or unlock-chain defect. The validator must reject each defect with an
    /// actionable message. The valid fixture must pass.
    /// </remarks>
    [TestFixture]
    public sealed class ChampionshipCatalogValidatorTests
    {
        private const string C1 = "c1-ignition-series";
        private const string C2 = "c2-apex-challenger";
        private const string C3 = "c3-velocity-masters";

        private readonly List<Object> _created = new List<Object>();
        private EnvironmentTestAssets _environmentAssets;

        [SetUp]
        public void SetUp()
        {
            _created.Clear();
            _environmentAssets = new EnvironmentTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            for (var i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            _environmentAssets.Dispose();
        }

        // ==================== APPROVED FIXTURE ====================

        private sealed class Fixture
        {
            public ChampionshipDefinition[] Championships;
            public List<RaceEventDefinition> Events;
            public ChampionshipCatalog Catalog;
            public EnvironmentCatalog Environments;

            public void ReplaceChampionships(ChampionshipDefinition[] championships)
            {
                Championships = championships;
                Catalog.ConfigureForAuthoring(championships);
            }

            public void ReplaceEvent(RaceEventDefinition replacement)
            {
                Events.RemoveAll(definition => definition.EventId == replacement.EventId);
                Events.Add(replacement);
            }
        }

        private Fixture CreateValidFixture()
        {
            var c1 = NewChampionship(C1, 1, 0.90f, EventUnlockPolicy.AlwaysAvailable, null);
            var c2 = NewChampionship(C2, 2, 0.97f, EventUnlockPolicy.RequiresPreviousChampionship, c1);
            var c3 = NewChampionship(C3, 3, 1.03f, EventUnlockPolicy.RequiresPreviousChampionship, c2);

            var events = new List<RaceEventDefinition>
            {
                NewEvent(C1, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.TimeTrial, EventUnlockPolicy.AlwaysAvailable, 0.90f),
                NewEvent(C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f),
                NewEvent(C1, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.90f),
                NewEvent(C1, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f),
                NewEvent(C1, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Reverse, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f),
                NewEvent(C2, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousChampionship, 0.97f),
                NewEvent(C2, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.97f),
                NewEvent(C2, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.97f),
                NewEvent(C2, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.97f),
                NewEvent(C2, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.97f),
                NewEvent(C3, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousChampionship, 1.03f),
                NewEvent(C3, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 1.03f),
                NewEvent(C3, 3, EnvironmentTheme.MountainPass, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 1.03f),
                NewEvent(C3, 4, EnvironmentTheme.NeonCity, EnvironmentLayoutVariant.Signature, RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 1.03f),
                NewEvent(C3, 5, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Reverse, RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 1.03f)
            };

            var catalogObject = new GameObject("Test Championship Catalog");
            _created.Add(catalogObject);
            var catalog = catalogObject.AddComponent<ChampionshipCatalog>();
            catalog.ConfigureForAuthoring(new[] { c1, c2, c3 });

            var environments = _environmentAssets.CreateCatalog(
                EnvironmentTestAssets.CreateFiveThemeDefinitions(_environmentAssets));

            return new Fixture
            {
                Championships = new[] { c1, c2, c3 },
                Events = events,
                Catalog = catalog,
                Environments = environments
            };
        }

        private ChampionshipDefinition NewChampionship(
            string id,
            int order,
            float pace,
            EventUnlockPolicy policy,
            ChampionshipDefinition prior)
        {
            var championship = ScriptableObject.CreateInstance<ChampionshipDefinition>();
            _created.Add(championship);
            championship.ConfigureForAuthoring(
                id,
                ChampionshipNameEn(id),
                ChampionshipNameAr(id),
                DifficultyNameEn(order),
                DifficultyNameAr(order),
                order,
                pace,
                policy,
                "Championship fixture for " + id,
                prior);
            return championship;
        }

        private RaceEventDefinition NewEvent(
            string championshipId,
            int round,
            EnvironmentTheme theme,
            EnvironmentLayoutVariant variant,
            RaceEventType type,
            EventUnlockPolicy policy,
            float pace,
            string eventId = null,
            EventOpponentMode? opponentMode = null,
            string nameOverrideEn = null,
            string nameOverrideAr = null)
        {
            var id = eventId ?? championshipId + "-round-" + round;
            var definition = ScriptableObject.CreateInstance<RaceEventDefinition>();
            _created.Add(definition);
            definition.ConfigureForAuthoring(
                id,
                nameOverrideEn ?? ChampionshipNameEn(championshipId) + " - Round " + round,
                nameOverrideAr ?? ChampionshipNameAr(championshipId) + " - الجولة " + round,
                type,
                theme,
                variant,
                championshipId,
                round,
                opponentMode ?? ChampionshipContentPolicy.GetExpectedOpponentMode(type),
                policy,
                pace,
                "Event fixture for " + id);
            return definition;
        }

        private static string ChampionshipNameEn(string id)
        {
            switch (id)
            {
                case C1:
                    return "Ignition Series";
                case C2:
                    return "Apex Challenger";
                case C3:
                    return "Velocity Masters";
                default:
                    return "Unknown Championship";
            }
        }

        private static string ChampionshipNameAr(string id)
        {
            switch (id)
            {
                case C1:
                    return "سلسلة الانطلاق";
                case C2:
                    return "تحدي القمة";
                case C3:
                    return "أسياد السرعة";
                default:
                    return "بطولة غير معروفة";
            }
        }

        private static string DifficultyNameEn(int order)
        {
            switch (order)
            {
                case 1:
                    return "Low";
                case 2:
                    return "Medium";
                default:
                    return "High";
            }
        }

        private static string DifficultyNameAr(int order)
        {
            switch (order)
            {
                case 1:
                    return "منخفض";
                case 2:
                    return "متوسط";
                default:
                    return "عالي";
            }
        }

        // ==================== VALID BASELINE ====================

        [Test]
        public void ValidApprovedMatrix_PassesWithFullCoverage()
        {
            var fixture = CreateValidFixture();

            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(
                fixture.Catalog,
                fixture.Events,
                fixture.Environments);

            Assert.IsTrue(
                result.IsValid,
                "Expected the approved fixture to pass, but got:\n" + string.Join("\n", result.Errors));
            Assert.AreEqual(0, result.Errors.Count);
            Assert.AreEqual(3, result.Coverage.ChampionshipCount);
            Assert.AreEqual(15, result.Coverage.TotalEventCount);
            Assert.AreEqual(9, result.Coverage.CircuitRaceCount);
            Assert.AreEqual(6, result.Coverage.TimeTrialCount);
            Assert.AreEqual(ChampionshipContentPolicy.RequiredConfigurationCount, result.Coverage.CoveredConfigurationCount);
            Assert.IsTrue(result.Coverage.UnlockChainValid);
            Assert.IsFalse(result.Coverage.HasCyclicUnlock);
            Assert.IsTrue(result.Coverage.AllEventsReachable);

            for (var theme = 0; theme < EnvironmentThemeRules.Themes.Count; theme++)
            {
                Assert.AreEqual(
                    ChampionshipContentPolicy.RequiredEventsPerEnvironment,
                    result.Coverage.EventsPerEnvironment[theme],
                    "Environment " + EnvironmentThemeRules.GetDisplayName((EnvironmentTheme)theme));
                Assert.Greater(result.Coverage.EventsPerConfiguration[theme * 2], 0);
                Assert.Greater(result.Coverage.EventsPerConfiguration[theme * 2 + 1], 0);
            }

            Assert.AreEqual(8, result.Coverage.EventsPerLayoutVariant[(int)EnvironmentLayoutVariant.Signature]);
            Assert.AreEqual(7, result.Coverage.EventsPerLayoutVariant[(int)EnvironmentLayoutVariant.Reverse]);
            Assert.AreEqual(5, result.Coverage.EventsPerChampionship[0]);
            Assert.AreEqual(5, result.Coverage.EventsPerChampionship[1]);
            Assert.AreEqual(5, result.Coverage.EventsPerChampionship[2]);
        }

        // ==================== TABLE-DRIVEN DEFECTS ====================

        public enum DefectKind
        {
            CyclicChampionshipChain,
            TwoRootChampionships,
            NoRootChampionship,
            SixteenEvents,
            FourteenEvents,
            CircuitRaceWithoutAi,
            TimeTrialWithAi,
            MissingEnvironment,
            FourEventsInEnvironment,
            MissingReverseConfiguration,
            MissingSignatureConfiguration,
            DuplicateEventId,
            DuplicateChampionshipId,
            InvalidRoundIndex,
            DuplicateRoundIndex,
            WrongEventPace,
            WrongChampionshipPace,
            FirstEventWrongUnlock,
            SecondEventAlwaysAvailable,
            MissingChampionshipReference,
            UnapprovedEventType,
            UnapprovedOpponentMode,
            CyclicEventChain
        }

        private static IEnumerable<TestCaseData> DefectCases()
        {
            yield return Case(DefectKind.CyclicChampionshipChain, "cyclic dependency");
            yield return Case(DefectKind.TwoRootChampionships, "exactly one root championship");
            yield return Case(DefectKind.NoRootChampionship, "exactly one root championship");
            yield return Case(DefectKind.SixteenEvents, "must contain exactly 15 events");
            yield return Case(DefectKind.FourteenEvents, "must contain exactly 15 events");
            yield return Case(DefectKind.CircuitRaceWithoutAi, "must use OpponentMode FixedFourAI");
            yield return Case(DefectKind.TimeTrialWithAi, "does not match OpponentMode None");
            yield return Case(DefectKind.MissingEnvironment, "World Arena' has 0 events");
            yield return Case(DefectKind.FourEventsInEnvironment, "Desert Circuit' has 4 events");
            yield return Case(DefectKind.MissingReverseConfiguration, "no Reverse variant");
            yield return Case(DefectKind.MissingSignatureConfiguration, "no Signature variant");
            yield return Case(DefectKind.DuplicateEventId, "Duplicate EventId");
            yield return Case(DefectKind.DuplicateChampionshipId, "Duplicate ChampionshipId");
            yield return Case(DefectKind.InvalidRoundIndex, "RoundIndex must be within 1..5");
            yield return Case(DefectKind.DuplicateRoundIndex, "duplicate RoundIndex");
            yield return Case(DefectKind.WrongEventPace, "pace scale");
            yield return Case(DefectKind.WrongChampionshipPace, "PaceScale");
            yield return Case(DefectKind.FirstEventWrongUnlock, "must have unlock policy");
            yield return Case(DefectKind.SecondEventAlwaysAvailable, "must have unlock policy RequiresPreviousEvent");
            yield return Case(DefectKind.MissingChampionshipReference, "not found for event");
            yield return Case(DefectKind.UnapprovedEventType, "not approved for M8");
            yield return Case(DefectKind.UnapprovedOpponentMode, "not approved for M8");
            yield return Case(DefectKind.CyclicEventChain, "cyclic or broken");
        }

        private static TestCaseData Case(DefectKind kind, string expected)
        {
            return new TestCaseData(kind, expected).SetName("Defect_" + kind + "_Rejected");
        }

        [Test, TestCaseSource(nameof(DefectCases))]
        public void SeededDefect_IsRejectedWithActionableMessage(DefectKind defect, string expectedSubstring)
        {
            var fixture = CreateValidFixture();
            ApplyDefect(fixture, defect);

            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(
                fixture.Catalog,
                fixture.Events,
                fixture.Environments);

            Assert.IsFalse(result.IsValid, "Defect '" + defect + "' was not rejected.");
            Assert.IsTrue(
                result.Errors.Any(error => error.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) >= 0),
                "Defect '" + defect + "' did not produce a message containing '" + expectedSubstring +
                "'.\nErrors:\n" + string.Join("\n", result.Errors));
        }

        private void ApplyDefect(Fixture fixture, DefectKind defect)
        {
            switch (defect)
            {
                case DefectKind.CyclicChampionshipChain:
                {
                    var c1 = NewChampionship(C1, 1, 0.90f, EventUnlockPolicy.RequiresPreviousChampionship, null);
                    var c2 = NewChampionship(C2, 2, 0.97f, EventUnlockPolicy.RequiresPreviousChampionship, c1);
                    var c3 = NewChampionship(C3, 3, 1.03f, EventUnlockPolicy.RequiresPreviousChampionship, c2);
                    // C1 now points at C3: C1 -> C3 -> C2 -> C1.
                    c1.ConfigureForAuthoring(
                        C1, ChampionshipNameEn(C1), ChampionshipNameAr(C1), "Low", "منخفض",
                        1, 0.90f, EventUnlockPolicy.RequiresPreviousChampionship,
                        "Cyclic fixture", c3);
                    fixture.ReplaceChampionships(new[] { c1, c2, c3 });
                    break;
                }

                case DefectKind.TwoRootChampionships:
                {
                    var c1 = fixture.Championships[0];
                    var brokenC2 = NewChampionship(C2, 2, 0.97f, EventUnlockPolicy.AlwaysAvailable, null);
                    var c3 = fixture.Championships[2];
                    fixture.ReplaceChampionships(new[] { c1, brokenC2, c3 });
                    break;
                }

                case DefectKind.NoRootChampionship:
                {
                    var c1 = NewChampionship(C1, 1, 0.90f, EventUnlockPolicy.RequiresPreviousChampionship, null);
                    var c2 = NewChampionship(C2, 2, 0.97f, EventUnlockPolicy.RequiresPreviousChampionship, c1);
                    var c3 = NewChampionship(C3, 3, 1.03f, EventUnlockPolicy.RequiresPreviousChampionship, c2);
                    c1.ConfigureForAuthoring(
                        C1, ChampionshipNameEn(C1), ChampionshipNameAr(C1), "Low", "منخفض",
                        1, 0.90f, EventUnlockPolicy.RequiresPreviousChampionship,
                        "No-root fixture", c3);
                    fixture.ReplaceChampionships(new[] { c1, c2, c3 });
                    break;
                }

                case DefectKind.SixteenEvents:
                    fixture.Events.Add(NewEvent(
                        C1, 6, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse,
                        RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        eventId: "c1-round-6"));
                    break;

                case DefectKind.FourteenEvents:
                    fixture.Events.RemoveAt(fixture.Events.Count - 1);
                    break;

                case DefectKind.CircuitRaceWithoutAi:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        opponentMode: EventOpponentMode.None));
                    break;

                case DefectKind.TimeTrialWithAi:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.TimeTrial, EventUnlockPolicy.AlwaysAvailable, 0.90f,
                        opponentMode: EventOpponentMode.FixedFourAI));
                    break;

                case DefectKind.MissingEnvironment:
                    MoveTheme(fixture, EnvironmentTheme.WorldArena, EnvironmentTheme.NeonCity);
                    break;

                case DefectKind.FourEventsInEnvironment:
                    MoveTheme(fixture, EnvironmentTheme.WorldArena, EnvironmentTheme.DesertCircuit, 1);
                    break;

                case DefectKind.MissingReverseConfiguration:
                    MoveVariant(fixture, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse, EnvironmentLayoutVariant.Signature);
                    break;

                case DefectKind.MissingSignatureConfiguration:
                    MoveVariant(fixture, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature, EnvironmentLayoutVariant.Reverse);
                    break;

                case DefectKind.DuplicateEventId:
                    fixture.Events.Add(NewEvent(
                        C1, 6, EnvironmentTheme.WorldArena, EnvironmentLayoutVariant.Signature,
                        RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        eventId: C1 + "-round-1",
                        nameOverrideEn: "Duplicate Event",
                        nameOverrideAr: "حدث مكرر"));
                    break;

                case DefectKind.DuplicateChampionshipId:
                {
                    var c1 = fixture.Championships[0];
                    var duplicate = NewChampionship(C1, 2, 0.97f, EventUnlockPolicy.RequiresPreviousChampionship, c1);
                    fixture.ReplaceChampionships(new[] { c1, duplicate, fixture.Championships[2] });
                    break;
                }

                case DefectKind.InvalidRoundIndex:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 0, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.TimeTrial, EventUnlockPolicy.AlwaysAvailable, 0.90f,
                        eventId: C1 + "-round-1"));
                    break;

                case DefectKind.DuplicateRoundIndex:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 1, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        eventId: C1 + "-round-2"));
                    break;

                case DefectKind.WrongEventPace:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 1.03f));
                    break;

                case DefectKind.WrongChampionshipPace:
                {
                    var c1 = fixture.Championships[0];
                    var c2 = NewChampionship(C2, 2, 1.03f, EventUnlockPolicy.RequiresPreviousChampionship, c1);
                    fixture.ReplaceChampionships(new[] { c1, c2, fixture.Championships[2] });
                    break;
                }

                case DefectKind.FirstEventWrongUnlock:
                    fixture.ReplaceEvent(NewEvent(
                        C2, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Reverse,
                        RaceEventType.CircuitRace, EventUnlockPolicy.AlwaysAvailable, 0.97f));
                    break;

                case DefectKind.SecondEventAlwaysAvailable:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.CircuitRace, EventUnlockPolicy.AlwaysAvailable, 0.90f));
                    break;

                case DefectKind.MissingChampionshipReference:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.TimeTrial, EventUnlockPolicy.AlwaysAvailable, 0.90f,
                        eventId: C1 + "-round-1"));
                    // Re-create under an id that is not present in the catalog.
                    var orphan = NewEvent(
                        "missing-championship", 1, EnvironmentTheme.DesertCircuit,
                        EnvironmentLayoutVariant.Signature, RaceEventType.TimeTrial,
                        EventUnlockPolicy.AlwaysAvailable, 0.90f,
                        eventId: C1 + "-round-1",
                        nameOverrideEn: "Orphan Event",
                        nameOverrideAr: "حدث يتيم");
                    fixture.ReplaceEvent(orphan);
                    break;

                case DefectKind.UnapprovedEventType:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        (RaceEventType)99, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        opponentMode: EventOpponentMode.None));
                    break;

                case DefectKind.UnapprovedOpponentMode:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 2, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.CircuitRace, EventUnlockPolicy.RequiresPreviousEvent, 0.90f,
                        opponentMode: (EventOpponentMode)99));
                    break;

                case DefectKind.CyclicEventChain:
                    fixture.ReplaceEvent(NewEvent(
                        C1, 1, EnvironmentTheme.DesertCircuit, EnvironmentLayoutVariant.Signature,
                        RaceEventType.TimeTrial, EventUnlockPolicy.RequiresPreviousEvent, 0.90f));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(defect), defect, null);
            }
        }

        private void MoveTheme(Fixture fixture, EnvironmentTheme from, EnvironmentTheme to, int count = int.MaxValue)
        {
            var moved = 0;
            for (var i = 0; i < fixture.Events.Count && moved < count; i++)
            {
                var definition = fixture.Events[i];
                if (definition.EnvironmentTheme != from)
                {
                    continue;
                }

                fixture.Events[i] = NewEvent(
                    definition.ChampionshipId,
                    definition.RoundIndex,
                    to,
                    definition.LayoutVariant,
                    definition.EventType,
                    definition.UnlockPolicy,
                    definition.PaceScale,
                    eventId: definition.EventId,
                    nameOverrideEn: definition.DisplayNameEn,
                    nameOverrideAr: definition.DisplayNameAr);
                moved++;
            }
        }

        private void MoveVariant(
            Fixture fixture,
            EnvironmentTheme theme,
            EnvironmentLayoutVariant from,
            EnvironmentLayoutVariant to)
        {
            for (var i = 0; i < fixture.Events.Count; i++)
            {
                var definition = fixture.Events[i];
                if (definition.EnvironmentTheme != theme || definition.LayoutVariant != from)
                {
                    continue;
                }

                fixture.Events[i] = NewEvent(
                    definition.ChampionshipId,
                    definition.RoundIndex,
                    definition.EnvironmentTheme,
                    to,
                    definition.EventType,
                    definition.UnlockPolicy,
                    definition.PaceScale,
                    eventId: definition.EventId,
                    nameOverrideEn: definition.DisplayNameEn,
                    nameOverrideAr: definition.DisplayNameAr);
            }
        }

        // ==================== NULL AND EDGE CASES ====================

        [Test]
        public void NullChampionshipCatalog_Rejected()
        {
            var fixture = CreateValidFixture();
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(null, fixture.Events, fixture.Environments);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("ChampionshipCatalog") && error.Contains("null")));
        }

        [Test]
        public void NullEventDefinitions_Rejected()
        {
            var fixture = CreateValidFixture();
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, null, fixture.Environments);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("EventDefinitions") && error.Contains("null")));
        }

        [Test]
        public void NullEnvironmentCatalog_Rejected()
        {
            var fixture = CreateValidFixture();
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, fixture.Events, null);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("EnvironmentCatalog") && error.Contains("null")));
        }

        [Test]
        public void EmptyChampionshipCatalog_Rejected()
        {
            var fixture = CreateValidFixture();
            fixture.ReplaceChampionships(Array.Empty<ChampionshipDefinition>());
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, fixture.Events, fixture.Environments);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("exactly 3 championships")));
        }

        [Test]
        public void NullEventEntry_Rejected()
        {
            var fixture = CreateValidFixture();
            fixture.Events.Add(null);
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, fixture.Events, fixture.Environments);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("entry") && error.Contains("null")));
        }

        [Test]
        public void MissingEnvironmentFromCatalog_Rejected()
        {
            var fixture = CreateValidFixture();
            var definitions = _environmentAssets.CreateDefinition(
                EnvironmentTheme.DesertCircuit,
                "desert-test",
                "Desert Circuit",
                "دائرة الصحراء");
            var incomplete = new[] { definitions };
            var incompleteCatalog = _environmentAssets.CreateCatalog(incomplete);
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(
                fixture.Catalog,
                fixture.Events,
                incompleteCatalog);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(error => error.Contains("World Arena") && error.Contains("missing")));
        }

        [Test]
        public void CoveragePolicy_DirectCall_ReportsMissingConfiguration()
        {
            var fixture = CreateValidFixture();
            MoveVariant(fixture, EnvironmentTheme.CoastalCircuit, EnvironmentLayoutVariant.Reverse, EnvironmentLayoutVariant.Signature);
            var issues = ChampionshipCatalogValidator.ValidateCoveragePolicy(fixture.Championships, fixture.Events);
            Assert.IsTrue(issues.Any(issue => issue.Contains("Coastal Circuit") && issue.Contains("Reverse")));
        }

        // ==================== CONTENT POLICY ====================

        [Test]
        public void ContentPolicy_MatchesApprovedMatrix()
        {
            Assert.AreEqual(3, ChampionshipContentPolicy.RequiredChampionshipCount);
            Assert.AreEqual(5, ChampionshipContentPolicy.EventsPerChampionship);
            Assert.AreEqual(15, ChampionshipContentPolicy.RequiredTotalEvents);
            Assert.AreEqual(3, ChampionshipContentPolicy.RequiredEventsPerEnvironment);
            Assert.AreEqual(10, ChampionshipContentPolicy.RequiredConfigurationCount);
            Assert.AreEqual(9, ChampionshipContentPolicy.RequiredCircuitRaceEvents);
            Assert.AreEqual(6, ChampionshipContentPolicy.RequiredTimeTrialEvents);
            Assert.AreEqual(4, ChampionshipContentPolicy.CircuitRaceAiCount);
            CollectionAssert.AreEqual(
                new[] { 0.90f, 0.97f, 1.03f },
                ChampionshipContentPolicy.ApprovedPaceScales);
            Assert.IsTrue(ChampionshipContentPolicy.IsApprovedEventType(RaceEventType.CircuitRace));
            Assert.IsTrue(ChampionshipContentPolicy.IsApprovedEventType(RaceEventType.TimeTrial));
            Assert.IsFalse(ChampionshipContentPolicy.IsApprovedEventType((RaceEventType)99));
            Assert.AreEqual(EventOpponentMode.FixedFourAI, ChampionshipContentPolicy.GetExpectedOpponentMode(RaceEventType.CircuitRace));
            Assert.AreEqual(EventOpponentMode.None, ChampionshipContentPolicy.GetExpectedOpponentMode(RaceEventType.TimeTrial));
            Assert.AreEqual(4, EventOpponentModeRules.ModeForEventType(RaceEventType.CircuitRace) == EventOpponentMode.FixedFourAI ? 4 : 0);
        }

        // ==================== REPORT ====================

        [Test]
        public void JsonReport_ValidResult_ContainsCoverageAndStableKeys()
        {
            var fixture = CreateValidFixture();
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, fixture.Events, fixture.Environments);
            var json = ChampionshipCatalogValidatorTool.BuildJsonReport(result);

            StringAssert.Contains("\"schemaVersion\": \"1.0\"", json);
            StringAssert.Contains("\"isValid\": true", json);
            StringAssert.Contains("\"totalEventCount\": 15", json);
            StringAssert.Contains("\"circuitRaceCount\": 9", json);
            StringAssert.Contains("\"timeTrialCount\": 6", json);
            StringAssert.Contains("\"eventsPerConfiguration\"", json);
            StringAssert.Contains("\"coveredConfigurationCount\": 10", json);
            StringAssert.Contains("\"unlockChainValid\": true", json);
        }

        [Test]
        public void JsonReport_InvalidResult_ContainsEscapedErrors()
        {
            var fixture = CreateValidFixture();
            fixture.Events.RemoveAt(0);
            var result = ChampionshipCatalogValidator.ValidateCompleteCatalog(fixture.Catalog, fixture.Events, fixture.Environments);
            var json = ChampionshipCatalogValidatorTool.BuildJsonReport(result);

            StringAssert.Contains("\"isValid\": false", json);
            StringAssert.Contains("must contain exactly 15 events", json);
        }

        [Test]
        public void ShouldFail_WarningsOnly_DependsOnFailOnWarnings()
        {
            var warningsOnly = ChampionshipCatalogValidator.ValidationResult.ValidWithWarnings(
                new[] { "advisory warning" },
                new ChampionshipCatalogValidator.CatalogCoverage());
            Assert.IsFalse(ChampionshipCatalogValidatorTool.ShouldFail(warningsOnly, false));
            Assert.IsTrue(ChampionshipCatalogValidatorTool.ShouldFail(warningsOnly, true));
        }
    }
}
