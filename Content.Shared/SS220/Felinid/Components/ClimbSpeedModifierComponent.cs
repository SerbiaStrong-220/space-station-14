namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class ClimbSpeedModifierComponent : Component
{
    [DataField]
    public float DelayMultiplier = 0.6667f;
}
