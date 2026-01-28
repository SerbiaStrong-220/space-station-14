// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Hands.Components;
using Content.Shared.SS220.InstastunResist;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.InstastunResistOnActiveBlocking;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class InstastunResistOnActiveBlockingComponent: Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Active = false;

    [DataField]
    [AutoNetworkedField]
    public List<StunSource> ResistedStunTypes = new List<StunSource>();
}
