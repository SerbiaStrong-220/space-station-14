// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.InstastunResist;

/// <summary>
/// This is used for giving entities instastun resist
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
    public List<StunSource> ResistedStunTypes = new List<StunSource>();
}
