using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.MiGo;

/// <summary>
/// Generic random weighting dataset to use.
/// </summary>
[Prototype]
public sealed partial class MiGoCapturePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public EntProtoId ReplacedProto { get; private set; }

    [DataField(required: true)]
    public EntProtoId ReplacementProto { get; private set; }

    [DataField]
    public TimeSpan ReplacementTime { get; private set; } = TimeSpan.FromSeconds(30);
}
