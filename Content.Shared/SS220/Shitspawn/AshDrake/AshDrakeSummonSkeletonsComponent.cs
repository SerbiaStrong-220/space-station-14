// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AshDrakeSummonSkeletonsComponent : Component
{
    [DataField] public int Count = 6;
    [DataField] public float SpawnRadius = 1.5f;
    [DataField] public EntProtoId ActionId = "ActionAshDrakeSummonSkeletons";
    [DataField, AutoNetworkedField] public EntityUid? ActionEntity;
}

public sealed partial class AshDrakeSummonSkeletonsActionEvent : InstantActionEvent { }
