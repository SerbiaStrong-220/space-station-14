// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// An item's permanent material identity and appearance when a sample was deposited or collected.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct StorageMicroFiberTrace
{
    /// <summary>
    /// The permanent identifier of the source item.
    /// </summary>
    [DataField(required: true)]
    public string Code;

    /// <summary>
    /// The source prototype when the sample was taken, if the item had a prototype.
    /// </summary>
    [DataField]
    public EntProtoId? Prototype;

    /// <summary>
    /// The source item's display name when the sample was taken, independent of later name changes.
    /// </summary>
    [DataField]
    public string SourceName;

    public StorageMicroFiberTrace(string code, EntProtoId? prototype, string sourceName = "")
    {
        Code = code;
        Prototype = prototype;
        SourceName = sourceName;
    }
}
