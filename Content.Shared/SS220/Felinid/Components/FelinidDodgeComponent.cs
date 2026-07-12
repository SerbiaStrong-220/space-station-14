namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class FelinidDodgeComponent : Component
{
    [DataField]
    public float BaseDodgeChance = 0.125f;

    [DataField]
    public float ExcellentHealthBonus = 0.025f;

    [DataField]
    public float PoorHealthPenalty = 0.025f;

    [DataField]
    public float TerribleHealthPenalty = 0.05f;

    [DataField]
    public float MinorNeedPenalty = 0.05f;

    [DataField]
    public float MajorNeedPenalty = 0.10f;

    [DataField]
    public float MaxDodgeChance = 0.15f;
}
