using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VelocityRush.Core;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode smoke tests for the Unity-dependent adapter (T5.1.1): interface separation,
    /// neutral construction, explicit reset, and the focus/lifecycle handler logic. Real key
    /// polling and actual OS focus callbacks require PlayMode or a browser session and are not
    /// claimed here.
    /// </summary>
    public sealed class LegacyKeyboardInputAdapterTests
    {
        private static readonly FieldInfo HasFocusField =
            typeof(LegacyKeyboardInputAdapter).GetField("_hasFocus", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo EnableHandler =
            typeof(LegacyKeyboardInputAdapter).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo FocusHandler =
            typeof(LegacyKeyboardInputAdapter).GetMethod("OnApplicationFocus", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo PauseHandler =
            typeof(LegacyKeyboardInputAdapter).GetMethod("OnApplicationPause", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo DestroyHandler =
            typeof(LegacyKeyboardInputAdapter).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo KeyboardStateField =
            typeof(LegacyKeyboardInputAdapter).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo AppIntentStateField =
            typeof(LegacyKeyboardInputAdapter).GetField("_appState", BindingFlags.Instance | BindingFlags.NonPublic);

        private GameObject _host;
        private LegacyKeyboardInputAdapter _adapter;

        [SetUp]
        public void SetUp()
        {
            Assert.That(HasFocusField, Is.Not.Null, "Adapter lifecycle test seam missing: _hasFocus field not found.");
            Assert.That(EnableHandler, Is.Not.Null, "Adapter lifecycle test seam missing: OnEnable not found.");
            Assert.That(FocusHandler, Is.Not.Null, "Adapter lifecycle test seam missing: OnApplicationFocus not found.");
            Assert.That(PauseHandler, Is.Not.Null, "Adapter lifecycle test seam missing: OnApplicationPause not found.");
            Assert.That(DestroyHandler, Is.Not.Null, "Adapter lifecycle test seam missing: OnDestroy not found.");
            Assert.That(KeyboardStateField, Is.Not.Null, "Adapter state test seam missing: _state field not found.");
            Assert.That(AppIntentStateField, Is.Not.Null, "Adapter state test seam missing: _appState field not found.");

            _host = new GameObject("T5.1.1 Input Adapter Test Host");
            _adapter = _host.AddComponent<LegacyKeyboardInputAdapter>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                UnityEngine.Object.DestroyImmediate(_host);
            }
        }

        [Test]
        public void Adapter_ImplementsVehicleAndAppInputInterfaces()
        {
            Assert.That(_adapter, Is.InstanceOf<IVehicleInput>());
            Assert.That(_adapter, Is.InstanceOf<IAppInput>());
        }

        [Test]
        public void Adapter_StartsNeutral_WithNoPendingIntents()
        {
            Assert.That(_adapter.Snapshot.IsNeutral, Is.True);
            Assert.That(_adapter.Throttle, Is.EqualTo(0f));
            Assert.That(_adapter.Brake, Is.EqualTo(0f));
            Assert.That(_adapter.Steering, Is.EqualTo(0f));
            Assert.That(_adapter.Handbrake, Is.False);
            Assert.That(_adapter.PausePressed, Is.False);
            Assert.That(_adapter.RetryPressed, Is.False);
        }

        [Test]
        public void Adapter_ResetInput_KeepsNeutralStateAndClearsIntents()
        {
            _adapter.ResetInput();
            _adapter.ResetInput();

            Assert.That(_adapter.Snapshot.IsNeutral, Is.True);
            Assert.That(_adapter.PausePressed, Is.False);
            Assert.That(_adapter.RetryPressed, Is.False);
        }

        [Test]
        public void Adapter_OnEnable_ResyncsFocusFromApplicationState()
        {
            // Simulate a disable that happened while the app was unfocused, then re-enable.
            HasFocusField.SetValue(_adapter, false);

            EnableHandler.Invoke(_adapter, null);

            Assert.That((bool)HasFocusField.GetValue(_adapter), Is.EqualTo(Application.isFocused),
                "OnEnable must restore the focus gate from the current application state, not leave it stuck off.");
        }

        [Test]
        public void Adapter_FocusLossAndRegain_UpdateFocusFlag()
        {
            FocusHandler.Invoke(_adapter, new object[] { false });
            Assert.That((bool)HasFocusField.GetValue(_adapter), Is.False);

            FocusHandler.Invoke(_adapter, new object[] { true });
            Assert.That((bool)HasFocusField.GetValue(_adapter), Is.True);
        }

        [Test]
        public void Adapter_PauseTransition_ClearsIntents()
        {
            PauseHandler.Invoke(_adapter, new object[] { true });

            Assert.That(_adapter.PausePressed, Is.False);
            Assert.That(_adapter.RetryPressed, Is.False);
            Assert.That(_adapter.Snapshot.IsNeutral, Is.True);
        }

        [Test]
        public void Adapter_StateChanged_ResetsDrivingInputAndIntents()
        {
            var source = new FakeAppStateSource();
            _adapter.BindAppStateSource(source);
            RaiseActiveInput();

            source.Raise(AppState.Race);

            AssertNeutral(_adapter);
        }

        [Test]
        public void Adapter_BindAppStateSource_IsIdempotent()
        {
            var source = new FakeAppStateSource();

            _adapter.BindAppStateSource(source);
            _adapter.BindAppStateSource(source);

            Assert.That(source.SubscriberCount, Is.EqualTo(1), "Re-binding the same source must not double-subscribe.");
        }

        [Test]
        public void Adapter_Rebind_SwitchesSubscription()
        {
            var first = new FakeAppStateSource();
            var second = new FakeAppStateSource();

            _adapter.BindAppStateSource(first);
            _adapter.BindAppStateSource(second);

            Assert.That(first.SubscriberCount, Is.Zero, "The previous source must be unsubscribed.");
            Assert.That(second.SubscriberCount, Is.EqualTo(1));

            RaiseActiveInput();
            second.Raise(AppState.Results);

            AssertNeutral(_adapter);
        }

        [Test]
        public void Adapter_UnbindAppStateSource_StopsResetting()
        {
            var source = new FakeAppStateSource();
            _adapter.BindAppStateSource(source);
            _adapter.UnbindAppStateSource();

            Assert.That(source.SubscriberCount, Is.Zero);

            RaiseActiveInput();
            source.Raise(AppState.MainMenu);

            Assert.That(_adapter.Snapshot.IsNeutral, Is.False, "An unsubscribed adapter must not react to transitions.");
        }

        [Test]
        public void Adapter_OnDestroy_UnsubscribesFromStateSource()
        {
            var source = new FakeAppStateSource();
            _adapter.BindAppStateSource(source);

            // EditMode does not raise OnDestroy from DestroyImmediate, so invoke the handler the
            // same way the other lifecycle tests do; the runtime path is Unity's normal callback.
            DestroyHandler.Invoke(_adapter, null);

            Assert.That(source.SubscriberCount, Is.Zero, "Destroying the adapter must release the event subscription.");
        }

        private void RaiseActiveInput()
        {
            ((KeyboardInputState)KeyboardStateField.GetValue(_adapter)).Sample(
                throttleDown: true,
                brakeDown: true,
                steerLeftDown: true,
                steerRightDown: false,
                handbrakeDown: true);

            ((AppInputState)AppIntentStateField.GetValue(_adapter)).Sample(pauseDown: true, retryDown: true);

            Assert.That(_adapter.Snapshot.IsNeutral, Is.False);
            Assert.That(_adapter.PausePressed, Is.True);
            Assert.That(_adapter.RetryPressed, Is.True);
        }

        private static void AssertNeutral(LegacyKeyboardInputAdapter adapter)
        {
            Assert.That(adapter.Snapshot.IsNeutral, Is.True);
            Assert.That(adapter.Throttle, Is.EqualTo(0f));
            Assert.That(adapter.Brake, Is.EqualTo(0f));
            Assert.That(adapter.Steering, Is.EqualTo(0f));
            Assert.That(adapter.Handbrake, Is.False);
            Assert.That(adapter.PausePressed, Is.False);
            Assert.That(adapter.RetryPressed, Is.False);
        }

        private sealed class FakeAppStateSource : IAppStateSource
        {
            public event Action<AppState> StateChanged;

            public int SubscriberCount => StateChanged == null ? 0 : StateChanged.GetInvocationList().Length;

            public void Raise(AppState state)
            {
                StateChanged?.Invoke(state);
            }
        }
    }
}
