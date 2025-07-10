// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.SS220.Damage.Components;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Damage.Systems;

public sealed class StunsContactsSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StunsContactsComponent, StartCollideEvent>(OnEntityEnter);

        SubscribeLocalEvent<DamageOnStunContactComponent, MapInitEvent>(OnDamageOnStunInit);
    }

    private void OnEntityEnter(Entity<StunsContactsComponent> entity, ref StartCollideEvent args)
    {
        if (_timing.InPrediction)
            return;

        if (!entity.Comp.IsActive)
            return;

        var target = args.OtherEntity;

        if (TryEffectEntity(target, entity))
        {
            DebugTools.Assert(entity.Comp.TimeEntitiesStunned.ContainsKey(target));

            if (TryComp<DamageOnStunContactComponent>(entity, out var damageOnStun))
                _damageable.TryChangeDamage(target, GetDamage(target, damageOnStun));
        }
    }

    private bool TryEffectEntity(EntityUid targetUid, Entity<StunsContactsComponent> source)
    {
        if (!source.Comp.TimeEntitiesStunned.TryGetValue(targetUid, out var timeLastStunned)
            && _stun.TryParalyze(targetUid, source.Comp.StunTime, true, null))
        {
            var debugFlag = source.Comp.TimeEntitiesStunned.TryAdd(targetUid, _timing.CurTime);
            DebugTools.Assert(debugFlag);
            return true;
        }

        if (_timing.CurTime < timeLastStunned + source.Comp.StunDelayTime + source.Comp.StunTime)
            return false;

        if (_stun.TryParalyze(targetUid, source.Comp.StunTime, true, null))
        {
            DebugTools.Assert(source.Comp.TimeEntitiesStunned.ContainsKey(targetUid));
            source.Comp.TimeEntitiesStunned[targetUid] = _timing.CurTime;
            return true;
        }

        return false;
    }

    private void OnDamageOnStunInit(Entity<DamageOnStunContactComponent> entity, ref MapInitEvent args)
    {
        if (!HasComp<StunsContactsComponent>(entity))
            Log.Error($"Entity {ToPrettyString(entity)} have {nameof(DamageOnStunContactComponent)} but dont have {nameof(StunsContactsComponent)}");
    }

    private DamageSpecifier GetDamage(EntityUid target, DamageOnStunContactComponent component)
    {
        return component.SpecialDamage is null || !_whitelist.IsWhitelistPass(component.SpecialDamageWhitelist, target)
                ? component.Damage
                : component.SpecialDamage;
    }
}
