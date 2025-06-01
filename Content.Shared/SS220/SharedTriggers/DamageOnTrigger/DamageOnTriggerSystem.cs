// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.SharedTriggers.DamageOnTrigger;

/// <summary>
/// This handles deals damage when triggered
/// </summary>
public sealed class DamageOnTriggerSystem : EntitySystem
{

    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DamageOnTriggerComponent, SharedTriggerEvent.SharedTriggerEvent>(OnCollide);
    }

    private void OnCollide(Entity<DamageOnTriggerComponent> ent, ref SharedTriggerEvent.SharedTriggerEvent args)
    {
        if (ent.Comp.Damage == null)
            return;

        if (!HasComp<DamageableComponent>(args.User))
            return;

        _damageableSystem.TryChangeDamage(args.User, ent.Comp.Damage, true);
    }
}
