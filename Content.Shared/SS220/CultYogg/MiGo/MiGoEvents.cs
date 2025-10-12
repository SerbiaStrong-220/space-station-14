// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGo;

[Serializable, NetSerializable]
public sealed partial class MiGoSacrificeDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class MiGoEnslaveDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class AfterMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}

[Serializable, NetSerializable]
public sealed partial class AfterDeMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone()
    {
        return this;
    }
}

[ByRefEvent, Serializable]
public record struct CultYoggEnslavedEvent(EntityUid? Target);

[Serializable, NetSerializable]
public sealed class MiGoTeleportToTargetMessage(NetEntity? target) : BoundUserInterfaceMessage
{
    public NetEntity? Target = target;
}

[Serializable, NetSerializable]
public sealed class MiGoSpectateMessage(NetEntity? target) : BoundUserInterfaceMessage
{
    public NetEntity? Target = target;
}
