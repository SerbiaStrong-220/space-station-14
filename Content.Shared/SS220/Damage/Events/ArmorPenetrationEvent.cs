using Content.Shared.Damage;
using Robust.Shared.GameObjects;

namespace Content.Shared.SS220.Damage.Events;

/// <summary>
/// Modifies damage according to resistances coefficients
/// should be called before all armor damage modifiers
/// </summary>
[ByRefEvent]
public record struct APDamageModifyEvent
{
    public readonly EntityUid Target;
    public readonly EntityUid? Source;
    public readonly EntityUid? Origin;
    public DamageSpecifier Damage;

    public APDamageModifyEvent(
        EntityUid target,
        DamageSpecifier damage,
        EntityUid? source = null,
        EntityUid? origin = null)
    {
        Target = target;
        Damage = damage;
        Source = source;
        Origin = origin;
    }
}
