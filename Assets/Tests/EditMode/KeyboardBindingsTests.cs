using NUnit.Framework;
using UnityEngine;
using VelocityRush.Gameplay;

namespace VelocityRush.Tests.EditMode
{
    /// <summary>
    /// EditMode guard tests for the provisional key map (T5.1.1): P is the pause intent,
    /// R is retry, and Escape stays unbound/reserved for overlay-close. This verifies the
    /// binding definition, not live key polling (which needs PlayMode or a browser).
    /// </summary>
    public sealed class KeyboardBindingsTests
    {
        [Test]
        public void Pause_IsP_AndNotEscape()
        {
            Assert.That(KeyboardBindings.Pause, Is.EqualTo(KeyCode.P));
            Assert.That(KeyboardBindings.Pause, Is.Not.EqualTo(KeyCode.Escape), "Escape is reserved for overlay-close (TDD §37.2).");
        }

        [Test]
        public void Retry_IsR_AndNotEscape()
        {
            Assert.That(KeyboardBindings.Retry, Is.EqualTo(KeyCode.R));
            Assert.That(KeyboardBindings.Retry, Is.Not.EqualTo(KeyCode.Escape));
        }

        [Test]
        public void DrivingBindings_MatchPrototypeLayout()
        {
            Assert.That(KeyboardBindings.ThrottlePrimary, Is.EqualTo(KeyCode.W));
            Assert.That(KeyboardBindings.ThrottleAlternate, Is.EqualTo(KeyCode.UpArrow));
            Assert.That(KeyboardBindings.BrakePrimary, Is.EqualTo(KeyCode.S));
            Assert.That(KeyboardBindings.BrakeAlternate, Is.EqualTo(KeyCode.DownArrow));
            Assert.That(KeyboardBindings.SteerLeftPrimary, Is.EqualTo(KeyCode.A));
            Assert.That(KeyboardBindings.SteerLeftAlternate, Is.EqualTo(KeyCode.LeftArrow));
            Assert.That(KeyboardBindings.SteerRightPrimary, Is.EqualTo(KeyCode.D));
            Assert.That(KeyboardBindings.SteerRightAlternate, Is.EqualTo(KeyCode.RightArrow));
            Assert.That(KeyboardBindings.Handbrake, Is.EqualTo(KeyCode.Space));
        }

        [Test]
        public void NoActiveBinding_UsesEscape()
        {
            Assert.That(KeyboardBindings.ThrottlePrimary, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.ThrottleAlternate, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.BrakePrimary, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.BrakeAlternate, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.SteerLeftPrimary, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.SteerLeftAlternate, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.SteerRightPrimary, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.SteerRightAlternate, Is.Not.EqualTo(KeyCode.Escape));
            Assert.That(KeyboardBindings.Handbrake, Is.Not.EqualTo(KeyCode.Escape));
        }
    }
}
