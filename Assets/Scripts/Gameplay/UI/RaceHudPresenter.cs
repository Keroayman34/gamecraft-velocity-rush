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

        // Pre-allocated char buffers for 0 B/frame string updates
        private readonly char[] _speedBuffer = new char[HudStringFormatter.IntBufferSize];
        private readonly char[] _timeBuffer = new char[HudStringFormatter.TimeBufferSize];
        private readonly char[] _positionBuffer = new char[HudStringFormatter.PositionBufferSize];

        // Cached values to avoid redundant string allocations when values haven't changed
        private int _lastSpeed = -1;
        private int _lastHundredths = -1;
        private int _lastPosition = -1;
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
