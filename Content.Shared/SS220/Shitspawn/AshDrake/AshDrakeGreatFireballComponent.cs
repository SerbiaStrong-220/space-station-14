// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AshDrakeGreatFireballComponent : Component
{
    [DataField]
    public float FireballSpeed = 5f;

    [DataField]
    public EntProtoId FireballProto = "ProjectileAshDrakeGreatFireball";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/Shitspawn/AshDrake/fireball_great.ogg");

    [DataField]
    public EntProtoId ActionId = "ActionAshDrakeGreatFireball";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;
}

public sealed partial class AshDrakeGreatFireballActionEvent : WorldTargetActionEvent { }
