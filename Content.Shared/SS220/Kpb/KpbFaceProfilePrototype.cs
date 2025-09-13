using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.Kpb;

/// <summary>
/// Prototype defining a collection of KPB face sprites.
/// </summary>
[Prototype("kpbFaceProfile")]
public sealed partial class KpbFaceProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Available face markings for this profile.
    /// </summary>
    [DataField("faces", required: true)]
    public List<ProtoId<MarkingPrototype>> Faces { get; private set; } = new();
}
