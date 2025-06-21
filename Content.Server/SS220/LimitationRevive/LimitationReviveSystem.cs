// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits;
using Content.Server.Zombies;
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
        SubscribeLocalEvent<LimitationReviveComponent, MobStateChangedEvent>(OnMobStateChanged, before: [typeof(ZombieSystem)]);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<LimitationReviveComponent, AddReviweDebuffsEvent>(OnAddReviweDebuffs);
    }

    private void OnMobStateChanged(Entity<LimitationReviveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DamageTime = _timing.CurTime + ent.Comp.BeforeDamageDelay;
        }

        if (args.OldMobState == MobState.Dead)
        {
            ent.Comp.DamageTime = null;
            ent.Comp.DeathCounter++;
        }
    }

    private void OnAddReviweDebuffs(Entity<LimitationReviveComponent> ent, ref AddReviweDebuffsEvent args)
    {
        //rn i am too tired to check if this ok
        if (!_random.Prob(ent.Comp.ChanceToAddTrait))
            return;

        var traitString = _prototype.Index<WeightedRandomPrototype>(ent.Comp.WeightListProto).Pick(_random);

        var traitProto = _prototype.Index<TraitPrototype>(traitString);

        if (traitProto.Components is not null)
            _entityManager.AddComponents(ent, traitProto.Components, false);
    }

    private void OnCloning(Entity<LimitationReviveComponent> ent, ref CloningEvent args)
    {
        var targetComp = EnsureComp<LimitationReviveComponent>(args.CloneUid);
        _serialization.CopyTo(ent.Comp, ref targetComp, notNullableOverride: true);

        targetComp.DeathCounter = 0;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LimitationReviveComponent>();

        while (query.MoveNext(out var ent, out var limitationRevive))
        {
            if (limitationRevive.DamageTime is null)
                continue;

            if (_timing.CurTime < limitationRevive.DamageTime)
                continue;

            _damageableSystem.TryChangeDamage(ent, limitationRevive.Damage, true);

            limitationRevive.DamageTime = null;
        }
    }
}
