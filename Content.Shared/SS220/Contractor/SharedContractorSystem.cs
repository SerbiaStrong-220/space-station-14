using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Contractor;

public abstract class SharedContractorSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ContractorPdaComponent, ComponentInit>(OnContractorPdaMapInit);
        SubscribeLocalEvent<ContractorPdaComponent, BoundUIOpenedEvent>(OnOpenUI);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorHubBuyItemMessage>(OnBuyItem);
    }

    private void OnContractorPdaMapInit(Entity<ContractorPdaComponent> ent, ref ComponentInit args)
    {
        if (ent.Comp.AvailableItems.Count > 0)
            return;

        foreach (var itemPrototype in _prototypeManager.EnumeratePrototypes<SharedContractorItemPrototype>())
        {
            foreach (var item in itemPrototype.Items)
            {
               ent.Comp.AvailableItems.Add(item.Key, item.Value);
            }
        }
    }

    private void OnOpenUI(Entity<ContractorPdaComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.PdaOwner != null)
            return;

        if (!HasComp<ContractorComponent>(args.Actor))
            return;

        ent.Comp.PdaOwner = GetNetEntity(args.Actor);

        Dirty(ent);
    }

    private void OnBuyItem(Entity<ContractorPdaComponent> ent, ref ContractorHubBuyItemMessage ev)
    {
        if (!TryComp<ContractorComponent>(ev.Actor, out var contractorComponent))
            return;

        if (ev.Actor != GetEntity(ent.Comp.PdaOwner))
            return;

        if (contractorComponent.Reputation < ev.Price.Int())
            return;

        contractorComponent.Reputation -= ev.Price.Int();

        var coordinates = Transform(ev.Actor).Coordinates;
        var itemToSpawn = Spawn(ev.Item, coordinates);

        _hands.PickupOrDrop(ev.Actor, itemToSpawn);
        _uiSystem.ServerSendUiMessage(ent.Owner, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());

        Dirty(ev.Actor, contractorComponent);
    }

    public Dictionary<NetEntity, ContractorContract>? GetContractsForPda(EntityUid contractor, EntityUid pdaEntity)
    {
        AddContractsToPda(contractor, pdaEntity);

        return !TryComp<ContractorPdaComponent>(pdaEntity, out var contractorPdaComponent) ? null : contractorPdaComponent.Contracts;
    }

    public void AddContractsToPda(EntityUid contractor, EntityUid pdaEntity)
    {
        if (!TryComp<ContractorPdaComponent>(pdaEntity, out var contractorPdaComponent))
            return;
        if (!TryComp<ContractorComponent>(contractor, out var contractorComponent))
            return;

        contractorPdaComponent.Contracts = contractorComponent.Contracts;
    }

}

[Serializable, NetSerializable]
public sealed partial class OpenPortalContractorEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
