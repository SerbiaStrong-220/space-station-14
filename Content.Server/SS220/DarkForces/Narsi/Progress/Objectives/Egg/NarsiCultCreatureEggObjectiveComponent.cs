using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Egg;

[RegisterComponent]
public sealed partial class NarsiCultCreatureEggObjectiveComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId? CreatureId;

    [DataField(required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntProtoId> AvailableCreatures;
}
