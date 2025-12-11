using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension.Events;

[Serializable, NetSerializable]
public sealed class RespawnTimeRequest : EntityEventArgs { }
