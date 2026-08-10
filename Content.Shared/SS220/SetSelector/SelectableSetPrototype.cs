using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SetSelector;

/// <summary>
/// daite pisat na rysskom plz
/// </summary>
[Prototype]
public sealed partial class SelectableSetPrototype : IPrototype
{
    [IdDataField] public string ID { get; private set; } = default!;
    [DataField] public string Name { get; private set; } = string.Empty;
    [DataField] public string Description { get; private set; } = string.Empty;
    [DataField] public SpriteSpecifier Sprite { get; private set; } = SpriteSpecifier.Invalid;

    [DataField] public List<EntProtoId> Content = new();
}
