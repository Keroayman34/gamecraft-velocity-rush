using System;
using System.Globalization;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using VelocityRush.Core.Diagnostics;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Development-only sampling harness that records a warm-up window and then prints one
    /// allocation-free summary of frame pacing and memory (TDD §40.18, T-7.4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The component exists in every build so the slice scene always deserialises cleanly, but it
    /// disables itself outside editor and Development players. It never runs in a release build.
    /// </para>
    /// <para>
    /// The hot path only writes into a preallocated <see cref="FrameTimeStatistics"/> ring buffer:
    /// no strings, no closures, no LINQ, and no per-frame managed allocations. The single summary
    /// line is composed once when sampling ends.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class PrototypePerformanceProbe : MonoBehaviour
    {
#if UNITY_EDITOR || DEBUG
        private const int SampleCapacity = 8192;
        private const string LogPrefix = "[VelocityRush.Perf] ";

        [SerializeField, Min(0f)] private float warmupSeconds = 3f;
        [SerializeField, Min(0.1f)] private float sampleSeconds = 10f;
        [SerializeField] private bool logSummary = true;

        private FrameTimeStatistics _statistics;
        private ProfilerRecorder _drawCalls;
        private ProfilerRecorder _batches;
        private ProfilerRecorder _triangles;
        private ProfilerRecorder _vertices;
        private float _warmupRemaining;
        private float _sampleRemaining;
        private long _managedBytesAtWindowStart;
        private long _totalAllocatedAtWindowStart;
        private bool _sampling;
        private bool _completed;

        private void Awake()
        {
            _statistics = new FrameTimeStatistics(SampleCapacity);
            _warmupRemaining = Mathf.Max(0f, warmupSeconds);
            _sampleRemaining = Mathf.Max(0.1f, sampleSeconds);
            _drawCalls = TryStartRecorder("Draw Calls Count");
            _batches = TryStartRecorder("Batches Count");
            _triangles = TryStartRecorder("Triangles Count");
            _vertices = TryStartRecorder("Vertices Count");
        }

        private void Update()
        {
            if (_completed)
            {
                return;
            }

            var delta = Time.unscaledDeltaTime;

            if (!_sampling)
            {
                _warmupRemaining -= delta;
                if (_warmupRemaining > 0f)
                {
                    return;
                }

                BeginSampling();
            }

            _statistics.Add(delta * 1000f);
            _sampleRemaining -= delta;
            if (_sampleRemaining <= 0f)
            {
                CompleteSampling();
            }
        }

        private void BeginSampling()
        {
            _sampling = true;
            _statistics.Clear();
            _managedBytesAtWindowStart = GC.GetTotalMemory(false);
            _totalAllocatedAtWindowStart = Profiler.GetTotalAllocatedMemoryLong();
        }

        private void CompleteSampling()
        {
            _completed = true;

            if (!logSummary || _statistics.IsEmpty)
            {
                return;
            }

            var meanMs = _statistics.MeanMilliseconds;
            var p95Ms = _statistics.PercentileMilliseconds(0.95f);
            var minMs = _statistics.MinMilliseconds;
            var maxMs = _statistics.MaxMilliseconds;
            var managedNow = GC.GetTotalMemory(false);
            var totalAllocatedNow = Profiler.GetTotalAllocatedMemoryLong();

            Debug.Log(
                LogPrefix +
                "frames=" + _statistics.Count.ToString(CultureInfo.InvariantCulture) +
                " meanMs=" + meanMs.ToString("0.00", CultureInfo.InvariantCulture) +
                " p95Ms=" + p95Ms.ToString("0.00", CultureInfo.InvariantCulture) +
                " minMs=" + minMs.ToString("0.00", CultureInfo.InvariantCulture) +
                " maxMs=" + maxMs.ToString("0.00", CultureInfo.InvariantCulture) +
                " meanFps=" + (meanMs > 0f ? (1000f / meanMs).ToString("0.0", CultureInfo.InvariantCulture) : "0") +
                " minFps=" + (maxMs > 0f ? (1000f / maxMs).ToString("0.0", CultureInfo.InvariantCulture) : "0") +
                " maxFps=" + (minMs > 0f ? (1000f / minMs).ToString("0.0", CultureInfo.InvariantCulture) : "0") +
                " managedMB=" + (managedNow / (1024f * 1024f)).ToString("0.00", CultureInfo.InvariantCulture) +
                " managedDeltaKB=" + ((managedNow - _managedBytesAtWindowStart) / 1024f).ToString("0.0", CultureInfo.InvariantCulture) +
                " totalAllocMB=" + (totalAllocatedNow / (1024f * 1024f)).ToString("0.00", CultureInfo.InvariantCulture) +
                " totalAllocDeltaMB=" + ((totalAllocatedNow - _totalAllocatedAtWindowStart) / (1024f * 1024f)).ToString("0.00", CultureInfo.InvariantCulture) +
                " drawCalls=" + ReadLastValue(_drawCalls) +
                " batches=" + ReadLastValue(_batches) +
                " triangles=" + ReadLastValue(_triangles) +
                " vertices=" + ReadLastValue(_vertices) +
                " device=" + SystemInfo.graphicsDeviceType +
                " renderer=" + SystemInfo.graphicsDeviceName);

            DisposeRecorders();
        }

        private void OnDisable()
        {
            DisposeRecorders();
        }

        private static ProfilerRecorder TryStartRecorder(string marker)
        {
            return ProfilerRecorder.StartNew(ProfilerCategory.Render, marker);
        }

        private static long ReadLastValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : 0L;
        }

        private void DisposeRecorders()
        {
            if (_drawCalls.Valid)
            {
                _drawCalls.Dispose();
            }

            if (_batches.Valid)
            {
                _batches.Dispose();
            }

            if (_triangles.Valid)
            {
                _triangles.Dispose();
            }

            if (_vertices.Valid)
            {
                _vertices.Dispose();
            }
        }
#else
        private void Awake()
        {
            enabled = false;
        }
#endif
    }
}
