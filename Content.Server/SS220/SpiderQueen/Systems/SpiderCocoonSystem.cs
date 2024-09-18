// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.DoAfter;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.SS220.SpiderQueen;
using Content.Shared.SS220.SpiderQueen.Components;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server.SS220.SpiderQueen.Systems;

public sealed partial class SpiderCocoonSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly SpiderQueenSystem _spiderQueen = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderCocoonComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpiderCocoonComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpiderCocoonComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<SpiderCocoonComponent, CocoonExtractManaEvent>(OnExtractMana);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderCocoonComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime < component.NextSecond)
                return;

            component.NextSecond = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
            UpdateManaAmount(uid, component);
        }
    }

    private void OnShutdown(Entity<SpiderCocoonComponent> entity, ref ComponentShutdown args)
    {
        var (uid, comp) = entity;
        if (comp.CocoonOwner is null ||
            !TryComp<SpiderQueenComponent>(comp.CocoonOwner, out var queenComponent))
            return;

        queenComponent.CocoonsList.Remove(uid);
        _spiderQueen.UpdateCocoonsBonus(uid);
    }

    private void OnExamine(Entity<SpiderCocoonComponent> entity, ref ExaminedEvent args)
    {
        if (HasComp<SpiderQueenComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString("spider-cocoon-mana-amount", ("amount", entity.Comp.ManaAmount)));
        }
    }

    private void OnAlternativeVerb(EntityUid uid, SpiderCocoonComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess ||
            !TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            return;

        var extractVerb = new AlternativeVerb
        {
            Text = Loc.GetString("spider-cocoon-extract-mana-verb"),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    spiderQueen.ExtractManaTime,
                    new CocoonExtractManaEvent(),
                    uid,
                    uid)
                {
                    Broadcast = false,
                    BreakOnDamage = false,
                    BreakOnMove = true,
                    NeedHand = false,
                    BlockDuplicate = true,
                    CancelDuplicate = true,
                    DuplicateCondition = DuplicateConditions.SameEvent
                };

                _doAfter.TryStartDoAfter(doAfterEventArgs);
            }
        };

        args.Verbs.Add(extractVerb);
    }

    private void OnExtractMana(Entity<SpiderCocoonComponent> entity, ref CocoonExtractManaEvent args)
    {
        if (args.Cancelled ||
            !TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            return;

        var amountToMax = spiderQueen.MaxMana - spiderQueen.CurrentMana;
        spiderQueen.CurrentMana += MathF.Min((float)amountToMax, (float)entity.Comp.ManaAmount);
        entity.Comp.ManaAmount -= MathF.Min((float)amountToMax, (float)entity.Comp.ManaAmount);

        Dirty(args.User, spiderQueen);
        Dirty(entity.Owner, entity.Comp);
    }

    private void UpdateManaAmount(EntityUid uid, SpiderCocoonComponent component)
    {
        if (!_container.TryGetContainer(uid, component.CocoonContainerId, out var container) ||
            container.ContainedEntities is not { } entities ||
            entities.Count <= 0)
            return;

        foreach (var entity in entities)
        {
            if (!TryComp<DamageableComponent>(entity, out var damageable))
                continue;

            var canDamage = true;
            foreach (var damageType in component.DamageCap)
            {
                var (type, value) = damageType;
                if (damageable.Damage.DamageDict.TryGetValue(type, out var total) &&
                    total >= value)
                {
                    canDamage = false;
                    break;
                }
            }

            if (!canDamage)
                continue;

            if (component.DamagePerSecond is { } damagePerSecond)
                _damageable.TryChangeDamage(entity, damagePerSecond);

            component.ManaAmount += component.ManaByEntity;
        }

        Dirty(uid, component);
    }
}
