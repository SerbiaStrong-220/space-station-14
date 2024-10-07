// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Photocopier;

public struct PrintableDocumentData(Dictionary<Type, IPhotocopiedComponentData> data, PhotocopyableMetaData metaData)
{
    /// <summary>
    /// Contains fields of components that will be copied.
    /// Is applied to a new entity that is created as a result of photocopying.
    /// </summary>
    [ViewVariables]
    public Dictionary<Type, IPhotocopiedComponentData> Data = data;

    /// <summary>
    /// Contains metadata that will be copied.
    /// Is applied to a new entity that is created as a result of photocopying.
    /// </summary>
    public PhotocopyableMetaData MetaData = metaData;
}