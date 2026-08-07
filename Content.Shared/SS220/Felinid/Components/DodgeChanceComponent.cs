namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class DodgeChanceComponent : Component
{
    [DataField]
    public float BaseDodgeChance = 0.15f;

    [DataField]
    public float CriticalHealthMultiplier = 0.5f;

    [DataField]
    public float MinorNeedMultiplier = 0.95f;

    [DataField]
    public float MajorNeedMultiplier = 0.8f;
}
