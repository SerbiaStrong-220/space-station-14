using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CruiseControl;

[Serializable, NetSerializable]
public sealed class ShuttleGyroPowerMessage : BoundUserInterfaceMessage
{
    public float Power { get; }

    public ShuttleGyroPowerMessage(float power) => Power = power;
}
