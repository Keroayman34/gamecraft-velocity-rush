using System;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Championship selection screen presenter (D-069). Displays 3 championships with
    /// lock/unlock state and total stars. Subscribes to localization for instant rebinding.
    /// </summary>
    public sealed class ChampionshipSelectPresenter
    {
        public struct ChampionshipDisplayData
        {
            public string ChampionshipId;
            public string NameKey;
            public string DifficultyKey;
            public bool IsUnlocked;
            public int TotalStars;
            public int MaxStars;
        }

        private readonly VisualElement _root;
        private Label _titleLabel;
        private Button _backButton;
        private readonly VisualElement[] _champCards = new VisualElement[3];
        private readonly Label[] _champNameLabels = new Label[3];
        private readonly Label[] _champDiffLabels = new Label[3];
        private readonly Label[] _champStarLabels = new Label[3];
        private readonly Label[] _champStatusLabels = new Label[3];
        private readonly Button[] _champSelectButtons = new Button[3];
        private ChampionshipDisplayData[] _data;

        public event Action<string> ChampionshipSelected;
        public event Action BackRequested;

        public ChampionshipSelectPresenter(VisualElement root)
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
            _root.style.paddingTop = 40f;

            _titleLabel = new Label { name = "champ-title" };
            _titleLabel.style.fontSize = 36f;
            _titleLabel.style.color = UnityEngine.Color.white;
            _titleLabel.style.marginBottom = 30f;
            _root.Add(_titleLabel);

            for (int i = 0; i < 3; i++)
            {
                var card = new VisualElement { name = "champ-card-" + i };
                card.style.width = 350f;
                card.style.paddingTop = 15f;
                card.style.paddingBottom = 15f;
                card.style.paddingLeft = 20f;
                card.style.paddingRight = 20f;
                card.style.marginBottom = 15f;
                card.style.backgroundColor = new UnityEngine.Color(0.12f, 0.14f, 0.18f, 0.9f);
                card.style.borderTopLeftRadius = 8f;
                card.style.borderTopRightRadius = 8f;
                card.style.borderBottomLeftRadius = 8f;
                card.style.borderBottomRightRadius = 8f;

                _champNameLabels[i] = new Label { name = "champ-name-" + i };
                _champNameLabels[i].style.fontSize = 22f;
                _champNameLabels[i].style.color = UnityEngine.Color.white;
                card.Add(_champNameLabels[i]);

                _champDiffLabels[i] = new Label { name = "champ-diff-" + i };
                _champDiffLabels[i].style.fontSize = 14f;
                _champDiffLabels[i].style.color = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
                card.Add(_champDiffLabels[i]);

                _champStarLabels[i] = new Label { name = "champ-stars-" + i };
                _champStarLabels[i].style.fontSize = 16f;
                _champStarLabels[i].style.color = new UnityEngine.Color(1f, 0.85f, 0.2f);
                card.Add(_champStarLabels[i]);

                _champStatusLabels[i] = new Label { name = "champ-status-" + i };
                _champStatusLabels[i].style.fontSize = 14f;
                card.Add(_champStatusLabels[i]);

                int capturedIndex = i;
                _champSelectButtons[i] = new Button { name = "champ-select-" + i };
                _champSelectButtons[i].style.width = 120f;
                _champSelectButtons[i].style.height = 35f;
                _champSelectButtons[i].style.marginTop = 8f;
                _champSelectButtons[i].clicked += () =>
                {
                    if (_data != null && capturedIndex < _data.Length && _data[capturedIndex].IsUnlocked)
                    {
                        ChampionshipSelected?.Invoke(_data[capturedIndex].ChampionshipId);
                    }
                };
                card.Add(_champSelectButtons[i]);

                _champCards[i] = card;
                _root.Add(card);
            }

            _backButton = new Button { name = "champ-back" };
            _backButton.style.width = 120f;
            _backButton.style.height = 40f;
            _backButton.style.marginTop = 20f;
            _backButton.clicked += () => BackRequested?.Invoke();
            _root.Add(_backButton);
        }

        public void SetData(ChampionshipDisplayData[] data)
        {
            _data = data;
            Refresh();
        }

        public void Refresh()
        {
            _titleLabel.text = LocalizationService.Tr("champ.title");
            _backButton.text = LocalizationService.Tr("champ.back");

            if (_data == null)
            {
                return;
            }

            for (int i = 0; i < 3 && i < _data.Length; i++)
            {
                var d = _data[i];
                _champNameLabels[i].text = LocalizationService.Tr(d.NameKey);
                _champDiffLabels[i].text = LocalizationService.Tr(d.DifficultyKey);
                _champStarLabels[i].text = "★ " + d.TotalStars + " / " + d.MaxStars;

                if (d.IsUnlocked)
                {
                    _champStatusLabels[i].text = LocalizationService.Tr("champ.unlocked");
                    _champStatusLabels[i].style.color = new UnityEngine.Color(0.3f, 0.9f, 0.3f);
                    _champSelectButtons[i].text = LocalizationService.Tr("menu.play");
                    _champSelectButtons[i].SetEnabled(true);
                }
                else
                {
                    _champStatusLabels[i].text = LocalizationService.Tr("champ.locked");
                    _champStatusLabels[i].style.color = new UnityEngine.Color(0.9f, 0.3f, 0.3f);
                    _champSelectButtons[i].text = LocalizationService.Tr("champ.locked");
                    _champSelectButtons[i].SetEnabled(false);
                }

                _champCards[i].style.display = DisplayStyle.Flex;
            }

            for (int i = _data.Length; i < 3; i++)
            {
                _champCards[i].style.display = DisplayStyle.None;
            }
        }

        private void OnLanguageChanged(string lang)
        {
            Refresh();
        }
    }
}
