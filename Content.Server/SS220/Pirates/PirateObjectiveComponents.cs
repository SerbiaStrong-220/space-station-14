namespace Content.Server.SS220.Pirates;

[RegisterComponent, Access(typeof(PirateObjectiveSystem))]
public sealed partial class PirateLootValueConditionComponent : Component
{
    [DataField]
    public int Target = 1_000_000;
}

[RegisterComponent, Access(typeof(PirateObjectiveSystem))]
public sealed partial class PirateCrewCaptureConditionComponent : Component
{
    [DataField]
    public int Target = 3;

    [ViewVariables]
    public List<EntityUid> Targets = new();
}
