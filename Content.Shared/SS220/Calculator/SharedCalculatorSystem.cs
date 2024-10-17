// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.Contracts;

namespace Content.Shared.SS220.Calculator;

public abstract class SharedCalculatorSystem : EntitySystem
{
    public bool TryAppendDigit(Entity<CalculatorComponent> calculator, byte digit)
    {
        if (digit > 9)
            throw new ArgumentOutOfRangeException(nameof(digit));
        var currentOperand = GetInputOperand(calculator);
        if (IsOperandOutOfCapacity(calculator, currentOperand))
            return false;
        var isNegative = decimal.IsNegative(currentOperand.Number);
        var toAdd = (int)digit;
        if (isNegative)
            toAdd *= -1;
        if (currentOperand.FractionLength is not { } fractionLength)
        {
            currentOperand.Number = currentOperand.Number * 10 + toAdd;
        }
        else
        {
            currentOperand.Number += (decimal)toAdd / (DecimalMath.GetPowerOfTen(fractionLength) * 10);
            currentOperand.FractionLength++;
        }
        SetInputOperand(calculator, currentOperand);
        OnChanged(calculator);
        return true;
    }

    public bool TryAppendDecimalPoint(Entity<CalculatorComponent> calculator)
    {
        var currentOperand = GetInputOperand(calculator);
        if (currentOperand.FractionLength.HasValue)
            return false;
        if (IsOperandOutOfCapacity(calculator, currentOperand))
            return false;
        currentOperand.FractionLength = 0;
        SetInputOperand(calculator, currentOperand);
        OnChanged(calculator);
        return true;
    }

    public void ClearState(Entity<CalculatorComponent> calculator)
    {
        calculator.Comp.State = default;
        OnChanged(calculator);
    }

    public void ClearInputOperand(Entity<CalculatorComponent> calculator)
    {
        SetInputOperand(calculator, default);
        OnChanged(calculator);
    }

    public void SetOperation(Entity<CalculatorComponent> calculator, CalculatorOperation operation)
    {
        if (calculator.Comp.State.OperandRight.HasValue)
            Calculate(calculator);
        calculator.Comp.State.Operation = operation;
        OnChanged(calculator);
    }

    public void NegateCurrentOperand(Entity<CalculatorComponent> calculator)
    {
        var currentOperand = GetInputOperand(calculator);
        if (IsOperandOutOfCapacity(calculator, currentOperand))
            return;
        currentOperand.Number = decimal.Negate(0m);
        SetInputOperand(calculator, currentOperand);
        OnChanged(calculator);
    }

    public void Calculate(Entity<CalculatorComponent> calculator)
    {
        var state = calculator.Comp.State;
        var left = state.OperandLeft.Number;
        var right = left;
        var result = left;
        if (state.OperandRight.HasValue)
        {
            right = state.OperandRight.Value.Number;
        }
        if (state.Operation.HasValue)
        {
            result = (state.Operation, right) switch
            {
                (CalculatorOperation.Addition, _) => left + right,
                (CalculatorOperation.Subtraction, _) => left - right,
                (CalculatorOperation.Multiplication, _) => left * right,
                (CalculatorOperation.Division, 0m) => 220, // CURSE 220 on attempt to divide by zero
                (CalculatorOperation.Division, _) => left / right,
                _ => left
            };
        }
        byte? fractionLength = result.Scale == 0 ? null : result.Scale;
        var resultOperand = new CalculatorOperand() { Number = result, FractionLength = fractionLength };
        var length = CountOperandLength(calculator, resultOperand);
        if (length > calculator.Comp.DigitsLimit)
        {
            var noFractionLength = length - fractionLength ?? 0;
            result = Math.Round(resultOperand.Number, Math.Max(calculator.Comp.DigitsLimit - noFractionLength, 0));
            resultOperand.Number = result;
            resultOperand.FractionLength = result.Scale == 0 ? null : result.Scale;
            length = CountOperandLength(calculator, resultOperand);
            if (length > calculator.Comp.DigitsLimit)
            {
                resultOperand = default;
            }
        }
        state.OperandLeft = resultOperand;
        state.OperandRight = default;
        state.Operation = null;
        calculator.Comp.State = state;
        OnChanged(calculator);
    }

    public void SetSubtractionOrNegate(Entity<CalculatorComponent> calculator)
    {
        var currentOperand = GetInputOperand(calculator);
        if (currentOperand.Number == 0)
        {
            NegateCurrentOperand(calculator);
        }
        else
        {
            SetOperation(calculator, CalculatorOperation.Subtraction);
        }
    }

    [Pure]
    public bool IsRightOperandDisplayed(Entity<CalculatorComponent> calculator)
    {
        return calculator.Comp.State.OperandRight.HasValue;
    }

    [Pure]
    public CalculatorOperand GetDisplayedOperand(Entity<CalculatorComponent> calculator)
    {
        return IsRightOperandDisplayed(calculator)
            ? calculator.Comp.State.OperandRight.GetValueOrDefault()
            : calculator.Comp.State.OperandLeft;
    }

    [Pure]
    public bool IsRightOperandInput(Entity<CalculatorComponent> calculator)
    {
        return calculator.Comp.State.Operation.HasValue;
    }

    [Pure]
    public CalculatorOperand GetInputOperand(Entity<CalculatorComponent> calculator)
    {
        var state = calculator.Comp.State;
        return IsRightOperandInput(calculator) ? state.OperandRight.GetValueOrDefault() : state.OperandLeft;
    }

    [Pure]
    public bool IsOperandOutOfCapacity(Entity<CalculatorComponent> calculator, CalculatorOperand operand)
    {
        return CountOperandLength(calculator, operand) >= calculator.Comp.DigitsLimit;
    }

    [Pure]
    public int CountOperandLength(Entity<CalculatorComponent> calculator, CalculatorOperand operand)
    {
        const bool countPoint = false;
        var intPartLength = DecimalMath.GetDecimalLength(DecimalMath.CastToIntOrDefault(operand.Number));
        var totalLength = intPartLength; // Integer digits
        if (operand.FractionLength is { } fractionLength)
            totalLength += fractionLength + (countPoint ? 1 : 0); // Dot and fraction digits
        if (decimal.IsNegative(operand.Number))
            totalLength += 1; // Sign
        return totalLength;
    }

    protected abstract void OnChanged(Entity<CalculatorComponent> calculator);

    private void SetInputOperand(Entity<CalculatorComponent> calculator, CalculatorOperand operand)
    {
        if (IsRightOperandInput(calculator))
        {
            calculator.Comp.State.OperandRight = operand;
        }
        else
        {
            calculator.Comp.State.OperandLeft = operand;
        }
    }
}
