// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent, NetworkedComponent]
public sealed partial class MalfAiActorComponent : Component
{
    [ViewVariables]
    public List<EntityUid> GrantedActions = new();

    [ViewVariables]
    public EntityUid? Mind;

    [ViewVariables]
    public bool GrantedThermalVision;

    [ViewVariables]
    public bool HadRemoteStore;

    [ViewVariables]
    public EntityUid? PreviousRemoteStore;

    [ViewVariables]
    public EntityUid? BoundStore;
}
