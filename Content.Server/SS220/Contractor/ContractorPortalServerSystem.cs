using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.Contractor;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Contractor;

public sealed class ContractorPortalServerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ContractorServerSystem _contractorServer = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ContractorPortalOnTriggerComponent, StartCollideEvent>(OnEnterPortal);
    }

    /// <summary>
    /// This is used for what doing when player enter portal
    /// </summary>
    private void OnEnterPortal(Entity<ContractorPortalOnTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<ContractorTargetComponent>(args.OtherEntity, out var targetComponent))
        {
            _popup.PopupClient(Loc.GetString("contractor-portal-for-non-target"), args.OtherEntity, PopupType.Medium);
            return;
        }

        var contractorEntity = targetComponent.Performer;
        if (!TryComp<ContractorComponent>(contractorEntity, out var contractorComponent))
            return;

        var contractorPdaEntity = contractorComponent.PdaEntity;
        if (!TryComp<ContractorPdaComponent>(contractorPdaEntity, out var contractorPdaComponent))
            return;

        if (contractorComponent.CurrentContractEntity != GetNetEntity(args.OtherEntity))
            return;

        targetComponent.PortalPosition = Transform(args.OtherEntity).Coordinates;
        targetComponent.EnteredPortalTime = _timing.CurTime;

        contractorComponent.CurrentContractEntity = null;
        contractorComponent.CurrentContractData = null;
        contractorComponent.Contracts.Remove(GetNetEntity(args.OtherEntity));

        contractorPdaComponent.CurrentContractEntity = null;
        contractorPdaComponent.CurrentContractData = null;
        contractorPdaComponent.Contracts.Remove(GetNetEntity(args.OtherEntity));

        var needsPortalEntity = targetComponent.PortalOnStationEntity;

        if (needsPortalEntity != args.OurEntity)
        {
            _popup.PopupClient(Loc.GetString("contractor-portal-for-another-target"), args.OtherEntity, PopupType.Medium);
            return;
        }

        if (targetComponent.PortalOnTajpanEntity == null)
        {
            _popup.PopupClient(Loc.GetString("contractor-portal-but-not-in-other-side"), args.OtherEntity, PopupType.Medium);
            return;
        }

        if (_mob.IsDead(args.OtherEntity))
        {
            targetComponent.AmountTc = MathF.Max(0, targetComponent.AmountTc.Float() * 0.2f);
            targetComponent.AmountTc = MathF.Ceiling(targetComponent.AmountTc.Float());
        }

        contractorComponent.Reputation += contractorComponent.ReputationAward;
        contractorComponent.AmountTc += targetComponent.AmountTc;
        contractorComponent.ContractsCompleted++;
        contractorComponent.Profiles.Remove(GetNetEntity(args.OtherEntity));

        _uiSystem.ServerSendUiMessage(contractorComponent.PdaEntity!.Value, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());

        contractorComponent.BlockUntil = 20f;

        _contractorServer.GenerateContracts((contractorEntity, contractorComponent)); // generate new contracts

        _transformSystem.SetCoordinates(args.OtherEntity, Transform(targetComponent.PortalOnTajpanEntity.Value).Coordinates);

        Dirty(contractorComponent.PdaEntity!.Value, contractorPdaComponent);
        Dirty(contractorEntity, contractorComponent);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.OtherEntity,
            5f,
            new TeleportTargetToStationEvent(),
            args.OtherEntity,
            args.OtherEntity)
        {
            Hidden = true,
            RequireCanInteract = false,
        });
    }
}
