using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// In-game Pause overlay presenter (D-069).
    /// Provides Resume, Retry, Settings, and Quit to Menu actions.
    /// Bilingual with instant RTL/LTR localization.
    /// </summary>
    public sealed class PauseOverlayPresenter
    {
        private readonly VisualElement _root;
        private Label _titleLabel;
        private Button _resumeButton;
        private Button _retryButton;
        private Button _settingsButton;
        private Button _quitButton;

        public event Action ResumeRequested;
        public event Action RetryRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;

        public PauseOverlayPresenter(VisualElement root)
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
            _root.style.backgroundColor = new Color(0.02f, 0.04f, 0.08f, 0.85f);
            _root.style.alignItems = Align.Center;
            _root.style.justifyContent = Justify.Center;

            _titleLabel = new Label { name = "pause-title" };
            _titleLabel.style.fontSize = 44f;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 30f;
            _root.Add(_titleLabel);

            _resumeButton = new Button { name = "pause-resume" };
            _resumeButton.style.width = 220f;
            _resumeButton.style.height = 48f;
            _resumeButton.style.marginBottom = 12f;
            _resumeButton.clicked += () => ResumeRequested?.Invoke();
            _root.Add(_resumeButton);

            _retryButton = new Button { name = "pause-retry" };
            _retryButton.style.width = 220f;
            _retryButton.style.height = 48f;
            _retryButton.style.marginBottom = 12f;
            _retryButton.clicked += () => RetryRequested?.Invoke();
            _root.Add(_retryButton);

            _settingsButton = new Button { name = "pause-settings" };
            _settingsButton.style.width = 220f;
            _settingsButton.style.height = 48f;
            _settingsButton.style.marginBottom = 12f;
            _settingsButton.clicked += () => SettingsRequested?.Invoke();
            _root.Add(_settingsButton);

            _quitButton = new Button { name = "pause-quit" };
            _quitButton.style.width = 220f;
            _quitButton.style.height = 48f;
            _quitButton.clicked += () => QuitRequested?.Invoke();
            _root.Add(_quitButton);
        }

        public void RefreshLocalization()
        {
            _titleLabel.text = LocalizationService.Tr("pause.title");
            _resumeButton.text = LocalizationService.Tr("pause.resume");
            _retryButton.text = LocalizationService.Tr("pause.retry");
            _settingsButton.text = LocalizationService.Tr("pause.settings");
            _quitButton.text = LocalizationService.Tr("pause.quit");
        }

        private void OnLanguageChanged(string lang)
        {
            RefreshLocalization();
        }
    }
}
