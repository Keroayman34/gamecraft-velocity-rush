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
            Assert.That(view.IsResultsVisible, Is.True);

            // A Finished snapshot keeps the overlay; the retry snapshot (Countdown) hides it.
            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Finished));
            Assert.That(view.IsResultsVisible, Is.True);

            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown));
            Assert.That(view.IsResultsVisible, Is.False);
        }

        [Test]
        public void Apply_CountdownSnapshot_ShowsCountdownGroup()
        {
            var view = new SliceHudView(new VisualElement());

            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown, countdownSeconds: 3));

            Assert.That(view.IsCountdownVisible, Is.True);
            Assert.That(view.IsPauseVisible, Is.False);
            Assert.That(view.IsResultsVisible, Is.False);
            Assert.That(view.DisplayedCountdownSeconds, Is.EqualTo(3));
        }

        [Test]
        public void Apply_PausedSnapshot_HidesCountdownAndShowsPauseOverlay()
        {
            var view = new SliceHudView(new VisualElement());
            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown, countdownSeconds: 2));

            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown, countdownSeconds: 2, isPaused: true));

            Assert.That(view.IsCountdownVisible, Is.False);
            Assert.That(view.IsPauseVisible, Is.True);

            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown, countdownSeconds: 2));
            Assert.That(view.IsPauseVisible, Is.False);
            Assert.That(view.IsCountdownVisible, Is.True);
        }

        [Test]
        public void ShowResults_HidesCountdownAndPause()
        {
            var view = new SliceHudView(new VisualElement());
            view.Apply(new SliceHudSnapshot(0, 0, 0, 0, 3, RaceState.Countdown, countdownSeconds: 1, isPaused: true));
            Assert.That(view.IsPauseVisible, Is.True);

            view.ShowResults(new RaceResult(20d, 3, 3, true));

            Assert.That(view.IsCountdownVisible, Is.False);
            Assert.That(view.IsPauseVisible, Is.False);
            Assert.That(view.IsResultsVisible, Is.True);
        }
    }
}
