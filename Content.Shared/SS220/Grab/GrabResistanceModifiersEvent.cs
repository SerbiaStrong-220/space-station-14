namespace Content.Shared.SS220.Grab;

public sealed partial class GrabResistanceModifiersEvent : EntityEventArgs
{
    public readonly EntityUid Grabbable;
    public Dictionary<GrabStage, float> CurrentStageBreakoutChance { get; private set; }

    public GrabResistanceModifiersEvent(EntityUid grabbable, Dictionary<GrabStage, float> currentStageBreakoutChance)
    {
        Grabbable = grabbable;
        CurrentStageBreakoutChance = currentStageBreakoutChance;
    }

    public void ModifyStage(GrabStage stage, float modifier)
    {
        if (!CurrentStageBreakoutChance.TryGetValue(stage, out var current))
            return;

        CurrentStageBreakoutChance[stage] = current * modifier;
    }

    public void ModifyAll(float modifier)
    {
        foreach (var (key, val) in CurrentStageBreakoutChance)
        {
            CurrentStageBreakoutChance[key] = val * modifier;
        }
    }
}
