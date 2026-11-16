using UnityEngine;
using VelocityRush.Core;

namespace VelocityRush.Gameplay.AI
{
    /// <summary>
    /// Exposes an <see cref="AIDriver"/> as a normalized <see cref="IVehicleInput"/> (T-10.2.2).
    /// </summary>
    /// <remarks>
    /// The kinematic driver integrates its own pose in Stage 2, but the command surface is kept
    /// interface-compatible so a future physics-backed AI could bind this adapter to a
    /// <c>VehicleController</c> without changing the AI or the input contract. The adapter itself
    /// does no computation and allocates nothing: every property is a cached field read.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class AIVehicleInput : MonoBehaviour, IVehicleInput
    {
        [SerializeField] private AIDriver driver;

        /// <inheritdoc />
        public float Throttle => driver != null ? driver.Throttle : 0f;

        /// <inheritdoc />
        public float Brake => driver != null ? driver.Brake : 0f;

        /// <inheritdoc />
        public float Steering => driver != null ? driver.Steering : 0f;

        /// <inheritdoc />
        public bool Handbrake => false;

        /// <summary>The bound AI driver, or null.</summary>
        public AIDriver Driver => driver;

        private void Awake()
        {
            if (driver == null)
            {
                driver = GetComponent<AIDriver>();
            }
        }

        /// <summary>Binds a driver explicitly; used by tests and future grid spawning.</summary>
        public bool TryBind(AIDriver source)
        {
            if (source == null)
            {
                return false;
            }

            driver = source;
            return true;
        }
    }
}
