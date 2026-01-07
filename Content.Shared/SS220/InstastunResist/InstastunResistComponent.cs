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
    public Dictionary<string,bool> ResistedStunTypes = new Dictionary<string,bool>();
}
