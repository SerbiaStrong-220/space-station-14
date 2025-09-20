// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions
namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
public abstract partial class BaseSkillDoAfterEffectComponent : Component
{
    [DataField]
    public float DurationScale = 1f;

    [DataField]
    public float FailureChance = 0f;

    [DataField]
    public bool FullBlock = false;

    [DataField]
    public LocId? FailurePopup = null;

    [DataField]
    public LocId? FullBlockPopup = null;
}
