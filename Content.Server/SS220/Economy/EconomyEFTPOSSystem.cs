// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.SS220.Paper;
using Content.Shared.Paper;
using Content.Shared.SS220.Economy;
using Robust.Server.Audio;

namespace Content.Server.SS220.Economy;

public sealed class EconomyEFTPOSSystem : SharedEconomyEFTPOSSystem
{
    [Dependency] private readonly EconomyBankCardSystem _bankCardSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly PaperSystem _paperSystem = default!;
    [Dependency] private readonly DocumentHelperSystem _documentHelper = default!;

    protected override void OnEnterButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSKeypadEnterMessage args)
    {
        if (ent.Comp.OwnerBankAccountId == default
            || ent.Comp.PayerBankAccountId == default
            || ent.Comp.OwnerBankAccountId == ent.Comp.PayerBankAccountId
            || ent.Comp.PayerPinInput.Length != 4 // This magic number again, what does it mean?
            || ent.Comp.Amount <= 0
            )
            return;

        if (_bankCardSystem.TryGetAccount(ent.Comp.PayerBankAccountId, out var payerBankAccount)
            && ent.Comp.PayerPinInput == payerBankAccount.AccountPin.ToString()
            && _bankCardSystem.CashWithdrawal(ent.Comp.PayerBankAccountId, out var _, ent.Comp.Amount)
            && _bankCardSystem.TryGetAccount(ent.Comp.OwnerBankAccountId, out var ownerBankAccount))
        {
            _bankCardSystem.TryChangeBalance(ent.Comp.OwnerBankAccountId, ownerBankAccount.Balance + ent.Comp.Amount);
            _popupSystem.PopupEntity(Loc.GetString("economy-eftpos-transaction-success"), ent);
            _audioSystem.PlayPvs(ent.Comp.SoundApply, ent);

            PrintReceipt(ent, args.Actor);

            ent.Comp.PayerBankAccountId = default;
        }
        else
        {
            _popupSystem.PopupEntity(Loc.GetString("economy-eftpos-transaction-error"), ent);
            _audioSystem.PlayPvs(ent.Comp.SoundDeny, ent);
        }

        ent.Comp.PayerPinInput = string.Empty;
        UpdateUiState(ent);
    }

    private void PrintReceipt(Entity<EconomyEFTPOSComponent> ent, EntityUid user)
    {
        if (!ent.Comp.PrintReceipt)
            return;

        var printed = Spawn(ent.Comp.MachineOutput, Transform(user).Coordinates);

        if (!TryComp<PaperComponent>(printed, out var paperComp))
        {
            Log.Error("Printed transaction receipt did not have PaperComponent.");
            QueueDel(printed);
            return;
        }

        var builder = new StringBuilder();

        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-begin"));
        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-amount", ("amount", ent.Comp.Amount)));

        var dateTime = $"{_documentHelper.GetGameDate()} {_documentHelper.GetStationTime()}";

        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-time", ("dateTime", dateTime)));
        builder.AppendLine();
        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-owner"));
        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-account-id", ("accountId", ent.Comp.OwnerBankAccountId)));

        if (_bankCardSystem.TryGetAccount(ent.Comp.OwnerBankAccountId, out var ownerBankAccount)
            && ownerBankAccount.AccountOwnerName != string.Empty
            )
            builder.AppendLine(Loc.GetString("economy-eftpos-receipt-account-name", ("name", ownerBankAccount.AccountOwnerName)));

        builder.AppendLine();
        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-payer"));
        builder.AppendLine(Loc.GetString("economy-eftpos-receipt-account-id", ("accountId", ent.Comp.PayerBankAccountId)));

        if (_bankCardSystem.TryGetAccount(ent.Comp.PayerBankAccountId, out var payerBankAccount)
            && payerBankAccount.AccountOwnerName != string.Empty
            )
            builder.AppendLine(Loc.GetString("economy-eftpos-receipt-account-name", ("name", payerBankAccount.AccountOwnerName)));

        _paperSystem.SetContent((printed, paperComp), builder.ToString());
        _handsSystem.PickupOrDrop(user, printed, checkActionBlocker: false);
        _audioSystem.PlayPvs(ent.Comp.SoundPrint, ent);

        ent.Comp.PrintReceipt = false;
    }

    protected override string GetOwner(int bankAccountId)
    {
        if (!_bankCardSystem.TryGetAccount(bankAccountId, out var account))
            return string.Empty;

        return account.AccountOwnerName;
    }
}
