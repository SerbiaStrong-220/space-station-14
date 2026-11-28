// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.LimitationRevive;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// The person lives in the brain: deaths, brain rot and brain damage leave the body together with the mind.
/// </summary>
public sealed partial class LimitationReviveSystem
{
    [Dependency] private MobStateSystem _mobState = default!;

    private static readonly ProtoId<DamageTypePrototype> CerebralDamage = "Cerebral";

    private void InitializeBrain()
    {
        SubscribeLocalEvent<LimitationReviveComponent, OrganRemovedFromEvent>(OnBrainRemoved);
        SubscribeLocalEvent<LimitationReviveComponent, OrganInsertedIntoEvent>(OnBrainInserted);
    }

    private void OnBrainRemoved(Entity<LimitationReviveComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (!HasComp<BrainComponent>(args.Organ) || TerminatingOrDeleted(args.Organ) || TerminatingOrDeleted(ent))
            return;

        var state = EnsureComp<BrainReviveStateComponent>(args.Organ);
        state.DeathCounter = ent.Comp.DeathCounter;
        // a brain taken out of a living body is clinically dead from now on
        state.DamageCountingTime = _mobState.IsDead(ent) ? ent.Comp.DamageCountingTime : TimeSpan.Zero;
        state.CerebralDamage = GetCerebralDamage(ent);

        ent.Comp.DeathCounter = 0;
        ent.Comp.DamageCountingTime = null;
        SetCerebralDamage(ent, FixedPoint2.Zero);
        Dirty(ent);
    }

    private void OnBrainInserted(Entity<LimitationReviveComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (!TryComp<BrainReviveStateComponent>(args.Organ, out var state))
            return;

        ent.Comp.DeathCounter = state.DeathCounter;
        ent.Comp.DamageCountingTime = _mobState.IsDead(ent) ? state.DamageCountingTime : null;
        SetCerebralDamage(ent, state.CerebralDamage);
        Dirty(ent);

        RemComp(args.Organ, state);
    }

    private FixedPoint2 GetCerebralDamage(EntityUid uid)
    {
        return _damageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(CerebralDamage, out var damage)
            ? damage
            : FixedPoint2.Zero;
    }

    private void SetCerebralDamage(EntityUid uid, FixedPoint2 damage)
    {
        var delta = damage - GetCerebralDamage(uid);
        if (delta == FixedPoint2.Zero)
            return;

        var change = new DamageSpecifier();
        change.DamageDict.Add(CerebralDamage, delta);
        _damageableSystem.TryChangeDamage(uid, change, ignoreResistances: true, interruptsDoAfters: false);
    }
}
