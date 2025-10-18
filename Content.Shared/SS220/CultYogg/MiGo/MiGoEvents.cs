// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.CultYogg.Buildings;

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
public sealed class MiGoErectBuildMessage : BoundUserInterfaceMessage
{
    public ProtoId<CultYoggBuildingPrototype> BuildingId;
    public NetCoordinates Location;
    public Direction Direction;
}

[Serializable, NetSerializable]
public sealed class MiGoErectEraseMessage : BoundUserInterfaceMessage
{
    public NetEntity BuildingFrame;
}

[Serializable, NetSerializable]
public sealed class MiGoErectCaptureMessage : BoundUserInterfaceMessage
{
    public NetEntity CapturedBuilding;
}
