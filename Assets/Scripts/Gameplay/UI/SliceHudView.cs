using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// UI Toolkit implementation of the vertical-slice HUD and results overlay.
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
    /// dependencies while still using the engine-native UI Toolkit runtime.
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
        private readonly Label _countdown;
        private readonly VisualElement _results;
        private readonly Label _resultsTitle;
        private readonly Label _resultsTime;
        private readonly Label _resultsCheckpoints;
        private bool _countdownVisible = true;
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

            _countdown = new Label("GET READY");
            _countdown.style.position = Position.Absolute;
            _countdown.style.left = 0f;
            _countdown.style.right = 0f;
            _countdown.style.top = Length.Percent(30f);
            _countdown.style.fontSize = 46f;
            _countdown.style.unityFontStyleAndWeight = FontStyle.Bold;
            _countdown.style.unityTextAlign = TextAnchor.MiddleCenter;
            _countdown.style.color = AccentColor;
            _countdown.pickingMode = PickingMode.Ignore;
            root.Add(_countdown);

            _results = new VisualElement();
            _results.style.position = Position.Absolute;
            _results.style.left = Length.Percent(25f);
            _results.style.right = Length.Percent(25f);
            _results.style.top = Length.Percent(22f);
            _results.style.paddingLeft = 28f;
            _results.style.paddingRight = 28f;
            _results.style.paddingTop = 24f;
            _results.style.paddingBottom = 24f;
            _results.style.backgroundColor = PanelColor;
            _results.style.borderTopLeftRadius = 10f;
            _results.style.borderTopRightRadius = 10f;
            _results.style.borderBottomLeftRadius = 10f;
            _results.style.borderBottomRightRadius = 10f;
            _results.style.alignItems = Align.Center;
            _results.pickingMode = PickingMode.Ignore;

            _resultsTitle = new Label("FINISHED");
            _resultsTitle.style.fontSize = 34f;
            _resultsTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _resultsTitle.style.color = AccentColor;
            _resultsTitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            _results.Add(_resultsTitle);

            _resultsTime = new Label("000.00 S");
            _resultsTime.style.fontSize = 30f;
            _resultsTime.style.marginTop = 12f;
            _resultsTime.style.color = ValueColor;
            _resultsTime.style.unityTextAlign = TextAnchor.MiddleCenter;
            _results.Add(_resultsTime);

            _resultsCheckpoints = new Label("CHECKPOINTS 0 / 0");
            _resultsCheckpoints.style.fontSize = 18f;
            _resultsCheckpoints.style.marginTop = 8f;
            _resultsCheckpoints.style.color = CaptionColor;
            _resultsCheckpoints.style.unityTextAlign = TextAnchor.MiddleCenter;
            _results.Add(_resultsCheckpoints);

            var retryHint = new Label("PRESS R TO RETRY");
            retryHint.style.fontSize = 15f;
            retryHint.style.marginTop = 18f;
            retryHint.style.color = CaptionColor;
            retryHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            _results.Add(retryHint);

            _results.style.display = DisplayStyle.None;
            root.Add(_results);
        }

        /// <inheritdoc />
        public void Apply(in SliceHudSnapshot snapshot)
        {
            _speed.SetValue(snapshot.SpeedKph);
            _timeWhole.SetValue(snapshot.TimeWholeSeconds);
            _timeFraction.SetValue(snapshot.TimeHundredths);
            _checkpointAccepted.SetValue(snapshot.AcceptedCheckpoints);
            _checkpointRequired.SetValue(snapshot.RequiredCheckpoints);

            var showCountdown = snapshot.State == RaceState.Countdown;
            if (showCountdown != _countdownVisible)
            {
                _countdownVisible = showCountdown;
                _countdown.style.display = showCountdown ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (snapshot.State != RaceState.Finished)
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

            if (_countdownVisible)
            {
                _countdownVisible = false;
                _countdown.style.display = DisplayStyle.None;
            }

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
