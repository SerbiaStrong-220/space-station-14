namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class DodgeChanceComponent : Component
{
    [DataField]
    public float BaseDodgeChance = 0.15f;

    [DataField]
    public float ExcellentHealthBonus = 0.025f;

    [DataField]
    public float BadHealthPenalty = 0.025f;

    [DataField]
    public float AwfulHealthPenalty = 0.05f;

    [DataField]
    public float MinorNeedPenalty = 0.05f;

    [DataField]
    public float MajorNeedPenalty = 0.10f;

    /// <summary>
    /// Damage as a share of the crit threshold (0 = healthy, 1 = crit) below which health is "excellent".
    /// </summary>
    [DataField]
    public float GoodHealthThreshold = 0.25f;

    /// <summary>
    /// Damage share above which health is "bad", and above <see cref="AwfulHealthThreshold"/> "awful".
    /// </summary>
    [DataField]
    public float BadHealthThreshold = 0.5f;

    [DataField]
    public float AwfulHealthThreshold = 0.75f;
}
