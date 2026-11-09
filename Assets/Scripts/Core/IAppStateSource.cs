using System;

namespace VelocityRush.Core
{
    /// <summary>
    /// Read-only source of application-state transitions (TDD §29 S2/§30.2).
    /// </summary>
    /// <remarks>
    /// <para>
    /// GameFlow owns the concrete FSM and raises <see cref="StateChanged"/> after a transition
    /// succeeds. This interface is the narrow observer seam input adapters depend on, so they
    /// never reference GameFlow's concrete type (dependency inversion; TDD §37.1/§37.3).
    /// </para>
    /// <para>
    /// The event signature matches the documented GameFlow public API
    /// (<c>StateChanged(AppState)</c>). Subscribers must not transition state; they react only.
    /// </para>
    /// </remarks>
    public interface IAppStateSource
    {
        /// <summary>Raised once for every accepted application-state transition.</summary>
        event Action<AppState> StateChanged;
    }
}
