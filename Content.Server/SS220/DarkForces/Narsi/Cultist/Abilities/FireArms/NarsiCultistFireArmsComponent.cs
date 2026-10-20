using Content.Shared.Damage;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.FireArms;

[RegisterComponent]
public sealed partial class NarsiCultistFireArmsComponent : Component
{
    [DataField("tickToRemove", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TickToRemove = TimeSpan.Zero;

    [DataField("additionDamage")]
    public DamageSpecifier DamageSpecifier = new();

    [DataField("canFireTargets")]
    public bool CanFireTargets;
}
