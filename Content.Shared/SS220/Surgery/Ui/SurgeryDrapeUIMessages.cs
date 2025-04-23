// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed class SurgeryDrapeUpdate(NetEntity user, NetEntity target) : BoundUserInterfaceState
{
    public NetEntity User { get; } = user;
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class StartSurgeryMessage(ProtoId<SurgeryGraphPrototype> id, NetEntity target, NetEntity? user) : BoundUserInterfaceMessage
{
    public ProtoId<SurgeryGraphPrototype> SurgeryGraphId { get; } = id;
    public NetEntity Target { get; } = target;
    public NetEntity? User { get; } = user;
}

public sealed class StartSurgeryEvent(ProtoId<SurgeryGraphPrototype> id, NetEntity target, NetEntity user) : CancellableEntityEventArgs
{
    public ProtoId<SurgeryGraphPrototype> SurgeryGraphId { get; } = id;
    public NetEntity Target { get; } = target;
    public NetEntity User { get; } = user;
}
