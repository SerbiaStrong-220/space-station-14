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

        var needsPortalEntity = targetComponent.PortalEntity;
        var contractorEntity = targetComponent.Performer;

        if (!TryComp<ContractorComponent>(contractorEntity, out var contractorComponent))
            return;

        var contractorPdaEntity = GetEntity(contractorComponent.PdaEntity)!.Value;

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

        if (needsPortalEntity != args.OurEntity)
        {
            _popup.PopupClient(Loc.GetString("contractor-portal-for-another-target"), args.OtherEntity, PopupType.Medium);
            return;
        }

        contractorComponent.Reputation += contractorComponent.ReputationAward;
        contractorComponent.AmountTc += targetComponent.AmountTc;
        contractorComponent.ContractsCompleted++;

        _uiSystem.ServerSendUiMessage(GetEntity(contractorComponent.PdaEntity)!.Value, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());
        _uiSystem.ServerSendUiMessage(GetEntity(contractorComponent.PdaEntity)!.Value, ContractorPdaKey.Key, new ContractorCompletedContractMessage());

        _contractorServer.GenerateContracts((contractorEntity, contractorComponent)); // generate new contracts

        // TODO: create new warp point for another map (tajpan???)
        _transformSystem.SetCoordinates(args.OtherEntity, Transform(contractorEntity).Coordinates);

        Dirty(contractorPdaEntity, contractorPdaComponent);
        Dirty(contractorEntity, contractorComponent);
    }
}
