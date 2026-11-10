using System;

namespace VelocityRush.Core.Diagnostics
{
    /// <summary>
    /// Fixed-capacity ring buffer that summarises frame durations without allocating per frame
    /// (TDD §40.7/§40.18).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The profiling harness feeds this type one sample per rendered frame. Storage is allocated
    /// exactly once in the constructor; <see cref="Add"/> performs no managed allocation, and the
    /// summary queries reuse a preallocated scratch buffer when they need an ordered copy.
    /// </para>
    /// <para>
    /// Samples are milliseconds. Non-finite and negative values are sanitised to zero so a stalled
    /// or suspended tab can never poison a mean, a percentile, or the derived frame rate.
    /// </para>
    /// </remarks>
    public sealed class FrameTimeStatistics
    {
        private readonly float[] _samples;
        private readonly float[] _scratch;
        private int _next;
        private int _count;

        /// <summary>Creates a statistics window with a fixed number of slots.</summary>
        /// <param name="capacity">Number of retained frame samples; must be positive.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="capacity"/> is not positive.</exception>
        public FrameTimeStatistics(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            }

            _samples = new float[capacity];
            _scratch = new float[capacity];
        }

        /// <summary>Number of samples the window can hold.</summary>
        public int Capacity => _samples.Length;

        /// <summary>Number of samples currently retained (never exceeds <see cref="Capacity"/>).</summary>
        public int Count => _count;

        /// <summary>True when no sample has been added since construction or the last <see cref="Clear"/>.</summary>
        public bool IsEmpty => _count == 0;

        /// <summary>Discards every retained sample without reallocating.</summary>
        public void Clear()
        {
            _next = 0;
            _count = 0;
        }

        /// <summary>
        /// Appends one frame duration in milliseconds. Once the window is full, the oldest sample is
        /// overwritten. Invalid values are stored as zero.
        /// </summary>
        /// <param name="milliseconds">Frame duration in milliseconds.</param>
        public void Add(float milliseconds)
        {
            if (float.IsNaN(milliseconds) || float.IsInfinity(milliseconds) || milliseconds < 0f)
            {
                milliseconds = 0f;
            }

            _samples[_next] = milliseconds;
            _next = (_next + 1) % _samples.Length;
            if (_count < _samples.Length)
            {
                _count++;
            }
        }

        /// <summary>Arithmetic mean frame duration in milliseconds (0 when empty).</summary>
        public float MeanMilliseconds
        {
            get
            {
                if (_count == 0)
                {
                    return 0f;
                }

                var sum = 0f;
                for (var i = 0; i < _count; i++)
                {
                    sum += _samples[RingIndex(i)];
                }

                return sum / _count;
            }
        }

        /// <summary>Smallest retained frame duration in milliseconds (0 when empty).</summary>
        public float MinMilliseconds
        {
            get
            {
                if (_count == 0)
                {
                    return 0f;
                }

                var min = float.MaxValue;
                for (var i = 0; i < _count; i++)
                {
                    var sample = _samples[RingIndex(i)];
                    if (sample < min)
                    {
                        min = sample;
                    }
                }

                return min;
            }
        }

        /// <summary>Largest retained frame duration in milliseconds (0 when empty).</summary>
        public float MaxMilliseconds
        {
            get
            {
                if (_count == 0)
                {
                    return 0f;
                }

                var max = 0f;
                for (var i = 0; i < _count; i++)
                {
                    var sample = _samples[RingIndex(i)];
                    if (sample > max)
                    {
                        max = sample;
                    }
                }

                return max;
            }
        }

        /// <summary>Mean frames per second derived from <see cref="MeanMilliseconds"/> (0 when empty).</summary>
        public float MeanFramesPerSecond => ToFramesPerSecond(MeanMilliseconds);

        /// <summary>Worst-case frames per second derived from <see cref="MaxMilliseconds"/> (0 when empty).</summary>
        public float MinimumFramesPerSecond => ToFramesPerSecond(MaxMilliseconds);

        /// <summary>Best-case frames per second derived from <see cref="MinMilliseconds"/> (0 when empty).</summary>
        public float MaximumFramesPerSecond => ToFramesPerSecond(MinMilliseconds);

        /// <summary>
        /// Returns the percentile of retained frame durations using linear interpolation between
        /// ranks. A percentile of 0.95 is the conventional 95th percentile used by the harness.
        /// The query sorts into a preallocated scratch buffer and therefore allocates nothing.
        /// </summary>
        /// <param name="percentile">Fraction in the inclusive range 0–1.</param>
        /// <returns>Percentile frame duration in milliseconds (0 when empty or the input is invalid).</returns>
        public float PercentileMilliseconds(float percentile)
        {
            if (_count == 0 || float.IsNaN(percentile) || percentile < 0f || percentile > 1f)
            {
                return 0f;
            }

            for (var i = 0; i < _count; i++)
            {
                _scratch[i] = _samples[RingIndex(i)];
            }

            Array.Sort(_scratch, 0, _count);

            var rank = percentile * (_count - 1);
            var lower = (int)Math.Floor(rank);
            var upper = (int)Math.Ceiling(rank);
            if (lower == upper)
            {
                return _scratch[lower];
            }

            var weight = rank - lower;
            return _scratch[lower] + ((_scratch[upper] - _scratch[lower]) * weight);
        }

        private int RingIndex(int offset)
        {
            var start = _count == _samples.Length ? _next : 0;
            return (start + offset) % _samples.Length;
        }

        private static float ToFramesPerSecond(float milliseconds)
        {
            return milliseconds > 0f ? 1000f / milliseconds : 0f;
        }
    }
}
