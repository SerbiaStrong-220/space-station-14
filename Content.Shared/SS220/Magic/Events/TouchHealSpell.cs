using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Shared.Magic.Events;

public sealed partial class TouchHealSpellEvent : EntityTargetActionEvent
{
    /// <summary>
    /// Heal by touch
    /// </summary>

    [DataField]
    public DamageSpecifier Heal = new();

    [DataField]
    public float BloodlossModifier;

    [DataField]
    public float ModifyBloodLevel;

    [DataField]
    public float ModifyStamina;

    [DataField]
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(2.5);
}
