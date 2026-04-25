// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.CartridgeLoader;
using Content.Shared.CartridgeLoader;
using Content.Shared.PDA;
using Content.Shared.SS220.Economy;

namespace Content.Server.SS220.Economy;

public sealed class EconomyBankingCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly EconomyBankCardSystem _bankCardSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyBankingCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
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
            AccountId = default,
            OwnerName = string.Empty,
            Balance = default
        };

        if (!TryComp<PdaComponent>(loaderUid, out var pdaComponent) || !pdaComponent.ContainedId.HasValue)
        {
            _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
            return;
        }

        if (pdaComponent.ContainedId.Value != default)
        {
            state.CardState = CardStateEnum.Invalid;

            if (TryComp<EconomyBankCardComponent>(pdaComponent.ContainedId.Value, out var economyBankCardComponent)
                && _bankCardSystem.TryGetAccount(economyBankCardComponent.AccountId, out var account))
            {
                state.CardState = CardStateEnum.Valid;
                state.AccountId = economyBankCardComponent.AccountId;
                state.OwnerName = account.AccountOwnerName;
                state.Balance = account.Balance;
            }
        }

        _cartridgeLoaderSystem?.UpdateCartridgeUiState(loaderUid, state);
    }
}
