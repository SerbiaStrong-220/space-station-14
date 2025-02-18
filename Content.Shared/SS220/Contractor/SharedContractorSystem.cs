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
        SubscribeLocalEvent<ContractorPdaComponent, ComponentInit>(OnContractorPdaCompInit);
        SubscribeLocalEvent<ContractorPdaComponent, BoundUIOpenedEvent>(OnOpenUI);
        SubscribeLocalEvent<ContractorPdaComponent, ContractorHubBuyItemMessage>(OnBuyItem);
    }

    /// <summary>
    /// On CompInit pda generate available items
    /// </summary>
    /// <param name="ent">Entity pda</param>
    /// <param name="args">Event</param>
    private void OnContractorPdaCompInit(Entity<ContractorPdaComponent> ent, ref ComponentInit args)
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

    /// <summary>
    /// Will generate a pda owner once for the person who first opened it
    /// </summary>
    /// <param name="ent">PDA entity</param>
    private void OnOpenUI(Entity<ContractorPdaComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (ent.Comp.PdaOwner != null)
            return;

        if (!TryComp<ContractorComponent>(args.Actor, out var contractorComponent))
            return;

        if (contractorComponent.PdaEntity != null)
            return;

        contractorComponent.PdaEntity = ent.Owner;

        ent.Comp.PdaOwner = GetNetEntity(args.Actor);

        Dirty(ent);
    }

    /// <summary>
    /// Raise event for buying an item, but only if buyer is pda owner
    /// </summary>
    private void OnBuyItem(Entity<ContractorPdaComponent> ent, ref ContractorHubBuyItemMessage ev)
    {
        if (!TryComp<ContractorComponent>(ev.Actor, out var contractorComponent))
            return;

        if (ev.Actor != GetEntity(ent.Comp.PdaOwner))
            return;

        if (ev.Price.Quantity is <= 0)
            return;

        if (contractorComponent.Reputation < ev.Price.Amount)
            return;

        contractorComponent.Reputation -= ev.Price.Amount.Int();

        var coordinates = Transform(ev.Actor).Coordinates;
        var itemToSpawn = Spawn(ev.Item, coordinates);

        _hands.PickupOrDrop(ev.Actor, itemToSpawn);

        ent.Comp.AvailableItems[ev.Item].Quantity--;
        Dirty(ent);
        _uiSystem.ServerSendUiMessage(ent.Owner, ContractorPdaKey.Key, new ContractorUpdateStatsMessage());

        Dirty(ev.Actor, contractorComponent);
    }
}

/// <summary>
/// Event for opening a portal
/// </summary>
[Serializable, NetSerializable]
public sealed partial class OpenPortalContractorEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Event for teleporting target to station
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TeleportTargetToStationEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
