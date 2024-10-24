// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SupaKitchen;

[RegisterComponent]
public sealed partial class CookingInstrumentComponent : Component
{
    [DataField(required: true)]
    [ViewVariables]
    public ProtoId<CookingInstrumentTypePrototype> InstrumentType;

    [DataField]
    public EntProtoId FailureResult = "FoodBadRecipe";

    [ViewVariables]
    [DataField]
    public bool IgnoreTime = false;
}
