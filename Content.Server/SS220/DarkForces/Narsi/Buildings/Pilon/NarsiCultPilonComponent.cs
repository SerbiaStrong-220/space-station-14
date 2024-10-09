using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.GameRules.Cult.Narsi.Buildings.Pilon;

[RegisterComponent]
public sealed partial class NarsiCultPilonComponent : Component
{
    [DataField("healingDamage")]
    public DamageSpecifier HealingDamage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            {"Blunt", -7},
            {"Slash", -7},
            {"Piercing", -7},
            {"Heat", -7},
            {"Cold", -7},
            {"Shock", -7}
        }
    };

    [DataField("lastTick", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan LastTick;
}
