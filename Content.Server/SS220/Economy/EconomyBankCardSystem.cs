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
using Content.Shared.Popups;
using Content.Shared.Roles;
using Content.Shared.SS220.Economy;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Server.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Economy;

public sealed class EconomyBankCardSystem : SharedEconomyBankCardSystem
{
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedStorageSystem _storageSystem = default!;
    [Dependency] private readonly ContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedStackSystem _stackSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedIdCardSystem _idCardSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IChatManager _chatManager = default!;

    public readonly List<BankAccount> Accounts = [];
    private static readonly EntProtoId SpaceCashProto = "SpaceCash";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawned);
        SubscribeLocalEvent<EconomyBankCardComponent, BeingMicrowavedEvent>(OnMicrowaved);
    }

    private void OnMicrowaved(Entity<EconomyBankCardComponent> ent, ref BeingMicrowavedEvent args)
    {
        if (!TryComp<MicrowaveComponent>(args.Microwave, out var micro) || micro.Broken)
            return;

        var randomPick = _random.NextFloat();

        if (randomPick <= 0.5f)
            ent.Comp.AccountId = default;
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

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        Accounts.Clear();
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

        var account = CreateAccount(default, startingBalance);
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
        if (!_inventorySystem.TryGetSlotContainer(user, "back", out var backSlot, out _)
            || !TryComp<StorageComponent>(backSlot.ContainedEntity, out var storageComponent)
            )
            return;

        var result = 0;

        foreach (var item in storageComponent.StoredItems)
            result += CashWithdrawalOnSpawn(item.Key);

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

        var itemToSpawn = EntityManager.SpawnEntity(SpaceCashProto, Transform(user).Coordinates);

        _stackSystem.SetCount(itemToSpawn, withdrawnAmount);

        // Try insert into the backpack
        if (backSlot is not null
            && backSlot.ContainedEntity.HasValue
            && _storageSystem.Insert(backSlot.ContainedEntity.Value, itemToSpawn, out _, playSound: false)
            )
            return;

        // Try insert into pockets
        if (_inventorySystem.TryGetSlotContainer(user, "pocket1", out var pocket1, out _)
            && _containerSystem.Insert(itemToSpawn, pocket1)
            )
            return;

        if (_inventorySystem.TryGetSlotContainer(user, "pocket2", out var pocket2, out _)
            && _containerSystem.Insert(itemToSpawn, pocket2)
            )
            return;

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

        _entityManager.QueueDeleteEntity(item);
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

        if (!TryGetAccount(accountId, out var account))
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
    /// Creates BankAccount with specified unique accountId and startingBalance.
    /// If accountId already taken — returns BankAccount with this accountId.
    /// If accountId = default — creates new random accountId.
    /// </summary>
    public BankAccount CreateAccount(int accountId = default, int startingBalance = 0)
    {
        if (TryGetAccount(accountId, out var acc))
            return acc;

        BankAccount account;

        var accountPin = _random.Next((int)Math.Pow(10, PinCodeLength - 1), (int)Math.Pow(10, PinCodeLength));

        if (accountId == default)
        {
            int accountNumber;

            do
            {
                accountNumber = _random.Next(100000, 1000000); // Строка с генерацией ПИН-кода выглядит сложнее, чем эта - не так ли? Зато без волшебных чисел
            } while (AccountExist(accountNumber));

            account = new BankAccount(accountNumber, accountPin, startingBalance);
        }
        else
        {
            account = new BankAccount(accountId, accountPin, startingBalance);
        }

        Accounts.Add(account);

        return account;
    }

    public bool AccountExist(int accountId)
    {
        if (accountId == default)
            return false;

        return Accounts.Any(x => x.AccountId == accountId);
    }

    public bool TryGetAccount(int accountId, [NotNullWhen(true)] out BankAccount? account)
    {
        if (accountId == default)
        {
            account = default;
            return false;
        }

        account = Accounts.FirstOrDefault(x => x.AccountId == accountId);
        return account != null;
    }

    public bool TryChangeBalance(int accountId, int amount)
    {
        if (!TryGetAccount(accountId, out var account))
            return false;

        account.Balance = amount;

        return true;
    }

    public static int GetEmaggedTax(int input)
    {
        return (int)Math.Floor((float)(5 + input / 100)); // HARDCODED TAX
    }
}
