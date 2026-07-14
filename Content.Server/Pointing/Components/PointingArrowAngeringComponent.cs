namespace Content.Server.Pointing.Components;

/// <summary>
/// Causes pointing arrows to go mode and murder this entity.
/// </summary>
[RegisterComponent]
public sealed partial class PointingArrowAngeringComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("remainingAnger")]
    public int RemainingAnger = 5;

    // SS220 Fix PointingArrowAngering range begin
    [DataField]
    public float MaxRange = 15f;
    // SS220 Fix PointingArrowAngering range end
}
