// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Zombies;
using Content.Shared.Body.Events;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
using Content.Shared.SS220.LimitationRevive;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;

namespace Content.Server.SS220.LimitationRevive;

/// <summary>
/// This handles limiting the number of defibrillator resurrections
/// </summary>
public sealed class LimitationReviveSystem : SharedLimitationReviveSystem
{
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ISerializationManager _serialization = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, MobStateChangedEvent>(OnMobStateChanged, before: [typeof(ZombieSystem)]);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<LimitationReviveComponent, AddReviveDebuffsEvent>(OnAddReviweDebuffs);
        SubscribeLocalEvent<LimitationReviveComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<LimitationReviveComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnMobStateChanged(Entity<LimitationReviveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DamageCountingTime = TimeSpan.Zero;
            return;
        }

        if (args.OldMobState == MobState.Dead)
        {
            if (ent.Comp.DamageCountingTime == null)//is null if we got brain dmg
                ent.Comp.DeathCounter++;
            else
                ent.Comp.DamageCountingTime = null;
        }
    }

    private void OnAddReviweDebuffs(Entity<LimitationReviveComponent> ent, ref AddReviveDebuffsEvent args)
    {
        TryAddTrait(ent);
    }

    public bool TryAddTrait(Entity<LimitationReviveComponent> ent)
    {
        //rn i am too tired to check if this ok
        if (!_random.Prob(ent.Comp.ChanceToAddTrait))
            return false;

        var traitString = _prototype.Index<WeightedRandomPrototype>(ent.Comp.WeightListProto).Pick(_random);

        var traitProto = _prototype.Index<TraitPrototype>(traitString);

        if (traitProto.Components is null)
            return false;

        foreach (var comp in traitProto.Components)
        {
            var reg = _componentFactory.GetRegistration(comp.Key);

            if (_entityManager.HasComponent(ent, reg))
            {
                return false;
            }
        }

        ent.Comp.RecievedDebuffs.Add(traitString);
        _entityManager.AddComponents(ent, traitProto.Components, false);
        return true;
    }

    private void OnCloning(Entity<LimitationReviveComponent> ent, ref CloningEvent args)
    {
        var targetComp = EnsureComp<LimitationReviveComponent>(args.CloneUid);
        _serialization.CopyTo(ent.Comp, ref targetComp, notNullableOverride: true);

        targetComp.DeathCounter = 0;
    }

    private void OnRejuvenate(Entity<LimitationReviveComponent> ent, ref RejuvenateEvent args)
    {
        ent.Comp.DeathCounter = 0;
        ClearAllRecievedDebuffs(ent);
    }

    public void ClearAllRecievedDebuffs(Entity<LimitationReviveComponent> ent)
    {
        foreach (var debufName in ent.Comp.RecievedDebuffs)
        {
            var debufProto = _prototype.Index<TraitPrototype>(debufName);

            if (debufProto.Components is not null)
                _entityManager.RemoveComponents(ent, debufProto.Components);
        }

        ent.Comp.RecievedDebuffs = [];
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<LimitationReviveComponent>();

        while (query.MoveNext(out var ent, out var limitationRevive))
        {
            if (limitationRevive.DamageCountingTime is null)
                continue;

            limitationRevive.DamageCountingTime += TimeSpan.FromSeconds(frameTime / limitationRevive.UpdateIntervalMultiplier);

            if (limitationRevive.DamageCountingTime < limitationRevive.BeforeDamageDelay)
                continue;

            _damageableSystem.TryChangeDamage(ent, limitationRevive.Damage, true);

            TryAddTrait((ent, limitationRevive));

            limitationRevive.DamageCountingTime = null;
        }
    }

    private void OnApplyMetabolicMultiplier(Entity<LimitationReviveComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        ent.Comp.UpdateIntervalMultiplier = args.Multiplier;
    }

    public override void IncreaseTimer(EntityUid ent, TimeSpan addTime)
    {
        if (!TryComp<LimitationReviveComponent>(ent, out var limComp))
            return;

        if (limComp.DamageCountingTime == null)
            return;

        // TODO-SS220: please make it logic to adjust time passed and not the time start point
        limComp.DamageCountingTime -= addTime;
    }
}
