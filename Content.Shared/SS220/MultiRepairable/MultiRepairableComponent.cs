// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Repairable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MultiRepairableComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<RepairOption> Options = new();

    [DataField, AutoNetworkedField]
    public float SelfRepairPenalty = 3f;

    [DataField, AutoNetworkedField]
    public bool AllowSelfRepair = true;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class RepairOption
{
    [DataField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    [DataField]
    public DamageSpecifier? Damage;

    [DataField]
    public float FuelCost = 5f;

    [DataField]
    public float DoAfterDelay = 1f;
}