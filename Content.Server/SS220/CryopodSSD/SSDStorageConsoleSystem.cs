using System.Linq;
using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.Forensics;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Objectives.Conditions;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Server.Storage.Components;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.StationRecords;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Content.Shared.Storage.SharedStorageComponent;

namespace Content.Server.SS220.CryopodSSD;

public sealed class SSDStorageConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly MindTrackerSystem _mindTrackerSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    
    private ISawmill _sawmill = default!;
    
    public override void Initialize()
    {
        base.Initialize();
        
        _sawmill = Logger.GetSawmill("SSDStorageConsole");
        
        SubscribeLocalEvent<SSDStorageConsoleComponent, CryopodSSDStorageInteractWithItemEvent>(OnInteractWithItem);
        SubscribeLocalEvent<SSDStorageConsoleComponent, EntRemovedFromContainerMessage>(OnStorageItemRemoved);
        SubscribeLocalEvent<SSDStorageConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        
        SubscribeLocalEvent<TransferredToCryoStorageEvent>(OnTransferredToCryo);
    }
    
    public void TransferToCryoStorage(EntityUid uid, EntityUid target)
    {
        if (TryComp<SSDStorageConsoleComponent>(uid, out var cryopodConsoleComp))
        {
            TransferToCryoStorage(uid, cryopodConsoleComp, target);
        }
    }
    
    private void OnInteractWithItem(EntityUid uid, SSDStorageConsoleComponent component, CryopodSSDStorageInteractWithItemEvent args)
    {
        if (args.Session.AttachedEntity is not EntityUid player)
            return;

        if (!Exists(args.InteractedItemUid))
        {
            _sawmill.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUid} stored in {ToPrettyString(uid)}");
            return;
        }

        if (!TryComp<ServerStorageComponent>(uid, out var storageComp))
        {
            return;
        }
        
        if (!_actionBlockerSystem.CanInteract(player, args.InteractedItemUid) || storageComp.Storage == null || !storageComp.Storage.Contains(args.InteractedItemUid))
            return;
        
        if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
            return;

        if (!_accessReaderSystem.IsAllowed(player, uid))
        {
            _sawmill.Info($"Player {ToPrettyString(player)} possibly exploits UI, trying to take item from {ToPrettyString(uid)} without access");
            return;
        }
        
        if (hands.ActiveHandEntity == null)
        {
            if (_handsSystem.TryPickupAnyHand(player, args.InteractedItemUid, handsComp: hands)
                && storageComp.StorageRemoveSound != null)
                _sawmill.Info($"{ToPrettyString(player)} takes {ToPrettyString(args.InteractedItemUid)} from {ToPrettyString(uid)}");
        }
    }

    private void OnTransferredToCryo(TransferredToCryoStorageEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        var entityEnumerator = EntityQueryEnumerator<SSDStorageConsoleComponent>();

        while (entityEnumerator.MoveNext(out var uid, out var ssdStorageConsoleComp))
        {
            if (ssdStorageConsoleComp.IsCryopod)
            {
                continue;
            }
            
            var consoleCoord = Transform(uid).Coordinates;
            var cryopodCoord = Transform(args.CryopodSSD).Coordinates;

            if (consoleCoord.InRange(_entityManager, _transformSystem, cryopodCoord, ssdStorageConsoleComp.RadiusToConnect))
            {
                args.Handled = true;
                TransferToCryoStorage(uid, ssdStorageConsoleComp, args.EntityToTransfer);
                return;
            }
        }
    }
    
    private void TransferToCryoStorage(EntityUid uid, SSDStorageConsoleComponent component, EntityUid entityToTransfer)
    {
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

    private void UndressEntity(EntityUid uid, SSDStorageConsoleComponent component, EntityUid target)
    {
        if (!TryComp<ServerStorageComponent>(uid, out var storageComponent)
            || storageComponent.Storage is null)
        {
            return;
        }
        
        /*
        * It would be great if we could instantly delete items when we know they are not whitelisted.
        * However, this could lead to a situation where we accidentally delete the uniform,
        * resulting in all items inside the pockets being dropped before we add them to the itemsToTransfer list.
        * So we should have itemsToDelete list.
        */

        List<EntityUid> itemsToTransfer = new();
        List<EntityUid> itemsToDelete = new();

        // Looking through all 
        SortContainedItems(in target,ref itemsToTransfer,ref itemsToDelete, in component.Whitelist);

        foreach (var item in itemsToTransfer)
        {
            storageComponent.Storage.Insert(item);
        }

        foreach (var item in itemsToDelete)
        {
            _entityManager.DeleteEntity(item);
        }
    }

    private void SortContainedItems(in EntityUid storageToLook, ref List<EntityUid> whitelistedItems,
        ref List<EntityUid> itemsToDelete, in EntityWhitelist? whitelist)
    {
        if (TryComp<TransformComponent>(storageToLook, out var transformComponent))
        {
            foreach (var childUid in transformComponent.ChildEntities)
            {
                if (!HasComp<ItemComponent>(childUid))
                {
                    continue;
                }

                if (whitelist is null || whitelist.IsValid(childUid))
                {
                    whitelistedItems.Add(childUid);
                }
                else
                {
                    itemsToDelete.Add(childUid);
                }

                // As far as I know, ChildEntities cannot be recursive 
                SortContainedItems(in childUid, ref whitelistedItems, ref itemsToDelete, in whitelist);
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
    
    private void OnStorageItemRemoved(EntityUid uid, SSDStorageConsoleComponent storageComp, EntRemovedFromContainerMessage args)
    {
        
        UpdateUserInterface(uid, storageComp, args.Entity, true);
    }

    private void UpdateUserInterface(EntityUid uid, SSDStorageConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (args.Session.AttachedEntity is null)
        {
            return;
        }
        UpdateUserInterface(uid, component, args.Session.AttachedEntity.Value);
    }

    private void UpdateUserInterface(EntityUid uid, SSDStorageConsoleComponent? component, EntityUid user,
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
            
            var state = new SSDStorageConsoleState(hasAccess, component.StoredEntities, storageState);
            SetStateForInterface(uid, state);
        }
    }
    
    private void SetStateForInterface(EntityUid uid, SSDStorageConsoleState storageConsoleState)
    {
        var ui = _userInterface.GetUiOrNull(uid, SSDStorageConsoleKey.Key);
        if (ui is not null)
        {
            _userInterface.SetUiState(ui, storageConsoleState);
        }
    }
}