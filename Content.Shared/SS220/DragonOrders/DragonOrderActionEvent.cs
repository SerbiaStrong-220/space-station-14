// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DragonOrders;

[Serializable, NetSerializable]
public enum DragonOrderType : byte
{
    Destroy,
    Guard,
}

public sealed partial class DragonOrderActionEvent : EntityTargetActionEvent
{
    [DataField(required: true)]
    public DragonOrderType Type;
}
