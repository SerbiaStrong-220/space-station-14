using Content.Shared.Damage;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// This handles...
/// </summary>
public sealed class TrapSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly SharedEnsnareableSystem _ensnareableSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<TrapComponent, StartCollideEvent>(EnsnareableOnContactCollide);
    }

    private void EnsnareableOnContactCollide(Entity<TrapComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (_entityWhitelist.IsBlacklistPass(ent.Comp.Blacklist, args.OtherEntity))
            return;

        if(ent.Comp.IsSlammed)
            return;

        if (!TryComp<StatusEffectsComponent>(args.OtherEntity, out var status))
            return;

        if (ent.Comp.DurationStun != TimeSpan.Zero)
        {
            _stunSystem.TryStun(args.OtherEntity, ent.Comp.DurationStun, true, status);
            _stunSystem.TryKnockdown(args.OtherEntity, ent.Comp.DurationStun, true, status);
        }

        if (!TryComp<EnsnaringComponent>(ent.Owner, out var ensnaring))
            return;

        _ensnareableSystem.TryEnsnare(args.OtherEntity, ent.Owner, ensnaring);

        if(!HasComp<DamageableComponent>(args.OtherEntity))
            return;

        if (!TryComp<EnsnareableComponent>(args.OurEntity, out var ensnareable))
            return;

        _damageableSystem.TryChangeDamage(args.OtherEntity, ent.Comp.DamageOnTrapped, true);

        //Todo: урон химикатами

        ent.Comp.IsSlammed = true;


    }
}
