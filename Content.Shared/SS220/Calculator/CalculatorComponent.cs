// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Calculator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CalculatorComponent : Component
{
    /// <summary>
    /// How many digits in the number can our calculator handle.
    /// </summary>
    [DataField]
    public int DigitsLimit = 8;
    /// <summary>
    /// Sound to play on button press.
    /// </summary>
    [DataField]
    public SoundSpecifier? ButtonSound;
    /// <summary>
    /// Timeout between popups from calculator.
    /// </summary>
    [DataField]
    public TimeSpan MinIntervalToPopup = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// State of the calculator: numbers and operation.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public CalculatorState State;

    /// <summary>
    /// Last player entity who opened this calculator.
    /// </summary>
    /// <remarks> Server only </remarks>
    public EntityUid? LastUser;
    /// <summary>
    /// Last time when this calculator showed popup.
    /// </summary>
    /// <remarks> Server only </remarks>
    public TimeSpan? LastPopupTimestamp;
}

/// <summary>
/// Entered numbers and operation of the <see cref="CalculatorComponent"/>.
/// </summary>
[Serializable, NetSerializable]
public partial struct CalculatorState
{
    /// <summary>
    /// Left operand of the entered expression. In expression "1 + 2" this will be "1".
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public CalculatorOperand OperandLeft;
    /// <summary>
    /// Right operand of the entered expression. In expression "1 + 2" this will be "2".
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public CalculatorOperand? OperandRight;
    /// <summary>
    /// Operation of the entered expression. In expression "1 + 2" this will be "+".
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public CalculatorOperation? Operation;

    public readonly override string ToString()
    {
        return $"Left: {OperandLeft}\nRight: {OperandRight}\nOperation: {Operation}";
    }
}

[Serializable, NetSerializable]
public partial struct CalculatorOperand
{
    /// <summary>
    /// The number itself. This handles integer part, sign, and most of the fraction part.
    /// </summary>
    public decimal Number;
    /// <summary>
    /// If the number is not integer (user entered dot) this will be not null, and the value
    /// mean number of digits after dot. For "12.000" <see cref="FractionLength"/> will have value of 3.
    /// </summary>
    public byte? FractionLength;

    public readonly override string ToString()
    {
        return $"{Number} (Scale: {Number.Scale}, Fraction Length: {FractionLength})";
    }
}

public enum CalculatorOperation
{
    Addition,
    Subtraction,
    Multiplication,
    Division,
}
