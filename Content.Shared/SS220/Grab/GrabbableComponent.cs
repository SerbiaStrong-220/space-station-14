using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrabbableComponent : Component
{
    public bool Grabbed => GrabbedBy != null && GrabbedBy.Value.IsValid();

    [DataField, AutoNetworkedField]
    public EntityUid? GrabbedBy;

    [DataField, AutoNetworkedField]
    public GrabStage GrabStage = GrabStage.None;

    [DataField, AutoNetworkedField]
    public string? GrabJointId;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "Grabbed";
}
