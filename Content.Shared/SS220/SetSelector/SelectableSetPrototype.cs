using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SetSelector;

[Prototype]
public sealed partial class SelectableSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField(required: true)] public LocId Name { get; private set; }
    [DataField(required: true)] public LocId Description { get; private set; }
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
