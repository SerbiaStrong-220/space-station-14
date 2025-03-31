// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Spray.Events;

/// <summary>
/// Raised on a spray when it would like to take the specified amount of ammo.
/// </summary>
public sealed class TakeSolutionEvent : EntityEventArgs
{
    public readonly EntityUid? User;

    /// <summary>
    /// How much solution u wanna take
    /// </summary>
    public byte SolutionAmount { get; }

}
