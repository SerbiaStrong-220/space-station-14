// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
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
