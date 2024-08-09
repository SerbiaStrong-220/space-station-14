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
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggDigestEvent>(DigestAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemEvent>(CorruptItemAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptItemInHandEvent>(CorruptItemInHandAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggAscendingEvent>(AscendingAction);
    }

    protected virtual void OnCompInit(Entity<CultYoggComponent> uid, ref ComponentStartup args)
    {
        //_actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);//delete after testing
        //_actions.AddAction(uid, ref uid.Comp.AscendingActionEntity, uid.Comp.AscendingAction);//delete after testing
        _actions.AddAction(uid, ref uid.Comp.CorruptItemActionEntity, uid.Comp.CorruptItemAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemInHandActionEntity, uid.Comp.CorruptItemInHandAction);
        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }

    #region Puke
    private void PukeAction(Entity<CultYoggComponent> uid, ref CultYoggPukeShroomEvent args)
    {
        if (args.Handled)
            return;

        if (_net.IsClient) // Have to do this because spawning stuff in shared is CBT.
            return;

        _entityManager.SpawnEntity(uid.Comp.PukedLiquid, Transform(uid).Coordinates);
        var shroom = _entityManager.SpawnEntity(uid.Comp.PukedEntity, Transform(uid).Coordinates);
        _audio.PlayPredicted(uid.Comp.PukeSound, uid, shroom);

        args.Handled = true;

        _actions.RemoveAction(uid, uid.Comp.PukeShroomActionEntity);
        _actions.AddAction(uid, ref uid.Comp.DigestActionEntity, uid.Comp.DigestAction);
    }
    private void DigestAction(Entity<CultYoggComponent> uid, ref CultYoggDigestEvent args)
    {
        if (TryComp<HungerComponent>(uid, out var hungerComp)
        && _hungerSystem.IsHungerBelowState(uid, uid.Comp.MinHungerThreshold, hungerComp.CurrentHunger - uid.Comp.HungerCost, hungerComp))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-nutritions"), uid);
            //_popup.PopupClient(Loc.GetString("cult-yogg-digest-no-nutritions"), uid, uid);//idk if it isn't working, but OnSericultureStart is an ok
            return;
        }

        _hungerSystem.ModifyHunger(uid, -uid.Comp.HungerCost);

        //maybe add thist
        /*
        if (TryComp<ThirstComponent>(uid, out var thirst))
            _thirst.ModifyThirst(uid, thirst, thirstAdded);
        */

        _actions.RemoveAction(uid, uid.Comp.DigestActionEntity);//if we digested, we should puke after

        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
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

    #region Ascending
    private void AscendingAction(Entity<CultYoggComponent> uid, ref CultYoggAscendingEvent args)
    {
        /* idk what is this
        if (!_timing.IsFirstTimePredicted)
            return;
        */

        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
        {
            _body.GibBody(uid, body: body);
        }
    }

    public void ModifyEatenShrooms(EntityUid uid, CultYoggComponent comp)//idk if it is canser or no, will be like that for a time
    {
        comp.ConsumedShrooms++; //Add shroom to buffer
        if (comp.ConsumedShrooms < comp.AmountShroomsToAscend) // if its not enough to ascend go next
            return;

        //Maybe in later version we will detiriorate the body and add some kind of effects

        //ToDo Needed some kind of check, how many MiGo exists, alive and less than 3

        //IDK how to check if he already has this action, so i did this markup
        if (_actions.AddAction(uid, ref comp.AscendingActionEntity, out var act, comp.AscendingAction) && act.UseDelay != null)
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(comp.AscendingActionEntity.Value, start, end);
        }
    }
    #endregion
}
