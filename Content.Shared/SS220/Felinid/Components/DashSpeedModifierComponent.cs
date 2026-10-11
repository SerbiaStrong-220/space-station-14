using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(DashSpeedModifierSystem))]
public sealed partial class DashSpeedModifierComponent : Component
{
    [DataField]
    public EntProtoId<InstantActionComponent> Action = "ActionDashSpeedModifier";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public float SpeedModifier = 1.3f;

    [DataField]
    public float HungerCost = 40f;

    [DataField]
    public float ThirstCost = 40f;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan? CostAt;
}
