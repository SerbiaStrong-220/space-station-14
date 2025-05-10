// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
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
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
    }

    private void OnMobStateChanged(Entity<LimitationReviveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DamageTime = _timing.CurTime + ent.Comp.DelayBeforeDamage;
        }
        else ent.Comp.DamageTime = null;
        /*
        if (args.NewMobState == MobState.Dead && ent.Comp.IsAlreadyDead == false)
        {
            ent.Comp.IsAlreadyDead = true;
            ent.Comp.IsDamageTaken = false;

            ent.Comp.DamageTime = _timing.CurTime + ent.Comp.DelayBeforeDamage;
        }

        else if (ent.Comp.IsAlreadyDead && args.NewMobState is MobState.Alive or MobState.Critical)
        {
            ent.Comp.IsAlreadyDead = false;
            ent.Comp.IsDamageTaken = false;
        }
        */
    }

    private void OnCloning(Entity<LimitationReviveComponent> entity, ref CloningEvent args)
    {
        var targetComp = EnsureComp<LimitationReviveComponent>(args.CloneUid);
        _serialization.CopyTo(entity.Comp, ref targetComp, notNullableOverride: true);

        targetComp.IsDamageTaken = false;
        targetComp.IsAlreadyDead = false;
        targetComp.DeathCounter = 0;
    }

    /// <summary>
    /// Attempt to damage and add a negative trait after death. Damage and Trait can only be received once per death.
    /// </summary>
    public void TryDamageAfterDeath(EntityUid uid)
    {
        if (!TryComp<LimitationReviveComponent>(uid, out var reviveComp))
            return;

        if (reviveComp.IsDamageTaken || reviveComp.IsAlreadyDead == false)
            return;

        reviveComp.DeathCounter++;
        reviveComp.IsDamageTaken = true;

        if (!TryComp<DamageableComponent>(uid, out var damageComp))
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

        while (query.MoveNext(out var uid, out var limitationRevive))
        {
            if (limitationRevive.DamageTime is null)
                return;

            if (_timing.CurTime < limitationRevive.DamageTime)
                return;

            if (!limitationRevive.IsDamageTaken) // ??
                return;

            if (!limitationRevive.IsAlreadyDead) // ??
                return;

            TryDamageAfterDeath(uid);
        }
    }
}
