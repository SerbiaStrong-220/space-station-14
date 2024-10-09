using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Stealth;

[RegisterComponent]
public sealed partial class NarsiCultistStealthComponent : Component
{
    [DataField("tickToRemove", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan TickToRemove = TimeSpan.Zero;
}
