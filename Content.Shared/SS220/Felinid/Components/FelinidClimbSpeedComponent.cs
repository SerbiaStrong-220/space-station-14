namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class FelinidClimbSpeedComponent : Component
{
    [DataField]
    public float DelayMultiplier = 0.6667f;
}
