using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Temperature;

[Serializable, NetSerializable]
public sealed partial class TemperatureChangeAttemptEvent : CancellableEntityEventArgs
{
}
