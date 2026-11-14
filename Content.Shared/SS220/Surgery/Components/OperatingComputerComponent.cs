// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
    [AutoNetworkedField]
    public float TableSearchRange = 2.5f;

    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<SurgeryGraphPrototype>> UnlockedSurgeries = new();
}
