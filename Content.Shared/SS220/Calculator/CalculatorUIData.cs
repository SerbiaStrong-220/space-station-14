// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Calculator;

[Serializable, NetSerializable]
public enum CalculatorUiKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class SetCalculatorStateMessage : EntityEventArgs
{
    public NetEntity Calculator;
    public CalculatorState State;
}

/// <summary>
/// To send from calculator client UI to server
/// </summary>
[Serializable, NetSerializable]
public sealed class CalculatorButtonPressedMessage : BoundUserInterfaceMessage
{

}

/// <summary>
/// To send from server to clients
/// </summary>
[Serializable, NetSerializable]
public sealed class CalculatorButtonPressedEvent : EntityEventArgs
{
    public NetEntity Calculator;
}
