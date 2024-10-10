// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Shared.SS220.Calculator;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Calculator;

public sealed class CalculatorSystem : SharedCalculatorSystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SetCalculatorStateMessage>(OnCalculatorStateMessage);
        SubscribeLocalEvent<CalculatorComponent, BoundUIOpenedEvent>(OnCalculatorUIOpened);
        SubscribeLocalEvent<CalculatorComponent, CalculatorButtonPressedMessage>(OnCalculatorButtonPressed);
    }

    protected override void OnChanged(Entity<CalculatorComponent> calculator)
    {
        Dirty(calculator);
    }

    private void OnCalculatorStateMessage(SetCalculatorStateMessage message, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } player)
            return;
        if (!TryGetEntity(message.Calculator, out var calculatorEntityOrNone) || calculatorEntityOrNone is not { } calculatorEntity)
            return;
        if (!TryComp<CalculatorComponent>(calculatorEntity, out var calculator))
            return;
        if (player != calculator.LastUser) // Ultimate anti-hack system here
            return;
        calculator.State = message.State;
        OnChanged((calculatorEntity, calculator));
    }

    private void OnCalculatorUIOpened(Entity<CalculatorComponent> calculator, ref BoundUIOpenedEvent args)
    {
        calculator.Comp.LastUser = args.Actor;
    }

    private void OnCalculatorButtonPressed(Entity<CalculatorComponent> calculator, ref CalculatorButtonPressedMessage args)
    {
        var filter = Filter.Pvs(Transform(calculator).Coordinates)
            .RemovePlayerByAttachedEntity(args.Actor);
        RaiseNetworkEvent(new CalculatorButtonPressedEvent()
        {
            Calculator = GetNetEntity(calculator),
        }, filter);

        var currentTime = _gameTiming.CurTime;
        if (calculator.Comp.LastPopupTimestamp is not { } lastPopupTimestamp ||
            currentTime - lastPopupTimestamp > calculator.Comp.MinIntervalToPopup)
        {
            var message = Loc.GetString("calculator-popup-buttons-press");
            _popupSystem.PopupEntity(message, args.Actor, Shared.Popups.PopupType.Small);
            calculator.Comp.LastPopupTimestamp = currentTime;
        }
    }
}
