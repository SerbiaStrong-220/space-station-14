using Content.Shared.Popups;
using Content.Shared.SS220.Contractor;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Contractor;

/// <summary>
/// This handles...
/// </summary>
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

    private void OnEnterPortal(Entity<ContractorPortalOnTriggerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<ContractorTargetComponent>(args.OtherEntity, out var targetComponent))
        {
            _popup.PopupClient("Портал недоступен для вас", args.OtherEntity, PopupType.Medium);
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
            _popup.PopupClient("Этот портал предназначен не для этой цели", args.OtherEntity, PopupType.Medium);
            return;
        }

        contractorComponent.Reputation += contractorComponent.ReputationAward;
        contractorComponent.AmountTc += targetComponent.AmountTc;
        contractorComponent.ContractsCompleted++;

        _uiSystem.ServerSendUiMessage(GetEntity(contractorComponent.PdaEntity)!.Value, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());
        _uiSystem.ServerSendUiMessage(GetEntity(contractorComponent.PdaEntity)!.Value, ContractorPdaKey.Key, new ContractorCompletedContractMessage());

        _contractorServer.GenerateContracts((contractorEntity, contractorComponent));

        _transformSystem.SetCoordinates(args.OtherEntity, Transform(contractorEntity).Coordinates); // tp target to other map in future

        Dirty(contractorPdaEntity, contractorPdaComponent);
        Dirty(contractorEntity, contractorComponent);
    }
}
