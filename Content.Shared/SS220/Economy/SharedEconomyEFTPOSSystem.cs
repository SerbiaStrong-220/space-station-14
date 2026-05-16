// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

public abstract partial class SharedEconomyEFTPOSSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSPaymentMessage>(OnPaymentButtonPressed);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSPrintReceiptMessage>(OnPrintReceiptButtonPressed);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSKeypadEnterMessage>(OnEnterButtonPressed);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSLockMessage>(OnCardLockButtonPressed);

        SubscribeLocalEvent<EconomyEFTPOSComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EconomyEFTPOSComponent, EconomyEFTPOSResetEvent>(OnEFTPOSReset);
    }

    private void OnPaymentButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSPaymentMessage args)
    {
        var itemInHands = _handsSystem.GetActiveItem(args.Actor);
        if (!itemInHands.HasValue || !TryComp<EconomyBankCardComponent>(itemInHands, out var bankCard))
            return;

        if (ent.Comp.OwnerBankAccountId == default
            || bankCard.AccountId == default
            || ent.Comp.OwnerBankAccountId == bankCard.AccountId
            )
            return;

        ent.Comp.PayerBankAccountId = bankCard.AccountId;

        UpdateUiState(ent);
    }

    private void OnPrintReceiptButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSPrintReceiptMessage args)
    {
        ent.Comp.PrintReceipt = !ent.Comp.PrintReceipt;
        UpdateUiState(ent);
    }

    private void OnKeypadButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSKeypadMessage args)
    {
        if (ent.Comp.PayerPinInput.Length >= SharedEconomyBankCardSystem.PinCodeLength)
            return;

        if (ent.Comp.PayerBankAccountId == default)
            return;

        ent.Comp.PayerPinInput += args.Value.ToString();

        UpdateUiState(ent);
    }

    private void OnClearButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSKeypadClearMessage args)
    {
        ent.Comp.PayerBankAccountId = default;
        ent.Comp.PayerPinInput = string.Empty;

        UpdateUiState(ent);
    }

    protected abstract void OnEnterButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSKeypadEnterMessage args);

    private void OnCardLockButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSLockMessage args)
    {
        var itemInHands = _handsSystem.GetActiveItem(args.Actor);
        if (!itemInHands.HasValue
            || !TryComp<EconomyBankCardComponent>(itemInHands, out var bankCard)
            || bankCard.AccountId == default)
        {
            return;
        }

        if (ent.Comp.OwnerBankAccountId == default)
        {
            ent.Comp.OwnerBankAccountId = bankCard.AccountId;
            ent.Comp.Amount = args.Amount;
        }
        else if (ent.Comp.OwnerBankAccountId == bankCard.AccountId)
        {
            ent.Comp.OwnerBankAccountId = default;
            ent.Comp.Amount = 0;
        }

        UpdateUiState(ent);
    }

    private void OnInteractUsing(Entity<EconomyEFTPOSComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var tool) && _tool.HasQuality(args.Used, ent.Comp.EFTPOSResetMethod, tool))
        {
            args.Handled = true;
            _tool.UseTool(args.Used, args.User, ent, ent.Comp.EFTPOSResetDelay, ent.Comp.EFTPOSResetMethod, new EconomyEFTPOSResetEvent(), toolComponent: tool);
        }
    }

    private void OnEFTPOSReset(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSResetEvent args)
    {
        if (args.Cancelled)
            return;

        ent.Comp.OwnerBankAccountId = default;
        ent.Comp.Amount = default;
        ent.Comp.PayerBankAccountId = default;
        ent.Comp.PayerPinInput = string.Empty;
        ent.Comp.PrintReceipt = false;

        UpdateUiState(ent);

        var locSelf = Loc.GetString("economy-eftpos-reset-self");
        var locOthers = Loc.GetString("economy-eftpos-reset-others", ("user", Identity.Name(args.User, EntityManager)));

        _popupSystem.PopupPredicted(locSelf, locOthers, ent, args.User);
    }

    protected void UpdateUiState(Entity<EconomyEFTPOSComponent> ent)
    {
        var state = new EconomyEFTPOSUiState
        {
            Locked = ent.Comp.OwnerBankAccountId != default,
            Amount = ent.Comp.Amount,
            OwnerBankAccountId = ent.Comp.OwnerBankAccountId,
            OwnerName = GetOwner(ent.Comp.OwnerBankAccountId),
            PayerBankAccountId = ent.Comp.PayerBankAccountId,
            PayerPinInput = ent.Comp.PayerPinInput,
            PrintReceipt = ent.Comp.PrintReceipt
        };

        _ui.SetUiState(ent.Owner, EconomyEFTPOSKey.Key, state);
    }

    protected abstract string GetOwner(int ownerBankAccountId);

    [Serializable, NetSerializable]
    public sealed partial class EconomyEFTPOSResetEvent : SimpleDoAfterEvent
    {
    }
}

