namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pure tyre-audio response curves: wheel slip to screech intensity, then intensity to loop
    /// volume and pitch (T-7.11).
    /// </summary>
    /// <remarks>
    /// Below the slip threshold the tyre is silent, so normal grip never produces a constant
    /// screech bed. All inputs are sanitized, which keeps NaN from a physics edge case out of the
    /// audio mixer.
    /// </remarks>
    public static class TyreAudioMath
    {
        /// <summary>
        /// Screech intensity in 0..1 for a slip magnitude. Returns zero below
        /// <paramref name="slipThreshold"/> and saturates at <paramref name="maxSlip"/>.
        /// </summary>
        public static float Intensity01(float slip, float slipThreshold, float maxSlip)
        {
            if (!IsFinite(slip) || slip <= 0f)
            {
                return 0f;
            }

            var threshold = IsFinite(slipThreshold) && slipThreshold > 0f ? slipThreshold : 0f;
            var ceiling = IsFinite(maxSlip) && maxSlip > threshold ? maxSlip : threshold + 1f;

            if (slip <= threshold)
            {
                return 0f;
            }

            var value = (slip - threshold) / (ceiling - threshold);
            return value > 1f ? 1f : value;
        }

        /// <summary>Screech loop volume for an intensity in 0..1.</summary>
        public static float Volume(float intensity01, float maxVolume)
        {
            var intensity = SanitizeUnit(intensity01);
            return SanitizeUnit(maxVolume) * intensity;
        }

        /// <summary>
        /// Screech loop pitch for an intensity in 0..1. Pitch falls as slip rises, which reads as a
        /// wider, lower scrub at the limit of grip.
        /// </summary>
        public static float Pitch(float intensity01, float minPitch, float maxPitch)
        {
            var low = IsFinite(minPitch) && minPitch > 0f ? minPitch : 0.01f;
            var high = IsFinite(maxPitch) && maxPitch > 0f ? maxPitch : low;
            if (high < low)
            {
                var swap = low;
                low = high;
                high = swap;
            }

            return high - ((high - low) * SanitizeUnit(intensity01));
        }

        private static float SanitizeUnit(float value)
        {
            if (!IsFinite(value) || value <= 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
