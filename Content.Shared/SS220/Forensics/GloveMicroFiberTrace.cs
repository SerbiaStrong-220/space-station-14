// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// A glove pair's identity and its type at the time the contact trace was deposited.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial record struct GloveMicroFiberTrace
{
    /// <summary>
    /// The permanent identifier of the pair of gloves.
    /// </summary>
    [DataField(required: true)]
    public string Code;

    /// <summary>
    /// The glove prototype when the trace was deposited, if the gloves had a prototype.
    /// </summary>
    [DataField]
    public EntProtoId? Prototype;

    public GloveMicroFiberTrace(string code, EntProtoId? prototype)
    {
        Code = code;
        Prototype = prototype;
    }
}
