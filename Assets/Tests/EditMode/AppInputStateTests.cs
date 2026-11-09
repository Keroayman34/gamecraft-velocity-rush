using NUnit.Framework;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode tests for the pure application-intent state (T5.1.1): frame-scoped latching,
    /// press/release behavior, repeated samples, and deterministic reset.
    /// </summary>
    public sealed class AppInputStateTests
    {
        [Test]
        public void NewState_IsIdle_AndImplementsAppInput()
        {
            var state = new AppInputState();

            Assert.That(state, Is.InstanceOf<IAppInput>());
            Assert.That(state.PausePressed, Is.False);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.True);
        }

        [Test]
        public void Sample_PauseDown_RaisesOnlyPause()
        {
            var state = new AppInputState();

            state.Sample(pauseDown: true, retryDown: false);

            Assert.That(state.PausePressed, Is.True);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.False);
        }

        [Test]
        public void Sample_RetryDown_RaisesOnlyRetry()
        {
            var state = new AppInputState();

            state.Sample(pauseDown: false, retryDown: true);

            Assert.That(state.PausePressed, Is.False);
            Assert.That(state.RetryPressed, Is.True);
            Assert.That(state.IsIdle, Is.False);
        }

        [Test]
        public void Sample_BothDown_RaisesBoth()
        {
            var state = new AppInputState();

            state.Sample(pauseDown: true, retryDown: true);

            Assert.That(state.PausePressed, Is.True);
            Assert.That(state.RetryPressed, Is.True);
            Assert.That(state.IsIdle, Is.False);
        }

        [Test]
        public void Sample_ReleaseAfterPress_ClearsIntents()
        {
            var state = new AppInputState();
            state.Sample(pauseDown: true, retryDown: true);
            Assert.That(state.IsIdle, Is.False);

            state.Sample(pauseDown: false, retryDown: false);

            Assert.That(state.PausePressed, Is.False);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.True);
        }

        [Test]
        public void Sample_RepeatedIdenticalFrames_DoNotAccumulate()
        {
            var state = new AppInputState();

            state.Sample(pauseDown: true, retryDown: false);
            state.Sample(pauseDown: true, retryDown: false);

            Assert.That(state.PausePressed, Is.True);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.False);
        }

        [Test]
        public void Sample_HeldValueIsNotSticky_ReleaseOnNextFrame()
        {
            var state = new AppInputState();
            state.Sample(pauseDown: true, retryDown: false);

            state.Sample(pauseDown: false, retryDown: false);

            Assert.That(state.PausePressed, Is.False, "An intent must not survive to the next sampled frame.");
        }

        [Test]
        public void Reset_ClearsActivePause()
        {
            var state = new AppInputState();
            state.Sample(pauseDown: true, retryDown: false);

            state.Reset();

            Assert.That(state.PausePressed, Is.False);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.True);
        }

        [Test]
        public void Reset_ClearsActiveRetry()
        {
            var state = new AppInputState();
            state.Sample(pauseDown: false, retryDown: true);

            state.Reset();

            Assert.That(state.PausePressed, Is.False);
            Assert.That(state.RetryPressed, Is.False);
            Assert.That(state.IsIdle, Is.True);
        }

        [Test]
        public void Reset_IsIdempotent_AndStateCanBeSampledAgain()
        {
            var state = new AppInputState();
            state.Sample(pauseDown: true, retryDown: true);

            state.Reset();
            state.Reset();

            Assert.That(state.IsIdle, Is.True);

            state.Sample(pauseDown: false, retryDown: true);

            Assert.That(state.RetryPressed, Is.True);
            Assert.That(state.PausePressed, Is.False);
        }
    }
}
