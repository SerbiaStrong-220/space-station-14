namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Building;

[RegisterComponent]
public sealed partial class NarsiCultBuildingObjectiveComponent : Component
{
    [DataField(required: true, serverOnly: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public NarsiBuilding BuildingType;
}
