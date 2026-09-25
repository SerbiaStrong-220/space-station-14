// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Chat.Managers;
using Content.Server.Kitchen.Components;
using Content.Server.Popups;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Kitchen;
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.SS220.Economy;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Economy;

public sealed partial class EconomyBankCardSystem : SharedEconomyBankCardSystem
{
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedHandsSystem _handsSystem = default!;
    [Dependency] private SharedStorageSystem _storageSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedStackSystem _stackSystem = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private PopupSystem _popupSystem = default!;
    [Dependency] private IChatManager _chatManager = default!;

    private static readonly EntProtoId SpaceCashProto = "SpaceCash";
    private const string BackSlot = "back";
    private const string Pocket1Slot = "pocket1";
    private const string Pocket2Slot = "pocket2";

    public const int FlatEmaggedTax = 5;
    public const int PercentEmaggedTax = 1;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<EconomyBankCardComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(Entity<EconomyBankCardComponent> ent, ref BeingMicrowavedEvent args)
    {
        if (!TryComp<MicrowaveComponent>(args.Microwave, out var micro) || micro.Broken)
            return;

        if (!_random.Prob(ent.Comp.MicrowaveResetChance))
            return;

        ent.Comp.AccountId = default;
        Dirty(ent);
    }

    public override void PonderForData(Entity<EconomySalaryReceiverComponent> user)
    {
        string msg;

        if (user.Comp.AccountId == default || user.Comp.AccountPin == default)
            msg = Loc.GetString("economy-ponder-for-data-failed");
        else
            msg = Loc.GetString("economy-ponder-for-data-success", ("accountId", user.Comp.AccountId), ("accountPin", user.Comp.AccountPin));

        if (!TryComp(user, out ActorComponent? actor))
            return;

        _popupSystem.PopupEntity(msg, user, user, PopupType.Medium);
        _chatManager.ChatMessageToOne(ChatChannel.Local, msg, msg, EntityUid.Invalid, false, actor.PlayerSession.Channel);
    }

    private void OnPlayerSpawned(PlayerSpawnCompleteEvent ev)
    {
        if (!_idCardSystem.TryFindIdCard(ev.Mob, out var idCard) || !TryComp<EconomyBankCardComponent>(idCard, out var bankCardComponent))
            return;

        if (ev.JobId is null || !_prototypeManager.Resolve<JobPrototype>(ev.JobId, out var jobPrototype))
            return;

        var startingBalance = 0;

        if (_prototypeManager.Resolve(jobPrototype.EconomySalaryPrototype, out var economySalaryPrototype))
            startingBalance = economySalaryPrototype.Amount;

        if (!TryCreateAccount(out var account, startingBalance: startingBalance))
            return;

        bankCardComponent.AccountId = account.AccountId;

        account.AccountOwnerName = Name(ev.Mob);

        Dirty(idCard, bankCardComponent);

        EnsureComp<EconomySalaryReceiverComponent>(ev.Mob, out var economySalaryReceiverComponent);

        economySalaryReceiverComponent.AccountId = account.AccountId;
        economySalaryReceiverComponent.AccountPin = account.AccountPin;

        SpaceCashWithdrawalOnSpawn(ev.Mob);
    }

    /// <summary>
    /// Handles logic if player selected SpaceCashWithdrawalOnSpawn item in loadout.
    /// </summary>
    /// In order to do so we need:
    /// 1 - find items with EconomyCashWithdrawalOnSpawnComponent.
    /// 2 - delete those items.
    /// 3 - empty player bank account.
    /// 4 - give player withdrawn money.
    private void SpaceCashWithdrawalOnSpawn(EntityUid user)
    {
        if (!_inventorySystem.TryGetSlotContainer(user, BackSlot, out var backSlot, out _)
            || !TryComp<StorageComponent>(backSlot.ContainedEntity, out var storageComponent))
        {
            return;
        }

        var result = 0;

        foreach (var item in storageComponent.StoredItems)
        {
            result += CashWithdrawalOnSpawn(item.Key);
        }

        // No items found
        if (result <= 0)
            return;

        if (!TryComp<EconomySalaryReceiverComponent>(user, out var economySalaryReceiverComponent))
            return;

        // No money in account
        if (!CashWithdrawal(economySalaryReceiverComponent.AccountId, out var withdrawnAmount))
            return;

        if (!HasComp<TransformComponent>(user))
            return;

        var itemToSpawn = Spawn(SpaceCashProto, Transform(user).Coordinates);
        _stackSystem.SetCount((itemToSpawn, null), withdrawnAmount);

        // Try insert into the backpack
        if (backSlot.ContainedEntity.HasValue &&
            _storageSystem.Insert(backSlot.ContainedEntity.Value, itemToSpawn, out _, playSound: false))
        {
            return;
        }

        // Try insert into pockets
        if (_inventorySystem.TryGetSlotContainer(user, Pocket1Slot, out var pocket1, out _)
            && _containerSystem.Insert(itemToSpawn, pocket1))
        {
            return;
        }

        if (_inventorySystem.TryGetSlotContainer(user, Pocket2Slot, out var pocket2, out _)
            && _containerSystem.Insert(itemToSpawn, pocket2))
        {
            return;
        }

        // Try insert into hands or drop on the floor
        _handsSystem.PickupOrDrop(user, itemToSpawn, checkActionBlocker: false, animate: false, dropNear: true);
    }

    /// <summary>
    /// If item has EconomyCashWithdrawalOnSpawnComponent returns 1 and deletes it. Otherwise return 0.
    /// </summary>
    private int CashWithdrawalOnSpawn(EntityUid item)
    {
        if (!HasComp<EconomyCashWithdrawalOnSpawnComponent>(item))
            return 0;

        QueueDel(item);
        return 1;
    }

    /// <summary>
    /// Withdraw specified amount from accountId.
    /// </summary>
    /// <param name="accountId">AccountId to withdraw from.</param>
    /// <param name="amountToWithdraw">Amount to withdraw. If default — withdraw all.</param>
    /// <returns>True if withdrawal is a success.</returns>
    public bool CashWithdrawal(int accountId, out int withdrawnAmount, int amountToWithdraw = default)
    {
        withdrawnAmount = 0;

        if (amountToWithdraw < 0 || !TryGetAccount(accountId, out var account))
            return false;

        var balance = account.Balance;

        if (balance <= 0 || balance < amountToWithdraw)
            return false;

        if (amountToWithdraw == default)
            amountToWithdraw = balance;

        TryChangeBalance(account.AccountId, balance - amountToWithdraw);

        withdrawnAmount = amountToWithdraw;
        return true;
    }

    /// <summary>
    /// Withdraws cash and the ATM fee without overflowing or charging for an empty payout.
    /// </summary>
    public bool TryWithdrawCash(int accountId, int amount, bool emagged, out int cash)
    {
        cash = 0;
        if (amount <= 0 || !TryGetAccount(accountId, out var account) || amount > account.Balance)
            return false;

        var fee = emagged ? GetEmaggedTax(amount) : 0;
        var debit = (int)Math.Min((long)amount + fee, account.Balance);
        if (debit <= fee || !CashWithdrawal(accountId, out _, debit))
            return false;

        cash = debit - fee;
        return true;
    }

    public bool TryDeposit(int accountId, int amount)
    {
        if (amount <= 0 || !TryGetAccount(accountId, out var account)
            || account.Balance < 0 || amount > int.MaxValue - account.Balance)
        {
            return false;
        }

        return TryChangeBalance(accountId, account.Balance + amount);
    }

    /// <summary>
    /// Validates both accounts before changing either balance.
    /// </summary>
    public bool TryTransfer(int payerId, int ownerId, int amount)
    {
        if (amount <= 0 || payerId == ownerId
            || !TryGetAccount(payerId, out var payer)
            || !TryGetAccount(ownerId, out var owner)
            || payer.Balance < amount || owner.Balance < 0
            || amount > int.MaxValue - owner.Balance)
        {
            return false;
        }

        payer.Balance -= amount;
        owner.Balance += amount;
        NotifyBalanceChanged(payerId);
        NotifyBalanceChanged(ownerId);
        return true;
    }

    /// <summary>
    /// Creates BankAccount with specified unique accountId and startingBalance.
    /// If accountId already taken — returns BankAccount with this accountId.
    /// If accountId = default — creates new random accountId.
    /// </summary>
    public bool TryCreateAccount([NotNullWhen(true)] out BankAccount? account, int accountId = default, int startingBalance = 0)
    {
        account = null;
        var bankAccounts = GetBankAccounts();
        if (bankAccounts == null || startingBalance < 0)
            return false;

        if (TryGetAccount(accountId, out account))
            return true;

        if (accountId == default)
        {
            do
            {
                accountId = _random.Next(100000, 1000000);
            } while (bankAccounts.Any(x => x.AccountId == accountId));
        }

        var accountPin = _random.Next((int)Math.Pow(10, PinCodeLength - 1), (int)Math.Pow(10, PinCodeLength));
        account = new BankAccount(accountId, accountPin, startingBalance);
        bankAccounts.Add(account);
        return true;
    }

    public bool TryGetAccount(int accountId, [NotNullWhen(true)] out BankAccount? account)
    {
        if (accountId == default)
        {
            account = default;
            return false;
        }

        var bankAccounts = GetBankAccounts();

        account = bankAccounts?.FirstOrDefault(x => x.AccountId == accountId);
        return account != null;
    }

    public bool TryChangeBalance(int accountId, int amount)
    {
        if (amount < 0 || !TryGetAccount(accountId, out var account))
            return false;

        account.Balance = amount;
        NotifyBalanceChanged(accountId);

        return true;
    }

    private void NotifyBalanceChanged(int accountId)
    {
        var ev = new EconomyBalanceChangedEvent(accountId);
        RaiseLocalEvent(ref ev);
    }

    public static int GetEmaggedTax(int input)
    {
        return FlatEmaggedTax + (int)((long)input * PercentEmaggedTax / 100);
    }

    public List<BankAccount>? GetBankAccounts()
    {
        var enumerator = EntityQueryEnumerator<EconomyDeCentralBankComponent>();
        while (enumerator.MoveNext(out _, out var deCentralBankComponent))
        {
            if (deCentralBankComponent.IsCentralNode)
                return deCentralBankComponent.Accounts;
        }

        return null;
    }
}

[ByRefEvent]
public readonly record struct EconomyBalanceChangedEvent(int AccountId);
