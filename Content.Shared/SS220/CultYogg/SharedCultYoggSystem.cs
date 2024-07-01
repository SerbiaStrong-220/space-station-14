// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Mind;
using Content.Shared.Body.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Nutrition.EntitySystems;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Timing;
using Content.Shared.Nutrition.Components;


namespace Content.Shared.SS220.CultYogg;

public abstract class SharedCultYoggSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
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
        SubscribeLocalEvent<CultYoggComponent, CultYoggCorruptDoAfterEvent>(CorruptOnDoAfter);
        SubscribeLocalEvent<CultYoggComponent, CultYoggShroomEatenEvent>(AddConsumed);
    }

    protected virtual void OnCompInit(Entity<CultYoggComponent> uid, ref ComponentStartup args)
    {
        //_actions.AddAction(uid, ref comp.PukeShroomActionEntity, comp.PukeShroomAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemActionEntity, uid.Comp.CorruptItemAction);
        _actions.AddAction(uid, ref uid.Comp.CorruptItemInHandActionEntity, uid.Comp.CorruptItemInHandAction);
        _actions.AddAction(uid, ref uid.Comp.AscendingActionEntity, uid.Comp.AscendingAction);//delete this when released it should be added through shrooms
        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _gameTiming.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }

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
    private void CorruptItemAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemEvent args)//ToDo some list of corruption
    {
        if (args.Handled)
            return;

        if (!CheckForCorruption(args.Target, out var corruption))
        {
            //_popup.PopupClient(Loc.GetString("cult-yogg-corrupt-no-protod"), uid, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }

        if (_entityManager.HasComponent<CultYoggCorruptedComponent>(args.Target))
        {
            //_popup.PopupCursor(Loc.GetString("cult-yogg-corrupt-already-corrupted"), PopupType.SmallCaution); //somehow isn't working
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-already-corrupted"), args.Target, uid);
            return;
        }


        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new CultYoggCorruptDoAfterEvent(corruption, false), uid, args.Target)//ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };


        _doAfter.TryStartDoAfter(doafterArgs);

        args.Handled = true;
    }
    private void CorruptItemInHandAction(Entity<CultYoggComponent> uid, ref CultYoggCorruptItemInHandEvent args)//ToDo some list of corruption
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

        if (!CheckForCorruption((EntityUid) handItem, out var corruption))
        {
            //_popup.PopupClient(Loc.GetString("cult-yogg-corrupt-no-protod"), uid, PopupType.SmallCaution);
            _popup.PopupEntity(Loc.GetString("cult-yogg-corrupt-no-proto"), uid);
            return;
        }

        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new CultYoggCorruptDoAfterEvent(corruption, true), uid, handItem)//ToDo estimate time for corruption
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doafterArgs);

        args.Handled = true;
    }
    private bool CheckForCorruption(EntityUid uid, [NotNullWhen(true)] out CultYoggCorruptedPrototype? corruption)//if item in list of corrupted
    {
        var idOfEnity = MetaData(uid).EntityPrototype!.ID;
        //var idOfEnity = _entityManager.GetComponent<MetaDataComponent>(uid).EntityPrototype!.ID;

        foreach (var entProto in _prototypeManager.EnumeratePrototypes<CultYoggCorruptedPrototype>())//idk if it isn't shitcode
        {
            if (idOfEnity == entProto.ID)
            {
                corruption = entProto;
                return true;
            }
        }
        corruption = null;
        return false;
    }
    private void CorruptOnDoAfter(Entity<CultYoggComponent> uid, ref CultYoggCorruptDoAfterEvent args)//DoAfter for corruption
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        if (_net.IsClient)
            return;

        if (args.Proto == null)
            return;

        var coords = Transform((EntityUid) args.Target).Coordinates;

        var corruptedEntity = Spawn(args.Proto.Result, coords);

        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(uid)} used corrupt on {ToPrettyString(args.Target)} and made {ToPrettyString(corruptedEntity)}");

        //ToDo if object is a storage, it should drop all its items

        //Every corrupted entity should have this  entity at start
        _entityManager.AddComponent<CultYoggCorruptedComponent>(corruptedEntity);//ToDo save previuos form here, so delete it when you do all the corrupted list
        if (!_entityManager.TryGetComponent<CultYoggCorruptedComponent>(corruptedEntity, out var corrupted))
            return;

        corrupted.PreviousForm = "";

        //Delete previous entity
        _entityManager.DeleteEntity(args.Target);


        if (args.InHand)
            _hands.PickupOrDrop(uid, corruptedEntity);

        args.Handled = true;
    }
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

    private void AddConsumed(Entity<CultYoggComponent> ent, ref CultYoggShroomEatenEvent args)
    {
        ent.Comp.ConsumedShrooms++; //Add shroom to buffer
        if (ent.Comp.ConsumedShrooms >= ent.Comp.AmountShroomsToAscend)
        {
            //_actions.AddAction(uid, ref comp.AscendingActionEntity, comp.AscendingAction)//uncomment when all MiGotests will be done
        }
    }
}
