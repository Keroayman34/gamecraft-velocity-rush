using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.PlayMode
{
    /// <summary>
    /// Frame-loop allocation guard for the development telemetry overlay
    /// (TDD §36.10/§36.12, Phase 6 acceptance evidence).
    /// </summary>
    /// <remarks>
    /// The test drives the real <see cref="VehicleTelemetryOverlay"/> refresh path, which is the
    /// per-frame work the overlay performs against live telemetry: fixed-point formatting for every
    /// field plus the per-wheel sampling copy. Rendering itself is an <c>OnGUI</c> repaint that
    /// cannot be exercised in a headless batch run, so the draw path is covered by the static
    /// no-<c>string.Format</c>/interpolation/<c>ToString</c>/LINQ rule and by the glyph atlas being
    /// built once in <c>Awake</c>.
    /// </remarks>
    public sealed class TelemetryOverlayAllocationPlayModeTests
    {
        [UnityTest]
        public IEnumerator Overlay_RefreshAfterWarmup_AllocatesZeroBytes()
        {
            var host = new GameObject("OverlayAllocationHost");
            host.SetActive(false);

            var source = host.AddComponent<StubTelemetrySource>();
            var overlay = host.AddComponent<VehicleTelemetryOverlay>();

            // The overlay resolves its source in Awake, so bind it while the host is inactive.
            var field = typeof(VehicleTelemetryOverlay).GetField(
                "telemetrySource",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "Overlay telemetrySource field was renamed.");
            field.SetValue(overlay, source);

            host.SetActive(true);
            yield return null;

            Assert.That(overlay.isActiveAndEnabled, Is.True, "Overlay must stay enabled with a valid source.");

            // Warm-up covers JIT, lazy property access, and any one-time buffer setup.
            for (var i = 0; i < 240; i++)
            {
                source.SetSample(i);
                overlay.RefreshNow();
            }

            var before = System.GC.GetAllocatedBytesForCurrentThread();
            const int frames = 600;
            for (var i = 0; i < frames; i++)
            {
                source.SetSample(i);
                overlay.RefreshNow();
            }

            var allocated = System.GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(
                allocated,
                Is.EqualTo(0L),
                "Overlay refresh must allocate 0 B/frame after warm-up; measured " + allocated + " B over " + frames + " frames.");

            Object.Destroy(host);
            yield return null;
        }

        private sealed class StubTelemetrySource : MonoBehaviour, IVehicleTelemetry, IVehicleWheelTelemetry
        {
            private VehicleTelemetry _telemetry = new VehicleTelemetry(24.5f, 22.1f, -4.75f, 0.31f, true);
            private WheelTelemetry[] _wheels =
            {
                new WheelTelemetry(0.12f, true, 0.55f),
                new WheelTelemetry(0.31f, true, 0.62f),
                new WheelTelemetry(0.08f, true, 0.48f),
                new WheelTelemetry(0.05f, false, 0f)
            };

            public VehicleTelemetry Telemetry => _telemetry;

            public int WheelCount => _wheels.Length;

            public WheelTelemetry GetWheel(int index)
            {
                return (uint)index < (uint)_wheels.Length ? _wheels[index] : default;
            }

            public void SetSample(int frame)
            {
                var speed = 10f + ((frame % 60) * 0.25f);
                _telemetry = new VehicleTelemetry(speed, speed - 1.5f, -3.5f + ((frame % 7) * 0.5f), 0.2f, true);
                _wheels[0] = new WheelTelemetry(0.05f + ((frame % 11) * 0.01f), true, 0.5f);
            }
        }
    }
}
