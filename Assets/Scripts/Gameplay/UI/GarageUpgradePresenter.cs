using System;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Garage and Upgrades screen presenter (D-069). Displays vehicle roster with
    /// lock/buy/select, 6 upgrade categories with tier pips and cost, and purchase actions.
    /// </summary>
    public sealed class GarageUpgradePresenter
    {
        public struct VehicleDisplayData
        {
            public string VehicleId;
            public string NameKey;
            public string ClassKey;
            public VehicleClass VehicleClass;
            public bool IsUnlocked;
            public bool IsSelected;
            public int UnlockCost;
        }

        public struct UpgradeDisplayData
        {
            public UpgradeCategory Category;
            public string NameKey;
            public int CurrentTier;
            public int MaxTier;
            public int NextTierCost;
            public bool CanAfford;
            public bool IsMaxed;
        }

        private readonly VisualElement _garageRoot;
        private readonly VisualElement _upgradesRoot;
        private readonly PlayerProgress _progress;
        private Label _garageTitleLabel;
        private Label _garageCreditsLabel;
        private Button _garageBackButton;
        private Button _upgradesBackButton;
        private Label _upgradesTitleLabel;
        private Label _upgradesCreditsLabel;
        private Label _upgradesVehicleLabel;

        // Garage vehicle list (up to 10)
        private readonly VisualElement[] _vehicleCards = new VisualElement[10];
        private readonly Label[] _vehicleNameLabels = new Label[10];
        private readonly Label[] _vehicleClassLabels = new Label[10];
        private readonly Button[] _vehicleActionButtons = new Button[10];
        private VehicleDisplayData[] _vehicleData;

        // Upgrade slots (6 categories)
        private readonly VisualElement[] _upgradeRows = new VisualElement[6];
        private readonly Label[] _upgradeNameLabels = new Label[6];
        private readonly Label[] _upgradeTierLabels = new Label[6];
        private readonly Label[] _upgradeCostLabels = new Label[6];
        private readonly Button[] _upgradeBuyButtons = new Button[6];
        private UpgradeDisplayData[] _upgradeData;

        public event Action<string> VehicleSelectRequested;
        public event Action<string> VehicleBuyRequested;
        public event Action<string> VehicleUpgradesRequested;
        public event Action<UpgradeCategory> UpgradeBuyRequested;
        public event Action GarageBackRequested;
        public event Action UpgradesBackRequested;

        public GarageUpgradePresenter(VisualElement garageRoot, VisualElement upgradesRoot, PlayerProgress progress)
        {
            _garageRoot = garageRoot ?? throw new ArgumentNullException(nameof(garageRoot));
            _upgradesRoot = upgradesRoot ?? throw new ArgumentNullException(nameof(upgradesRoot));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            BuildGarage();
            BuildUpgrades();
            LocalizationService.LanguageChanged += OnLanguageChanged;
        }

        public void Dispose()
        {
            LocalizationService.LanguageChanged -= OnLanguageChanged;
        }

        private void BuildGarage()
        {
            _garageRoot.Clear();
            _garageRoot.style.alignItems = Align.Center;
            _garageRoot.style.justifyContent = Justify.FlexStart;
            _garageRoot.style.paddingTop = 30f;

            _garageTitleLabel = new Label { name = "garage-title" };
            _garageTitleLabel.style.fontSize = 36f;
            _garageTitleLabel.style.color = UnityEngine.Color.white;
            _garageTitleLabel.style.marginBottom = 10f;
            _garageRoot.Add(_garageTitleLabel);

            _garageCreditsLabel = new Label { name = "garage-credits" };
            _garageCreditsLabel.style.fontSize = 18f;
            _garageCreditsLabel.style.color = new UnityEngine.Color(1f, 0.85f, 0.2f);
            _garageCreditsLabel.style.marginBottom = 20f;
            _garageRoot.Add(_garageCreditsLabel);

            var scrollContainer = new VisualElement { name = "garage-vehicle-list" };
            scrollContainer.style.flexDirection = FlexDirection.Column;
            scrollContainer.style.alignItems = Align.Center;

            for (int i = 0; i < 10; i++)
            {
                var card = new VisualElement { name = "vehicle-card-" + i };
                card.style.width = 320f;
                card.style.paddingTop = 10f;
                card.style.paddingBottom = 10f;
                card.style.paddingLeft = 15f;
                card.style.paddingRight = 15f;
                card.style.marginBottom = 8f;
                card.style.backgroundColor = new UnityEngine.Color(0.1f, 0.12f, 0.16f, 0.9f);
                card.style.borderTopLeftRadius = 6f;
                card.style.borderTopRightRadius = 6f;
                card.style.borderBottomLeftRadius = 6f;
                card.style.borderBottomRightRadius = 6f;
                card.style.display = DisplayStyle.None;

                _vehicleNameLabels[i] = new Label { name = "vehicle-name-" + i };
                _vehicleNameLabels[i].style.fontSize = 18f;
                _vehicleNameLabels[i].style.color = UnityEngine.Color.white;
                card.Add(_vehicleNameLabels[i]);

                _vehicleClassLabels[i] = new Label { name = "vehicle-class-" + i };
                _vehicleClassLabels[i].style.fontSize = 13f;
                _vehicleClassLabels[i].style.color = new UnityEngine.Color(0.6f, 0.7f, 0.8f);
                card.Add(_vehicleClassLabels[i]);

                int capturedIndex = i;
                _vehicleActionButtons[i] = new Button { name = "vehicle-action-" + i };
                _vehicleActionButtons[i].style.width = 110f;
                _vehicleActionButtons[i].style.height = 32f;
                _vehicleActionButtons[i].style.marginTop = 6f;
                _vehicleActionButtons[i].clicked += () => OnVehicleAction(capturedIndex);
                card.Add(_vehicleActionButtons[i]);

                _vehicleCards[i] = card;
                scrollContainer.Add(card);
            }

            _garageRoot.Add(scrollContainer);

            _garageBackButton = new Button { name = "garage-back" };
            _garageBackButton.style.width = 120f;
            _garageBackButton.style.height = 40f;
            _garageBackButton.style.marginTop = 15f;
            _garageBackButton.clicked += () => GarageBackRequested?.Invoke();
            _garageRoot.Add(_garageBackButton);
        }

        private void BuildUpgrades()
        {
            _upgradesRoot.Clear();
            _upgradesRoot.style.alignItems = Align.Center;
            _upgradesRoot.style.justifyContent = Justify.FlexStart;
            _upgradesRoot.style.paddingTop = 30f;

            _upgradesTitleLabel = new Label { name = "upgrades-title" };
            _upgradesTitleLabel.style.fontSize = 32f;
            _upgradesTitleLabel.style.color = UnityEngine.Color.white;
            _upgradesTitleLabel.style.marginBottom = 10f;
            _upgradesRoot.Add(_upgradesTitleLabel);

            _upgradesVehicleLabel = new Label { name = "upgrades-vehicle" };
            _upgradesVehicleLabel.style.fontSize = 20f;
            _upgradesVehicleLabel.style.color = new UnityEngine.Color(0.8f, 0.9f, 1f);
            _upgradesVehicleLabel.style.marginBottom = 5f;
            _upgradesRoot.Add(_upgradesVehicleLabel);

            _upgradesCreditsLabel = new Label { name = "upgrades-credits" };
            _upgradesCreditsLabel.style.fontSize = 16f;
            _upgradesCreditsLabel.style.color = new UnityEngine.Color(1f, 0.85f, 0.2f);
            _upgradesCreditsLabel.style.marginBottom = 20f;
            _upgradesRoot.Add(_upgradesCreditsLabel);

            for (int i = 0; i < 6; i++)
            {
                var row = new VisualElement { name = "upgrade-row-" + i };
                row.style.flexDirection = FlexDirection.Row;
                row.style.justifyContent = Justify.SpaceBetween;
                row.style.alignItems = Align.Center;
                row.style.width = 400f;
                row.style.paddingTop = 8f;
                row.style.paddingBottom = 8f;
                row.style.marginBottom = 5f;
                row.style.display = DisplayStyle.None;

                _upgradeNameLabels[i] = new Label { name = "upgrade-name-" + i };
                _upgradeNameLabels[i].style.fontSize = 16f;
                _upgradeNameLabels[i].style.color = UnityEngine.Color.white;
                _upgradeNameLabels[i].style.width = 130f;
                row.Add(_upgradeNameLabels[i]);

                _upgradeTierLabels[i] = new Label { name = "upgrade-tier-" + i };
                _upgradeTierLabels[i].style.fontSize = 14f;
                _upgradeTierLabels[i].style.color = new UnityEngine.Color(0.8f, 0.8f, 0.8f);
                _upgradeTierLabels[i].style.width = 80f;
                row.Add(_upgradeTierLabels[i]);

                _upgradeCostLabels[i] = new Label { name = "upgrade-cost-" + i };
                _upgradeCostLabels[i].style.fontSize = 14f;
                _upgradeCostLabels[i].style.color = new UnityEngine.Color(0.7f, 0.7f, 0.7f);
                _upgradeCostLabels[i].style.width = 80f;
                row.Add(_upgradeCostLabels[i]);

                int capturedIndex = i;
                _upgradeBuyButtons[i] = new Button { name = "upgrade-buy-" + i };
                _upgradeBuyButtons[i].style.width = 90f;
                _upgradeBuyButtons[i].style.height = 28f;
                _upgradeBuyButtons[i].clicked += () =>
                {
                    if (_upgradeData != null && capturedIndex < _upgradeData.Length)
                    {
                        UpgradeBuyRequested?.Invoke(_upgradeData[capturedIndex].Category);
                    }
                };
                row.Add(_upgradeBuyButtons[i]);

                _upgradeRows[i] = row;
                _upgradesRoot.Add(row);
            }

            _upgradesBackButton = new Button { name = "upgrades-back" };
            _upgradesBackButton.style.width = 120f;
            _upgradesBackButton.style.height = 40f;
            _upgradesBackButton.style.marginTop = 15f;
            _upgradesBackButton.clicked += () => UpgradesBackRequested?.Invoke();
            _upgradesRoot.Add(_upgradesBackButton);
        }

        public void SetVehicleData(VehicleDisplayData[] data)
        {
            _vehicleData = data;
            RefreshGarage();
        }

        public void SetUpgradeData(UpgradeDisplayData[] data, string vehicleNameKey)
        {
            _upgradeData = data;
            _upgradesVehicleLabel.text = LocalizationService.Tr(vehicleNameKey);
            RefreshUpgrades();
        }

        public void RefreshGarage()
        {
            _garageTitleLabel.text = LocalizationService.Tr("garage.title");
            _garageCreditsLabel.text = _progress.Credits + " " + LocalizationService.Tr("common.cr");
            _garageBackButton.text = LocalizationService.Tr("garage.back");

            if (_vehicleData == null)
            {
                return;
            }

            for (int i = 0; i < 10 && i < _vehicleData.Length; i++)
            {
                var v = _vehicleData[i];
                _vehicleNameLabels[i].text = LocalizationService.Tr(v.NameKey);
                _vehicleClassLabels[i].text = LocalizationService.Tr(v.ClassKey);

                if (v.IsSelected)
                {
                    _vehicleActionButtons[i].text = LocalizationService.Tr("garage.selected");
                    _vehicleActionButtons[i].SetEnabled(false);
                }
                else if (v.IsUnlocked)
                {
                    _vehicleActionButtons[i].text = LocalizationService.Tr("garage.select");
                    _vehicleActionButtons[i].SetEnabled(true);
                }
                else
                {
                    _vehicleActionButtons[i].text = LocalizationService.Tr("garage.buy") + " " + v.UnlockCost + " " + LocalizationService.Tr("common.cr");
                    _vehicleActionButtons[i].SetEnabled(_progress.Credits >= v.UnlockCost);
                }

                _vehicleCards[i].style.display = DisplayStyle.Flex;
            }

            for (int i = (_vehicleData != null ? _vehicleData.Length : 0); i < 10; i++)
            {
                _vehicleCards[i].style.display = DisplayStyle.None;
            }
        }

        public void RefreshUpgrades()
        {
            _upgradesTitleLabel.text = LocalizationService.Tr("garage.upgrades");
            _upgradesCreditsLabel.text = _progress.Credits + " " + LocalizationService.Tr("common.cr");
            _upgradesBackButton.text = LocalizationService.Tr("upgrade.back");

            if (_upgradeData == null)
            {
                return;
            }

            for (int i = 0; i < 6 && i < _upgradeData.Length; i++)
            {
                var u = _upgradeData[i];
                _upgradeNameLabels[i].text = LocalizationService.Tr(u.NameKey);

                if (u.IsMaxed)
                {
                    _upgradeTierLabels[i].text = LocalizationService.Tr("upgrade.tier") + " " + u.CurrentTier + "/" + u.MaxTier;
                    _upgradeCostLabels[i].text = LocalizationService.Tr("upgrade.max");
                    _upgradeBuyButtons[i].text = LocalizationService.Tr("upgrade.max");
                    _upgradeBuyButtons[i].SetEnabled(false);
                }
                else
                {
                    _upgradeTierLabels[i].text = LocalizationService.Tr("upgrade.tier") + " " + u.CurrentTier + "/" + u.MaxTier;
                    _upgradeCostLabels[i].text = u.NextTierCost + " " + LocalizationService.Tr("common.cr");
                    _upgradeBuyButtons[i].text = LocalizationService.Tr("upgrade.buy");
                    _upgradeBuyButtons[i].SetEnabled(u.CanAfford);
                }

                _upgradeRows[i].style.display = DisplayStyle.Flex;
            }

            for (int i = (_upgradeData != null ? _upgradeData.Length : 0); i < 6; i++)
            {
                _upgradeRows[i].style.display = DisplayStyle.None;
            }
        }

        private void OnVehicleAction(int index)
        {
            if (_vehicleData == null || index >= _vehicleData.Length)
            {
                return;
            }

            var v = _vehicleData[index];
            if (v.IsSelected)
            {
                VehicleUpgradesRequested?.Invoke(v.VehicleId);
            }
            else if (v.IsUnlocked)
            {
                VehicleSelectRequested?.Invoke(v.VehicleId);
            }
            else
            {
                VehicleBuyRequested?.Invoke(v.VehicleId);
            }
        }

        private void OnLanguageChanged(string lang)
        {
            RefreshGarage();
            RefreshUpgrades();
        }
    }
}
