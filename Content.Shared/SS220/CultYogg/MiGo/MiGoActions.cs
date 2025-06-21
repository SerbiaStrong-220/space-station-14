// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Damage;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.CultYogg.MiGo;
public sealed partial class MiGoEnslavementActionEvent : EntityTargetActionEvent
{
}

public sealed partial class MiGoHealEvent : EntityTargetActionEvent
{
    [DataField]
    public DamageSpecifier Heal = new();

    [DataField]
    public float BloodlossModifier;

    [DataField]
    public float ModifyBloodLevel;

    [DataField]
    public float ModifyStamina;

    [DataField]
    public TimeSpan TimeBetweenIncidents = TimeSpan.FromSeconds(2.5); // most balanced value

    [DataField]
    public SpriteSpecifier.Rsi EffectSprite = new(new("SS220/Effects/cult_yogg_healing.rsi"), "healingEffect");
}

public sealed partial class MiGoAstralEvent : InstantActionEvent
{
}

public sealed partial class MiGoErectEvent : InstantActionEvent
{
}

public sealed partial class MiGoSacrificeEvent : InstantActionEvent
{
}
