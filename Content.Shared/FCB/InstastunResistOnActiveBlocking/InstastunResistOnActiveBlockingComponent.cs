// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXTt
using Content.Shared.Hands.Components;
using Content.Shared.FCB.InstastunResist;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.FCB.InstastunResistOnActiveBlocking;

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
