// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Kitchen.FoodProcessor;

/// <summary>
/// Marks an entity as a valid food processor ingredient and specifies its output.
/// </summary>
[RegisterComponent]
public sealed partial class FoodProcessorIngredientComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Result;

    [DataField]
    public int ResultCount = 1;
}
