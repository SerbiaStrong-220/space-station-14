// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ClinkGlasses;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedClinkGlassesSystem))]
public sealed partial class ClinkGlassesInitiatorComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    [AutoNetworkedField]
    public HashSet<EntityUid> Items = [];

    /// <summary>
    /// Minimum time that must pass after clink action before this entity can do it again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField]
    public float Cooldown = 1.0f;

    /// <summary>
    /// Time when the cooldown will have elapsed and the entity can clink again.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextClinkTime = TimeSpan.Zero;
}
