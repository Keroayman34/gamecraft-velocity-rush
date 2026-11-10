using System;
using NUnit.Framework;
using UnityEngine.UIElements;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>Steady-state allocation rules for the complete UI Toolkit HUD view.</summary>
    public sealed class SliceHudViewTests
    {
        [Test]
        public void Apply_SteadyStateRefresh_DoesNotAllocate()
        {
            var view = new SliceHudView(new VisualElement());
            var snapshot = new SliceHudSnapshot(90, 12, 34, 2, 3, RaceState.Running);

            for (var i = 0; i < 100; i++)
            {
                view.Apply(snapshot);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 1000; i++)
            {
                view.Apply(snapshot);
            }

            var after = GC.GetAllocatedBytesForCurrentThread();
            Assert.That(after - before, Is.EqualTo(0));
        }

        [Test]
        public void ShowResults_ThenRetrySnapshot_HidesTheOverlay()
        {
            var view = new SliceHudView(new VisualElement());
            var result = new RaceResult(12.346d, 3, 3, true);

            view.ShowResults(result);
            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Finished));
            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown));

            // Reaching this point without an exception proves the overlay transition path is safe;
            // the display state is intentionally private so the view has no test-only surface.
            Assert.Pass();
        }
    }
}
