// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

// THE only reason if DoAfter living in on folder and namespace is it "WA DA IS IT" nature
namespace Content.Shared.SS220.Experience.DoAfterEffect;

/// <summary>
/// This component hold data for changing DoAfter events parameters started by entity with <see cref="ExperienceComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class SkillDoAfterEffectContainerComponent : Component
{
    /// <summary>
    /// Key is typeof(DoAfterEvent).Guid
    /// Value is effects that should be applied to events with type Guid
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<Guid, DoAfterEffect> EffectContainer;
}
