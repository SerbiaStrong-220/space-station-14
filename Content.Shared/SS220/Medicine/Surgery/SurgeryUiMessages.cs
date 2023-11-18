// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Body.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Medicine.Surgery;

[Serializable, NetSerializable]
public sealed partial class InstrumentUsedAfterInteractEvent : BoundUserInterfaceMessage
{
    public NetEntity Target { get; set; }

    public InstrumentUsedAfterInteractEvent(NetEntity target)
    {
        Target = target;
    }
}

[Serializable, NetSerializable]
public sealed partial class SelectorButtonPressed : BoundUserInterfaceMessage
{
    public NetEntity TargetId { get; set; }
    public SelectorButtonPressed(NetEntity targetid)
    {
        TargetId = targetid;
    }
}