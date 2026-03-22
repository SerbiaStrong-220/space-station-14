// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AshDrakeMeteorComponent : Component
{
    [DataField] public EntProtoId ProjectileProto = "AshDrakeFireMeteorFalling";
    [DataField] public int Count = 20;
    [DataField] public float Speed = 8f;
    [DataField] public float Radius = 8f;
    [DataField] public float SpawnHeight = 10f;
    [DataField] public float SpawnInterval = 0.15f;
    [DataField] public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/Shitspawn/AshDrake/meteor.ogg");

    [DataField] public EntProtoId ActionId = "ActionAshDrakeFireMeteor";
    [DataField, AutoNetworkedField] public EntityUid? ActionEntity;
}

public sealed partial class AshDrakeMeteorActionEvent : InstantActionEvent { }
