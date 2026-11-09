using System;
using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Pairs one physics <see cref="WheelCollider"/> with its separate visual transform
    /// (TDD §36.6 "visual pose sync"). The two are intentionally different objects so the
    /// WheelCollider's raycast/suspension pose can drive the visual without feeding back.
    /// </summary>
    [Serializable]
    public sealed class WheelBinding
    {
        [SerializeField] private WheelCollider collider;
        [SerializeField] private Transform visual;

        /// <summary>The physics wheel. Never a renderer.</summary>
        public WheelCollider Collider => collider;

        /// <summary>The transform whose pose is copied from <see cref="Collider"/> each frame.</summary>
        public Transform Visual => visual;
    }
}
