// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent]
public sealed partial class AshDrakeCursedChalkComponent : Component
{
    [DataField]
    public EntProtoId RuneProto = "AshDrakeSkeletonRune";

    [DataField]
    public float CastTime = 3f;
}

[Serializable, NetSerializable]
public sealed partial class AshDrakeCursedChalkDoAfterEvent : DoAfterEvent
{
    public NetCoordinates ClickLocation;

    public AshDrakeCursedChalkDoAfterEvent(NetCoordinates clickLocation)
    {
        ClickLocation = clickLocation;
    }

    public override DoAfterEvent Clone() => new AshDrakeCursedChalkDoAfterEvent(ClickLocation);
}
