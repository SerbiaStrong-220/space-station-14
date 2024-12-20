using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Roles.Jobs;
using Content.Server.Stack;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Ghost;
using Content.Shared.SS220.Contractor;
using Content.Shared.SSDIndicator;
using Content.Shared.Store;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
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
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    //TODO client system
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ContractorComponent, ComponentInit>(OnContractorCompInit);
        SubscribeLocalEvent<ContractorComponent, OpenPortalContractorEvent>(OnOpenPortalEvent);

        SubscribeLocalEvent<ContractorPdaComponent, ContractorNewContractAcceptedMessage>(OnNewContractAccepted);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorWithdrawTcMessage>(OnWithdrawTc);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorExecutionButtonPressedMessage>(OnExecuteContract);

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

            var isEnabled = IsCloseWithPosition(GetNetEntity(uid));

            if (_uiSystem.IsUiOpen(GetEntity(comp.PdaEntity)!.Value, ContractorPdaKey.Key))
            {
                _uiSystem.ServerSendUiMessage(
                    GetEntity(comp.PdaEntity.Value),
                    ContractorPdaKey.Key,
                    new ContractorUpdateButtonStateMessage(isEnabled));
            }
        }
    }

    private void OnContractorCompInit(Entity<ContractorComponent> ent, ref ComponentInit ev)
    {
        GenerateContracts(ent);
    }

    private void OnOpenPortalEvent(Entity<ContractorComponent> ent, ref OpenPortalContractorEvent args)
    {
        if (!TryComp<ContractorTargetComponent>(GetEntity(ent.Comp.CurrentContractEntity), out var target))
            return;

        if (args.Cancelled || args.Handled || target.PortalEntity != null)
            return;

        target.PortalEntity = SpawnAtPosition("ContractorPortal", Transform(ent.Owner).Coordinates);
        target.PositionVector = Transform(ent.Owner).Coordinates.Position;

        args.Handled = true;

        Dirty(GetEntity(ent.Comp.CurrentContractEntity!.Value), target);
    }

    //TODO server and checks
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

        if (target.AmountTc > 8)
        {
            _adminLogger.Add(
                LogType.Action,
                LogImpact.Extreme,
                $"Contractor {ev.Actor} accepted unknown contract {ev.ContractEntity} with very big amount");

            return;
        }

        foreach (var amountPosition in ev.ContractData.AmountPositions)
        {
            target.Position = GetCoordinates(amountPosition.Value);
            target.AmountTc = amountPosition.Key;
            target.Performer = ev.Actor;
        }
        Dirty(GetEntity(ev.ContractEntity), target);

        contractorComponent.CurrentContractData = ev.ContractData;
        contractorComponent.CurrentContractEntity = ev.ContractEntity;
        contractorComponent.PdaEntity = ev.Entity;

        Dirty(ev.Actor, contractorComponent);

        if (TryComp<ContractorPdaComponent>(GetEntity(ev.Entity), out var contractorPdaComponent))
        {
            contractorPdaComponent.CurrentContractEntity = ev.ContractEntity;
            contractorPdaComponent.CurrentContractData = ev.ContractData;
            Dirty(GetEntity(ev.Entity), contractorPdaComponent);
        }
    }

    // TODO on server //DONE
    private void OnExecuteContract(Entity<ContractorPdaComponent> ent, ref ContractorExecutionButtonPressedMessage ev)
    {
        var entity = GetEntity(ent.Comp.CurrentContractEntity);

        if (!TryComp<ContractorTargetComponent>(entity, out var contractorComponent))
            return;

        if (contractorComponent.PortalEntity != null)
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, ev.Actor, 5f, new OpenPortalContractorEvent(), ev.Actor)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        });
    }

    private void OnWithdrawTc(Entity<ContractorPdaComponent> ent, ref ContractorWithdrawTcMessage ev)
    {
        if (!TryComp<ContractorComponent>(GetEntity(ent.Comp.PdaOwner), out var contractorComponent))
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

    public void GenerateContracts(Entity<ContractorComponent> ent)
    {
        var playerPool = _lookup.GetEntitiesInRange(ent.Owner, 10f).ToList();

        _random.Shuffle(playerPool);

        foreach (var player in playerPool)
        {
            if (HasComp<GhostComponent>(player) ||
                HasComp<ContractorComponent>(player) ||
                HasComp<SSDIndicatorComponent>(player) ||
                HasComp<ContractorTargetComponent>(player) ||
                !HasComp<MetaDataComponent>(player))
            {
                continue;
            }

            if (_container.IsEntityInContainer(player))
                continue;

            if (ent.Comp.Contracts.ContainsKey(GetNetEntity(player)))
                continue;

            _jobs.MindTryGetJobName(player, out var jobName);

            if (ent.Comp.Contracts is { Count: >= 5 })
                return;

            ent.Comp.Contracts.Add(GetNetEntity(player),
                new ContractorContract
                {
                    Job = jobName,
                    AmountPositions = GeneratePositionsForTargets(ent.Owner, player),
                });
        }
    }
    private Dictionary<FixedPoint2, NetCoordinates> GeneratePositionsForTargets(EntityUid contractor, EntityUid target)
    {
        return new Dictionary<FixedPoint2, NetCoordinates> { { FixedPoint2.New(10) + target.Id, GetNetCoordinates(Transform(target).Coordinates)} };
    }

    public bool IsCloseWithPosition(NetEntity playerNet)
    {
        var player = GetEntity(playerNet);

        if (!TryComp<ContractorComponent>(player, out var contractorComponent))
            return false;

        if (contractorComponent.CurrentContractEntity is null &&
            contractorComponent.CurrentContractData is null)
            return false;

        var playerPosition = GetNetCoordinates(Transform(player).Coordinates).Position;

        var targetPosition = contractorComponent.CurrentContractData!.Value.AmountPositions.FirstOrDefault().Value.Position;

        var distance = (playerPosition - targetPosition).Length();

        return distance < 4f; //4 tiles distance
    }
}
