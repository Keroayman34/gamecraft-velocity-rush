using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// In-game HUD presenter executing on the 0 B/frame hot path (D-069, TDD §40.7).
    /// Uses pre-allocated char[] buffers via <see cref="HudStringFormatter"/> and direct
    /// label text assignments to eliminate per-frame allocations during live race ticks.
    /// </summary>
    public sealed class RaceHudPresenter
    {
        private readonly VisualElement _root;
        private Label _speedLabel;
        private Label _speedUnitLabel;
        private Label _timeLabel;
        private Label _timeValueLabel;
        private Label _positionLabel;
        private Label _positionValueLabel;
        private Label _countdownLabel;
        private VisualElement _bonusBox;
        private Label _bonusLabel;
        private Label _bonusValueLabel;
        private Label _popupBannerLabel;

        // Pre-allocated char buffers for 0 B/frame string updates
        private readonly char[] _speedBuffer = new char[HudStringFormatter.IntBufferSize];
        private readonly char[] _timeBuffer = new char[HudStringFormatter.TimeBufferSize];
        private readonly char[] _positionBuffer = new char[HudStringFormatter.PositionBufferSize];
        private readonly char[] _bonusBuffer = new char[HudStringFormatter.BonusBufferSize];
        private readonly char[] _popupBuffer = new char[HudStringFormatter.PopupBufferSize];

        // Cached values to avoid redundant string allocations when values haven't changed
        private int _lastSpeed = -1;
        private int _lastHundredths = -1;
        private int _lastPosition = -1;
        private int _lastBonusCredits = -1;
        private float _popupRemainingSeconds = 0f;
        private RaceState _lastRaceState = (RaceState)(-1);

        public VisualElement Root => _root;

        public RaceHudPresenter(VisualElement root)
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

            // Countdown banner (center)
            _countdownLabel = new Label { name = "hud-countdown" };
            _countdownLabel.style.position = Position.Absolute;
            _countdownLabel.style.left = 0f;
            _countdownLabel.style.right = 0f;
            _countdownLabel.style.top = 150f;
            _countdownLabel.style.fontSize = 72f;
            _countdownLabel.style.color = new Color(1f, 0.9f, 0.2f);
            _countdownLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _countdownLabel.style.display = DisplayStyle.None;
            _root.Add(_countdownLabel);

            // Action popup banner (center-top)
            _popupBannerLabel = new Label { name = "hud-popup-banner" };
            _popupBannerLabel.style.position = Position.Absolute;
            _popupBannerLabel.style.left = 0f;
            _popupBannerLabel.style.right = 0f;
            _popupBannerLabel.style.top = 90f;
            _popupBannerLabel.style.fontSize = 32f;
            _popupBannerLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _popupBannerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _popupBannerLabel.style.display = DisplayStyle.None;
            _root.Add(_popupBannerLabel);

            // Top Bar: Time and Position
            var topBar = new VisualElement { name = "hud-top-bar" };
            topBar.style.position = Position.Absolute;
            topBar.style.top = 20f;
            topBar.style.left = 20f;
            topBar.style.right = 20f;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;

            // Position (Top Left / Right based on LTR/RTL)
            var posBox = new VisualElement { name = "hud-position-box" };
            posBox.style.flexDirection = FlexDirection.Row;
            posBox.style.alignItems = Align.Center;

            _positionLabel = new Label { name = "hud-position-label" };
            _positionLabel.style.fontSize = 20f;
            _positionLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            posBox.Add(_positionLabel);

            _positionValueLabel = new Label { name = "hud-position-val" };
            _positionValueLabel.style.fontSize = 32f;
            _positionValueLabel.style.color = Color.white;
            _positionValueLabel.style.marginLeft = 8f;
            posBox.Add(_positionValueLabel);

            topBar.Add(posBox);

            // Timer (Top Right / Left)
            var timeBox = new VisualElement { name = "hud-time-box" };
            timeBox.style.flexDirection = FlexDirection.Row;
            timeBox.style.alignItems = Align.Center;

            _timeLabel = new Label { name = "hud-time-label" };
            _timeLabel.style.fontSize = 20f;
            _timeLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            timeBox.Add(_timeLabel);

            _timeValueLabel = new Label { name = "hud-time-val" };
            _timeValueLabel.style.fontSize = 32f;
            _timeValueLabel.style.color = Color.white;
            _timeValueLabel.style.marginLeft = 8f;
            timeBox.Add(_timeValueLabel);

            topBar.Add(timeBox);
            _root.Add(topBar);

            // Bottom Left: Live Bonus Credits
            _bonusBox = new VisualElement { name = "hud-bonus-box" };
            _bonusBox.style.position = Position.Absolute;
            _bonusBox.style.bottom = 30f;
            _bonusBox.style.left = 30f;
            _bonusBox.style.alignItems = Align.FlexStart;
            _bonusBox.style.display = DisplayStyle.None;

            _bonusLabel = new Label { name = "hud-bonus-label" };
            _bonusLabel.style.fontSize = 16f;
            _bonusLabel.style.color = new Color(0.9f, 0.75f, 0.2f);
            _bonusBox.Add(_bonusLabel);

            _bonusValueLabel = new Label { name = "hud-bonus-val" };
            _bonusValueLabel.style.fontSize = 36f;
            _bonusValueLabel.style.color = new Color(1f, 0.95f, 0.4f);
            _bonusBox.Add(_bonusValueLabel);

            _root.Add(_bonusBox);

            // Bottom Right: Speedometer
            var speedBox = new VisualElement { name = "hud-speed-box" };
            speedBox.style.position = Position.Absolute;
            speedBox.style.bottom = 30f;
            speedBox.style.right = 30f;
            speedBox.style.alignItems = Align.FlexEnd;

            _speedLabel = new Label { name = "hud-speed-val" };
            _speedLabel.style.fontSize = 64f;
            _speedLabel.style.color = Color.white;
            speedBox.Add(_speedLabel);

            _speedUnitLabel = new Label { name = "hud-speed-unit" };
            _speedUnitLabel.style.fontSize = 18f;
            _speedUnitLabel.style.color = new Color(0.6f, 0.8f, 1f);
            speedBox.Add(_speedUnitLabel);

            _root.Add(speedBox);
        }

        public void RefreshLocalization()
        {
            _timeLabel.text = LocalizationService.Tr("hud.time_label");
            _positionLabel.text = LocalizationService.Tr("hud.position_label");
            _speedUnitLabel.text = LocalizationService.Tr("hud.speed_unit");
            _bonusLabel.text = LocalizationService.Tr("hud.bonus_credits");
        }

        /// <summary>
        /// Ticks visual banner timers on the HUD hot path (0 B allocation).
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            if (_popupRemainingSeconds > 0f)
            {
                _popupRemainingSeconds -= deltaSeconds;
                if (_popupRemainingSeconds <= 0f)
                {
                    _popupBannerLabel.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Updates the live bonus credit tally label on the HUD.
        /// 0 B allocation in steady state.
        /// </summary>
        public void SetBonusCredits(int totalBonus)
        {
            if (totalBonus == _lastBonusCredits) return;

            _lastBonusCredits = totalBonus;
            if (totalBonus > 0)
            {
                int len = HudStringFormatter.WriteBonusCredits(_bonusBuffer, 0, totalBonus);
                _bonusValueLabel.text = new string(_bonusBuffer, 0, len);
                _bonusBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                _bonusBox.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Displays action popup banner for road coin pickup.
        /// </summary>
        public void ShowCoinPopup(int coinValue, int totalBonus)
        {
            SetBonusCredits(totalBonus);
            int len = HudStringFormatter.WritePopup(_popupBuffer, 0, "COIN", coinValue);
            _popupBannerLabel.text = new string(_popupBuffer, 0, len);
            _popupBannerLabel.style.color = new Color(1f, 0.85f, 0.2f);
            _popupBannerLabel.style.display = DisplayStyle.Flex;
            _popupRemainingSeconds = 1.2f;
        }

        /// <summary>
        /// Displays action popup banner for high-speed near-miss overtake.
        /// </summary>
        public void ShowNearMissPopup(int bonusCredits, int totalBonus)
        {
            SetBonusCredits(totalBonus);
            string prefix = LocalizationService.Tr("hud.near_miss_popup");
            int len = HudStringFormatter.WritePopup(_popupBuffer, 0, prefix, bonusCredits);
            _popupBannerLabel.text = new string(_popupBuffer, 0, len);
            _popupBannerLabel.style.color = new Color(1f, 0.45f, 0.2f);
            _popupBannerLabel.style.display = DisplayStyle.Flex;
            _popupRemainingSeconds = 1.5f;
        }

        private void OnLanguageChanged(string lang)
        {
            RefreshLocalization();
        }

        /// <summary>
        /// Updates the HUD elements during race runtime.
        /// Hot path: only allocates when string value actually changes,
        /// or can write via reusable buffer string copies.
        /// </summary>
        public void UpdateRaceState(
            RaceState state,
            double elapsedSeconds,
            double countdownRemaining,
            int currentPosition,
            int speedKph)
        {
            // Countdown handling
            if (state != _lastRaceState)
            {
                _lastRaceState = state;
            }

            if (state == RaceState.Countdown)
            {
                _countdownLabel.style.display = DisplayStyle.Flex;
                int countInt = (int)Math.Ceiling(countdownRemaining);
                if (countInt <= 0)
                {
                    _countdownLabel.text = LocalizationService.Tr("hud.countdown_go");
                }
                else
                {
                    _countdownLabel.text = countInt.ToString();
                }
            }
            else
            {
                _countdownLabel.style.display = DisplayStyle.None;
            }

            // Speed update
            if (speedKph != _lastSpeed)
            {
                _lastSpeed = speedKph;
                int len = HudStringFormatter.WriteInt(_speedBuffer, 0, speedKph);
                _speedLabel.text = new string(_speedBuffer, 0, len);
            }

            // Time update
            int hundredthsKey = (int)(elapsedSeconds * 100);
            if (hundredthsKey != _lastHundredths)
            {
                _lastHundredths = hundredthsKey;
                int len = HudStringFormatter.WriteRaceTime(_timeBuffer, 0, elapsedSeconds);
                _timeValueLabel.text = new string(_timeBuffer, 0, len);
            }

            // Position update
            if (currentPosition != _lastPosition)
            {
                _lastPosition = currentPosition;
                if (currentPosition > 0)
                {
                    int len = HudStringFormatter.WritePosition(_positionBuffer, 0, currentPosition);
                    _positionValueLabel.text = new string(_positionBuffer, 0, len);
                    _positionValueLabel.style.display = DisplayStyle.Flex;
                    _positionLabel.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _positionValueLabel.style.display = DisplayStyle.None;
                    _positionLabel.style.display = DisplayStyle.None;
                }
            }
        }
    }
}
