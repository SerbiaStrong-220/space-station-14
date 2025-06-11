// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Corruption;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.CultYogg.Cultists;

public abstract class SharedCultYoggSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentStartup>(OnCompInit);

        SubscribeLocalEvent<CultYoggComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemActionEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemInHandActionEvent>(CorruptItemInHandAction);

        SubscribeLocalEvent<CultYoggComponent, ComponentRemove>(OnRemove);
    }

    protected virtual void OnCompInit(Entity<CultYoggComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.CorruptItemActionEntity, uid.Comp.CorruptItemAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemInHandActionEntity, uid.Comp.CorruptItemInHandAction);
        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _timing.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }

    #region Stage
    private void OnExamined(EntityUid uid, CultYoggComponent component, ExaminedEvent args)
    {
        if (component.CurrentStage < CultYoggStage.Reveal)
            return;

        if (TryComp<InventoryComponent>(uid, out var item)
            && _inventory.TryGetSlotEntity(uid, "eyes", out _, item))
            return;

        if (_inventory.TryGetSlotEntity(uid, "head", out var itemHead, item))
        {
            if (TryComp(itemHead, out IdentityBlockerComponent? block)
                && (block.Coverage == IdentityBlockerCoverage.EYES || block.Coverage == IdentityBlockerCoverage.FULL))
                return;
        }

        if (_inventory.TryGetSlotEntity(uid, "mask", out var itemMask, item))
        {
            if (TryComp(itemMask, out IdentityBlockerComponent? block)
                && (block.Coverage == IdentityBlockerCoverage.EYES || block.Coverage == IdentityBlockerCoverage.FULL))
            {
                return;
            }
        }

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-stage-eyes-markups", ("ent", uid))}[/color]");
    }
    #endregion

    #region Corruption
    private void CorruptItemAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemActionEvent args)
    {
        if (args.Handled)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-corrupt-already-corrupted"), args.Target, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, uid.Comp, args.Target, false))
        {
            return;
        }
        args.Handled = true;

        Spawn(uid.Comp.CorruptionEffect, Transform(args.Target).Coordinates);
    }

    private void CorruptItemInHandAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemInHandActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_entityManager.TryGetComponent<HandsComponent>(uid, out var hands))
            return;

        if (hands.ActiveHand == null)
            return;

        var handItem = hands.ActiveHand.HeldEntity;
        if (handItem == null)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(handItem.Value))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-corrupt-already-corrupted"), handItem.Value, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, uid.Comp, handItem.Value, true))
        {
            return;
        }
        args.Handled = true;
    }
    #endregion

    protected void OnRemove(Entity<CultYoggComponent> uid, ref ComponentRemove args)
    {
        RemComp<CultYoggPurifiedComponent>(uid);

        //remove all actions cause they won't disappear with component
        _actions.RemoveAction(uid.Comp.CorruptItemActionEntity);
        _actions.RemoveAction(uid.Comp.CorruptItemInHandActionEntity);
        _actions.RemoveAction(uid.Comp.DigestActionEntity);
        _actions.RemoveAction(uid.Comp.PukeShroomActionEntity);

        //sending to a gamerule so it would be deleted and added in one place
        var ev = new CultYoggDeCultingEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
    }
}
