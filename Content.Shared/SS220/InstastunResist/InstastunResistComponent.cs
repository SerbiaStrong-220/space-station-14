using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.InstastunResist;

/// <summary>
/// This is used for change damage for activating weapon
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class InstastunResistComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Active = false;

    [DataField]
    [AutoNetworkedField]
    public bool CreampieResist = false;

    [DataField]
    [AutoNetworkedField]
    public bool ProjectileResist = false;
}
