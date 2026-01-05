using Content.Shared.Hands.Components;
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
    public bool CreampieResist = false;

    [DataField]
    [AutoNetworkedField]
    public bool ProjectileResist = false;
}
