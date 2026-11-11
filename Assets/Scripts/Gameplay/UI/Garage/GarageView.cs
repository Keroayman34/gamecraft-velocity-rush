using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// UI Toolkit Garage foundation: browse the ten vehicles, read class/stats, select and confirm
    /// (T-8.9/T-8.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The tree is built once. Every roster row and every detail panel is created in the
    /// constructor, and each label owns its final string at that point. Navigation therefore only
    /// toggles <see cref="DisplayStyle"/> and two background colors, which keeps the selection path
    /// free of string construction, layout rebuilds, and managed allocation (the EditMode/PlayMode
    /// guards measure 0 B over repeated selections).
    /// </para>
    /// <para>
    /// Scope is deliberately foundation-only: no ownership, unlock, upgrade purchase, save, or
    /// progression state. Those remain Phase 11 (OQ-07/OQ-09).
    /// </para>
    /// </remarks>
    public sealed class GarageView : IGarageView
    {
        private static readonly Color Background = new Color(0.035f, 0.045f, 0.06f, 1f);
        private static readonly Color Panel = new Color(0.07f, 0.08f, 0.105f, 0.96f);
        private static readonly Color RowNormal = new Color(0.09f, 0.10f, 0.13f, 1f);
        private static readonly Color RowSelected = new Color(0.22f, 0.18f, 0.08f, 1f);
        private static readonly Color Accent = new Color(0.98f, 0.80f, 0.25f, 1f);
        private static readonly Color Caption = new Color(0.62f, 0.66f, 0.72f, 1f);
        private static readonly Color Value = new Color(0.94f, 0.95f, 0.97f, 1f);

        private readonly VisualElement[] _rows;
        private readonly VisualElement[] _detailPanels;
        private readonly string[] _confirmationText;
        private readonly Label _confirmation;
        private int _selectedIndex = -1;

        /// <summary>Builds the Garage tree inside <paramref name="root"/>.</summary>
        /// <param name="root">Root visual element (UIDocument root or a test-owned element).</param>
        /// <param name="vehicles">Roster summaries in display order.</param>
        public GarageView(VisualElement root, GarageCatalogModel vehicles)
        {
            if (root == null)
            {
                throw new System.ArgumentNullException(nameof(root));
            }

            if (vehicles == null)
            {
                throw new System.ArgumentNullException(nameof(vehicles));
            }

            root.Clear();
            root.style.position = Position.Absolute;
            root.style.left = 0f;
            root.style.top = 0f;
            root.style.right = 0f;
            root.style.bottom = 0f;
            root.style.backgroundColor = Background;

            var header = new Label("GARAGE");
            header.style.fontSize = 34f;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.color = Accent;
            header.style.marginLeft = 30f;
            header.style.marginTop = 22f;
            header.style.marginBottom = 2f;
            root.Add(header);

            var subtitle = new Label("SELECT A VEHICLE");
            subtitle.style.fontSize = 13f;
            subtitle.style.color = Caption;
            subtitle.style.marginLeft = 31f;
            subtitle.style.marginBottom = 18f;
            root.Add(subtitle);

            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1f;
            body.style.marginLeft = 30f;
            body.style.marginRight = 30f;
            body.style.marginBottom = 24f;
            root.Add(body);

            var listPanel = CreatePanel();
            listPanel.style.width = Length.Percent(34f);
            listPanel.style.marginRight = 18f;
            listPanel.style.paddingTop = 12f;
            listPanel.style.paddingBottom = 12f;
            body.Add(listPanel);

            var detailHost = CreatePanel();
            detailHost.style.flexGrow = 1f;
            detailHost.style.paddingLeft = 26f;
            detailHost.style.paddingRight = 26f;
            detailHost.style.paddingTop = 22f;
            detailHost.style.paddingBottom = 22f;
            body.Add(detailHost);

            _rows = new VisualElement[vehicles.Count];
            _detailPanels = new VisualElement[vehicles.Count];
            _confirmationText = new string[vehicles.Count];

            for (var i = 0; i < vehicles.Count; i++)
            {
                var summary = vehicles[i];
                _rows[i] = BuildRow(listPanel, summary, i);
                _detailPanels[i] = BuildDetail(detailHost, summary);
                _confirmationText[i] = "SELECTED  " + summary.DisplayNameEn.ToUpperInvariant();
            }

            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.alignItems = Align.Center;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.marginLeft = 30f;
            footer.style.marginRight = 30f;
            footer.style.marginBottom = 24f;
            root.Add(footer);

            PreviousButton = CreateButton("PREVIOUS");
            NextButton = CreateButton("NEXT");
            ConfirmButton = CreateButton("CONFIRM");
            PreviousButton.name = "GaragePrevious";
            NextButton.name = "GarageNext";
            ConfirmButton.name = "GarageConfirm";
            ConfirmButton.style.backgroundColor = Accent;
            ConfirmButton.style.color = Background;

            var buttonRow = new VisualElement();
            buttonRow.style.flexDirection = FlexDirection.Row;
            buttonRow.Add(PreviousButton);
            buttonRow.Add(NextButton);
            buttonRow.Add(ConfirmButton);
            footer.Add(buttonRow);

            _confirmation = new Label(string.Empty);
            _confirmation.name = "GarageConfirmation";
            _confirmation.style.fontSize = 16f;
            _confirmation.style.unityFontStyleAndWeight = FontStyle.Bold;
            _confirmation.style.color = Accent;
            _confirmation.style.display = DisplayStyle.None;
            footer.Add(_confirmation);
        }

        /// <summary>Previous-vehicle button.</summary>
        public Button PreviousButton { get; }

        /// <summary>Next-vehicle button.</summary>
        public Button NextButton { get; }

        /// <summary>Confirm-selection button.</summary>
        public Button ConfirmButton { get; }

        /// <summary>Currently shown index, or -1 before the first refresh.</summary>
        public int ShownIndex => _selectedIndex;

        /// <inheritdoc />
        public void Refresh(in GarageVehicleSummary summary, int index, int count)
        {
            if (index == _selectedIndex || (uint)index >= (uint)_rows.Length)
            {
                return;
            }

            if (_selectedIndex >= 0)
            {
                _rows[_selectedIndex].style.backgroundColor = RowNormal;
                _detailPanels[_selectedIndex].style.display = DisplayStyle.None;
            }

            _selectedIndex = index;
            _rows[index].style.backgroundColor = RowSelected;
            _detailPanels[index].style.display = DisplayStyle.Flex;

            if (_confirmation.style.display == DisplayStyle.Flex)
            {
                _confirmation.style.display = DisplayStyle.None;
            }
        }

        /// <inheritdoc />
        public void ShowConfirmation(in GarageVehicleSummary summary)
        {
            if ((uint)_selectedIndex >= (uint)_confirmationText.Length)
            {
                return;
            }

            // Confirmation is always for the currently shown row; the index is cached by Refresh
            // so this path performs no lookup, interpolation, or string construction.
            _confirmation.text = _confirmationText[_selectedIndex];
            _confirmation.style.display = DisplayStyle.Flex;
        }

        private VisualElement BuildRow(VisualElement parent, in GarageVehicleSummary summary, int index)
        {
            var row = new VisualElement();
            row.name = "GarageRow" + index;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.paddingLeft = 18f;
            row.style.paddingRight = 18f;
            row.style.paddingTop = 12f;
            row.style.paddingBottom = 12f;
            row.style.marginBottom = 4f;
            row.style.backgroundColor = RowNormal;

            var name = new Label(summary.DisplayNameEn);
            name.style.fontSize = 18f;
            name.style.color = Value;
            row.Add(name);

            var classLabel = new Label(ClassText(summary.Class));
            classLabel.style.fontSize = 12f;
            classLabel.style.color = Caption;
            row.Add(classLabel);

            parent.Add(row);
            return row;
        }

        private VisualElement BuildDetail(VisualElement parent, in GarageVehicleSummary summary)
        {
            var panel = new VisualElement();
            panel.name = "GarageDetail_" + summary.VehicleId;
            panel.style.display = DisplayStyle.None;
            panel.style.flexGrow = 1f;

            var name = new Label(summary.DisplayNameEn);
            name.style.fontSize = 40f;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.color = Value;
            panel.Add(name);

            var arabic = new Label(summary.DisplayNameAr);
            arabic.style.fontSize = 22f;
            arabic.style.color = Caption;
            arabic.style.marginBottom = 18f;
            panel.Add(arabic);

            var classLabel = new Label(ClassText(summary.Class));
            classLabel.style.fontSize = 15f;
            classLabel.style.color = Accent;
            classLabel.style.marginBottom = 20f;
            panel.Add(classLabel);

            panel.Add(StatLine("MASS", summary.Mass.ToString("0", CultureInfo.InvariantCulture) + " KG"));
            panel.Add(StatLine("TORQUE", summary.MotorTorque.ToString("0", CultureInfo.InvariantCulture) + " N\u00b7m"));
            panel.Add(StatLine("GRIP F / S",
                summary.ForwardGrip.ToString("0.00", CultureInfo.InvariantCulture) + " / " +
                summary.SidewaysGrip.ToString("0.00", CultureInfo.InvariantCulture)));
            panel.Add(StatLine("BRAKE BIAS",
                (summary.BrakeBiasFront * 100f).ToString("0", CultureInfo.InvariantCulture) + "% FRONT"));
            panel.Add(StatLine("STEERING",
                summary.SteeringAngle.ToString("0", CultureInfo.InvariantCulture) + "\u00b0"));

            parent.Add(panel);
            return panel;
        }

        private static VisualElement StatLine(string caption, string value)
        {
            var line = new VisualElement();
            line.style.flexDirection = FlexDirection.Row;
            line.style.marginBottom = 8f;

            var captionLabel = new Label(caption);
            captionLabel.style.width = 130f;
            captionLabel.style.fontSize = 13f;
            captionLabel.style.color = Caption;
            line.Add(captionLabel);

            var valueLabel = new Label(value);
            valueLabel.style.fontSize = 15f;
            valueLabel.style.color = Value;
            line.Add(valueLabel);

            return line;
        }

        private static string ClassText(VehicleClass vehicleClass)
        {
            switch (vehicleClass)
            {
                case VehicleClass.Starter:
                    return "STARTER";
                case VehicleClass.Sport:
                    return "SPORT";
                case VehicleClass.Drift:
                    return "DRIFT";
                case VehicleClass.Supercar:
                    return "SUPERCAR";
                default:
                    return "UNKNOWN";
            }
        }

        private static VisualElement CreatePanel()
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = Panel;
            panel.style.borderTopLeftRadius = 8f;
            panel.style.borderTopRightRadius = 8f;
            panel.style.borderBottomLeftRadius = 8f;
            panel.style.borderBottomRightRadius = 8f;
            return panel;
        }

        private static Button CreateButton(string text)
        {
            var button = new Button { text = text };
            button.style.fontSize = 14f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.paddingLeft = 20f;
            button.style.paddingRight = 20f;
            button.style.paddingTop = 10f;
            button.style.paddingBottom = 10f;
            button.style.marginRight = 10f;
            button.style.backgroundColor = Panel;
            button.style.color = Value;
            return button;
        }
    }
}
