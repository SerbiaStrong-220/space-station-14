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

public sealed class ATMSystem : SharedEconomyATMSystem
{
    [Dependency] private readonly EconomyBankCardSystem _bankCardSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EconomyATMComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<EconomyATMComponent, EntInsertedIntoContainerMessage>(OnCardInserted);
        SubscribeLocalEvent<EconomyATMComponent, EntRemovedFromContainerMessage>(OnCardRemoved);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMBankAccountLinkMessage>(OnLinkMessage);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMBankAccountCreateMessage>(OnCreateMessage);
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

        if (!TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlotsComponent)
            || !_itemSlotsSystem.TryGetSlot(ent.Owner, IdCardSlotName, out var itemSlot, component: itemSlotsComponent)
            || !itemSlot.HasItem
            || !TryComp<EconomyBankCardComponent>(itemSlot.Item, out var bankCard)
            || !_bankCardSystem.TryGetAccount(bankCard.AccountId, out var account))
        {
            PopupSystem.PopupEntity(Loc.GetString("economy-atm-insert-cash-error-popup"), args.Target, args.User, PopupType.Medium);
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent.Owner);
            return;
        }

        _bankCardSystem.TryChangeBalance(account.AccountId, account.Balance + stack.Count);
        _audioSystem.PlayPvs(ent.Comp.SoundInsertCurrency, ent.Owner);
        ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-select-withdraw-amount");
        UpdateUiState(ent);
        QueueDel(args.Used);
        args.Handled = true;
    }

    private void OnCardInserted(Entity<EconomyATMComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
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
        SoftResetATM(ent);
        UpdateUiState(ent);
    }

    protected override void OnEnterButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadEnterMessage args)
    {
        if (!TryComp<ItemSlotsComponent>(ent.Owner, out var itemSlotsComponent)
            || !_itemSlotsSystem.TryGetSlot(ent.Owner, IdCardSlotName, out var itemSlot, component: itemSlotsComponent)
            || !itemSlot.HasItem
            || !TryComp<EconomyBankCardComponent>(itemSlot.Item, out var bankCard)
            || ent.Comp.PinInput.Length != SharedEconomyBankCardSystem.PinCodeLength
            || args.Amount <= 0
            )
            return;

        var isATMEmagged = HasComp<EmaggedComponent>(ent.Owner);

        if (!_bankCardSystem.TryGetAccount(bankCard.AccountId, out var account)
            || account.AccountPin.ToString() != ent.Comp.PinInput && !isATMEmagged)
        {
            PopupSystem.PopupEntity(Loc.GetString("economy-atm-wrong-pin"), ent.Owner);
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent.Owner);
            ent.Comp.PinInput = string.Empty;
            UpdateUiState(ent);
            return;
        }

        var emaggedTax = 0;

        if (isATMEmagged)
            emaggedTax = EconomyBankCardSystem.GetEmaggedTax(args.Amount);

        var amount = args.Amount + emaggedTax;

        var isWithdrawingAll = int.IsNegative(account.Balance - amount);

        var amountToWithdraw = isWithdrawingAll ? account.Balance : amount;

        _bankCardSystem.TryChangeBalance(account.AccountId, account.Balance - amountToWithdraw);
        _stackSystem.Spawn(amountToWithdraw - emaggedTax, CashProto, Transform(ent.Owner).Coordinates);
        _audioSystem.PlayPvs(ent.Comp.SoundWithdrawCurrency, ent.Owner);
        ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-select-withdraw-amount");
        ent.Comp.PinInput = string.Empty;
        UpdateUiState(ent);
    }

    private void OnLinkMessage(Entity<EconomyATMComponent> ent, ref EconomyATMBankAccountLinkMessage args)
    {
        if (ent.Comp.CardState != CardStateEnum.Invalid || ent.Comp.BankAccount.AccountId != default)
            return;

        if (!TryComp<EconomySalaryReceiverComponent>(args.Actor, out var economySalaryReceiverComponent))
        {
            ent.Comp.UnemployedAlert = true;
            ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-no-account-unemployed");
            UpdateUiState(ent);
            return;
        }

        var container = _container.GetContainer(ent.Owner, IdCardSlotName);
        foreach (var item in container.ContainedEntities)
        {
            if (!HasComp<IdCardComponent>(item))
                continue;

            // We do expect only one item in list, but whatever
            var comp = EnsureComp<EconomyBankCardComponent>(item);
            comp.AccountId = economySalaryReceiverComponent.AccountId;
            SyncATMWithBankCard(ent, (item, comp));
            break;
        }

        UpdateUiState(ent);
    }

    private void OnCreateMessage(Entity<EconomyATMComponent> ent, ref EconomyATMBankAccountCreateMessage args)
    {
        if (HasComp<EconomySalaryReceiverComponent>(args.Actor)
            || !HasComp<HumanoidAppearanceComponent>(args.Actor)
            || ent.Comp.BankAccount.AccountId != default
            || !ent.Comp.UnemployedAlert
            )
            return;

        var container = _container.GetContainer(ent.Owner, IdCardSlotName);
        foreach (var item in container.ContainedEntities)
        {
            if (!TryComp<IdCardComponent>(item, out var idCardComponent))
                continue;

            // We do expect only one item in list, but whatever
            var comp = EnsureComp<EconomyBankCardComponent>(item);

            var account = _bankCardSystem.CreateAccount();
            account.AccountOwnerName = idCardComponent.FullName ?? string.Empty;

            EnsureComp<EconomySalaryReceiverComponent>(args.Actor, out var economySalaryReceiverComponent);

            economySalaryReceiverComponent.AccountId = account.AccountId;
            economySalaryReceiverComponent.AccountPin = account.AccountPin;

            comp.AccountId = economySalaryReceiverComponent.AccountId;

            SyncATMWithBankCard(ent, (item, comp));
            break;
        }

        UpdateUiState(ent);
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
