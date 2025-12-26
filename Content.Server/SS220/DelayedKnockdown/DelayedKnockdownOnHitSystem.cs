using Content.Server.Stunnable;
using Content.Shared.Damage.Events;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Timing;
using Robust.Shared.Timing;
using Content.Shared.SS220.DelayedKnockdown;

namespace Content.Server.SS220.DelayedKnockdown;

public sealed class DelayedKnockdownOnHitSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DelayedKnockdownOnHitComponent, MeleeHitEvent>(OnMeleeHit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveDelayedKnockdownComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Delay > _gameTiming.CurTime)
                continue;

            _stunSystem.TryKnockdown(
                uid,
                comp.KnockdownTime,
                refresh: comp.Refresh,
                autoStand: comp.AutoStand,
                drop: comp.Drop,
                force: true
            );

            RemCompDeferred<ActiveDelayedKnockdownComponent>(uid);
        }
    }

    private void OnMeleeHit(EntityUid uid, DelayedKnockdownOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!component.OnHeavyAttack && args.Direction != null)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
        {
            _useDelay.TryResetDelay((uid, useDelay), id: component.UseDelay);
        }

        foreach (var hitEntity in args.HitEntities)
        {
            var activeComp = EnsureComp<ActiveDelayedKnockdownComponent>(hitEntity);
            
            var delayTime = component.Delay;
            var knockdownTime = component.Duration;
           
            var staminaEvent = new BeforeStaminaDamageEvent(1f);
            RaiseLocalEvent(hitEntity, ref staminaEvent);
            
            if (staminaEvent.Value <= component.ResistanceThreshold)
            {
                delayTime += component.ResistanceDelayBonus;
                knockdownTime -= component.ResistanceKnockdownPenalty;
            }

            activeComp.Delay = _gameTiming.CurTime + delayTime;
            activeComp.KnockdownTime = knockdownTime;
            activeComp.Refresh = component.Refresh;
            activeComp.AutoStand = component.AutoStand;
            activeComp.Drop = component.Drop;
        }
    }
}
