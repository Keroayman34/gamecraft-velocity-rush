using System;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Event selection screen presenter (D-069). Displays rounds 1-5 for the selected championship
    /// with track name, event type, earned stars, best time, and launch button.
    /// </summary>
    public sealed class EventSelectPresenter
    {
        public struct EventDisplayData
        {
            public string EventId;
            public string DisplayName;
            public string EventTypeKey;
            public bool IsUnlocked;
            public int StarsEarned;
            public double BestTimeSeconds;
            public int RoundIndex;
        }

        private readonly VisualElement _root;
        private Label _titleLabel;
        private Button _backButton;
        private readonly VisualElement[] _eventCards = new VisualElement[5];
        private readonly Label[] _eventNameLabels = new Label[5];
        private readonly Label[] _eventTypeLabels = new Label[5];
        private readonly Label[] _eventStarLabels = new Label[5];
        private readonly Label[] _eventTimeLabels = new Label[5];
        private readonly Button[] _eventLaunchButtons = new Button[5];
        private EventDisplayData[] _data;

        public event Action<string> EventLaunched;
        public event Action BackRequested;

        public EventSelectPresenter(VisualElement root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            Build();
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }

        public void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void Build()
        {
            _root.Clear();
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.FlexStart;
            _root.style.paddingTop = 30f;

            _titleLabel = new Label { name = "event-title" };
            _titleLabel.style.fontSize = 32f;
            _titleLabel.style.color = UnityEngine.Color.white;
            _titleLabel.style.marginBottom = 20f;
            _root.Add(_titleLabel);

            for (int i = 0; i < 5; i++)
            {
                var card = new VisualElement { name = "event-card-" + i };
                card.style.width = 400f;
                card.style.paddingTop = 10f;
                card.style.paddingBottom = 10f;
                card.style.paddingLeft = 15f;
                card.style.paddingRight = 15f;
                card.style.marginBottom = 10f;
                card.style.backgroundColor = new UnityEngine.Color(0.1f, 0.12f, 0.16f, 0.9f);
                card.style.borderTopLeftRadius = 6f;
                card.style.borderTopRightRadius = 6f;
                card.style.borderBottomLeftRadius = 6f;
                card.style.borderBottomRightRadius = 6f;

                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;
                headerRow.style.alignItems = Align.Center;

                _eventNameLabels[i] = new Label { name = "event-name-" + i };
                _eventNameLabels[i].style.fontSize = 18f;
                _eventNameLabels[i].style.color = UnityEngine.Color.white;
                headerRow.Add(_eventNameLabels[i]);

                _eventTypeLabels[i] = new Label { name = "event-type-" + i };
                _eventTypeLabels[i].style.fontSize = 13f;
                _eventTypeLabels[i].style.color = new UnityEngine.Color(0.6f, 0.8f, 1f);
                headerRow.Add(_eventTypeLabels[i]);
                card.Add(headerRow);

                var detailRow = new VisualElement();
                detailRow.style.flexDirection = FlexDirection.Row;
                detailRow.style.justifyContent = Justify.SpaceBetween;
                detailRow.style.alignItems = Align.Center;
                detailRow.style.marginTop = 5f;

                _eventStarLabels[i] = new Label { name = "event-stars-" + i };
                _eventStarLabels[i].style.fontSize = 16f;
                _eventStarLabels[i].style.color = new UnityEngine.Color(1f, 0.85f, 0.2f);
                detailRow.Add(_eventStarLabels[i]);

                _eventTimeLabels[i] = new Label { name = "event-time-" + i };
                _eventTimeLabels[i].style.fontSize = 14f;
                _eventTimeLabels[i].style.color = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
                detailRow.Add(_eventTimeLabels[i]);
                card.Add(detailRow);

                int capturedIndex = i;
                _eventLaunchButtons[i] = new Button { name = "event-launch-" + i };
                _eventLaunchButtons[i].style.width = 100f;
                _eventLaunchButtons[i].style.height = 32f;
                _eventLaunchButtons[i].style.marginTop = 8f;
                _eventLaunchButtons[i].clicked += () =>
                {
                    if (_data != null && capturedIndex < _data.Length && _data[capturedIndex].IsUnlocked)
                    {
                        EventLaunched?.Invoke(_data[capturedIndex].EventId);
                    }
                };
                card.Add(_eventLaunchButtons[i]);

                _eventCards[i] = card;
                _root.Add(card);
            }

            _backButton = new Button { name = "event-back" };
            _backButton.style.width = 120f;
            _backButton.style.height = 40f;
            _backButton.style.marginTop = 15f;
            _backButton.clicked += () => BackRequested?.Invoke();
            _root.Add(_backButton);
        }

        public void SetData(EventDisplayData[] data)
        {
            _data = data;
            Refresh();
        }

        public void Refresh()
        {
            _titleLabel.text = LocalizationService.Tr("event.title");
            _backButton.text = LocalizationService.Tr("event.back");

            if (_data == null)
            {
                return;
            }

            for (int i = 0; i < 5 && i < _data.Length; i++)
            {
                var d = _data[i];
                _eventNameLabels[i].text = LocalizationService.Tr("event.round") + " " + d.RoundIndex
                    + " - " + d.DisplayName;
                _eventTypeLabels[i].text = LocalizationService.Tr(d.EventTypeKey);
                _eventStarLabels[i].text = "★ " + d.StarsEarned + " / 3";

                if (d.BestTimeSeconds > 0d)
                {
                    int whole = (int)d.BestTimeSeconds;
                    int hundredths = (int)((d.BestTimeSeconds - whole) * 100);
                    _eventTimeLabels[i].text = LocalizationService.Tr("event.best_time") + ": " + whole + "." + hundredths.ToString("D2");
                }
                else
                {
                    _eventTimeLabels[i].text = LocalizationService.Tr("event.no_time");
                }

                if (d.IsUnlocked)
                {
                    _eventLaunchButtons[i].text = LocalizationService.Tr("event.launch");
                    _eventLaunchButtons[i].SetEnabled(true);
                }
                else
                {
                    _eventLaunchButtons[i].text = LocalizationService.Tr("event.locked");
                    _eventLaunchButtons[i].SetEnabled(false);
                }

                _eventCards[i].style.display = DisplayStyle.Flex;
            }

            for (int i = (_data != null ? _data.Length : 0); i < 5; i++)
            {
                _eventCards[i].style.display = DisplayStyle.None;
            }
        }

        private void OnLanguageChanged(string lang)
        {
            Refresh();
        }
    }
}
