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
    /// Dead-percentage (0 = healthy, 1 = dead) below which health counts as "excellent".
    /// </summary>
    [DataField]
    public float GoodHealthThreshold = 0.25f;

    /// <summary>
    /// Dead-percentage above which health counts as "bad", and above <see cref="AwfulHealthThreshold"/> as "awful".
    /// </summary>
    [DataField]
    public float BadHealthThreshold = 0.5f;

    [DataField]
    public float AwfulHealthThreshold = 0.75f;
}
