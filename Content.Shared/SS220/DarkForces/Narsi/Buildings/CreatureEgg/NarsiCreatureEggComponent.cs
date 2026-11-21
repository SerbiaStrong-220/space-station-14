using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.CreatureEgg;

[RegisterComponent]
public sealed partial class NarsiCreatureEggComponent : Component
{
    [DataField]
    public TimeSpan CreatureNextStepTick;

    [DataField]
    public List<CreatureStep> CreatureSteps = new();

    [DataField]
    public CreatureStep? CurrentStep;
}

[Serializable]
[DataDefinition]
public sealed partial class CreatureStep
{
    [DataField]
    public EntProtoId? EntityProtoId;

    [DataField]
    public CreatureStage Stage;

    [DataField]
    public TimeSpan Delay;
}

[Serializable, NetSerializable]
public enum CreatureStage : byte
{
    StageOne,
    StageTwo,
    StageThree,
    StageFour
}

[Serializable, NetSerializable]
public enum CreatureVisuals : byte
{
    Egg
}
