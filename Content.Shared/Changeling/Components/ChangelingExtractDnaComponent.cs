// SS220 Changeling
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChangelingExtractDnaComponent : Component
{
    [DataField]
    public EntProtoId? Action = "ActionChangelingExtractDna";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField]
    public int ChemicalCost = 25;

    public override bool SendOnlyToOwner => true;
}
