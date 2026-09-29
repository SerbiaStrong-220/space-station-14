using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.LiquidSplatters;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype()]
public sealed partial class LiquidSplattersSettingsPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public FixedPoint2 WeaponContainerIncrement = 1;

    [DataField]
    public FixedPoint2 ClothingContainerIncrement = 0.5;

    [DataField]
    public Dictionary<string, float> clothingSlotChances = new();
}
