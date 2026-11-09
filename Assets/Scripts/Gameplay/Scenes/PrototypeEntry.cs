using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay
{
    /// <summary>
    /// Temporary prototype entry point: after the Bootstrap composition root completes its
    /// startup sequence, request the additive TestTrack scene (T5.2.1).
    /// </summary>
    /// <remarks>
    /// Deliberately a separate component (not part of <see cref="Bootstrapper"/>) so the
    /// prototype entry is explicit, isolated and reversible: remove this component to return
    /// Bootstrap to a content-free root. It is NOT the commercial GameFlow and must not grow
    /// into one. Requires a <see cref="Bootstrapper"/> on the same GameObject.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Bootstrapper))]
    public sealed class PrototypeEntry : MonoBehaviour
    {
        private const string LogPrefix = "[VelocityRush.Prototype] ";

        private Bootstrapper _bootstrapper;

        private void Awake()
        {
            _bootstrapper = GetComponent<Bootstrapper>();
        }

        private void Start()
        {
            // Start runs after every Awake, so the Bootstrapper's exactly-once initialization
            // has already completed (or failed) by this point.
            if (_bootstrapper == null)
            {
                Debug.LogError(LogPrefix + "Bootstrapper missing; TestTrack will not load.", this);
                return;
            }

            if (!_bootstrapper.StartupSucceeded)
            {
                // Fail closed: never load content when boot composition failed.
                Debug.LogWarning(LogPrefix + "Bootstrap startup did not complete; TestTrack not loaded.", this);
                return;
            }

            if (!PrototypeSceneLoader.TryLoadTestTrack(out var error))
            {
                Debug.LogError(LogPrefix + error, this);
            }
        }
    }
}
