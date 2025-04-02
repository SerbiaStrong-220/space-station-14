// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.SupaKitchen.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Dictionary;
using System.Linq;

namespace Content.Shared.SS220.SupaKitchen;

[Prototype("cookingRecipe")]
public sealed class CookingRecipePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    private string _name = string.Empty;

    [DataField("reagents")]
    private readonly Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> _ingsReagents = new();

    [DataField("solids")]
    private readonly Dictionary<ProtoId<EntityPrototype>, uint> _ingsSolids = new();

    [DataField]
    public ProtoId<EntityPrototype> Result { get; } = string.Empty;

    [DataField(required: true)]
    public ProtoId<CookingInstrumentTypePrototype> InstrumentType { get; }

    [DataField("time")]
    public uint CookTime { get; } = 5;

    public string Name => _name;
    public string LocName => Loc.GetString($"cookingRecipe-{ID}");

    public IReadOnlyDictionary<string, FixedPoint2> IngredientsReagents => _ingsReagents.ToDictionary(d => d.Key.Id, d => d.Value);
    public IReadOnlyDictionary<string, uint> IngredientsSolids => _ingsSolids.ToDictionary(d => d.Key.Id, d => d.Value);

    [DataField]
    public List<string> RecipeTags = new();

    /// <summary>
    /// Is this recipe unavailable in normal circumstances?
    /// </summary>
    [DataField]
    public bool SecretRecipe = false;

    [DataField]
    public Color? Color;

    /// <summary>
    ///    Count the number of ingredients in a recipe for sorting the recipe list.
    ///    This makes sure that where ingredient lists overlap, the more complex
    ///    recipe is picked first.
    /// </summary>
    public FixedPoint2 IngredientCount()
    {
        FixedPoint2 n = 0;
        n += _ingsReagents.Count; // number of distinct reagents
        foreach (FixedPoint2 i in _ingsSolids.Values) // sum the number of solid ingredients
        {
            n += i;
        }
        return n;
    }
}
