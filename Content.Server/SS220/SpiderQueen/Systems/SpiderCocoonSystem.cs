// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.DoAfter;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition.EntitySystems;
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
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderCocoonComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SpiderCocoonComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpiderCocoonComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<SpiderCocoonComponent, CocoonExtractBloodPointsEvent>(OnExtractBloodPoints);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<SpiderCocoonComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (_gameTiming.CurTime < component.NextSecond)
                continue;

            component.NextSecond = _gameTiming.CurTime + TimeSpan.FromSeconds(1);
            if (!_container.TryGetContainer(uid, component.CocoonContainerId, out var container) ||
                container.ContainedEntities is not { } entities ||
                entities.Count <= 0)
                continue;

            foreach (var entity in entities)
            {
                ConvertBloodIntoBloodPoints(uid, component, entity, component.BloodConversionPerSecond);
                CauseCocoonDamage(uid, component, entity);
            }
        }
    }

    private void OnShutdown(Entity<SpiderCocoonComponent> entity, ref ComponentShutdown args)
    {
        var (uid, comp) = entity;
        if (comp.CocoonOwner is null ||
            !TryComp<SpiderQueenComponent>(comp.CocoonOwner, out var queenComponent))
            return;

        queenComponent.CocoonsList.Remove(uid);
        queenComponent.MaxBloodPoints -= entity.Comp.BloodPointsBonus;
    }

    private void OnExamine(Entity<SpiderCocoonComponent> entity, ref ExaminedEvent args)
    {
        if (HasComp<SpiderQueenComponent>(args.Examiner))
        {
            args.PushMarkup(Loc.GetString("spider-cocoon-blood-points-amount", ("amount", entity.Comp.BloodPointsAmount)));
        }
    }

    private void OnAlternativeVerb(EntityUid uid, SpiderCocoonComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess ||
            !TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            return;

        var extractVerb = new AlternativeVerb
        {
            Text = Loc.GetString("spider-cocoon-extract-blood-points-verb"),
            Act = () =>
            {
                var doAfterEventArgs = new DoAfterArgs(EntityManager,
                    args.User,
                    spiderQueen.CocoonExtractTime,
                    new CocoonExtractBloodPointsEvent(),
                    uid,
                    uid)
                {
                    Broadcast = false,
                    BreakOnDamage = false,
                    BreakOnMove = true,
                    NeedHand = false
                };

                _doAfter.TryStartDoAfter(doAfterEventArgs);
            }
        };

        args.Verbs.Add(extractVerb);
    }

    private void OnExtractBloodPoints(Entity<SpiderCocoonComponent> entity, ref CocoonExtractBloodPointsEvent args)
    {
        if (args.Cancelled ||
            !TryComp<SpiderQueenComponent>(args.User, out var spiderQueen))
            return;

        var amountToMax = spiderQueen.MaxBloodPoints - spiderQueen.CurrentBloodPoints;
        var extractedValue = MathF.Min((float)amountToMax, (float)entity.Comp.BloodPointsAmount);
        entity.Comp.BloodPointsAmount -= extractedValue;
        spiderQueen.CurrentBloodPoints += extractedValue;

        _hunger.ModifyHunger(args.User, extractedValue * spiderQueen.HungerExtractCoefficient);

        Dirty(args.User, spiderQueen);
        Dirty(entity);
        _spiderQueen.UpdateAlert((args.User, spiderQueen));
    }

    /// <summary>
    /// Converts entity blood into blood points based on the <see cref="SpiderCocoonComponent.BloodConversionCoefficient"/>
    /// </summary>
    private void ConvertBloodIntoBloodPoints(EntityUid uid, SpiderCocoonComponent component, EntityUid target, FixedPoint2 amount)
    {
        if (!TryComp<BloodstreamComponent>(target, out var bloodstream) ||
            !_solutionContainer.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution))
            return;

        var solutionEnt = bloodstream.BloodSolution.Value;
        if (solutionEnt.Comp.Solution.Volume <= FixedPoint2.Zero)
            return;

        _bloodstream.TryModifyBleedAmount(target, -1f, bloodstream);
        _solutionContainer.SplitSolution(solutionEnt, amount);
        component.BloodPointsAmount += amount * component.BloodConversionCoefficient;
        Dirty(uid, component);
    }

    private void CauseCocoonDamage(EntityUid uid, SpiderCocoonComponent component, EntityUid target)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable) ||
            component.DamagePerSecond is not { } damagePerSecond)
            return;

        DamageSpecifier causedDamage = new();
        foreach (var damage in damagePerSecond.DamageDict)
        {
            var (type, value) = damage;
            if (component.DamageCap.TryGetValue(type, out var cap) &&
                damageable.Damage.DamageDict.TryGetValue(type, out var total) &&
                total >= cap)
                continue;

            causedDamage.DamageDict.Add(type, value);
        }

        _damageable.TryChangeDamage(target, causedDamage, true);
        Dirty(uid, component);
    }
}
