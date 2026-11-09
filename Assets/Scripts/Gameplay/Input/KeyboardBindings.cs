using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Provisional fixed legacy-Input key bindings (TDD §37.2/§37.7). Centralized so the adapter
    /// and the EditMode tests agree on one mapping, and so a future rebinding surface (Settings,
    /// OQ-A03) has a single place to start. Bindings are prototype-only and not final.
    /// </summary>
    /// <remarks>
    /// <see cref="KeyCode"/> values are positional, so non-QWERTY layouts (for example Arabic)
    /// drive the same physical keys. <see cref="KeyCode.Escape"/> is deliberately absent: it is
    /// reserved for overlay-close and must never be a pause dependency (TDD §37.2/§37.6).
    /// </remarks>
    public static class KeyboardBindings
    {
        /// <summary>Primary throttle key.</summary>
        public const KeyCode ThrottlePrimary = KeyCode.W;

        /// <summary>Alternate throttle key.</summary>
        public const KeyCode ThrottleAlternate = KeyCode.UpArrow;

        /// <summary>Primary brake key.</summary>
        public const KeyCode BrakePrimary = KeyCode.S;

        /// <summary>Alternate brake key.</summary>
        public const KeyCode BrakeAlternate = KeyCode.DownArrow;

        /// <summary>Primary steer-left key.</summary>
        public const KeyCode SteerLeftPrimary = KeyCode.A;

        /// <summary>Alternate steer-left key.</summary>
        public const KeyCode SteerLeftAlternate = KeyCode.LeftArrow;

        /// <summary>Primary steer-right key.</summary>
        public const KeyCode SteerRightPrimary = KeyCode.D;

        /// <summary>Alternate steer-right key.</summary>
        public const KeyCode SteerRightAlternate = KeyCode.RightArrow;

        /// <summary>Handbrake key.</summary>
        public const KeyCode Handbrake = KeyCode.Space;

        /// <summary>Pause intent key (dedicated; Escape stays reserved for overlay-close).</summary>
        public const KeyCode Pause = KeyCode.P;

        /// <summary>Retry intent key.</summary>
        public const KeyCode Retry = KeyCode.R;
    }
}
