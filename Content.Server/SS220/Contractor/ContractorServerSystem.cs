using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Server.Stack;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.Mind;
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
/// This handles...
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

    //TODO on client and on server target is closed to position
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ContractorComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.PdaEntity == null)
                continue;

            var isEnabled = IsCloseWithPosition(uid);

            if (_uiSystem.IsUiOpen(GetEntity(comp.PdaEntity)!.Value, ContractorPdaKey.Key))
            {
                _uiSystem.SetUiState(
                    GetEntity(comp.PdaEntity.Value),
                    ContractorPdaKey.Key,
                    new ContractorExecutionBoundUserInterfaceState(isEnabled));
            }
        }
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
        if (!TryComp<ContractorTargetComponent>(GetEntity(ent.Comp.CurrentContractEntity), out var target))
            return;

        if (args.Cancelled || args.Handled || target.PortalEntity != null)
            return;

        target.PortalEntity = SpawnAtPosition("ContractorPortal", Transform(ent.Owner).Coordinates);
        target.PositionOnStation = Transform(ent.Owner).Coordinates.Position;

        args.Handled = true;

        Dirty(GetEntity(ent.Comp.CurrentContractEntity!.Value), target);
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

        Dirty(GetEntity(ev.ContractEntity), target);

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

        if (contractorComponent.PortalEntity != null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ev.Actor, 5f, new OpenPortalContractorEvent(), ev.Actor, ev.Actor)
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
    public void HandleContractAccepted(NetEntity acceptedPlayer, EntityUid contractor)
    {
        var query = EntityQueryEnumerator<ContractorComponent>();

        while (query.MoveNext(out var uid, out var contractorComp))
        {
            if (uid == contractor)
                continue;

            if (!contractorComp.Contracts.Remove(acceptedPlayer))
                continue;

            if (contractorComp.Contracts.Count >= contractorComp.MaxAvailableContracts)
                continue;

            var newContract = GenerateContractForContractor((uid, contractorComp));

            if (newContract != null)
            {
                contractorComp.Contracts[newContract.Value.Target] = newContract.Value.Contract;
            }

            Dirty(uid, contractorComp);

            _uiSystem.ServerSendUiMessage(GetEntity(contractorComp.PdaEntity!.Value), ContractorPdaKey.Key, new ContractorUpdateStatsMessage());
        }
    }

    /// <summary>
    /// Generate a new contract or contracts for contractors, if another contractor has accepted their contract
    /// </summary>
    /// <param name="contractor"></param>
    /// <returns></returns>
    private (NetEntity Target, ContractorContract Contract)? GenerateContractForContractor(Entity<ContractorComponent> contractor)
    {
        var playerPool = _playerManager.Sessions
            .Where(p => p is { Status: SessionStatus.InGame, AttachedEntity: not null })
            .Select(p => p.AttachedEntity!.Value)
            .ToList();

        _random.Shuffle(playerPool);

        foreach (var player in playerPool)
        {
            if (HasComp<GhostComponent>(player) ||
                HasComp<ContractorComponent>(player) ||
                HasComp<ContractorTargetComponent>(player) ||
                (TryComp<SSDIndicatorComponent>(player, out var ssdIndicatorComponent) && ssdIndicatorComponent.IsSSD))
            {
                continue;
            }

            if (contractor.Comp.Contracts.ContainsKey(GetNetEntity(player)))
                continue;

            if (!_mindSystem.TryGetMind(player, out var mindId, out _))
                continue;

            if (_roleSystem.MindHasRole<TraitorRoleComponent>(mindId))
                continue;

            _jobs.MindTryGetJobName(mindId, out var jobName); // && jobName == "JobCaptain" - disable for testing

            return (GetNetEntity(player),
                new ContractorContract
                {
                    Job = jobName,
                    AmountPositions = GeneratePositionsForTarget(),
                });
        }

        return null;
    }

    public void GenerateContracts(Entity<ContractorComponent> ent)
    {
        var playerPool = _playerManager.Sessions
            .Where(p => p is { Status: SessionStatus.InGame, AttachedEntity: not null })
            .Select(p => p.AttachedEntity!.Value)
            .ToList();

        _random.Shuffle(playerPool);

        foreach (var player in playerPool)
        {
            if (HasComp<GhostComponent>(player) ||
                HasComp<ContractorComponent>(player) ||
                HasComp<ContractorTargetComponent>(player) ||
                (TryComp<SSDIndicatorComponent>(player, out var ssdIndicatorComponent) && ssdIndicatorComponent.IsSSD))
            {
                continue;
            }

            if (!_mindSystem.TryGetMind(player, out var mindId, out _))
                continue;

            if (_roleSystem.MindHasRole<TraitorRoleComponent>(mindId))
                continue;

            if (ent.Comp.Contracts.ContainsKey(GetNetEntity(player)))
                continue;

            _jobs.MindTryGetJobName(mindId, out var jobName); // && jobName == "JobCaptain" - disable for testing

            if (ent.Comp.Contracts.Count == ent.Comp.MaxAvailableContracts)
                return;

            ent.Comp.Contracts.Add(GetNetEntity(player),
                new ContractorContract
                {
                    Job = jobName,
                    AmountPositions = GeneratePositionsForTarget(),
                });
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

        allLocations.Clear();

        if (easyLocations.Count > 0)
            allLocations.Add(easyLocations[0]);

        if (mediumLocations.Count > 0)
            allLocations.Add(mediumLocations[0]);

        if (hardLocations.Count > 0)
            allLocations.Add(hardLocations[0]);

        return allLocations;
    }

    private bool IsCloseWithPosition(EntityUid player)
    {
        if (!TryComp<ContractorComponent>(player, out var contractorComponent))
            return false;

        if (contractorComponent.CurrentContractEntity is null &&
            contractorComponent.CurrentContractData is null)
            return false;

        var targetEntity = GetEntity(contractorComponent.CurrentContractEntity);

        if (!TryComp<ContractorTargetComponent>(targetEntity, out var targetComponent))
            return false;

        var playerPosition = Transform(player).Coordinates.Position;
        var targetPosition = Transform(targetEntity.Value).Coordinates.Position;

        var targetPortalPosition = targetComponent.PortalPosition.Position;

        var isPlayerCloseToPortal = (playerPosition - targetPortalPosition).Length() < 1f;
        var isTargetCloseToPortal = (targetPosition - targetPortalPosition).Length() < 1f;

        return isPlayerCloseToPortal && isTargetCloseToPortal;
    }

    private void AbortContract(Entity<ContractorPdaComponent> ent, ref ContractorAbortContractMessage ev)
    {
        var pdaOwner = GetEntity(ent.Comp.PdaOwner);

        if (ev.Actor != pdaOwner)
            return;

        if (!TryComp<ContractorComponent>(pdaOwner, out var contractorComponent))
            return;

        if (!contractorComponent.Contracts.Remove(ev.ContractEntity))
            return;

        _adminLogger.Add(
            LogType.Action,
            LogImpact.High,
            $"Contractor {ev.Actor} aborted unknown contract {ev.ContractEntity}");

        contractorComponent.MaxAvailableContracts--;
        contractorComponent.Reputation--;
        contractorComponent.CurrentContractEntity = null;
        contractorComponent.CurrentContractData = null;

        ent.Comp.CurrentContractEntity = null;
        ent.Comp.CurrentContractData = null;

        _uiSystem.ServerSendUiMessage(ent.Owner, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());

        Dirty(ent);
        Dirty(pdaOwner.Value, contractorComponent);
    }
}
