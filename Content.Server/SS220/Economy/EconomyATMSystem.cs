// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Stack;
using Content.Shared.Emag.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.SS220.Economy;
using Robust.Server.Containers;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Content.Shared.Cargo.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.IdentityManagement;
using Content.Shared.Tools.Components;
using Content.Shared.Access.Components;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.Economy;

public sealed partial class ATMSystem : SharedEconomyATMSystem
{
    [Dependency] private EconomyBankCardSystem _bankCardSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private StackSystem _stackSystem = default!;
    [Dependency] private SharedAudioSystem _audioSystem = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EconomyATMComponent, ComponentStartup>(OnComponentStartup);

        SubscribeLocalEvent<EconomyATMComponent, EntInsertedIntoContainerMessage>(OnCardInserted);
        SubscribeLocalEvent<EconomyATMComponent, EntRemovedFromContainerMessage>(OnCardRemoved);

        SubscribeLocalEvent<EconomyATMComponent, EconomyATMBankAccountLinkMessage>(OnLinkMessage);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMBankAccountCreateMessage>(OnCreateMessage);

        SubscribeLocalEvent<EconomyBalanceChangedEvent>(OnBalanceChanged);
    }

    private void OnBalanceChanged(ref EconomyBalanceChangedEvent args)
    {
        var query = EntityQueryEnumerator<EconomyATMComponent>();
        while (query.MoveNext(out var uid, out var atm))
        {
            if (atm.BankAccount.AccountId == args.AccountId)
                UpdateUiState((uid, atm));
        }
    }

    private bool TryGetBankCard(EntityUid atm, out Entity<EconomyBankCardComponent> card)
    {
        card = default;
        if (_itemSlotsSystem.GetItemOrNull(atm, IdCardSlotName) is not { } uid
            || !TryComp<EconomyBankCardComponent>(uid, out var comp))
        {
            return false;
        }

        card = (uid, comp);
        return true;
    }

    private void LinkCard(Entity<EconomyATMComponent> atm, Entity<EconomyBankCardComponent> card, int accountId)
    {
        card.Comp.AccountId = accountId;
        Dirty(card);

        SyncATMWithBankCard(atm, card);
        UpdateUiState(atm);
    }

    private void OnComponentStartup(Entity<EconomyATMComponent> ent, ref ComponentStartup args)
    {
        SoftResetATM(ent);
        UpdateUiState(ent);
    }

    protected override void OnInteractUsing(Entity<EconomyATMComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var tool) && Tool.HasQuality(args.Used, ent.Comp.ATMResetMethod, tool))
        {
            args.Handled = true;
            Tool.UseTool(args.Used, args.User, ent, ent.Comp.ATMResetDelay, ent.Comp.ATMResetMethod, new EconomyATMResetEvent(), toolComponent: tool);
            return;
        }

        if (!HasComp<CashComponent>(args.Used) || !TryComp<StackComponent>(args.Used, out var stack))
            return;

        if (!TryGetBankCard(ent, out var bankCard)
            || !_bankCardSystem.TryDeposit(bankCard.Comp.AccountId, stack.Count))
        {
            PopupSystem.PopupEntity(Loc.GetString("economy-atm-insert-cash-error-popup"), args.Target, args.User, PopupType.Medium);
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent.Owner);
            return;
        }

        _audioSystem.PlayPvs(ent.Comp.SoundInsertCurrency, ent.Owner);
        ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-select-withdraw-amount");
        UpdateUiState(ent);
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnCardInserted(Entity<EconomyATMComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != IdCardSlotName)
            return;

        if (!TryComp<EconomyBankCardComponent>(args.Entity, out var bankCard))
        {
            _container.EmptyContainer(args.Container);
            return;
        }

        SyncATMWithBankCard(ent, (args.Entity, bankCard));
        UpdateUiState(ent);
    }

    private void SyncATMWithBankCard(Entity<EconomyATMComponent> entATM, Entity<EconomyBankCardComponent> entBankCard)
    {
        entATM.Comp.PinInput = string.Empty;
        entATM.Comp.UnemployedAlert = false;
        entATM.Comp.BankAccount = new();

        if (!_bankCardSystem.TryGetAccount(entBankCard.Comp.AccountId, out var account))
        {
            entATM.Comp.InfoMessage = Loc.GetString("economy-atm-ui-no-account");
            entATM.Comp.CardState = CardStateEnum.Invalid;
            return;
        }

        entATM.Comp.InfoMessage = Loc.GetString("economy-atm-ui-select-withdraw-amount");
        entATM.Comp.CardState = CardStateEnum.Valid;
        entATM.Comp.BankAccount = account;
    }

    private void OnCardRemoved(Entity<EconomyATMComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != IdCardSlotName)
            return;

        SoftResetATM(ent);
        UpdateUiState(ent);
    }

    protected override void OnEnterButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadEnterMessage args)
    {
        if (!TryGetBankCard(ent, out var bankCard)
            || ent.Comp.PinInput.Length != SharedEconomyBankCardSystem.PinCodeLength
            || args.Amount <= 0)
        {
            return;
        }

        var isATMEmagged = HasComp<EmaggedComponent>(ent.Owner);

        if (!_bankCardSystem.TryGetAccount(bankCard.Comp.AccountId, out var account)
            || account.AccountPin.ToString() != ent.Comp.PinInput && !isATMEmagged)
        {
            PopupSystem.PopupEntity(Loc.GetString("economy-atm-wrong-pin"), ent.Owner);
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent.Owner);
            ent.Comp.PinInput = string.Empty;
            UpdateUiState(ent);
            return;
        }

        ent.Comp.PinInput = string.Empty;
        if (!_bankCardSystem.TryWithdrawCash(account.AccountId, args.Amount, isATMEmagged, out var cash))
        {
            ent.Comp.InfoMessage = Loc.GetString("economy-atm-withdraw-failed");
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent.Owner);
            UpdateUiState(ent);
            return;
        }

        _stackSystem.SpawnAtPosition(cash, CashProto, Transform(ent.Owner).Coordinates);
        _audioSystem.PlayPvs(ent.Comp.SoundWithdrawCurrency, ent.Owner);
        ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-select-withdraw-amount");
        UpdateUiState(ent);
    }

    private void OnLinkMessage(Entity<EconomyATMComponent> ent, ref EconomyATMBankAccountLinkMessage args)
    {
        if (ent.Comp.CardState != CardStateEnum.Invalid
            || !TryGetBankCard(ent, out var card) || !HasComp<IdCardComponent>(card))
        {
            return;
        }

        if (!TryComp<EconomySalaryReceiverComponent>(args.Actor, out var economySalaryReceiverComponent))
        {
            ent.Comp.UnemployedAlert = true;
            ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-no-account-unemployed");
            UpdateUiState(ent);
            return;
        }

        LinkCard(ent, card, economySalaryReceiverComponent.AccountId);
    }

    private void OnCreateMessage(Entity<EconomyATMComponent> ent, ref EconomyATMBankAccountCreateMessage args)
    {
        if (HasComp<EconomySalaryReceiverComponent>(args.Actor)
            || !HasComp<HumanoidProfileComponent>(args.Actor)
            || ent.Comp.CardState != CardStateEnum.Invalid
            || !ent.Comp.UnemployedAlert
            || !TryGetBankCard(ent, out var card)
            || !TryComp<IdCardComponent>(card, out var idCard))
        {
            return;
        }

        if (!_bankCardSystem.TryCreateAccount(out var account))
            return;

        account.AccountOwnerName = idCard.FullName ?? string.Empty;
        var receiver = EnsureComp<EconomySalaryReceiverComponent>(args.Actor);
        receiver.AccountId = account.AccountId;
        receiver.AccountPin = account.AccountPin;
        LinkCard(ent, card, account.AccountId);
    }

    protected override void OnATMReset(Entity<EconomyATMComponent> ent, ref EconomyATMResetEvent args)
    {
        if (args.Cancelled)
            return;

        RemComp<EmaggedComponent>(ent);
        SoftResetATM(ent);
        UpdateUiState(ent);

        var container = _container.GetContainer(ent.Owner, IdCardSlotName);
        _container.EmptyContainer(container);

        var locSelf = Loc.GetString("economy-atm-reset-self");
        var locOthers = Loc.GetString("economy-atm-reset-others", ("user", Identity.Name(args.User, EntityManager)));

        PopupSystem.PopupPredicted(locSelf, locOthers, ent, args.User);
    }
}
