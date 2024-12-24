// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.SupaKitchen;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Server.SS220.SupaKitchen;
public sealed class CookingInstrumentSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SupaRecipeManager _recipeManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public static (CookingRecipePrototype, int) CanSatisfyRecipe(
        CookingInstrumentComponent component,
        CookingRecipePrototype recipe,
        Dictionary<string, int> solids,
        Dictionary<string, FixedPoint2> reagents,
        uint cookingTimer
        )
    {
        var portions = 0;

        if (component.InstrumentType != recipe.InstrumentType)
            return (recipe, 0);

        if (
            cookingTimer % recipe.CookTime != 0
            && !component.IgnoreTime
            )
        {
            //can't be a multiple of this recipe
            return (recipe, 0);
        }

        foreach (var solid in recipe.IngredientsSolids)
        {
            if (!solids.ContainsKey(solid.Key))
                return (recipe, 0);

            if (solids[solid.Key] < solid.Value)
                return (recipe, 0);

            portions = portions == 0
                ? solids[solid.Key] / solid.Value.Int()
                : Math.Min(portions, solids[solid.Key] / solid.Value.Int());
        }

        foreach (var reagent in recipe.IngredientsReagents)
        {
            if (!reagents.ContainsKey(reagent.Key))
                return (recipe, 0);

            if (reagents[reagent.Key] < reagent.Value)
                return (recipe, 0);

            portions = portions == 0
                ? reagents[reagent.Key].Int() / reagent.Value.Int()
                : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
        }

        //cook only as many of those portions as time allows
        if (!component.IgnoreTime)
            portions = (int) Math.Min(portions, cookingTimer / recipe.CookTime);


        return (recipe, portions);
    }

    public void SubtractContents(Container container, CookingRecipePrototype recipe)
    {
        var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

        // this is spaghetti ngl
        foreach (var item in container.ContainedEntities)
        {
            if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                continue;

            // go over every solution
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((item, solMan)))
            {
                var solution = soln.Comp.Solution;
                foreach (var (reagent, _) in recipe.IngredientsReagents)
                {
                    // removed everything
                    if (!totalReagentsToRemove.ContainsKey(reagent))
                        continue;

                    var quant = solution.GetTotalPrototypeQuantity(reagent);

                    if (quant >= totalReagentsToRemove[reagent])
                    {
                        quant = totalReagentsToRemove[reagent];
                        totalReagentsToRemove.Remove(reagent);
                    }
                    else
                    {
                        totalReagentsToRemove[reagent] -= quant;
                    }

                    _solutionContainer.RemoveReagent(soln, reagent, quant);
                }
            }
        }

        foreach (var recipeSolid in recipe.IngredientsSolids)
        {
            for (var i = 0; i < recipeSolid.Value; i++)
            {
                foreach (var item in container.ContainedEntities)
                {
                    var metaData = MetaData(item);
                    if (metaData.EntityPrototype == null)
                    {
                        continue;
                    }

                    if (metaData.EntityPrototype.ID == recipeSolid.Key)
                    {
                        _container.Remove(item, container);
                        EntityManager.DeleteEntity(item);
                        break;
                    }
                }
            }
        }
    }

    public (CookingRecipePrototype, int) GetSatisfiedPortionedRecipe(
        CookingInstrumentComponent component,
        Dictionary<string, int> solidsDict,
        Dictionary<string, FixedPoint2> reagentDict,
        uint cookingTimer
        )
    {
        return _recipeManager.Recipes.Select(r =>
            CanSatisfyRecipe(component, r, solidsDict, reagentDict, cookingTimer)).FirstOrDefault(r => r.Item2 > 0);
    }
}
