using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DarkForces.Ratvar.Prototypes;

[Serializable, Prototype("ratvarMidasTouch")]
public sealed partial class RatvarMidasTouchablePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public EntProtoId Item;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public SpriteSpecifier? Icon;
}
