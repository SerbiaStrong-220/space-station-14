// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class AshDrakeFlyComponent : Component
{
    [DataField]
    public float FlySpeed = 18f;

    [DataField]
    public int LavaRadius = 3;

    [DataField]
    public float LavaChance = 0.75f;

    [DataField]
    public EntProtoId LavaProto = "AshDrakeFlyLava";

    [DataField]
    public SoundSpecifier FlySound = new SoundPathSpecifier("/Audio/SS220/Shitspawn/AshDrake/fly.ogg");

    [DataField]
    public EntProtoId ActionId = "ActionAshDrakeFly";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsFlying;
}

public sealed partial class AshDrakeFlyActionEvent : WorldTargetActionEvent { }
