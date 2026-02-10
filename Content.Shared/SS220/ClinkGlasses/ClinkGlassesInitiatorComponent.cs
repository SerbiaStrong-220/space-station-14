// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ClinkGlasses;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedClinkGlassesSystem))]
public sealed partial class ClinkGlassesInitiatorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<EntityUid> Items = [];

    /// <summary>
    /// Minimum time that must pass after clink action before this entity can do it again.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time when the cooldown will have elapsed and the entity can clink again.
    /// </summary>
    [DataField]
    public TimeSpan NextClinkTime;
}
