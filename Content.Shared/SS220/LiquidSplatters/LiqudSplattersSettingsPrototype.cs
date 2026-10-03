using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.LiquidSplatters;

[Prototype()]
public sealed partial class LiquidSplattersSettingsPrototype : IPrototype
{

    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public FixedPoint2 ClothingSolutionScale = 0.15f;

    [DataField]
    public FixedPoint2 DamageSolutionScale = 0.3f;

    [DataField]
    public Dictionary<string, float> ClothingSlotChances = new();

    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> CleaningReagentsEffectiveness = new();
}
