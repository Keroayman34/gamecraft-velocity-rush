using System;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Main Menu screen presenter (D-069). Builds Play, Garage, Settings, Language Toggle,
    /// and live credit badge into the provided screen root. Subscribes to localization
    /// changes for instant EN/AR switching.
    /// </summary>
    public sealed class MainMenuPresenter
    {
        private readonly VisualElement _root;
        private readonly PlayerProgress _progress;
        private Label _titleLabel;
        private Button _playButton;
        private Button _garageButton;
        private Button _settingsButton;
        private Button _langToggleButton;
        private Label _creditsLabel;

        public event Action PlayRequested;
        public event Action GarageRequested;
        public event Action SettingsRequested;
        public event Action LanguageToggleRequested;

        public void RequestPlay() => PlayRequested?.Invoke();
        public void RequestGarage() => GarageRequested?.Invoke();
        public void RequestSettings() => SettingsRequested?.Invoke();
        public void RequestLanguageToggle() => LanguageToggleRequested?.Invoke();

        public MainMenuPresenter(VisualElement root, PlayerProgress progress)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Build();
            Refresh();
            LocalizationService.LanguageChanged += OnLanguageChanged;
            _progress.OnChanged += OnProgressChanged;
        }

        public void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
            if (_progress != null)
            {
                _progress.OnChanged -= OnProgressChanged;
            }
        }

        private void Build()
        {
            _root.Clear();
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;

            _titleLabel = new Label { name = "main-menu-title" };
            _titleLabel.style.fontSize = 48f;
            _titleLabel.style.color = UnityEngine.Color.white;
            _titleLabel.style.marginBottom = 40f;
            _root.Add(_titleLabel);

            _creditsLabel = new Label { name = "main-menu-credits" };
            _creditsLabel.style.fontSize = 20f;
            _creditsLabel.style.color = new UnityEngine.Color(1f, 0.85f, 0.2f);
            _creditsLabel.style.marginBottom = 30f;
            _root.Add(_creditsLabel);

            _playButton = new Button { name = "main-menu-play" };
            _playButton.style.width = 200f;
            _playButton.style.height = 50f;
            _playButton.style.marginBottom = 10f;
            _playButton.clicked += () => PlayRequested?.Invoke();
            _root.Add(_playButton);

            _garageButton = new Button { name = "main-menu-garage" };
            _garageButton.style.width = 200f;
            _garageButton.style.height = 50f;
            _garageButton.style.marginBottom = 10f;
            _garageButton.clicked += () => GarageRequested?.Invoke();
            _root.Add(_garageButton);

            _settingsButton = new Button { name = "main-menu-settings" };
            _settingsButton.style.width = 200f;
            _settingsButton.style.height = 50f;
            _settingsButton.style.marginBottom = 10f;
            _settingsButton.clicked += () => SettingsRequested?.Invoke();
            _root.Add(_settingsButton);

            _langToggleButton = new Button { name = "main-menu-lang-toggle" };
            _langToggleButton.style.width = 100f;
            _langToggleButton.style.height = 40f;
            _langToggleButton.style.marginTop = 20f;
            _langToggleButton.clicked += () => LanguageToggleRequested?.Invoke();
            _root.Add(_langToggleButton);
        }

        public void Refresh()
        {
            _titleLabel.text = LocalizationService.Tr("menu.title");
            _playButton.text = LocalizationService.Tr("menu.play");
            _garageButton.text = LocalizationService.Tr("menu.garage");
            _settingsButton.text = LocalizationService.Tr("menu.settings");
            _langToggleButton.text = LocalizationService.Tr("menu.language_toggle");
            _creditsLabel.text = _progress.Credits + " " + LocalizationService.Tr("common.cr");

            ApplyLayoutDirection();
        }

        private void OnLanguageChanged(string lang)
        {
            Refresh();
        }

        private void OnProgressChanged()
        {
            _creditsLabel.text = _progress.Credits + " " + LocalizationService.Tr("common.cr");
        }

        private void ApplyLayoutDirection()
        {
            _root.style.flexDirection = LocalizationService.IsRTL
                ? FlexDirection.ColumnReverse
                : FlexDirection.Column;
        }
    }
}
