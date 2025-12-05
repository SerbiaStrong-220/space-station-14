using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension;

[Serializable, NetSerializable]
public record struct TrailPoint(NetEntity Id, TrailPointMetaData MetaData, BodyStateToEnter State, bool ByAdmin);

