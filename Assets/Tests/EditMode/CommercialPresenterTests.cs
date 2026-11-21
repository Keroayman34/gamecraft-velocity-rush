using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    [TestFixture]
    public sealed class CommercialPresenterTests
    {
        private LocalizationTable _table;
        private PlayerProgress _progress;

        [SetUp]
        public void SetUp()
        {
            _table = new LocalizationTable();
            LocalizationService.SetTable(_table);
            LocalizationService.SetLanguage("en");
            _progress = new PlayerProgress();
        }

        [TearDown]
        public void TearDown()
        {
            LocalizationService.Reset();
        }

        [Test]
        public void MainMenuPresenter_PlayAndGarageActions_FireEvents()
        {
            var root = new VisualElement();
            var presenter = new MainMenuPresenter(root, _progress);

            bool playFired = false;
            bool garageFired = false;
            presenter.PlayRequested += () => playFired = true;
            presenter.GarageRequested += () => garageFired = true;

            var playBtn = root.Q<Button>("main-menu-play");
            var garageBtn = root.Q<Button>("main-menu-garage");

            Assert.That(playBtn, Is.Not.Null);
            Assert.That(garageBtn, Is.Not.Null);

            presenter.RequestPlay();
            presenter.RequestGarage();

            Assert.That(playFired, Is.True);
            Assert.That(garageFired, Is.True);

            var title = root.Q<Label>("main-menu-title");
            Assert.That(title.text, Is.EqualTo("Velocity Rush"));

            presenter.Dispose();
        }

        [Test]
        public void ChampionshipSelectPresenter_LockedChampionship_DisabledButton()
        {
            var root = new VisualElement();
            var presenter = new ChampionshipSelectPresenter(root);

            var data = new ChampionshipSelectPresenter.ChampionshipDisplayData[]
            {
                new ChampionshipSelectPresenter.ChampionshipDisplayData
                {
                    ChampionshipId = "c1-ignition-series",
                    NameKey = "champ.c1_name",
                    DifficultyKey = "champ.c1_diff",
                    IsUnlocked = true,
                    TotalStars = 5,
                    MaxStars = 15
                },
                new ChampionshipSelectPresenter.ChampionshipDisplayData
                {
                    ChampionshipId = "c2-apex-challenger",
                    NameKey = "champ.c2_name",
                    DifficultyKey = "champ.c2_diff",
                    IsUnlocked = false,
                    TotalStars = 0,
                    MaxStars = 15
                }
            };

            presenter.SetData(data);

            var btn0 = root.Q<Button>("champ-select-0");
            var btn1 = root.Q<Button>("champ-select-1");

            Assert.That(btn0.enabledSelf, Is.True);
            Assert.That(btn1.enabledSelf, Is.False);

            presenter.Dispose();
        }

        [Test]
        public void EventSelectPresenter_RoundsDisplayed_CorrectStars()
        {
            var root = new VisualElement();
            var presenter = new EventSelectPresenter(root);

            var events = new EventSelectPresenter.EventDisplayData[]
            {
                new EventSelectPresenter.EventDisplayData
                {
                    EventId = "c1-round-1",
                    DisplayName = "Round 1",
                    EventTypeKey = "event.circuit",
                    IsUnlocked = true,
                    StarsEarned = 3,
                    BestTimeSeconds = 48.25d,
                    RoundIndex = 1
                },
                new EventSelectPresenter.EventDisplayData
                {
                    EventId = "c1-round-2",
                    DisplayName = "Round 2",
                    EventTypeKey = "event.timetrial",
                    IsUnlocked = false,
                    StarsEarned = 0,
                    BestTimeSeconds = 0d,
                    RoundIndex = 2
                }
            };

            presenter.SetData(events);

            var starLabel0 = root.Q<Label>("event-stars-0");
            var launchBtn0 = root.Q<Button>("event-launch-0");
            var launchBtn1 = root.Q<Button>("event-launch-1");

            Assert.That(starLabel0.text, Does.Contain("3 / 3"));
            Assert.That(launchBtn0.enabledSelf, Is.True);
            Assert.That(launchBtn1.enabledSelf, Is.False);

            presenter.Dispose();
        }

        [Test]
        public void ResultsPresenter_DisplaysOutcome_FormatsCorrectly()
        {
            var root = new VisualElement();
            var presenter = new ResultsPresenter(root);

            var outcome = new ProgressionOutcome(
                "c1-round-1",
                starsBefore: 0,
                starsEarnedOnRun: 3,
                newStarsEarned: 3,
                totalStarsForEvent: 3,
                isNewBestTime: true,
                bestTimeSeconds: 45.50d,
                baseCredits: 100,
                starBonusCredits: 450,
                firstPlaceBonusCredits: 200,
                totalCreditsAwarded: 750,
                newlyUnlockedEventIds: new[] { "c1-round-2" },
                newlyUnlockedChampionshipIds: System.Array.Empty<string>());

            presenter.DisplayOutcome(outcome, finishPosition: 1);

            var posLabel = root.Q<Label>("results-position");
            var totalLabel = root.Q<Label>("results-total-credits");

            Assert.That(posLabel.text, Does.Contain("1"));
            Assert.That(totalLabel.text, Does.Contain("750"));

            presenter.Dispose();
        }

        [Test]
        public void SettingsPresenter_SliderChanges_EmitsSettingsEvent()
        {
            var root = new VisualElement();
            var presenter = new SettingsPresenter(root, _progress);

            PlayerSettingsDto received = null;
            presenter.SettingsChanged += s => received = s;

            var slider = root.Q<Slider>("settings-master-slider");
            Assert.That(slider, Is.Not.Null);

            presenter.SetMasterVolume(0.5f);

            Assert.That(received, Is.Not.Null);
            Assert.That(received.masterVolume, Is.EqualTo(0.5f).Within(0.01f));

            presenter.Dispose();
        }
    }
}
