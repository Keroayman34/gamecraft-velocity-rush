using System;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Numeric conversions for the vertical-slice HUD. Pure functions only, so the hot path can be
    /// unit-tested without a UI panel and every conversion is allocation-free (TDD §40.7).
    /// </summary>
    public static class SliceHudFormat
    {
        /// <summary>Largest whole-second value the three-digit HUD field can display.</summary>
        public const int MaxWholeSeconds = 999;

        /// <summary>Largest speed in km/h the three-digit HUD field can display.</summary>
        public const int MaxSpeedKph = 999;

        /// <summary>
        /// Splits a race time into whole seconds and hundredths. Negative, non-finite, and
        /// out-of-range values are clamped so the display can never show a broken field.
        /// </summary>
        /// <param name="seconds">Elapsed race time in seconds.</param>
        /// <param name="wholeSeconds">Whole seconds, 0–<see cref="MaxWholeSeconds"/>.</param>
        /// <param name="hundredths">Hundredths of a second, 0–99.</param>
        public static void SplitRaceTime(double seconds, out int wholeSeconds, out int hundredths)
        {
            if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds <= 0d)
            {
                wholeSeconds = 0;
                hundredths = 0;
                return;
            }

            var totalHundredths = (long)Math.Floor((seconds * 100d) + 0.5d);
            var whole = totalHundredths / 100L;
            var fraction = (int)(totalHundredths % 100L);

            if (whole > MaxWholeSeconds)
            {
                whole = MaxWholeSeconds;
                fraction = 99;
            }

            wholeSeconds = (int)whole;
            hundredths = fraction;
        }

        /// <summary>
        /// Converts a telemetry speed in metres per second to whole km/h, clamped to the HUD field.
        /// Negative and non-finite values map to zero.
        /// </summary>
        /// <param name="metersPerSecond">Velocity magnitude in m/s.</param>
        /// <returns>Display speed in km/h, 0–<see cref="MaxSpeedKph"/>.</returns>
        public static int SpeedToKph(float metersPerSecond)
        {
            if (float.IsNaN(metersPerSecond) || float.IsInfinity(metersPerSecond) || metersPerSecond <= 0f)
            {
                return 0;
            }

            var kph = (int)Math.Round(metersPerSecond * 3.6f, MidpointRounding.AwayFromZero);
            return kph > MaxSpeedKph ? MaxSpeedKph : kph;
        }
    }
}
