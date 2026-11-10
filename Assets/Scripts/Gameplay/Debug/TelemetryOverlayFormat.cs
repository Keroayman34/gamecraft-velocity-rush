#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Allocation-free numeric formatting and slip-angle math for the development telemetry
    /// overlay. Kept separate from rendering so EditMode tests can prove the hot path allocates
    /// nothing (TDD §36.10/§36.12).
    /// </summary>
    public static class TelemetryOverlayFormat
    {
        /// <summary>Recommended fixed-width field used by the overlay.</summary>
        public const int DefaultBufferWidth = 10;

        /// <summary>
        /// Writes a fixed-width, right-aligned decimal number into a preallocated buffer.
        /// The buffer receives exactly <paramref name="width"/> characters (space padded).
        /// </summary>
        /// <returns>The number of characters written (0 when the buffer or width is invalid).</returns>
        public static int WriteFixed(char[] buffer, float value, int decimals, int width)
        {
            if (buffer == null || buffer.Length == 0 || width <= 0)
            {
                return 0;
            }

            if (width > buffer.Length)
            {
                width = buffer.Length;
            }

            if (decimals < 0)
            {
                decimals = 0;
            }
            else if (decimals > 4)
            {
                decimals = 4;
            }

            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                value = 0f;
            }

            var power = 1L;
            for (var i = 0; i < decimals; i++)
            {
                power *= 10L;
            }

            var negative = value < 0f;
            var absolute = (double)Mathf.Abs(value);

            var integerDigits = width - decimals - (decimals > 0 ? 1 : 0) - (negative ? 1 : 0);
            if (integerDigits < 1)
            {
                integerDigits = 1;
            }

            var maxValue = Math.Pow(10d, integerDigits) - (1d / power);
            if (absolute > maxValue)
            {
                absolute = maxValue;
            }

            var scaled = (long)Math.Round(absolute * power, MidpointRounding.AwayFromZero);
            var maxScaled = (long)Math.Round(maxValue * power, MidpointRounding.AwayFromZero);
            if (scaled > maxScaled)
            {
                scaled = maxScaled;
            }

            var position = width;

            for (var i = 0; i < decimals; i++)
            {
                buffer[--position] = (char)('0' + (int)(scaled % 10L));
                scaled /= 10L;
            }

            if (decimals > 0)
            {
                buffer[--position] = '.';
            }

            if (scaled == 0)
            {
                buffer[--position] = '0';
            }

            while (scaled > 0 && position > 0)
            {
                buffer[--position] = (char)('0' + (int)(scaled % 10L));
                scaled /= 10L;
            }

            if (negative && position > 0)
            {
                buffer[--position] = '-';
            }

            while (position > 0)
            {
                buffer[--position] = ' ';
            }

            return width;
        }

        /// <summary>Writes a fixed-width integer (no decimal point).</summary>
        public static int WriteInt(char[] buffer, float value, int width)
        {
            return WriteFixed(buffer, value, 0, width);
        }

        /// <summary>
        /// Absolute chassis slip angle in degrees: 0 when travelling straight, growing toward 90
        /// as lateral speed dominates. Non-finite inputs return 0.
        /// </summary>
        public static float SlipAngleDegrees(float forwardSpeed, float lateralSpeed)
        {
            if (!IsFinite(forwardSpeed) || !IsFinite(lateralSpeed))
            {
                return 0f;
            }

            var forward = Mathf.Abs(forwardSpeed);
            var lateral = Mathf.Abs(lateralSpeed);
            if (forward < 0.0001f && lateral < 0.0001f)
            {
                return 0f;
            }

            return Mathf.Atan2(lateral, forward) * Mathf.Rad2Deg;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
#endif
