// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DeviceLinking;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OperatingComputerComponent : Component
{
    [DataField]
    public string DiskSlotId = "surgery_disk_slot";

    [DataField]
    public ProtoId<SourcePortPrototype> TablePort = "OperatingTableSender";

    [DataField]
    [AutoNetworkedField]
    public float MaxLinkDistance = 5f;

    [ViewVariables]
    [AutoNetworkedField]
    public EntityUid? LinkedTable;

    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<SurgeryGraphPrototype>> UnlockedSurgeries = new();

    [ViewVariables]
    public HashSet<ProtoId<SurgeryGraphPrototype>> BaseUnlockedSurgeries = new();
}
