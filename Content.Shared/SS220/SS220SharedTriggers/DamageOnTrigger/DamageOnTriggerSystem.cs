// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.SS220.SS220SharedTriggers.Events;

namespace Content.Shared.SS220.SS220SharedTriggers.DamageOnTrigger;

/// <summary>
/// This handles deals damage when triggered
/// </summary>
public sealed class DamageOnTriggerSystem : EntitySystem
{

    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnTriggerComponent, SharedTriggerEvent>(OnTriggered);
    }

    private void OnTriggered(Entity<DamageOnTriggerComponent> ent, ref SharedTriggerEvent args)
    {
        if (ent.Comp.Damage == null || !args.Activator.HasValue)
            return;

        if (!HasComp<DamageableComponent>(args.Activator.Value))
            return;

        _damageableSystem.TryChangeDamage(args.Activator, ent.Comp.Damage, true);
    }
}
