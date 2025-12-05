using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension;

[Serializable, NetSerializable]
public record class TrailPointMetaData
{
    public bool IsAbandoned { get; set; } = false;

    public string EntityName { get; set; } = string.Empty;

    public string EntityDescription { get; set; } = string.Empty;
}
