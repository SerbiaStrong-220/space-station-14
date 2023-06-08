using System.Linq;
using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.Forensics;
using Content.Server.Inventory;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Conditions;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Access.Systems;
using static Content.Shared.Storage.SharedStorageComponent;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Inventory;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.StationRecords;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CryopodSSD;


/// <summary>
/// SS220
/// Implemented leaving from game via climbing in cryopod
/// <seealso cref="CryopodSSDComponent"/>
/// </summary>
public sealed class CryopodSSDSystem : SharedCryopodSSDSystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly ServerInventorySystem _inventorySystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly MindTrackerSystem _mindTrackerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("cryopodSSD");

        SubscribeLocalEvent<CryopodSSDComponent, ComponentInit>(OnComponentInit);
        
        SubscribeLocalEvent<CryopodSSDComponent, GetVerbsEvent<AlternativeVerb>>(AddAlternativeVerbs);
        SubscribeLocalEvent<CryopodSSDComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);
        SubscribeLocalEvent<CryopodSSDComponent, BoundUIOpenedEvent>(UpdateUserInterface);

        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDLeaveActionEvent>(OnCryopodSSDLeaveAction);
        SubscribeLocalEvent<CryopodSSDComponent, CryopodSSDDragFinished>(OnDragFinished);
        SubscribeLocalEvent<CryopodSSDComponent, DragDropTargetEvent>(HandleDragDropOn);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currTime = _gameTiming.CurTime;

        var entityEnumerator = EntityQueryEnumerator<CryopodSSDComponent>();
        
        while (entityEnumerator.MoveNext(out var uid, out var cryopodSSDComp))
        {
            if (cryopodSSDComp.BodyContainer.ContainedEntity is null ||
                currTime < cryopodSSDComp.CurrentEntityLyingInCryopodTime + TimeSpan.FromSeconds(cryopodSSDComp.AutoTransferDelay))
            {
                continue;
            }
            
            TransferToCryoStorage(uid, cryopodSSDComp);
        }
    }
    
    private void OnStorageItemRemoved(EntityUid uid, CryopodSSDComponent storageComp, EntRemovedFromContainerMessage args)
    {
        
        UpdateUserInterface(uid, storageComp, args.Entity, true);
    }

    private void UpdateUserInterface(EntityUid uid, CryopodSSDComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is null)
        {
            return;
        }
        UpdateUserInterface(uid, component, args.Session.AttachedEntity.Value);
    }

    private void UpdateUserInterface(EntityUid uid, CryopodSSDComponent? component, EntityUid user,
        bool forseAccess = false)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }
        
        if (TryComp<ServerStorageComponent>(uid, out var storageComponent) && storageComponent.StoredEntities is not null)
        {
            var hasAccess = _accessReaderSystem.IsAllowed(user, uid) || forseAccess;
            var storageState = hasAccess ?  
                new StorageBoundUserInterfaceState((List<EntityUid>) storageComponent.StoredEntities, 
                    storageComponent.StorageUsed, 
                    storageComponent.StorageCapacityMax)
              : new StorageBoundUserInterfaceState(new List<EntityUid>(),
                    0,
                    storageComponent.StorageCapacityMax);
            
            var state = new CryopodSSDState(hasAccess, component.StoredEntities, storageState);
            SetStateForInterface(uid, state);
        }
    }

    public override EntityUid? EjectBody(EntityUid uid, CryopodSSDComponent? cryopodSsdComponent)
    {
        if (!Resolve(uid, ref cryopodSsdComponent))
        {
            return null;
        }

        if (cryopodSsdComponent.BodyContainer.ContainedEntity is not { Valid: true } contained)
        {
            return null;
        }

        base.EjectBody(uid, cryopodSsdComponent);
        return contained;
    }

    private void SetStateForInterface(EntityUid uid, CryopodSSDState state)
    {
        var ui = _userInterface.GetUiOrNull(uid, CryopodSSDKey.Key);
        if (ui is not null)
        {
            _userInterface.SetUiState(ui, state);
        }
    }

    private void TransferToCryoStorage(EntityUid uid, CryopodSSDComponent component)
    {
        if (component.BodyContainer.ContainedEntity is null)
        {
            return;
        }
        
        var entityToTransfer = component.BodyContainer.ContainedEntity.Value;
        
        _sawmill.Info($"{ToPrettyString(entityToTransfer)} moved to cryo storage");

        var station = _stationSystem.GetOwningStation(uid);

        if (station is not null)
        {
            DeleteEntityRecord(entityToTransfer, station.Value, out var job);
            
            _chatSystem.DispatchStationAnnouncement(station.Value, 
                Loc.GetString(
                    "cryopodSSD-entered-cryo",
                    ("character", MetaData(entityToTransfer).EntityName),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job))),
                Loc.GetString("cryopodSSD-sender"));
            
            component.StoredEntities.Add($"{MetaData(entityToTransfer).EntityName} - [{job}] - {_gameTiming.RealTime}");
        }

        UndressEntity(uid, component, entityToTransfer);

        _entityManager.QueueDeleteEntity(entityToTransfer);
        
        UpdateAppearance(uid, component);
        
        ReplaceKillEntityObjectives(entityToTransfer);
    }

    private void ReplaceKillEntityObjectives(EntityUid uid)
    {
        var objectiveToReplace = new List<Objective>();
        foreach (var mind in _mindTrackerSystem.AllMinds)
        {
            if (mind.OwnedEntity is null)
            {
                continue;
            }
            
            objectiveToReplace.Clear();
            
            foreach (var objective in mind.AllObjectives)
            {
                if (objective.Conditions.Any(condition => (condition as KillPersonCondition)?.IsTarget(uid) ?? false))
                {
                    objectiveToReplace.Add(objective);
                }
            }

            foreach (var objective in objectiveToReplace)
            {
                mind.TryRemoveObjective(objective);
                var newObjective = _objectivesManager.GetRandomObjective(mind, "TraitorObjectiveGroups");
                if (newObjective is null || !mind.TryAddObjective(newObjective))
                {
                    _sawmill.Error($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he lost his objective and didn't get a new one");
                    continue;
                }
                    
                _sawmill.Info($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he get a new one");
            }
        }

    }

    private void UndressEntity(EntityUid uid, CryopodSSDComponent component, EntityUid target)
    {
        if (!TryComp<InventoryComponent>(target, out var inventoryComponent))
        {
            return;
        }

        if (!TryComp<ServerStorageComponent>(uid, out var storageComponent) || storageComponent.Storage is null)
        {
            return;
        }

        if (_prototypeManager.TryIndex(inventoryComponent.TemplateId,
                out InventoryTemplatePrototype? inventoryTemplate))
        {
            List<EntityUid> itemsToTransfer = new();
            foreach (var slot in inventoryTemplate.Slots)
            {
                if (_inventorySystem.TryGetSlotContainer(target, slot.Name, out var containerSlot, out _) && containerSlot.ContainedEntity is not null)
                {
                    itemsToTransfer.Add(containerSlot.ContainedEntity.Value);
                }
            }

            foreach (var item in itemsToTransfer)
            {
                storageComponent.Storage.Insert(item);
            }
        }
    }
    
    private void DeleteEntityRecord(EntityUid uid, EntityUid station, out string job)
    {
        job = string.Empty;
        var stationRecord = FindEntityStationRecordKey(station, uid);

        if (stationRecord is null)
        {
            return;
        }

        job = stationRecord.Value.Item2.JobTitle;

        _stationRecordsSystem.RemoveRecord(station, stationRecord.Value.Item1);
    }
    
    private (StationRecordKey, GeneralStationRecord)? FindEntityStationRecordKey(EntityUid station, EntityUid uid)
    {
        if (TryComp<DnaComponent>(uid, out var dnaComponent))
        {
            var stationRecords = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(station);
            var result = stationRecords.FirstOrNull(records => records.Item2.DNA == dnaComponent.DNA);
            if (result is not null)
            {
                return result.Value;
            }
        }

        return null;
    }

    private void HandleDragDropOn(EntityUid uid, CryopodSSDComponent cryopodSsdComponent, ref DragDropTargetEvent args)
    {
        if (cryopodSsdComponent.BodyContainer.ContainedEntity != null)
        {
            return;
        }
        
        if (!TryComp(args.Dragged, out MindComponent? mind) || !mind.HasMind)
        {
            _sawmill.Error($"{ToPrettyString(args.User)} tries to put non-playable entity into SSD cryopod {ToPrettyString(args.Dragged)}");
            return;
        }

        var doAfterArgs = new DoAfterArgs(args.User, cryopodSsdComponent.EntryDelay, new CryopodSSDDragFinished(), uid,
            target: args.Dragged, used: uid)
        {
            BreakOnDamage = true,
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            NeedHand = false,
        };
        _doAfterSystem.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnDragFinished(EntityUid uid, CryopodSSDComponent component, CryopodSSDDragFinished args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target is null)
        {
            return;
        }

        if (InsertBody(uid, args.Args.Target.Value, component))
        {
            _sawmill.Info($"{ToPrettyString(args.Args.User)} put {ToPrettyString(args.Args.Target.Value)} inside cryopod.");
        }

        args.Handled = true;
    }
    
    private void OnCryopodSSDLeaveAction(EntityUid uid, CryopodSSDComponent component, CryopodSSDLeaveActionEvent args)
    {
        if (component.BodyContainer.ContainedEntity is null)
        {
            _sawmill.Error("This action cannot be called if no one is in the cryopod.");
            return;
        }
        TransferToCryoStorage(uid, component);
    }
}