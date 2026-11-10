using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Renders an integer as a fixed row of single-character labels, using ten cached digit strings
    /// and no allocation on update (TDD §40.7).
    /// </summary>
    /// <remarks>
    /// The UI Toolkit text setter compares strings, so assigning one of the cached one-character
    /// strings never creates a new managed string. This is what makes a per-frame numeric HUD
    /// possible under the 0 B/frame rule.
    /// </remarks>
    public sealed class DigitDisplay
    {
        private static readonly string[] DigitStrings = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

        private readonly Label[] _labels;
        private readonly int[] _placeValues;

        /// <summary>Creates a digit row and attaches it to <paramref name="parent"/>.</summary>
        /// <param name="parent">Container that receives the row.</param>
        /// <param name="digitCount">Number of digits; must be positive.</param>
        /// <param name="suppressLeadingZeros">When false, every digit is always drawn (e.g. 05).</param>
        /// <param name="pixelWidthPerDigit">Fixed cell width in pixels, preventing digit-width jitter.</param>
        public DigitDisplay(
            VisualElement parent,
            int digitCount,
            bool suppressLeadingZeros = true,
            float pixelWidthPerDigit = 24f)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (digitCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(digitCount), "Digit count must be positive.");
            }

            if (pixelWidthPerDigit <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(pixelWidthPerDigit), "Digit width must be positive.");
            }

            SuppressLeadingZeros = suppressLeadingZeros;
            _labels = new Label[digitCount];
            _placeValues = new int[digitCount];

            Root = new VisualElement();
            Root.style.flexDirection = FlexDirection.Row;

            var place = 1;
            for (var i = digitCount - 1; i >= 0; i--)
            {
                _placeValues[i] = place;
                place *= 10;

                var label = new Label(string.Empty);
                label.style.width = pixelWidthPerDigit;
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.marginLeft = 0f;
                label.style.marginRight = 0f;
                _labels[i] = label;
                Root.Add(label);
            }

            parent.Add(Root);
        }

        /// <summary>The row container, available for caller styling.</summary>
        public VisualElement Root { get; }

        /// <summary>Number of digits in the row.</summary>
        public int DigitCount => _labels.Length;

        /// <summary>Whether leading zero cells are hidden.</summary>
        public bool SuppressLeadingZeros { get; }

        /// <summary>Returns the currently displayed glyph for one cell (empty for a suppressed cell).</summary>
        /// <param name="index">Zero-based cell index from the most significant digit.</param>
        /// <returns>The cached glyph string.</returns>
        public string TextAt(int index)
        {
            if (index < 0 || index >= _labels.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _labels[index].text;
        }

        /// <summary>
        /// Displays <paramref name="value"/> clamped to the row's range. Negative values display as
        /// zero; values wider than the row saturate at the maximum representable number.
        /// </summary>
        /// <param name="value">Integer to display.</param>
        public void SetValue(int value)
        {
            if (value < 0)
            {
                value = 0;
            }

            var max = (_placeValues[0] * 10) - 1;
            if (value > max)
            {
                value = max;
            }

            var leading = SuppressLeadingZeros;
            for (var i = 0; i < _labels.Length; i++)
            {
                var place = _placeValues[i];
                var digit = (value / place) % 10;

                if (leading && digit == 0 && place > 1)
                {
                    SetText(i, string.Empty);
                    continue;
                }

                leading = false;
                SetText(i, DigitStrings[digit]);
            }
        }

        private void SetText(int index, string text)
        {
            if (_labels[index].text != text)
            {
                _labels[index].text = text;
            }
        }
    }
}
