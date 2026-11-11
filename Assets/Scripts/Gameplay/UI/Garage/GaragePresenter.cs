namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Connects <see cref="GarageCatalogModel"/> selection state to an <see cref="IGarageView"/>
    /// (T-8.9). Plain C# so the navigation path is directly measurable in EditMode and PlayMode.
    /// </summary>
    public sealed class GaragePresenter
    {
        private readonly GarageCatalogModel _model;
        private readonly IGarageView _view;

        /// <summary>Creates the presenter and renders the initial selection.</summary>
        public GaragePresenter(GarageCatalogModel model, IGarageView view)
        {
            _model = model ?? throw new System.ArgumentNullException(nameof(model));
            _view = view ?? throw new System.ArgumentNullException(nameof(view));

            _view.Refresh(_model.Current, _model.SelectedIndex, _model.Count);
        }

        /// <summary>Number of vehicles in the catalog.</summary>
        public int Count => _model.Count;

        /// <summary>Currently highlighted index.</summary>
        public int SelectedIndex => _model.SelectedIndex;

        /// <summary>Last confirmed index, or -1.</summary>
        public int ConfirmedIndex => _model.ConfirmedIndex;

        /// <summary>The currently highlighted vehicle.</summary>
        public GarageVehicleSummary Current => _model.Current;

        /// <summary>Highlights the next vehicle and refreshes the view.</summary>
        public bool Next()
        {
            return Apply(_model.Next());
        }

        /// <summary>Highlights the previous vehicle and refreshes the view.</summary>
        public bool Previous()
        {
            return Apply(_model.Previous());
        }

        /// <summary>Highlights an explicit index and refreshes the view.</summary>
        public bool Select(int index)
        {
            return Apply(_model.Select(index));
        }

        /// <summary>Confirms the current selection and shows the confirmation label.</summary>
        public bool Confirm()
        {
            _model.Confirm();
            _view.ShowConfirmation(_model.Current);
            return true;
        }

        private bool Apply(bool changed)
        {
            if (changed)
            {
                _view.Refresh(_model.Current, _model.SelectedIndex, _model.Count);
            }

            return changed;
        }
    }
}
