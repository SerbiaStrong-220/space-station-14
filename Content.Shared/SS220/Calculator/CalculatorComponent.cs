// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Calculator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CalculatorComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int DigitsLimit = 8;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ButtonSound;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan MinIntervalToPopup = TimeSpan.FromSeconds(10f);

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public CalculatorState State;

    /// <summary>
    /// Server only
    /// </summary>
    public EntityUid? LastUser;
    /// <summary>
    /// Server only
    /// </summary>
    public TimeSpan? LastPopupTimestamp;
}

[Serializable, NetSerializable]
public partial struct CalculatorState
{
    [ViewVariables(VVAccess.ReadWrite)]
    public CalculatorOperand OperandLeft;
    [ViewVariables(VVAccess.ReadWrite)]
    public CalculatorOperand? OperandRight;
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
    public decimal Number;
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
