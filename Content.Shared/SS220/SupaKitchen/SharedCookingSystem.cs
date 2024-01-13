using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.SupaKitchen;

public abstract class SharedCookingSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SupaRecipeManager _recipeManager = default!;

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
        return CanSatisfyRecipe(component.InstrumentType, component.IgnoreTime ? 0 : cookingTimer, recipe, solids, reagents);
    }

    // TODO: refactor FoodComponent to make it shared, PR it to main build,
    // afterwards refactor this method to use FoodComponent.Solution, delet this constant.
    private const string FoodSolutionKey = "food";

    public void TransferFoodSolution(EntityUid to, EntityUid from)
    {
        if (_solutionContainer.TryGetSolution(from, FoodSolutionKey, out var solutionFrom))
        {
            var targetSolution = _solutionContainer.EnsureSolution(to, FoodSolutionKey);
            _solutionContainer.SetCapacity(to, targetSolution, targetSolution.MaxVolume + solutionFrom.Volume);
            _solutionContainer.TryTransferSolution(from, to, solutionFrom, targetSolution, solutionFrom.Volume);
        }
    }

    public bool TryAddIngredientToDicts(
        Dictionary<string, int> solidsDict,
        Dictionary<string, FixedPoint2> reagentDict,
        EntityUid item,
        bool recursive)
    {
        var metaData = MetaData(item); //this still begs for cooking refactor
        if (metaData.EntityPrototype == null)
            return false;

        var id = metaData.EntityPrototype.ID;

        if (solidsDict.ContainsKey(id))
            solidsDict[id]++;
        else
            solidsDict.Add(id, 1);

        if (TryComp<SolutionContainerManagerComponent>(item, out var solMan))
        {
            foreach (var (_, solution) in solMan.Solutions)
            {
                foreach (var (reagent, quantity) in solution.Contents)
                {
                    if (reagentDict.ContainsKey(reagent.Prototype))
                        reagentDict[reagent.Prototype] += quantity;
                    else
                        reagentDict.Add(reagent.Prototype, quantity);
                }
            }
        }

        if (recursive && TryComp<FoodIngredientComponent>(item, out var ingredientComponent))
        {
            foreach (var ingredient in ingredientComponent.IngredientContainer.ContainedEntities)
            {
                TryAddIngredientToDicts(solidsDict, reagentDict, ingredient, true);
            }
        }

        return true;
    }

    public bool TryCookContainer(Container storage, string? instrumentType, uint cookingTimer, [NotNullWhen(true)] out EntityUid? result)
    {
        result = null;

        if (storage.ContainedEntities.Count == 0)
            return false;

        // Check recipes
        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();

        foreach (var item in storage.ContainedEntities.ToList())
        {
            TryAddIngredientToDicts(solidsDict, reagentDict, item, true);
        }

        var (recipe, resultAmount) = GetSatisfiedPortionedRecipe(
            instrumentType, solidsDict, reagentDict, cookingTimer);

        if (recipe == null || resultAmount == 0)
            return false;

        var success = false;
        for (var i = 0; i < resultAmount; i++)
        {
            success |= TryCookContainerByRecipe(storage, recipe, out result);
        }

        return success;
    }

    public bool TryCookContainerByRecipe(Container storage, CookingRecipePrototype recipe, [NotNullWhen(true)] out EntityUid? result)
    {
        if (storage.ContainedEntities.Count == 0)
        {
            result = null;
            return false;
        }

        var anyEnt = storage.ContainedEntities[0];
        var coords = Transform(anyEnt).Coordinates;

        result = Spawn(recipe.Result, coords);
        SubtractContents(storage, recipe, result);
        return true;
    }

    public EntityUid CookEntityByRecipe(EntityUid entityToCook, CookingRecipePrototype recipe)
    {
        var coords = Transform(entityToCook).Coordinates;
        var result = Spawn(recipe.Result, coords);
        TransferFoodSolution(result, entityToCook);
        EntityManager.DeleteEntity(entityToCook);

        return result;
    }

    public bool TryCookEntity(EntityUid entityToCook, string? instrumentType, uint? cookingTimer, [NotNullWhen(true)] out EntityUid? result)
    {
        result = null;

        var solidsDict = new Dictionary<string, int>();
        var reagentDict = new Dictionary<string, FixedPoint2>();
        if (!TryAddIngredientToDicts(solidsDict, reagentDict, entityToCook, true))
            return false;

        var portionedRecipe = GetSatisfiedPortionedRecipe(
            instrumentType, solidsDict, reagentDict, cookingTimer);
        if (portionedRecipe.Item1 == null)
            return false;

        result = CookEntityByRecipe(entityToCook, portionedRecipe.Item1);
        return true;
    }

    public static (CookingRecipePrototype, int) CanSatisfyRecipe(
        string? instrumentType,
        uint? cookingTimer,
        CookingRecipePrototype recipe,
        Dictionary<string, int> solids,
        Dictionary<string, FixedPoint2> reagents
        )
    {
        var portions = 0;

        if (instrumentType != recipe.InstrumentType)
            return (recipe, 0);

        if (
            cookingTimer is not null &&
            cookingTimer % recipe.CookTime != 0
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
        if (cookingTimer is uint cookingTimerValid)
            portions = (int) Math.Min(portions, cookingTimerValid / recipe.CookTime);


        return (recipe, portions);
    }
    public void SubtractContents(Container container, CookingRecipePrototype recipe, EntityUid? transferSolutionTo = null)
    {
        var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

        // this is spaghetti ngl
        foreach (var item in container.ContainedEntities)
        {
            if (!TryComp<SolutionContainerManagerComponent>(item, out var solMan))
                continue;

            // go over every solution
            foreach (var (_, solution) in solMan.Solutions)
            {
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

                    _solutionContainer.RemoveReagent(item, solution, reagent, quant);
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
                        // transfer whatever we haven't removed previously.
                        // need it so you can inject poison into dough etc.
                        // as a bonus a product won't have lower nutritional value than a sum of its ingredients.
                        if (transferSolutionTo.HasValue)
                            TransferFoodSolution(transferSolutionTo.Value, item);

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

    public (CookingRecipePrototype, int) GetSatisfiedPortionedRecipe(
        string? instrumentType,
        Dictionary<string, int> solidsDict,
        Dictionary<string, FixedPoint2> reagentDict,
        uint? cookingTimer
        )
    {
        return _recipeManager.Recipes.Select(r =>
            CanSatisfyRecipe(instrumentType, cookingTimer, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);
    }
}
