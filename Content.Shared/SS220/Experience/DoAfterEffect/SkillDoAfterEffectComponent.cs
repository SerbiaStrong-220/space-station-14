// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;

// THE only reason if DoAfter living in on folder and namespace is it "WA DA IS IT" nature
namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class SkillDoAfterEffectComponent : Component
{
    [DataField(required: true)]
    public Dictionary<DoAfterEvent, DoAfterEffect> Effect;
}

[DataDefinition]
public abstract partial class DoAfterEffect
{
    [DataField]
    public float? DurationScale = null;

    [DataField]
    public float? FailureChance = null;

    [DataField]
    public bool? FullBlock = null;
}
