using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities;

[RegisterComponent]
public sealed partial class RatvarEnchantmentableComponent : Component
{
    [DataField]
    public EntityUid? ActionEntity;

    [DataField]
    public string? ActionId;

    [DataField(customTypeSerializer: typeof(TimespanSerializer))]
    public TimeSpan DisableAbilityTick = TimeSpan.Zero;

    [DataField]
    public List<RatvarEnchantment> Enchantments = new();

    [DataField]
    public bool IsEnchantmentActive;

    [DataField]
    public string ActiveVisuals = string.Empty;
}

[ImplicitDataDefinitionForInheritors]
public sealed partial class RatvarEnchantment
{
    [DataField(required: true)]
    public string Action = string.Empty;

    [DataField(required: true)]
    public SpriteSpecifier Icon = SpriteSpecifier.Invalid;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField(required: true)]
    public string Visuals = string.Empty;
}
