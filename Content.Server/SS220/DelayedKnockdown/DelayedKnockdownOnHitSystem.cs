using Content.Server.Stunnable;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Inventory;
using Content.Shared.Timing;
using Robust.Shared.Timing;
using Content.Shared.SS220.DelayedKnockdown;
using System.Linq;

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
        SubscribeLocalEvent<DelayedKnockdownImmunityComponent, BeforeDelayedKnockdownEvent>(OnCancelDelayedKnockdownDirect);
        SubscribeLocalEvent<DelayedKnockdownImmunityComponent, InventoryRelayedEvent<BeforeDelayedKnockdownEvent>>(OnCancelDelayedKnockdownRelayed);
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
    private void OnCancelDelayedKnockdownDirect(Entity<DelayedKnockdownImmunityComponent> ent, ref BeforeDelayedKnockdownEvent args)
    {
        args.Cancelled = true;
    }

    private void OnCancelDelayedKnockdownRelayed(Entity<DelayedKnockdownImmunityComponent> ent, ref InventoryRelayedEvent<BeforeDelayedKnockdownEvent> args)
    {
        if (ent.Comp.Worn)
            args.Args.Cancelled = true;
    }

    private void OnMeleeHit(EntityUid uid, DelayedKnockdownOnHitComponent component, MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!component.OnHeavyAttack && args.Direction != null)
            return;

        if (TryComp<UseDelayComponent>(uid, out var useDelay))
            _useDelay.TryResetDelay((uid, useDelay), id: component.UseDelay);

        foreach (var hitEntity in args.HitEntities)
        {
            if (HasComp<ActiveDelayedKnockdownComponent>(hitEntity)) //no stacking of multiple knockdowns
                continue;

            var cancelEvent = new BeforeDelayedKnockdownEvent(Value: 0f);
            RaiseLocalEvent(hitEntity, ref cancelEvent); //knockdown immunity component check

            if (cancelEvent.Cancelled)
                continue;

            var activeComp = EnsureComp<ActiveDelayedKnockdownComponent>(hitEntity);

            var delayTime = component.Delay;
            var knockdownTime = component.Duration;

            var staminaEvent = new BeforeStaminaDamageEvent(1f);
            RaiseLocalEvent(hitEntity, ref staminaEvent);

            float currentResistance = staminaEvent.Value;

            // Apply modifiers based on resistance thresholds
            var sortedModifiers = component.ResistanceModifiers
                .OrderByDescending(kvp => kvp.Key)
                .ToList();

            foreach (var (threshold, (delayBonus, knockdownPenalty)) in sortedModifiers)
            {
                if (currentResistance >= threshold)
                {
                    if (delayTime > TimeSpan.Zero) //if KnockdownDelay = 0, it means that knockdown should be instant, so no bonus for ya
                        delayTime += delayBonus;
                    knockdownTime -= knockdownPenalty;
                    break;
                }
            }

            knockdownTime = TimeSpan.FromTicks(Math.Max(TimeSpan.FromSeconds(0.5).Ticks, knockdownTime.Ticks)); //0,5 sec is min knockdown time

            activeComp.Delay = _gameTiming.CurTime + delayTime;
            activeComp.KnockdownTime = knockdownTime;
            activeComp.Refresh = component.Refresh;
            activeComp.AutoStand = component.AutoStand;
            activeComp.Drop = component.Drop;
        }
    }
}
