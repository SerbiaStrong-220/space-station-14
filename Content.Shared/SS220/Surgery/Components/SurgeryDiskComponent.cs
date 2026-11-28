// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Surgery.Components;

/// <summary>
/// In the disk slot of an operating computer it opens the steps of its protocols on the linked table.
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SurgeryDiskComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<SurgeryProtocolPrototype>> Protocols = new();
}
