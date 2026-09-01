// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Grab;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class GrabbableComponent : Component
{
    [MemberNotNullWhen(true, nameof(GrabbedBy))]
    public bool Grabbed => GrabbedBy != null && GrabbedBy.Value.IsValid();

    [DataField, AutoNetworkedField]
    public EntityUid? GrabbedBy;

    [DataField, AutoNetworkedField]
    public GrabStage GrabStage = GrabStage.None;

    [DataField, AutoNetworkedField]
    public string? GrabJointId;

    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "Grabbed";

    [DataField]
    public LocId BreakFreePopup = "grabbable-component-break-free";
}
