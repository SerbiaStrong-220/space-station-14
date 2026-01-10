// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

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
    public Dictionary<string, bool> ResistedStunTypes = new Dictionary<string, bool>();
}
