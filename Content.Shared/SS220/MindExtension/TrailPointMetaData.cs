using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MindExtension;

[Serializable, NetSerializable]
public struct TrailPointMetaData
{
    public bool IsAbandoned { get; set; }

    public string EntityName { get; set; }

    public string EntityDescription { get; set; }

    public TrailPointMetaData(bool isAbandoned, string entityName, string entityDescription)
    {
        IsAbandoned = isAbandoned;
        EntityName = entityName;
        EntityDescription = entityDescription;
    }
}
