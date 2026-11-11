using System;
using UnityEngine;
using UnityEngine.UIElements;
using VelocityRush.Core;
using VelocityRush.Data;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Scene adapter for the UI Toolkit Garage foundation (T-8.9/T-8.10).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Builds the model/view/presenter once from the scene's <see cref="VehicleRosterCatalog"/>
    /// and forwards keyboard and button intents. Keyboard polling is allocation-free; button
    /// callbacks are bound once in <see cref="Start"/> and released in <see cref="OnDestroy"/>.
    /// </para>
    /// <para>
    /// This screen intentionally has no ownership, unlock, upgrade, save, or race handoff logic.
    /// Confirmation only records the selected index for a later Phase-11 handoff.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class GarageScreen : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Garage] ";

        [SerializeField] private UIDocument document;
        [SerializeField] private VehicleRosterCatalog catalog;

        private GaragePresenter _presenter;
        private bool _subscribed;

        /// <summary>The active presenter, or null before <see cref="Start"/>.</summary>
        public GaragePresenter Presenter => _presenter;

        /// <summary>
        /// Raised after a confirmed selection is written to <see cref="SelectionContext"/>. The
        /// Phase-8 debug launcher subscribes; Phase 11's GameFlow replaces it.
        /// </summary>
        public event Action<string> SelectionConfirmed;

        private void Start()
        {
            if (document == null)
            {
                Debug.LogError(LogPrefix + "No UIDocument assigned; Garage screen disabled.", this);
                enabled = false;
                return;
            }

            if (catalog == null)
            {
                Debug.LogError(LogPrefix + "No VehicleRosterCatalog assigned; Garage screen disabled.", this);
                enabled = false;
                return;
            }

            if (!catalog.TryValidate(out var catalogError))
            {
                Debug.LogError(LogPrefix + "Roster catalog is invalid: " + catalogError, this);
                enabled = false;
                return;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(LogPrefix + "UIDocument did not create a runtime root.", this);
                enabled = false;
                return;
            }

            var model = new GarageCatalogModel(catalog.Vehicles);
            var view = new GarageView(root, model);
            _presenter = new GaragePresenter(model, view);

            view.PreviousButton.clicked += HandlePrevious;
            view.NextButton.clicked += HandleNext;
            view.ConfirmButton.clicked += HandleConfirm;
            _subscribed = true;
        }

        private void Update()
        {
            if (_presenter == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                HandlePrevious();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                HandleNext();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                HandleConfirm();
            }
        }

        private void OnDestroy()
        {
            ReleaseButtons();
        }

        private void HandlePrevious()
        {
            _presenter?.Previous();
        }

        private void HandleNext()
        {
            _presenter?.Next();
        }

        private void HandleConfirm()
        {
            ConfirmSelection();
        }

        /// <summary>
        /// Confirms the highlighted vehicle, records its stable id in
        /// <see cref="SelectionContext"/>, and notifies debug-flow listeners (T-8.11). Ownership,
        /// unlock checks, and the production GameFlow handoff remain Phase 11 work.
        /// </summary>
        /// <returns>True when a vehicle id was recorded.</returns>
        public bool ConfirmSelection()
        {
            if (_presenter == null)
            {
                return false;
            }

            _presenter.Confirm();
            var vehicleId = _presenter.Current.VehicleId;
            if (!SelectionContext.SetSelection(vehicleId))
            {
                Debug.LogError(LogPrefix + "Confirmed vehicle has no stable id.", this);
                return false;
            }

            SelectionConfirmed?.Invoke(vehicleId);
            return true;
        }

        private void ReleaseButtons()
        {
            if (!_subscribed || document == null)
            {
                return;
            }

            var root = document.rootVisualElement;
            if (root == null)
            {
                return;
            }

            var previous = root.Q<Button>("GaragePrevious");
            var next = root.Q<Button>("GarageNext");
            var confirm = root.Q<Button>("GarageConfirm");
            if (previous != null)
            {
                previous.clicked -= HandlePrevious;
            }

            if (next != null)
            {
                next.clicked -= HandleNext;
            }

            if (confirm != null)
            {
                confirm.clicked -= HandleConfirm;
            }

            _subscribed = false;
        }
    }
}
