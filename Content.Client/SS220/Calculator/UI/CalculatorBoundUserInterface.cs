// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Calculator;
using JetBrains.Annotations;

namespace Content.Client.SS220.Calculator.UI;

[UsedImplicitly]
public sealed class CalculatorBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private readonly ISawmill _sawmill;
    private readonly CalculatorSystem _calculatorSystem;
    private readonly Entity<CalculatorComponent> _calculator;

    [ViewVariables]
    private CalculatorMenu? _menu;

    public CalculatorBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _sawmill = _logManager.GetSawmill(nameof(CalculatorBoundUserInterface));
        _calculatorSystem = _entityManager.System<CalculatorSystem>();
        if (!_entityManager.TryGetComponent<CalculatorComponent>(owner, out var calculatorComponent))
        {
            _sawmill.Error("Can not be initialized, owner should have {0}", nameof(CalculatorComponent));
            return;
        }
        _calculator = (owner, calculatorComponent);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(this);
        _menu.OnClose += Close;
        _menu.OpenCenteredLeft();
        DisplayState();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    public void OnDigitPressed(byte digit)
    {
        if (!_calculatorSystem.TryAppendDigit(_calculator, digit))
            return;
        OnChanged();
    }

    public void OnDotPressed()
    {
        if (!_calculatorSystem.TryAppendDecimalPoint(_calculator))
            return;
        OnChanged();
    }

    public void OnClearPressed()
    {
        _calculatorSystem.ClearState(_calculator);
        OnChanged();
    }

    public void OnClearEntryPressed()
    {
        _calculatorSystem.ClearInputOperand(_calculator);
        OnChanged();
    }

    public void OnEqualsPressed()
    {
        _calculatorSystem.Calculate(_calculator);
        OnChanged();
    }

    public void OnAddPressed()
    {
        _calculatorSystem.SetOperation(_calculator, CalculatorOperation.Addition);
        OnChanged();
    }

    public void OnSubtractPressed()
    {
        _calculatorSystem.SetSubtractionOrNegate(_calculator);
        OnChanged();
    }

    public void OnMultiplyPressed()
    {
        _calculatorSystem.SetOperation(_calculator, CalculatorOperation.Multiplication);
        OnChanged();
    }

    public void OnDividePressed()
    {
        _calculatorSystem.SetOperation(_calculator, CalculatorOperation.Division);
        OnChanged();
    }

    public void OnButtonPressed()
    {
        SendMessage(new CalculatorButtonPressedMessage());
        PlayButtonSound();
    }

    private void OnChanged()
    {
        DisplayState();
    }

    private void DisplayState()
    {
        var currentOperand = _calculatorSystem.GetDisplayedOperand(_calculator);
        _menu?.SetNumber(currentOperand.Number, currentOperand.FractionLength);
    }

    private void PlayButtonSound()
    {
        _calculatorSystem.PlayButtonSound(_calculator, true);
    }
}
