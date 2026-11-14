namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Offering;

[RegisterComponent]
public sealed partial class NarsiCultOfferObjectiveComponent : Component
{
    [DataField]
    [ViewVariables(VVAccess.ReadWrite)]
    public EntityUid? Target;
}
