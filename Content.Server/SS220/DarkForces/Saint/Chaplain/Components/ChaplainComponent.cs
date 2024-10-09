using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain.Components;

[RegisterComponent]
public sealed partial class ChaplainComponent : Component
{
    [DataField]
    public EntProtoId NarsiExileAction = "ActionChaplainNarsiExile";

    [DataField]
    public EntityUid? NarsiExileActionEntity;

    // Fel damage is unused in our build
    // [DataField]
    // public EntProtoId GreatPrayerAction = "ActionChaplainGreatPrayer";

    // [DataField]
    // public EntityUid? GreatPrayerActionEntity;

    [DataField]
    public EntProtoId DefenceBarrierAction = "ActionChaplainDefenceBarrier";

    [DataField]
    public EntityUid? DefenceBarrierActionEntity;

    [DataField]
    public EntProtoId ExorcismAction = "ActionChaplainExorcism";

    [DataField]
    public EntityUid? ExorcismActionEntity;

    [DataField]
    public SoundSpecifier GreatPrayerSound = new SoundPathSpecifier("/Audio/SS220/DarkForces/Chaplain/great_prayer.ogg");

    [DataField]
    public EntityUid? GreatPrayerSoundEntity;

    [DataField]
    public DamageSpecifier FelHealDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Fel", -20}
        }
    };
}
