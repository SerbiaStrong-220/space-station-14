// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public sealed class LimitationReviveSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, UpdateMobStateEvent>(OnDeadMobState);
        SubscribeLocalEvent<LimitationReviveComponent, RevivingCorpseEvent>(OnAlive);
    }

    private void OnDeadMobState(Entity<LimitationReviveComponent> ent, ref UpdateMobStateEvent args)
    {
        if(args.State != MobState.Dead)
            return;

        if(ent.Comp.IsAlreadyDead)
            return;

        ent.Comp.IsAlreadyDead = true;
        ent.Comp.IsDamageTaken = false;

        ent.Comp.EndTimeToDamage = _timing.CurTime + ent.Comp.TimeToDamage;
    }

    private void OnAlive(Entity<LimitationReviveComponent> ent, ref RevivingCorpseEvent args)
    {
        ent.Comp.IsAlreadyDead = false;
        ent.Comp.IsDamageTaken = false;
    }
    /// <summary>
    /// Attempt to damage and add a negative trait after death. Damage and Trait can only be received once per death.
    /// </summary>
    public void TryDamageAfterDeath(EntityUid uid)
    {
        if(!TryComp<LimitationReviveComponent>(uid, out var reviveComp))
            return;

        if(reviveComp.IsDamageTaken || reviveComp.IsAlreadyDead == false)
            return;

        reviveComp.CounterOfDead++;
        reviveComp.IsDamageTaken = true;

        if(!TryComp<DamageableComponent>(uid, out var damageComp))
            return;

        _damageableSystem.TryChangeDamage(uid, reviveComp.TypeDamageOnDead, true);

        var tryAddTraitAfterDeath = _random.NextFloat(0.0f, 1.0f);

        if (tryAddTraitAfterDeath < reviveComp.ChanceToAddTrait ) {

            var traitString = _prototype.Index<WeightedRandomPrototype>(reviveComp.WeightListProto)
                .Pick(_random);

            var traitProto = _prototype.Index<TraitPrototype>(traitString);

            if (traitProto.Components is not null)
                _entityManager.AddComponents(uid, traitProto.Components, false);

        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<LimitationReviveComponent>();
        var curTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var limitationRevive))
            if (curTime >= limitationRevive.EndTimeToDamage &&
                limitationRevive.EndTimeToDamage != TimeSpan.Zero &&
                limitationRevive.IsDamageTaken == false)
                TryDamageAfterDeath(uid);
    }
}
