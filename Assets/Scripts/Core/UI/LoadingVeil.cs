using UnityEngine;
using UnityEngine.UIElements;

namespace VelocityRush.Core
{
    /// <summary>
    /// Persistent, non-allocating UI Toolkit overlay displayed during asynchronous scene transitions (TDD §35.4; D-069, D-070).
    /// Enforces 0 B/frame steady state allocations when idle or active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LoadingVeil : MonoBehaviour, ILoadingVeil
    {
        [SerializeField] private UIDocument uiDocument;

        private VisualElement _veilRoot;
        private VisualElement _progressFill;
        private Label _messageLabel;
        private bool _isVisible;
        private float _currentProgress;
        private string _currentMessage;

        public bool IsVisible => _isVisible;
        public string CurrentMessage => _currentMessage;
        public float CurrentProgress => _currentProgress;

        private void Awake()
        {
            EnsureElements();
            Hide();
        }

        /// <summary>
        /// Builds or resolves the UI element hierarchy once on startup.
        /// </summary>
        public void EnsureElements()
        {
            if (_veilRoot != null)
            {
                return;
            }

            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            VisualElement parent = uiDocument != null ? uiDocument.rootVisualElement : null;

            _veilRoot = new VisualElement
            {
                name = "loading-veil-root"
            };

            // Fullscreen translucent backdrop (0 B/frame)
            _veilRoot.style.position = Position.Absolute;
            _veilRoot.style.left = 0f;
            _veilRoot.style.top = 0f;
            _veilRoot.style.right = 0f;
            _veilRoot.style.bottom = 0f;
            _veilRoot.style.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 0.95f);
            _veilRoot.style.alignItems = Align.Center;
            _veilRoot.style.justifyContent = Justify.Center;
            _veilRoot.style.display = DisplayStyle.None;

            _messageLabel = new Label("Loading...")
            {
                name = "loading-veil-message"
            };
            _messageLabel.style.color = Color.white;
            _messageLabel.style.fontSize = 24f;
            _messageLabel.style.marginBottom = 20f;
            _messageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _veilRoot.Add(_messageLabel);

            var progressBarTrack = new VisualElement
            {
                name = "loading-veil-bar-track"
            };
            progressBarTrack.style.width = 320f;
            progressBarTrack.style.height = 8f;
            progressBarTrack.style.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 1f);
            progressBarTrack.style.borderTopLeftRadius = 4f;
            progressBarTrack.style.borderTopRightRadius = 4f;
            progressBarTrack.style.borderBottomLeftRadius = 4f;
            progressBarTrack.style.borderBottomRightRadius = 4f;
            progressBarTrack.style.overflow = Overflow.Hidden;

            _progressFill = new VisualElement
            {
                name = "loading-veil-bar-fill"
            };
            _progressFill.style.height = new Length(100f, LengthUnit.Percent);
            _progressFill.style.width = new Length(0f, LengthUnit.Percent);
            _progressFill.style.backgroundColor = new Color(0.2f, 0.75f, 1f, 1f);
            progressBarTrack.Add(_progressFill);

            _veilRoot.Add(progressBarTrack);

            if (parent != null)
            {
                parent.Add(_veilRoot);
            }
        }

        /// <summary>
        /// Explicitly binds the veil to a provided root VisualElement (supports runtime UI composition and test harnesses).
        /// </summary>
        public void BindRoot(VisualElement root)
        {
            EnsureElements();
            if (root != null && _veilRoot.parent != root)
            {
                _veilRoot.RemoveFromHierarchy();
                root.Add(_veilRoot);
            }
        }

        public void Show(string message = null)
        {
            EnsureElements();
            _currentMessage = message ?? "Loading...";
            if (_messageLabel != null)
            {
                _messageLabel.text = _currentMessage;
            }
            SetProgress(0f);
            _isVisible = true;
            _veilRoot.style.display = DisplayStyle.Flex;
        }

        public void SetProgress(float progress)
        {
            EnsureElements();
            _currentProgress = Mathf.Clamp01(progress);
            if (_progressFill != null)
            {
                _progressFill.style.width = new Length(_currentProgress * 100f, LengthUnit.Percent);
            }
        }

        public void Hide()
        {
            EnsureElements();
            _isVisible = false;
            _currentProgress = 0f;
            _currentMessage = null;
            if (_veilRoot != null)
            {
                _veilRoot.style.display = DisplayStyle.None;
            }
        }
    }
}
