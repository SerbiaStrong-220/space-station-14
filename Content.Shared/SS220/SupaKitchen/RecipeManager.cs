// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SupaKitchen;
public sealed class SupaRecipeManager
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public List<CookingRecipePrototype> Recipes { get; private set; } = new();

    public void Initialize()
    {
        Recipes = new List<CookingRecipePrototype>();
        foreach (var item in _prototypeManager.EnumeratePrototypes<CookingRecipePrototype>())
        {
            Recipes.Add(item);
        }

        Recipes.Sort(new RecipeComparer());
    }
    /// <summary>
    /// Check if a prototype ids appears in any of the recipes that exist.
    /// </summary>
    public bool SolidAppears(string solidId)
    {
        return Recipes.Any(recipe => recipe.IngredientsSolids.ContainsKey(solidId));
    }

    private sealed class RecipeComparer : Comparer<CookingRecipePrototype>
    {
        public override int Compare(CookingRecipePrototype? x, CookingRecipePrototype? y)
        {
            if (x == null || y == null)
            {
                return 0;
            }

            var nx = x.IngredientCount();
            var ny = y.IngredientCount();
            return -nx.CompareTo(ny);
        }
    }
}
