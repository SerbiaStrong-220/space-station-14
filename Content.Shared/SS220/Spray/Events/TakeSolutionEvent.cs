using Robust.Shared.Map;
using Content.Shared.Weapons.Ranged;

namespace Content.Shared.SS220.Spray.Events;

/// <summary>
/// Raised on a gun when it would like to take the specified amount of ammo.
/// </summary>
public sealed class TakeSolutionEvent : EntityEventArgs
{
    public readonly EntityUid? User;
    public byte SolutionAmount { get; }

    /// <summary>
    /// If no ammo returned what is the reason for it?
    /// </summary>
    public string? Reason;

    public TakeSolutionEvent(EntityUid? user, byte solutionAmount)
    {
    }

}
