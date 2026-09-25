// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.PDA;
using Content.Shared.SS220.Economy;
using Robust.Shared.Containers;

namespace Content.Server.SS220.Economy;

public sealed partial class EconomyBankingCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private EconomyBankCardSystem _bankCardSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyBankingCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);

        SubscribeLocalEvent<EconomyBalanceChangedEvent>(OnBalanceChanged);

        SubscribeLocalEvent<EconomyBankCardComponent, EntGotInsertedIntoContainerMessage>(OnCardInserted);
        SubscribeLocalEvent<EconomyBankCardComponent, EntGotRemovedFromContainerMessage>(OnCardRemoved);
    }

    private bool IsBankingActive(EntityUid loader)
    {
        return TryComp<CartridgeLoaderComponent>(loader, out var cartridgeLoader)
            && HasComp<EconomyBankingCartridgeComponent>(cartridgeLoader.ActiveProgram);
    }

    private void OnBalanceChanged(ref EconomyBalanceChangedEvent args)
    {
        var query = EntityQueryEnumerator<PdaComponent, CartridgeLoaderComponent>();
        while (query.MoveNext(out var uid, out var pda, out var loader))
        {
            if (HasComp<EconomyBankingCartridgeComponent>(loader.ActiveProgram)
                && TryComp<EconomyBankCardComponent>(pda.ContainedId, out var card)
                && card.AccountId == args.AccountId)
            {
                UpdateUiState(uid);
            }
        }
    }

    private void OnCardInserted(Entity<EconomyBankCardComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId && IsBankingActive(args.Container.Owner))
            UpdateUiState(args.Container.Owner);
    }

    private void OnCardRemoved(Entity<EconomyBankCardComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId
            && !TerminatingOrDeleted(args.Container.Owner)
            && IsBankingActive(args.Container.Owner))
        {
            UpdateUiState(args.Container.Owner);
        }
    }

    private void OnUiReady(Entity<EconomyBankingCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        UpdateUiState(args.Loader);
    }

    private void UpdateUiState(EntityUid loaderUid)
    {
        var state = new EconomyBankingCartridgeUiState
        {
            CardState = CardStateEnum.Absent,
        };

        if (_itemSlots.GetItemOrNull(loaderUid, PdaComponent.PdaIdSlotId) is { } id)
        {
            state.CardState = CardStateEnum.Invalid;

            if (TryComp<EconomyBankCardComponent>(id, out var economyBankCardComponent)
                && _bankCardSystem.TryGetAccount(economyBankCardComponent.AccountId, out var account))
            {
                state.CardState = CardStateEnum.Valid;
                state.AccountId = economyBankCardComponent.AccountId;
                state.OwnerName = account.AccountOwnerName;
                state.Balance = account.Balance;
            }
        }

        _cartridgeLoaderSystem.UpdateCartridgeUiState(loaderUid, state);
    }
}
