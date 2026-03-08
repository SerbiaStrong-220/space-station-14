// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AstralLeap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AstralLeapComponent : Component
{
    [DataField]
    public EntProtoId AstralAction = "ActionMiGoAstral";//ToDo_SS220 make it required Datafield?

    [ViewVariables, AutoNetworkedField]
    public EntityUid? AstralActionEntity;
}
