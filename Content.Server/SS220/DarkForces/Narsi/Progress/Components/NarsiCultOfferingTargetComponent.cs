namespace Content.Server.SS220.DarkForces.Narsi.Progress.Components;

[RegisterComponent]
public sealed partial class NarsiCultOfferingTargetComponent : Component
{
    [DataField]
    public List<EntityUid> Objectives = new();
}
