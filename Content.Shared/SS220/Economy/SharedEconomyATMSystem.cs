// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Economy;

public abstract partial class SharedEconomyATMSystem : EntitySystem
{
    [Dependency] protected readonly SharedToolSystem Tool = default!;
    [Dependency] protected readonly SharedPopupSystem PopupSystem = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    protected readonly ProtoId<StackPrototype> CashProto = "Credit";
    protected readonly string IdCardSlotName = "idCardSlot";

    public override void Initialize()
    {
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMKeypadMessage>(OnKeypadButtonPressed);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMKeypadClearMessage>(OnClearButtonPressed);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMKeypadEnterMessage>(OnEnterButtonPressed);

        SubscribeLocalEvent<EconomyATMComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<EconomyATMComponent, EconomyATMResetEvent>(OnATMReset);

        SubscribeLocalEvent<EconomyATMComponent, GotEmaggedEvent>(OnEmag);
    }

    private void OnKeypadButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadMessage args)
    {
        if (ent.Comp.PinInput.Length >= SharedEconomyBankCardSystem.PinCodeLength)
            return;

        if (ent.Comp.CardState != CardStateEnum.Valid)
            return;

        ent.Comp.PinInput += args.Value.ToString();

        UpdateUiState(ent);
    }

    private void OnClearButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadClearMessage args)
    {
        ent.Comp.PinInput = string.Empty;

        UpdateUiState(ent);
    }

    protected abstract void OnEnterButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadEnterMessage args);

    protected abstract void OnInteractUsing(Entity<EconomyATMComponent> ent, ref InteractUsingEvent args);

    protected abstract void OnATMReset(Entity<EconomyATMComponent> ent, ref EconomyATMResetEvent args);

    private void OnEmag(Entity<EconomyATMComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;

        var state = new EconomyATMUiState
        {
            CardState = ent.Comp.CardState,
            InfoMessage = ent.Comp.InfoMessage,
            BankAccount = ent.Comp.BankAccount,
            PinInput = ent.Comp.PinInput,
            UnemployedAlert = ent.Comp.UnemployedAlert,
            Emagged = true
        };

        UpdateUiState(ent, state);
    }

    protected void UpdateUiState(Entity<EconomyATMComponent> ent, EconomyATMUiState? state = null)
    {
        state ??= new EconomyATMUiState
        {
            CardState = ent.Comp.CardState,
            InfoMessage = ent.Comp.InfoMessage,
            BankAccount = ent.Comp.BankAccount,
            PinInput = ent.Comp.PinInput,
            UnemployedAlert = ent.Comp.UnemployedAlert,
            Emagged = HasComp<EmaggedComponent>(ent)
        };

        _ui.SetUiState(ent.Owner, EconomyATMUiKey.Key, state);
    }

    protected void SoftResetATM(Entity<EconomyATMComponent> ent)
    {
        ent.Comp.CardState = CardStateEnum.Absent;
        ent.Comp.InfoMessage = Loc.GetString("economy-atm-ui-insert-card");
        ent.Comp.BankAccount = new();
        ent.Comp.PinInput = string.Empty;
        ent.Comp.UnemployedAlert = false;
    }

    [Serializable, NetSerializable]
    public sealed partial class EconomyATMResetEvent : SimpleDoAfterEvent
    {
    }
}

