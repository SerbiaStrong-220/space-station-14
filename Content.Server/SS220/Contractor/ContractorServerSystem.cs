using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Preferences.Managers;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Stack;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.SS220.Contractor;
using Content.Shared.SSDIndicator;
using Content.Shared.Store;
using Robust.Server.GameObjects;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.SS220.Contractor;

/// <summary>
/// This handles generation contracts for contractor,
/// any other stuff with contractor pda, opening portal,
/// handling any contracts, and buy listing in uplink
/// </summary>
public sealed class ContractorServerSystem : SharedContractorSystem
{
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly StackSystem _stack = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ISharedPlayerManager _playerManager = default!;
    [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
    [Dependency] private readonly IServerPreferencesManager _pref = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContractorComponent, ComponentInit>(OnContractorCompInit);
        SubscribeLocalEvent<ContractorComponent, OpenPortalContractorEvent>(OnOpenPortalEvent);

        SubscribeLocalEvent<ContractorPdaComponent, ContractorNewContractAcceptedMessage>(OnNewContractAccepted);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorWithdrawTcMessage>(OnWithdrawTc);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorExecutionButtonPressedMessage>(OnExecuteContract);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorAbortContractMessage>(AbortContract);

        SubscribeLocalEvent<StoreBuyListingMessage>(OnBuyContractorKit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ContractorComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PdaEntity == null)
                continue;

            if (comp.BlockUntil >= 0f)
                comp.BlockUntil -= frameTime;

            if (_uiSystem.IsUiOpen(comp.PdaEntity.Value, ContractorPdaKey.Key))
                UpdateState(uid, comp);
        }
    }

    private void UpdateState(EntityUid contractor, ContractorComponent comp)
    {
        _uiSystem.SetUiState(
            comp.PdaEntity!.Value,
            ContractorPdaKey.Key,
            new ContractorExecutionBoundUserInterfaceState(
                isEnabledExecution: IsCloseWithPosition(contractor) || (comp.BlockUntil > 0),
                isEnabledPosition: !comp.CurrentContractEntity.HasValue || (comp.BlockUntil > 0),
                blockExecutionTime: comp.BlockUntil,
                blockPositionsTime: comp.BlockUntil
            ));
    }

    /// <summary>
    /// Generate contracts on server, once time while called CompInit event
    /// </summary>
    private void OnContractorCompInit(Entity<ContractorComponent> ent, ref ComponentInit ev)
    {
        GenerateContracts(ent);
    }

    /// <summary>
    /// Handle open portal event
    /// </summary>
    private void OnOpenPortalEvent(Entity<ContractorComponent> ent, ref OpenPortalContractorEvent args)
    {
        if (!TryComp<ContractorTargetComponent>(GetEntity(ent.Comp.CurrentContractEntity), out var targetComponent))
            return;

        if (args.Cancelled || args.Handled || targetComponent.PortalOnStationEntity != null)
            return;

        targetComponent.PortalOnStationEntity = SpawnAtPosition("ContractorPortal", Transform(ent.Owner).Coordinates);

        var query = EntityQueryEnumerator<ContractorTajpanWarpPointComponent>();

        var warpPoints = new List<EntityUid>();

        while (query.MoveNext(out var portalOnTajpan, out _))
        {
            warpPoints.Add(portalOnTajpan);
        }

        if (warpPoints.Count > 0)
        {
            var randomIndex = _random.Next(warpPoints.Count);
            targetComponent.PortalOnTajpanEntity = warpPoints[randomIndex];
        }

        ent.Comp.BlockUntil = 15f;

        args.Handled = true;
        Dirty(ent);
    }

    //TODO server and checks
    /// <summary>
    /// Raised when clicked on position button on pda
    /// </summary>
    private void OnNewContractAccepted(Entity<ContractorPdaComponent> ent, ref ContractorNewContractAcceptedMessage ev)
    {
        if (!TryComp<ContractorComponent>(ev.Actor, out var contractorComponent))
            return;

        if (!contractorComponent.Contracts.ContainsKey(ev.ContractEntity))
        {
            _adminLogger.Add(LogType.Action, LogImpact.Extreme, $"Contractor {ev.Actor} accepted unknown contract {ev.ContractEntity}");
            return;
        }

        EnsureComp<ContractorTargetComponent>(GetEntity(ev.ContractEntity), out var target);

        target.CanBeAssigned = false;

        if (target.AmountTc > 8 || ev.TcReward > 8)
        {
            _adminLogger.Add(
                LogType.Action,
                LogImpact.Extreme,
                $"Contractor {ev.Actor} accepted unknown contract {ev.ContractEntity} with very big amount");

            return;
        }

        target.PortalPosition = Transform(GetEntity(ev.WarpPointEntity)).Coordinates;
        target.AmountTc = ev.TcReward;
        target.Performer = ev.Actor;

        contractorComponent.CurrentContractData = ev.ContractData;
        contractorComponent.CurrentContractEntity = ev.ContractEntity;

        Dirty(ev.Actor, contractorComponent);

        if (TryComp<ContractorPdaComponent>(GetEntity(ev.Entity), out var contractorPdaComponent))
        {
            contractorPdaComponent.CurrentContractEntity = ev.ContractEntity;
            contractorPdaComponent.CurrentContractData = ev.ContractData;
            Dirty(GetEntity(ev.Entity), contractorPdaComponent);
        }

        HandleContractAccepted(ev.ContractEntity, ev.Actor);
    }

    /// <summary>
    /// Raised when clicked on execute button, then doAfter, then open portal.
    /// </summary>
    private void OnExecuteContract(Entity<ContractorPdaComponent> ent, ref ContractorExecutionButtonPressedMessage ev)
    {
        var entity = GetEntity(ent.Comp.CurrentContractEntity);

        if (!TryComp<ContractorTargetComponent>(entity, out var contractorComponent))
            return;

        if (contractorComponent.PortalOnStationEntity != null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ev.Actor, 5f, new OpenPortalContractorEvent(), ev.Actor, entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnWithdrawTc(Entity<ContractorPdaComponent> ent, ref ContractorWithdrawTcMessage ev)
    {
        if (!TryComp<ContractorComponent>(ev.Actor, out var contractorComponent))
            return;

        if (ev.Amount > contractorComponent.AmountTc)
            return;

        if (ev.Actor != GetEntity(ent.Comp.PdaOwner))
            return;

        var coordinates = Transform(ev.Actor).Coordinates;

        var telCrystal = _stack.SpawnMultiple("Telecrystal", (int)ev.Amount, coordinates);

        if (telCrystal.FirstOrDefault() is {} tUid)
            _hands.PickupOrDrop(ev.Actor, tUid);

        contractorComponent.AmountTc -= ev.Amount;

        _uiSystem.ServerSendUiMessage(ent.Owner, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());
        Dirty(ev.Actor, contractorComponent);
    }

    private void OnBuyContractorKit(StoreBuyListingMessage ev)
    {
        if (ev.Listing.Id != "UplinkContractor")
            return;

        EnsureComp<ContractorComponent>(ev.Actor);
    }

    /// <summary>
    /// When contractor accepts new contract, remove this contract from other contractors and generate another contract for them
    /// </summary>
    /// <param name="acceptedPlayer">Target, on what contractor accepted</param>
    /// <param name="contractor">Contractor, who accepted</param>
    private void HandleContractAccepted(NetEntity acceptedPlayer, EntityUid contractor)
    {
        var query = EntityQueryEnumerator<ContractorComponent>();

        while (query.MoveNext(out var uid, out var contractorComp))
        {
            if (uid == contractor || !contractorComp.Contracts.Remove(acceptedPlayer))
                continue;

            if (contractorComp.Contracts.Count >= contractorComp.MaxAvailableContracts)
                continue;

            var newContract = GenerateContractForContractor((uid, contractorComp));

            if (!newContract.HasValue)
                continue;

            contractorComp.Contracts[newContract.Value.Target] = newContract.Value.Contract;
            Dirty(uid, contractorComp);

            if (contractorComp.PdaEntity.HasValue)
            {
                _uiSystem.ServerSendUiMessage(
                    contractorComp.PdaEntity.Value,
                    ContractorPdaKey.Key,
                    new ContractorUpdateStatsMessage());
            }
        }
    }

    /// <summary>
    /// Generate a new contract or contracts for contractors, if another contractor has accepted their contract
    /// </summary>
    /// <param name="contractor"></param>
    /// <returns></returns>
    private (NetEntity Target, ContractorContract Contract)? GenerateContractForContractor(Entity<ContractorComponent> contractor)
    {
        var playerPool = GetPlayerPool(contractor);
        _random.Shuffle(playerPool);

        foreach (var player in playerPool)
        {
            if (contractor.Comp.Contracts.Count >= contractor.Comp.MaxAvailableContracts)
                return null;

            if (!_mindSystem.TryGetMind(player, out var mindId, out _))
                continue;

            if (!_jobs.MindTryGetJob(mindId, out var jobProto)) // || jobProto.ID == "Captain" - disabled for testing
                continue;

            if (!_playerManager.TryGetSessionByEntity(player, out var session))
                continue;

            if (!TryComp(player, out MetaDataComponent? metaDataComponent))
                continue;

            if (_pref.GetPreferences(session.UserId).SelectedCharacter is HumanoidCharacterProfile pref)
            {
                contractor.Comp.Profiles.TryAdd(GetNetEntity(player), pref);
            }

            return (GetNetEntity(player),
                new ContractorContract
                {
                    Name = metaDataComponent.EntityName,
                    Job = jobProto,
                    AmountPositions = GeneratePositionsForTarget(),
                });
        }

        return null;
    }

    public void GenerateContracts(Entity<ContractorComponent> contractor)
    {
        var playerPool = GetPlayerPool(contractor);
        _random.Shuffle(playerPool);

        foreach (var player in playerPool)
        {
            if (contractor.Comp.Contracts.Count >= contractor.Comp.MaxAvailableContracts)
                return;

            if (!_mindSystem.TryGetMind(player, out var mindId, out _))
                continue;

            if (!_jobs.MindTryGetJob(mindId, out var jobProto)) // || jobProto.ID == "Captain" - disabled for testing
                continue;

            if (!TryComp(player, out MetaDataComponent? metaDataComponent))
                continue;

            contractor.Comp.Contracts[GetNetEntity(player)] = new ContractorContract
            {
                Name = metaDataComponent.EntityName,
                Job = jobProto,
                AmountPositions = GeneratePositionsForTarget(),
            };

            if (!_playerManager.TryGetSessionByEntity(player, out var session))
                continue;

            if (_pref.GetPreferences(session.UserId).SelectedCharacter is HumanoidCharacterProfile pref)
            {
                contractor.Comp.Profiles.TryAdd(GetNetEntity(player), pref);
            }
        }
    }

    private List<(NetEntity Uid, string Location, FixedPoint2 TcReward, Difficulty Difficulty)> GeneratePositionsForTarget()
    {
        var allLocations = new List<(NetEntity Uid, string Location, FixedPoint2 TcReward, Difficulty Difficulty)>();

        var query = EntityQueryEnumerator<ContractorWarpPointComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            allLocations.Add((GetNetEntity(uid), comp.LocationName, comp.AmountTc, comp.Difficulty));
        }

        var easyLocations = allLocations.Where(loc => loc.Difficulty == Difficulty.Easy).ToList();
        var mediumLocations = allLocations.Where(loc => loc.Difficulty == Difficulty.Medium).ToList();
        var hardLocations = allLocations.Where(loc => loc.Difficulty == Difficulty.Hard).ToList();

        _random.Shuffle(easyLocations);
        _random.Shuffle(mediumLocations);
        _random.Shuffle(hardLocations);

        var result = new List<(NetEntity, string, FixedPoint2, Difficulty)>
        {
            easyLocations.FirstOrDefault(),
            mediumLocations.FirstOrDefault(),
            hardLocations.FirstOrDefault()
        };

        return result.Where(loc => loc.Item1.Valid).ToList();
    }

    private bool IsCloseWithPosition(EntityUid contractor)
    {
        if (!TryComp<ContractorComponent>(contractor, out var contractorComponent))
            return false;

        if (contractorComponent.CurrentContractEntity is null &&
            contractorComponent.CurrentContractData is null)
            return false;

        var targetEntity = GetEntity(contractorComponent.CurrentContractEntity);

        if (!TryComp<ContractorTargetComponent>(targetEntity, out var targetComponent))
            return false;

        var contractorPosition = Transform(contractor).Coordinates.Position;
        var targetPosition = Transform(targetEntity.Value).Coordinates.Position;

        var targetPortalPosition = targetComponent.PortalPosition.Position;

        var isCloseToPortal = (contractorPosition - targetPortalPosition).Length() < 1f &&
                              (targetPosition - targetPortalPosition).Length() < 1f;


        return isCloseToPortal;
    }

    private void AbortContract(Entity<ContractorPdaComponent> ent, ref ContractorAbortContractMessage ev)
    {
        var pdaOwner = GetEntity(ent.Comp.PdaOwner);

        if (ev.Actor != pdaOwner)
            return;

        if (!TryComp<ContractorComponent>(pdaOwner, out var contractorComponent))
            return;

        if (!contractorComponent.Contracts.Remove(ev.ContractEntity))
        {
            _adminLogger.Add(
                LogType.Action,
                LogImpact.High,
                $"Contractor {ev.Actor} aborted unknown contract {ev.ContractEntity}");

            return;
        }

        contractorComponent.MaxAvailableContracts--;
        contractorComponent.Reputation--;
        contractorComponent.CurrentContractEntity = null;
        contractorComponent.CurrentContractData = null;

        ent.Comp.CurrentContractEntity = null;
        ent.Comp.CurrentContractData = null;

        GenerateContracts((pdaOwner.Value, contractorComponent));
        _uiSystem.ServerSendUiMessage(ent.Owner, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());

        Dirty(ent);
        Dirty(pdaOwner.Value, contractorComponent);
    }

    private List<EntityUid> GetPlayerPool(Entity<ContractorComponent> ent)
    {
        return _playerManager.Sessions
            .Where(p => p is { Status: SessionStatus.InGame, AttachedEntity: not null })
            .Select(p => p.AttachedEntity!.Value)
            .Where(player =>
                TryComp<SSDIndicatorComponent>(player, out var ssdIndicatorComponent) && !ssdIndicatorComponent.IsSSD &&
                !HasComp<GhostComponent>(player) &&
                !HasComp<ContractorComponent>(player) &&
                (!TryComp<ContractorTargetComponent>(player, out var targetComponent) || targetComponent.CanBeAssigned) &&
                (!_mindSystem.TryGetMind(player, out var mindId, out _) || !_roleSystem.MindHasRole<TraitorRoleComponent>(mindId)) &&
                !ent.Comp.Contracts.ContainsKey(GetNetEntity(player)))
            .ToList();
    }
}
