// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Server.Zombies;
using Content.Shared.Cloning.Events;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Rejuvenate;
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
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<LimitationReviveComponent, MobStateChangedEvent>(OnMobStateChanged, before: [typeof(ZombieSystem)]);
        SubscribeLocalEvent<LimitationReviveComponent, CloningEvent>(OnCloning);
        SubscribeLocalEvent<LimitationReviveComponent, AddReviweDebuffsEvent>(OnAddReviweDebuffs);
		SubscribeLocalEvent<LimitationReviveComponent, RejuvenateEvent>(OnRejuvenate);
		SubscribeLocalEvent<LimitationReviveComponent, ApplyMetabolicMultiplierEvent>(OnApplyMetabolicMultiplier);
    }

    private void OnMobStateChanged(Entity<LimitationReviveComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DamageTime = _timing.CurTime + ent.Comp.BeforeDamageDelay;
            return;
        }

        if (args.OldMobState == MobState.Dead)
        {
            if (ent.Comp.DamageTime == null)//is null if we got brain dmg
                ent.Comp.DeathCounter++;
            else
                ent.Comp.DamageTime = null;
        }
    }

    private void OnAddReviweDebuffs(Entity<LimitationReviveComponent> ent, ref AddReviweDebuffsEvent args)
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
            if (limitationRevive.DamageTime is null)
                continue;

            if (_timing.CurTime < limitationRevive.DamageTime)
                continue;

            _damageableSystem.TryChangeDamage(ent, limitationRevive.Damage, true);

            TryAddTrait((ent, limitationRevive));

            limitationRevive.DamageTime = null;
        }
    }

    private void OnApplyMetabolicMultiplier(Entity<LimitationReviveComponent> ent, ref ApplyMetabolicMultiplierEvent args)
    {
        if (ent.Comp.DamageTime is null)//update timer before damage, cause we cant get current metabolism from anywhere
        {
            if (args.Apply)
                ent.Comp.BeforeDamageDelay *= args.Multiplier * ent.Comp.MetabolismModifierAffect;
            else
                ent.Comp.BeforeDamageDelay /= args.Multiplier * ent.Comp.MetabolismModifierAffect;

            return;
        }

        var newTime = (ent.Comp.DamageTime - _timing.CurTime);

        if (args.Apply)
            newTime *= args.Multiplier * ent.Comp.MetabolismModifierAffect;
        else
            newTime /= args.Multiplier * ent.Comp.MetabolismModifierAffect;

        ent.Comp.DamageTime = _timing.CurTime + newTime;
    }
}
