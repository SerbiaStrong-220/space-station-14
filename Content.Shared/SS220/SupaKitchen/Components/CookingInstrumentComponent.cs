// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SupaKitchen.Components;

public abstract partial class BaseCookingInstrumentComponent : Component
{
    [DataField(required: true)]
    public ProtoId<CookingInstrumentTypePrototype> InstrumentType;

    [DataField]
    public EntProtoId FailureResult = "FoodBadRecipe";

    [DataField]
    public bool IgnoreTime = false;

    [DataField]
    public List<ProtoId<CookingRecipePrototype>> AdditionalRecipes = [];
}
