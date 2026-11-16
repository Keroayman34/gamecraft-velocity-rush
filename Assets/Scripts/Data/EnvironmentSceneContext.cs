using UnityEngine;

namespace VelocityRush.Data
{
    /// <summary>
    /// Scene-local binding between a generated environment scene and its
    /// <see cref="EnvironmentDefinition"/> (T-9.5, D-057).
    /// </summary>
    /// <remarks>
    /// The component stores a reference only. It exists so a scene can be audited for correct
    /// wiring without a resources lookup or a new loading strategy decision (D-035 remains open).
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class EnvironmentSceneContext : MonoBehaviour
    {
        [Tooltip("Definition this generated scene represents.")]
        [SerializeField] private EnvironmentDefinition definition;

        /// <summary>The referenced environment definition.</summary>
        public EnvironmentDefinition Definition => definition;

        /// <summary>Validates that the scene has a valid definition reference.</summary>
        public bool TryValidate(out string error)
        {
            if (definition == null)
            {
                error = "EnvironmentSceneContext: Definition reference is missing.";
                return false;
            }

            if (!definition.TryValidate(out var definitionError))
            {
                error = "EnvironmentSceneContext: Definition is invalid: " + definitionError;
                return false;
            }

            error = null;
            return true;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only authoring seam used by the deterministic scene builder.</summary>
        public void ConfigureForAuthoring(EnvironmentDefinition value)
        {
            definition = value;
        }
#endif
    }
}
