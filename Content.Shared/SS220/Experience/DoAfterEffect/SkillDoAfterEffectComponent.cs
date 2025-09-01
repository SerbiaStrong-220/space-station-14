// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it abstract nature to match DoAfterEvents and base functions
namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
[RegisterComponent]
public abstract partial class BaseSkillDoAfterEffectComponent : Component
{
    [DataField]
    public float? DurationScale = null;

    [DataField]
    public float? FailureChance = null;

    [DataField]
    public bool? FullBlock = null;
}
