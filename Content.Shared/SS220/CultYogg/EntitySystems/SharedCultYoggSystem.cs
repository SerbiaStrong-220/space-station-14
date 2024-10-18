// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Inventory;
using Content.Shared.SS220.CultYogg.Components;


namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedCultYoggSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentStartup>(OnCompInit);

        SubscribeLocalEvent<CultYoggComponent, ExaminedEvent>(OnExamined);

        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemInHandEvent>(CorruptItemInHandAction);

        SubscribeLocalEvent<CultYoggComponent, ComponentRemove>(OnRemove);
    }

    protected virtual void OnCompInit(Entity<CultYoggComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.CorruptItemActionEntity, uid.Comp.CorruptItemAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemInHandActionEntity, uid.Comp.CorruptItemInHandAction);
        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }

    #region Stage
    private void OnExamined(EntityUid uid, CultYoggComponent component, ExaminedEvent args)
    {
        if (component.CurrentStage == 0)
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

        args.PushMarkup($"[color=green]{Loc.GetString("Глаза горят неестественно зелёным пламенем", ("ent", uid))}[/color]"); // no locale for right now
    }
    #endregion

    #region Corruption
    private void CorruptItemAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemEvent args)
    {
        if (args.Handled)
            return;

        if (_cultYoggCorruptedSystem.IsCorrupted(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), args.Target, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, args.Target, false))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }

    private void CorruptItemInHandAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemInHandEvent args)
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
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), handItem.Value, uid);
            return;
        }

        if (!_cultYoggCorruptedSystem.TryCorruptContinuously(uid, handItem.Value, true))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }
        args.Handled = true;
    }
    #endregion

    public void OnRemove(Entity<CultYoggComponent> uid, ref ComponentRemove args)
    {
        RemComp<CultYoggCleansedComponent>(uid);

        var ev = new CultYoggDeCultingEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);

        //remove all actions cause they won't disappear with component
        _actions.RemoveAction(uid.Comp.CorruptItemActionEntity);
        _actions.RemoveAction(uid.Comp.CorruptItemInHandActionEntity);
        _actions.RemoveAction(uid.Comp.DigestActionEntity);
        _actions.RemoveAction(uid.Comp.PukeShroomActionEntity);

        //ToDo CultYoggComponent remove stages visualization
    }
}

[ByRefEvent, Serializable]
public sealed class CultYoggDeCultingEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public CultYoggDeCultingEvent(EntityUid entity)
    {
        Entity = entity;
    }
}

