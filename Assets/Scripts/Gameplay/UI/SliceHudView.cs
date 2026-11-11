using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// UI Toolkit implementation of the vertical-slice HUD, countdown, pause overlay, and results
    /// screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The visual tree is built once in the constructor. Per-refresh values are written through
    /// <see cref="DigitDisplay"/>, which reuses ten cached glyph strings, so the steady-state HUD
    /// path allocates no managed memory. The only string construction is the one-shot results text
    /// when a race finishes, which is a cold event rather than a per-frame path.
    /// </para>
    /// <para>
    /// No UXML or USS asset is required: this keeps the slice free of new package or importer
    /// dependencies while still using the engine-native UI Toolkit runtime. Every overlay uses
    /// <see cref="PickingMode.Ignore"/> so the HUD can never intercept driving input.
    /// </para>
    /// </remarks>
    public sealed class SliceHudView : ISliceHudView
    {
        private static readonly Color PanelColor = new Color(0.04f, 0.05f, 0.07f, 0.78f);
        private static readonly Color CaptionColor = new Color(0.78f, 0.80f, 0.84f, 1f);
        private static readonly Color AccentColor = new Color(0.98f, 0.80f, 0.25f, 1f);
        private static readonly Color ValueColor = new Color(0.95f, 0.96f, 0.98f, 1f);

        private readonly DigitDisplay _speed;
        private readonly DigitDisplay _timeWhole;
        private readonly DigitDisplay _timeFraction;
        private readonly DigitDisplay _checkpointAccepted;
        private readonly DigitDisplay _checkpointRequired;
        private readonly VisualElement _countdownGroup;
        private readonly DigitDisplay _countdown;
        private readonly VisualElement _pauseOverlay;
        private readonly VisualElement _results;
        private readonly Label _resultsTitle;
        private readonly Label _resultsTime;
        private readonly Label _resultsCheckpoints;
        private bool _countdownVisible = true;
        private bool _pauseVisible;
        private bool _resultsVisible;

        /// <summary>Builds the HUD inside <paramref name="root"/>, replacing any existing children.</summary>
        /// <param name="root">Root visual element supplied by the scene's <c>UIDocument</c>.</param>
        public SliceHudView(VisualElement root)
        {
            if (root == null)
            {
                throw new System.ArgumentNullException(nameof(root));
            }

            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.top = 0f;
            root.style.right = 0f;
            root.style.bottom = 0f;

            var topBar = new VisualElement();
            topBar.style.position = Position.Absolute;
            topBar.style.left = 24f;
            topBar.style.right = 24f;
            topBar.style.top = 20f;
            topBar.style.flexDirection = FlexDirection.Row;
            topBar.style.justifyContent = Justify.SpaceBetween;
            topBar.style.alignItems = Align.FlexStart;
            root.Add(topBar);

            var speedPanel = CreatePanel();
            _speed = new DigitDisplay(speedPanel, 3, true, 30f);
            speedPanel.Add(CreateCaption("KM/H"));
            topBar.Add(speedPanel);

            var checkpointPanel = CreatePanel();
            checkpointPanel.Add(CreateCaption("CP"));
            _checkpointAccepted = new DigitDisplay(checkpointPanel, 2, true, 22f);
            checkpointPanel.Add(CreateCaption("/"));
            _checkpointRequired = new DigitDisplay(checkpointPanel, 2, true, 22f);
            topBar.Add(checkpointPanel);

            var timePanel = CreatePanel();
            _timeWhole = new DigitDisplay(timePanel, 3, true, 30f);
            timePanel.Add(CreateCaption("."));
            _timeFraction = new DigitDisplay(timePanel, 2, false, 30f);
            timePanel.Add(CreateCaption("S"));
            topBar.Add(timePanel);

            _countdownGroup = new VisualElement();
            _countdownGroup.name = "CountdownGroup";
            _countdownGroup.style.position = Position.Absolute;
            _countdownGroup.style.left = 0f;
            _countdownGroup.style.right = 0f;
            _countdownGroup.style.top = Length.Percent(28f);
            _countdownGroup.style.alignItems = Align.Center;
            _countdownGroup.pickingMode = PickingMode.Ignore;

            var readyLabel = new Label("GET READY");
            readyLabel.style.fontSize = 30f;
            readyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            readyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            readyLabel.style.color = AccentColor;
            _countdownGroup.Add(readyLabel);

            _countdown = new DigitDisplay(_countdownGroup, 2, true, 64f);
            _countdown.Root.style.marginTop = 4f;
            _countdown.Root.style.unityTextAlign = TextAnchor.MiddleCenter;
            root.Add(_countdownGroup);

            _pauseOverlay = CreateOverlay(root, "PAUSED", "PRESS P TO RESUME", 30f);
            _pauseOverlay.name = "PauseOverlay";
            _results = CreateResults(root, out _resultsTitle, out _resultsTime, out _resultsCheckpoints);
        }

        /// <summary>True when the countdown group is currently shown.</summary>
        public bool IsCountdownVisible => _countdownVisible;

        /// <summary>True when the pause overlay is currently shown.</summary>
        public bool IsPauseVisible => _pauseVisible;

        /// <summary>True when the results overlay is currently shown.</summary>
        public bool IsResultsVisible => _resultsVisible;

        /// <summary>The value currently drawn in the countdown cell.</summary>
        public int DisplayedCountdownSeconds { get; private set; }

        /// <inheritdoc />
        public void Apply(in SliceHudSnapshot snapshot)
        {
            _speed.SetValue(snapshot.SpeedKph);
            _timeWhole.SetValue(snapshot.TimeWholeSeconds);
            _timeFraction.SetValue(snapshot.TimeHundredths);
            _checkpointAccepted.SetValue(snapshot.AcceptedCheckpoints);
            _checkpointRequired.SetValue(snapshot.RequiredCheckpoints);

            var showCountdown = snapshot.State == RaceState.Countdown && !snapshot.IsPaused;
            if (showCountdown != _countdownVisible)
            {
                _countdownVisible = showCountdown;
                _countdownGroup.style.display = showCountdown ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (showCountdown)
            {
                _countdown.SetValue(snapshot.CountdownSeconds);
                DisplayedCountdownSeconds = snapshot.CountdownSeconds;
            }

            var showPause = snapshot.IsPaused && snapshot.State != RaceState.Finished;
            if (showPause != _pauseVisible)
            {
                _pauseVisible = showPause;
                _pauseOverlay.style.display = showPause ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (snapshot.State != RaceState.Finished || snapshot.IsPaused)
            {
                HideResults();
            }
        }

        /// <inheritdoc />
        public void ShowResults(in RaceResult result)
        {
            _resultsTitle.text = result.Completed ? "FINISHED" : "RUN ENDED";
            _resultsTime.text = FormatTime(result.ElapsedSeconds);
            _resultsCheckpoints.text =
                "CHECKPOINTS " +
                result.AcceptedCheckpointCount.ToString(CultureInfo.InvariantCulture) +
                " / " +
                result.RequiredCheckpointCount.ToString(CultureInfo.InvariantCulture);

            HideCountdown();
            HidePause();

            if (!_resultsVisible)
            {
                _resultsVisible = true;
                _results.style.display = DisplayStyle.Flex;
            }
        }

        /// <inheritdoc />
        public void HideResults()
        {
            if (!_resultsVisible)
            {
                return;
            }

            _resultsVisible = false;
            _results.style.display = DisplayStyle.None;
        }

        private void HideCountdown()
        {
            if (!_countdownVisible)
            {
                return;
            }

            _countdownVisible = false;
            _countdownGroup.style.display = DisplayStyle.None;
        }

        private void HidePause()
        {
            if (!_pauseVisible)
            {
                return;
            }

            _pauseVisible = false;
            _pauseOverlay.style.display = DisplayStyle.None;
        }

        private static VisualElement CreateOverlay(VisualElement root, string title, string hint, float titleSize)
        {
            var overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.left = Length.Percent(30f);
            overlay.style.right = Length.Percent(30f);
            overlay.style.top = Length.Percent(34f);
            overlay.style.paddingLeft = 28f;
            overlay.style.paddingRight = 28f;
            overlay.style.paddingTop = 22f;
            overlay.style.paddingBottom = 22f;
            overlay.style.backgroundColor = PanelColor;
            overlay.style.borderTopLeftRadius = 10f;
            overlay.style.borderTopRightRadius = 10f;
            overlay.style.borderBottomLeftRadius = 10f;
            overlay.style.borderBottomRightRadius = 10f;
            overlay.style.alignItems = Align.Center;
            overlay.pickingMode = PickingMode.Ignore;

            var titleLabel = new Label(title);
            titleLabel.style.fontSize = titleSize;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.color = AccentColor;
            titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            overlay.Add(titleLabel);

            var hintLabel = new Label(hint);
            hintLabel.style.fontSize = 16f;
            hintLabel.style.marginTop = 10f;
            hintLabel.style.color = CaptionColor;
            hintLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            overlay.Add(hintLabel);

            overlay.style.display = DisplayStyle.None;
            root.Add(overlay);
            return overlay;
        }

        private static VisualElement CreateResults(
            VisualElement root,
            out Label title,
            out Label time,
            out Label checkpoints)
        {
            var results = new VisualElement();
            results.name = "ResultsPanel";
            results.style.position = Position.Absolute;
            results.style.left = Length.Percent(25f);
            results.style.right = Length.Percent(25f);
            results.style.top = Length.Percent(22f);
            results.style.paddingLeft = 28f;
            results.style.paddingRight = 28f;
            results.style.paddingTop = 24f;
            results.style.paddingBottom = 24f;
            results.style.backgroundColor = PanelColor;
            results.style.borderTopLeftRadius = 10f;
            results.style.borderTopRightRadius = 10f;
            results.style.borderBottomLeftRadius = 10f;
            results.style.borderBottomRightRadius = 10f;
            results.style.alignItems = Align.Center;
            results.pickingMode = PickingMode.Ignore;

            title = new Label("FINISHED");
            title.name = "ResultsTitle";
            title.style.fontSize = 34f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = AccentColor;
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            results.Add(title);

            time = new Label("000.00 S");
            time.name = "ResultsTime";
            time.style.fontSize = 30f;
            time.style.marginTop = 12f;
            time.style.color = ValueColor;
            time.style.unityTextAlign = TextAnchor.MiddleCenter;
            results.Add(time);

            checkpoints = new Label("CHECKPOINTS 0 / 0");
            checkpoints.name = "ResultsCheckpoints";
            checkpoints.style.fontSize = 18f;
            checkpoints.style.marginTop = 8f;
            checkpoints.style.color = CaptionColor;
            checkpoints.style.unityTextAlign = TextAnchor.MiddleCenter;
            results.Add(checkpoints);

            var retryHint = new Label("PRESS R TO RETRY");
            retryHint.style.fontSize = 15f;
            retryHint.style.marginTop = 18f;
            retryHint.style.color = CaptionColor;
            retryHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            results.Add(retryHint);

            results.style.display = DisplayStyle.None;
            root.Add(results);
            return results;
        }

        private static VisualElement CreatePanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Row;
            panel.style.alignItems = Align.Center;
            panel.style.paddingLeft = 14f;
            panel.style.paddingRight = 14f;
            panel.style.paddingTop = 8f;
            panel.style.paddingBottom = 8f;
            panel.style.backgroundColor = PanelColor;
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;
            return panel;
        }

        private static Label CreateCaption(string text)
        {
            var label = new Label(text);
            label.style.fontSize = 14f;
            label.style.marginLeft = 6f;
            label.style.color = CaptionColor;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        private static string FormatTime(double seconds)
        {
            SliceHudFormat.SplitRaceTime(seconds, out var wholeSeconds, out var hundredths);
            return wholeSeconds.ToString("000", CultureInfo.InvariantCulture) +
                   "." +
                   hundredths.ToString("00", CultureInfo.InvariantCulture) +
                   " S";
        }
    }
}
