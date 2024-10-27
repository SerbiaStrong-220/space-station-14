// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.Contracts;

namespace Content.Shared.SS220.Calculator;

public abstract class SharedCalculatorSystem : EntitySystem
{
    /// <summary>
    /// Appends decimal <paramref name="digit"/> to the current <paramref name="calculator"/> state. 
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
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

    /// <summary>
    /// Appends decimal point to the current <paramref name="calculator"/> state.
    /// </summary>
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

    /// <summary>
    /// Completely clears the current <paramref name="calculator"/> state (both operands and operation).
    /// </summary>
    public void ClearState(Entity<CalculatorComponent> calculator)
    {
        calculator.Comp.State = default;
        OnChanged(calculator);
    }

    /// <summary>
    /// Clears currently active operand (left if no operation is entered, otherwise right) of the <paramref name="calculator"/>.
    /// </summary>
    public void ClearInputOperand(Entity<CalculatorComponent> calculator)
    {
        SetInputOperand(calculator, default);
        OnChanged(calculator);
    }

    /// <summary>
    /// Sets specified operation for the <paramref name="calculator"/> state.
    /// This also have a side effect of changing input operand to the right one.
    /// </summary>
    public void SetOperation(Entity<CalculatorComponent> calculator, CalculatorOperation operation)
    {
        if (calculator.Comp.State.OperandRight.HasValue)
            Calculate(calculator);
        calculator.Comp.State.Operation = operation;
        OnChanged(calculator);
    }

    /// <summary>
    /// Negates current input operate (see <see cref="GetInputOperand(Entity{CalculatorComponent})"/>) of the <paramref name="calculator"/>.
    /// </summary>
    public void NegateCurrentOperand(Entity<CalculatorComponent> calculator)
    {
        var currentOperand = GetInputOperand(calculator);
        if (IsOperandOutOfCapacity(calculator, currentOperand))
            return;
        currentOperand.Number = decimal.Negate(0m);
        SetInputOperand(calculator, currentOperand);
        OnChanged(calculator);
    }

    /// <summary>
    /// Evaluates the entered expression in the current <paramref name="calculator"/> state,
    /// writes result to the left operand, and clears right operand and operation.
    /// </summary>
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

    /// <summary>
    /// Will negate current input operand (see <see cref="NegateCurrentOperand(Entity{CalculatorComponent})"/>)
    /// if current input operand is zero, otherwise will set substraction operation
    /// (see <see cref="SetOperation(Entity{CalculatorComponent}, CalculatorOperation)"/>).
    /// </summary>
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

    /// <summary>
    /// Returns <see langword="true"/> if right operand should be currently displayed,
    /// or <see langword="false"/> if the left one.
    /// </summary>
    [Pure]
    public bool IsRightOperandDisplayed(Entity<CalculatorComponent> calculator)
    {
        return calculator.Comp.State.OperandRight.HasValue;
    }

    /// <summary>
    /// Returns operand that is currently displayed (see <see cref="IsRightOperandDisplayed(Entity{CalculatorComponent})"/>).
    /// </summary>
    [Pure]
    public CalculatorOperand GetDisplayedOperand(Entity<CalculatorComponent> calculator)
    {
        return IsRightOperandDisplayed(calculator)
            ? calculator.Comp.State.OperandRight.GetValueOrDefault()
            : calculator.Comp.State.OperandLeft;
    }

    /// <summary>
    /// Returns <see langword="true"/> if right operand is currently editable,
    /// or <see langword="false"/> if the left one.
    /// </summary>
    [Pure]
    public bool IsRightOperandInput(Entity<CalculatorComponent> calculator)
    {
        return calculator.Comp.State.Operation.HasValue;
    }

    /// <summary>
    /// Returns operand that is currently editable (see <see cref="IsRightOperandInput(Entity{CalculatorComponent})"/>).
    /// </summary>
    [Pure]
    public CalculatorOperand GetInputOperand(Entity<CalculatorComponent> calculator)
    {
        var state = calculator.Comp.State;
        return IsRightOperandInput(calculator) ? state.OperandRight.GetValueOrDefault() : state.OperandLeft;
    }

    /// <summary>
    /// Returns <see langword="true"/> if the <paramref name="operand"/> can not longer accept digits.
    /// </summary>
    [Pure]
    public bool IsOperandOutOfCapacity(Entity<CalculatorComponent> calculator, CalculatorOperand operand)
    {
        return CountOperandLength(calculator, operand) >= calculator.Comp.DigitsLimit;
    }

    /// <summary>
    /// Returns lenght in characters of the specified <paramref name="operand"/>.
    /// </summary>
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
