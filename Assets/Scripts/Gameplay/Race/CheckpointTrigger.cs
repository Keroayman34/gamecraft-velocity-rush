using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Ordered prototype checkpoint gate. Reports contact to the assigned
    /// <see cref="RaceController"/>; it does not judge validity itself (TDD §29 S4).
    /// </summary>
    /// <remarks>
    /// The serialized <see cref="Index"/> is the stable race order (0-based, contiguous). The
    /// controller rejects out-of-order and duplicate reports, so multiple colliders on the same
    /// vehicle or repeated contacts cannot advance progress incorrectly. Trigger geometry is
    /// content; this component only reports the contact.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField, Min(0)] private int index;
        [SerializeField] private RaceController race;

        /// <summary>Stable 0-based checkpoint index reported to the race.</summary>
        public int Index => index;

        private void OnTriggerEnter(Collider other)
        {
            if (race == null || other == null)
            {
                return;
            }

            // The race performs the authoritative participant identity check.
            race.ReportCheckpoint(index, other.attachedRigidbody);
        }
    }
}
