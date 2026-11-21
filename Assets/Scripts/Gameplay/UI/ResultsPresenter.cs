using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Post-race Results & Rewards screen presenter (D-068, D-069).
    /// Displays position, time, star awards, itemized credit rewards, unlocks,
    /// and Next Event / Retry / Menu actions.
    /// </summary>
    public sealed class ResultsPresenter
    {
        private readonly VisualElement _root;
        private Label _titleLabel;
        private Label _positionLabel;
        private Label _timeLabel;
        private Label _starsLabel;
        private Label _creditsBreakdownLabel;
        private Label _totalCreditsLabel;
        private Label _unlocksLabel;
        private Button _nextEventButton;
        private Button _retryButton;
        private Button _menuButton;

        private ProgressionOutcome _outcome;
        private bool _hasOutcome;
        private int _finishPosition;
        private int _roadCoinCredits;
        private int _nearMissCredits;

        public event Action NextEventRequested;
        public event Action RetryRequested;
        public event Action MenuRequested;

        public ResultsPresenter(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            Build();
            LocalizationService.LanguageChanged += OnLanguageChanged;
            RefreshLocalization();
        }

        public void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void Build()
        {
            _root.Clear();
            _root.style.position = Position.Absolute;
            _root.style.left = 0f;
            _root.style.top = 0f;
            _root.style.right = 0f;
            _root.style.bottom = 0f;
            _root.style.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;

            _titleLabel = new Label { name = "results-title" };
            _titleLabel.style.fontSize = 42f;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 15f;
            _root.Add(_titleLabel);

            var summaryBox = new VisualElement { name = "results-summary-box" };
            summaryBox.style.width = 420f;
            summaryBox.style.paddingLeft = 20f;
            summaryBox.style.paddingRight = 20f;
            summaryBox.style.paddingTop = 15f;
            summaryBox.style.paddingBottom = 15f;
            summaryBox.style.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            summaryBox.style.borderTopLeftRadius = 8f;
            summaryBox.style.borderTopRightRadius = 8f;
            summaryBox.style.borderBottomLeftRadius = 8f;
            summaryBox.style.borderBottomRightRadius = 8f;
            summaryBox.style.marginBottom = 20f;

            _positionLabel = new Label { name = "results-position" };
            _positionLabel.style.fontSize = 24f;
            _positionLabel.style.color = Color.white;
            _positionLabel.style.marginBottom = 6f;
            summaryBox.Add(_positionLabel);

            _timeLabel = new Label { name = "results-time" };
            _timeLabel.style.fontSize = 20f;
            _timeLabel.style.color = new Color(0.8f, 0.9f, 1f);
            _timeLabel.style.marginBottom = 8f;
            summaryBox.Add(_timeLabel);

            _starsLabel = new Label { name = "results-stars" };
            _starsLabel.style.fontSize = 28f;
            _starsLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _starsLabel.style.marginBottom = 12f;
            summaryBox.Add(_starsLabel);

            _creditsBreakdownLabel = new Label { name = "results-breakdown" };
            _creditsBreakdownLabel.style.fontSize = 15f;
            _creditsBreakdownLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _creditsBreakdownLabel.style.marginBottom = 6f;
            summaryBox.Add(_creditsBreakdownLabel);

            _totalCreditsLabel = new Label { name = "results-total-credits" };
            _totalCreditsLabel.style.fontSize = 22f;
            _totalCreditsLabel.style.color = new Color(0.3f, 1f, 0.4f);
            _totalCreditsLabel.style.marginBottom = 8f;
            summaryBox.Add(_totalCreditsLabel);

            _unlocksLabel = new Label { name = "results-unlocks" };
            _unlocksLabel.style.fontSize = 16f;
            _unlocksLabel.style.color = new Color(1f, 0.6f, 0.2f);
            summaryBox.Add(_unlocksLabel);

            _root.Add(summaryBox);

            var buttonRow = new VisualElement { name = "results-buttons" };
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.style.justifyContent = Justify.Center;

            _nextEventButton = new Button { name = "results-next" };
            _nextEventButton.style.width = 130f;
            _nextEventButton.style.height = 42f;
            _nextEventButton.style.marginRight = 10f;
            _nextEventButton.clicked += () => NextEventRequested?.Invoke();
            buttonRow.Add(_nextEventButton);

            _retryButton = new Button { name = "results-retry" };
            _retryButton.style.width = 130f;
            _retryButton.style.height = 42f;
            _retryButton.style.marginRight = 10f;
            _retryButton.clicked += () => RetryRequested?.Invoke();
            buttonRow.Add(_retryButton);

            _menuButton = new Button { name = "results-menu" };
            _menuButton.style.width = 130f;
            _menuButton.style.height = 42f;
            _menuButton.clicked += () => MenuRequested?.Invoke();
            buttonRow.Add(_menuButton);

            _root.Add(buttonRow);
        }

        public void DisplayOutcome(ProgressionOutcome outcome, int finishPosition = 0)
        {
            DisplayOutcome(outcome, finishPosition, 0, 0);
        }

        public void DisplayOutcome(ProgressionOutcome outcome, int finishPosition, int roadCoinCredits, int nearMissCredits)
        {
            _outcome = outcome;
            _hasOutcome = true;
            _finishPosition = finishPosition;
            _roadCoinCredits = roadCoinCredits;
            _nearMissCredits = nearMissCredits;
            RefreshDisplay();
        }

        public void RefreshLocalization()
        {
            _titleLabel.text = LocalizationService.Tr("results.title");
            _nextEventButton.text = LocalizationService.Tr("results.next_event");
            _retryButton.text = LocalizationService.Tr("results.retry");
            _menuButton.text = LocalizationService.Tr("results.menu");

            if (_hasOutcome)
            {
                RefreshDisplay();
            }
        }

        private void RefreshDisplay()
        {
            if (!_hasOutcome) return;

            if (_finishPosition > 0)
            {
                _positionLabel.text = LocalizationService.Tr("results.position") + ": " + _finishPosition;
                _positionLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                _positionLabel.style.display = DisplayStyle.None;
            }

            int whole = (int)_outcome.BestTimeSeconds;
            int hundredths = (int)((_outcome.BestTimeSeconds - whole) * 100);
            string timeStr = whole + "." + hundredths.ToString("D2");
            if (_outcome.IsNewBestTime)
            {
                _timeLabel.text = LocalizationService.Tr("results.time") + ": " + timeStr + " (" + LocalizationService.Tr("results.new_best") + ")";
            }
            else
            {
                _timeLabel.text = LocalizationService.Tr("results.time") + ": " + timeStr;
            }

            _starsLabel.text = "★ " + _outcome.StarsEarnedOnRun + " / 3  (+" + _outcome.NewStarsEarned + ")";

            string breakdown = LocalizationService.Tr("results.base_reward") + ": +" + _outcome.BaseCredits + " " + LocalizationService.Tr("common.cr");
            if (_outcome.StarBonusCredits > 0)
            {
                breakdown += "\n" + LocalizationService.Tr("results.star_bonus") + ": +" + _outcome.StarBonusCredits + " " + LocalizationService.Tr("common.cr");
            }
            if (_outcome.FirstPlaceBonusCredits > 0)
            {
                breakdown += "\n" + LocalizationService.Tr("results.first_place_bonus") + ": +" + _outcome.FirstPlaceBonusCredits + " " + LocalizationService.Tr("common.cr");
            }
            if (_roadCoinCredits > 0)
            {
                breakdown += "\n" + LocalizationService.Tr("results.road_coins") + ": +" + _roadCoinCredits + " " + LocalizationService.Tr("common.cr");
            }
            if (_nearMissCredits > 0)
            {
                breakdown += "\n" + LocalizationService.Tr("results.near_miss_bonus") + ": +" + _nearMissCredits + " " + LocalizationService.Tr("common.cr");
            }
            _creditsBreakdownLabel.text = breakdown;

            int totalCredits = _outcome.TotalCreditsAwarded + _roadCoinCredits + _nearMissCredits;
            _totalCreditsLabel.text = LocalizationService.Tr("results.total_credits") + ": +" + totalCredits + " " + LocalizationService.Tr("common.cr");

            if (_outcome.NewlyUnlockedEventIds.Count > 0 || _outcome.NewlyUnlockedChampionshipIds.Count > 0)
            {
                _unlocksLabel.text = LocalizationService.Tr("results.unlocked");
                _unlocksLabel.style.display = DisplayStyle.Flex;
                _nextEventButton.SetEnabled(true);
            }
            else
            {
                _unlocksLabel.style.display = DisplayStyle.None;
            }
        }

        private void OnLanguageChanged(string lang)
        {
            RefreshLocalization();
        }
    }
}
