using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Audio, language, and controls settings overlay presenter (D-069).
    /// Provides volume sliders (Master, SFX, Engine), instant language toggle ("en" / "ar"),
    /// read-only controls reference, and Apply/Back actions.
    /// </summary>
    public sealed class SettingsPresenter
    {
        private readonly VisualElement _root;
        private readonly PlayerProgress _progress;
        private Label _titleLabel;
        private Label _masterLabel;
        private Slider _masterSlider;
        private Label _sfxLabel;
        private Slider _sfxSlider;
        private Label _engineLabel;
        private Slider _engineSlider;
        private Label _langLabel;
        private Button _langToggleButton;
        private Label _controlsLabel;
        private Label _controlsRefLabel;
        private Button _backButton;

        public event Action BackRequested;
        public event Action<PlayerSettingsDto> SettingsChanged;

        public SettingsPresenter(VisualElement root, PlayerProgress progress)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            Build();
            LocalizationService.LanguageChanged += OnLanguageChanged;
            Refresh();
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

            _titleLabel = new Label { name = "settings-title" };
            _titleLabel.style.fontSize = 40f;
            _titleLabel.style.color = Color.white;
            _titleLabel.style.marginBottom = 25f;
            _root.Add(_titleLabel);

            var panel = new VisualElement { name = "settings-panel" };
            panel.style.width = 440f;
            panel.style.paddingLeft = 20f;
            panel.style.paddingRight = 20f;
            panel.style.paddingTop = 20f;
            panel.style.paddingBottom = 20f;
            panel.style.backgroundColor = new Color(0.1f, 0.12f, 0.16f, 0.9f);
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;

            // Master Volume
            _masterLabel = new Label { name = "settings-master-label" };
            _masterLabel.style.fontSize = 16f;
            _masterLabel.style.color = Color.white;
            panel.Add(_masterLabel);

            _masterSlider = new Slider(0f, 1f) { name = "settings-master-slider" };
            _masterSlider.value = _progress.Settings.masterVolume;
            _masterSlider.RegisterValueChangedCallback(evt => OnSliderChanged());
            _masterSlider.style.marginBottom = 12f;
            panel.Add(_masterSlider);

            // SFX Volume
            _sfxLabel = new Label { name = "settings-sfx-label" };
            _sfxLabel.style.fontSize = 16f;
            _sfxLabel.style.color = Color.white;
            panel.Add(_sfxLabel);

            _sfxSlider = new Slider(0f, 1f) { name = "settings-sfx-slider" };
            _sfxSlider.value = _progress.Settings.sfxVolume;
            _sfxSlider.RegisterValueChangedCallback(evt => OnSliderChanged());
            _sfxSlider.style.marginBottom = 12f;
            panel.Add(_sfxSlider);

            // Engine Volume
            _engineLabel = new Label { name = "settings-engine-label" };
            _engineLabel.style.fontSize = 16f;
            _engineLabel.style.color = Color.white;
            panel.Add(_engineLabel);

            _engineSlider = new Slider(0f, 1f) { name = "settings-engine-slider" };
            _engineSlider.value = _progress.Settings.engineVolume;
            _engineSlider.RegisterValueChangedCallback(evt => OnSliderChanged());
            _engineSlider.style.marginBottom = 16f;
            panel.Add(_engineSlider);

            // Language Row
            var langRow = new VisualElement { name = "settings-lang-row" };
            langRow.style.flexDirection = FlexDirection.Row;
            langRow.style.justifyContent = Justify.SpaceBetween;
            langRow.style.alignItems = Align.Center;
            langRow.style.marginBottom = 16f;

            _langLabel = new Label { name = "settings-lang-label" };
            _langLabel.style.fontSize = 16f;
            _langLabel.style.color = Color.white;
            langRow.Add(_langLabel);

            _langToggleButton = new Button { name = "settings-lang-toggle" };
            _langToggleButton.style.width = 100f;
            _langToggleButton.style.height = 36f;
            _langToggleButton.clicked += OnLanguageToggle;
            langRow.Add(_langToggleButton);
            panel.Add(langRow);

            // Controls Reference
            _controlsLabel = new Label { name = "settings-controls-label" };
            _controlsLabel.style.fontSize = 16f;
            _controlsLabel.style.color = new Color(0.8f, 0.9f, 1f);
            _controlsLabel.style.marginBottom = 4f;
            panel.Add(_controlsLabel);

            _controlsRefLabel = new Label { name = "settings-controls-ref" };
            _controlsRefLabel.style.fontSize = 13f;
            _controlsRefLabel.style.color = new Color(0.6f, 0.65f, 0.7f);
            _controlsRefLabel.style.marginBottom = 15f;
            panel.Add(_controlsRefLabel);

            _root.Add(panel);

            _backButton = new Button { name = "settings-back" };
            _backButton.style.width = 140f;
            _backButton.style.height = 42f;
            _backButton.style.marginTop = 20f;
            _backButton.clicked += () => BackRequested?.Invoke();
            _root.Add(_backButton);
        }

        public void SetMasterVolume(float volume)
        {
            _masterSlider.value = Mathf.Clamp01(volume);
            OnSliderChanged();
        }

        public void SetSfxVolume(float volume)
        {
            _sfxSlider.value = Mathf.Clamp01(volume);
            OnSliderChanged();
        }

        public void SetEngineVolume(float volume)
        {
            _engineSlider.value = Mathf.Clamp01(volume);
            OnSliderChanged();
        }

        public void Refresh()
        {
            _titleLabel.text = LocalizationService.Tr("settings.title");
            _masterLabel.text = LocalizationService.Tr("settings.master_volume");
            _sfxLabel.text = LocalizationService.Tr("settings.sfx_volume");
            _engineLabel.text = LocalizationService.Tr("settings.engine_volume");
            _langLabel.text = LocalizationService.Tr("settings.language");
            _langToggleButton.text = LocalizationService.IsRTL ? "English" : "عربي";
            _controlsLabel.text = LocalizationService.Tr("settings.controls");
            _controlsRefLabel.text = LocalizationService.Tr("settings.controls_ref");
            _backButton.text = LocalizationService.Tr("settings.back");
        }

        private void OnSliderChanged()
        {
            var updated = new PlayerSettingsDto
            {
                language = LocalizationService.CurrentLanguage,
                masterVolume = _masterSlider.value,
                sfxVolume = _sfxSlider.value,
                engineVolume = _engineSlider.value
            };
            SettingsChanged?.Invoke(updated);
        }

        private void OnLanguageToggle()
        {
            string nextLang = LocalizationService.IsRTL ? LocalizationService.LanguageEnglish : LocalizationService.LanguageArabic;
            LocalizationService.SetLanguage(nextLang);
            OnSliderChanged();
        }

        private void OnLanguageChanged(string lang)
        {
            Refresh();
        }
    }
}
