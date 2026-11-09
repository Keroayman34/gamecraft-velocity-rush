using UnityEngine;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Prototype finish gate for the one-way timed run. Reports contact to the assigned
    /// <see cref="RaceController"/>; the race decides whether the attempt may finish
    /// (TDD §29 S4/S5).
    /// </summary>
    /// <remarks>
    /// The finish is accepted only while running and after every required checkpoint. Repeated
    /// contacts, including multiple vehicle colliders in the same frame, cannot emit more than one
    /// result.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class FinishTrigger : MonoBehaviour
    {
        [SerializeField] private RaceController race;

        private void OnTriggerEnter(Collider other)
        {
            if (race == null || other == null)
            {
                return;
            }

            race.ReportFinish(other.attachedRigidbody);
        }
    }
}
