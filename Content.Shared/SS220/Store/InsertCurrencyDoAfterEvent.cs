// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Store.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Store;

[Serializable, NetSerializable]
public sealed partial class InsertCurrencyDoAfterEvent : DoAfterEvent
{
    public NetEntity? TargetOverride;

    public InsertCurrencyDoAfterEvent(NetEntity? targetOverride)
    {
        TargetOverride = targetOverride;
    }

    public override DoAfterEvent Clone() => this;
}
