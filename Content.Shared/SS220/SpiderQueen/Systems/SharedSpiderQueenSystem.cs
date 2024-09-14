// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.SpiderQueen.Components;
using Robust.Shared.Network;

namespace Content.Shared.SS220.SpiderQueen.Systems;

public abstract class SharedSpiderQueenSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpiderQueenComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<SpiderQueenComponent, SpiderCocooningActionEvent>(OnCocooningAction);
    }

    private void OnExamine(Entity<SpiderQueenComponent> entity, ref ExaminedEvent args)
    {
        if (args.Examined == args.Examiner)
        {
            args.PushMarkup(Loc.GetString("spider-queen-mana-amount",
                ("current", entity.Comp.CurrentMana.Int()), ("max", entity.Comp.MaxMana.Int())));
        }
    }

    private void OnCocooningAction(Entity<SpiderQueenComponent> entity, ref SpiderCocooningActionEvent args)
    {
        if (args.Handled)
            return;

        var performer = args.Performer;
        var target = args.Target;

        foreach (var entityInRange in _entityLookup.GetEntitiesInRange(target, entity.Comp.CocoonsMinDistance))
        {
            if (!HasComp<SpiderCocoonComponent>(entityInRange))
                continue;

            _popup.PopupEntity(Loc.GetString("cocooning-too-close"), performer, performer);
            return;
        }

        if (!_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("cocooning-target-not-dead"), performer, performer);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("cocooning-target-not-humanoid"), performer, performer);
            return;
        }

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            args.CocooningTime,
            new AfterCocooningEvent(),
            performer,
            target
        )
        {
            Broadcast = false,
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(doAfterArgs);
        if (started)
        {
            args.Handled = true;
        }
        else
        {
            if (_net.IsClient)
                Log.Error($"Failed to start DoAfter by {performer}");

            return;
        }
    }
}
