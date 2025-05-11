// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.SupaKitchen.Components;
using Robust.Shared.Containers;
using System.Linq;

namespace Content.Shared.SS220.SupaKitchen.Systems;

public abstract partial class CookingInstrumentSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SupaRecipeManager _recipeManager = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public static (CookingRecipePrototype, int) SatisfyRecipe(
        BaseCookingInstrumentComponent component,
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

            portions = (int)(portions == 0
                ? solids[solid.Key] / solid.Value
                : Math.Min(portions, solids[solid.Key] / solid.Value));
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
            portions = (int)Math.Min(portions, cookingTimer / recipe.CookTime);


        return (recipe, portions);
    }

    public void SubtractContents(Container container, CookingRecipePrototype recipe)
    {
        SubtractContents(container.ContainedEntities, recipe, container);
    }

    public void SubtractContents(IEnumerable<EntityUid> entities, CookingRecipePrototype recipe, Container? contaiter = null)
    {
        SubtractContents(entities.ToList(), recipe, contaiter);
    }

    public void SubtractContents(List<EntityUid> entities, CookingRecipePrototype recipe, Container? contaiter = null)
    {
        var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

        // this is spaghetti ngl
        foreach (var item in entities)
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
            var amount = recipeSolid.Value;
            for (var i = 0; i < entities.Count && amount > 0; i++)
            {
                var currentEnt = entities[i];
                var proto = Prototype(currentEnt);

                if (proto?.ID == recipeSolid.Key)
                {
                    if (contaiter != null)
                        _container.Remove(currentEnt, contaiter);

                    EntityManager.DeleteEntity(currentEnt);
                    entities.RemoveAt(i);
                    i--;
                    amount--;
                }
            }
        }
    }

    public (CookingRecipePrototype, int) GetSatisfiedPortionedRecipe(
        BaseCookingInstrumentComponent component,
        IEnumerable<EntityUid> entities,
        uint cookingTimer)
    {
        return GetSatisfiedPortionedRecipe(component, GetSolids(entities), GetReagents(entities), cookingTimer);
    }

    public (CookingRecipePrototype, int) GetSatisfiedPortionedRecipe(
        BaseCookingInstrumentComponent component,
        Dictionary<string, int> solidsDict,
        Dictionary<string, FixedPoint2> reagentDict,
        uint cookingTimer
        )
    {
        if (component.AdditionalRecipes.Count > 0)
        {
            foreach (var recipeId in component.AdditionalRecipes)
            {
                var recipe = _recipeManager.TryGetRecipePrototype(recipeId);
                if (recipe is null)
                    continue;

                var satisfiedRecipe = SatisfyRecipe(component, recipe, solidsDict, reagentDict, cookingTimer);
                if (satisfiedRecipe.Item2 > 0)
                    return satisfiedRecipe;
            }
        }

        return _recipeManager.Recipes.Select(r =>
            SatisfyRecipe(component, r, solidsDict, reagentDict, cookingTimer)).FirstOrDefault(r => r.Item2 > 0);
    }

    public Dictionary<CookingRecipePrototype, int> GetSatisfiedRecipes(
    BaseCookingInstrumentComponent component,
    IEnumerable<EntityUid> entities,
    uint cookingTimer)
    {
        return _recipeManager.Recipes.Select(r =>
            SatisfyRecipe(component, r, GetSolids(entities), GetReagents(entities), cookingTimer))
            .Where(r => r.Item2 > 0)
            .ToDictionary(r => r.Item1, r => r.Item2);
    }

    public Dictionary<string, int> GetSolids(IEnumerable<EntityUid> entities)
    {
        Dictionary<string, int> solids = new();
        foreach (var entity in entities)
        {
            var metaData = MetaData(entity);
            if (metaData.EntityPrototype is null)
                continue;

            if (!solids.TryAdd(metaData.EntityPrototype.ID, 1))
                solids[metaData.EntityPrototype.ID]++;
        }

        return solids;
    }

    public Dictionary<string, FixedPoint2> GetReagents(IEnumerable<EntityUid> entities)
    {
        Dictionary<string, FixedPoint2> reagents = new();
        foreach (var entity in entities)
        {
            if (!TryComp<SolutionContainerManagerComponent>(entity, out var solMan))
                continue;

            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((entity, solMan)))
            {
                var solution = soln.Comp.Solution;
                {
                    foreach (var (reagent, quantity) in solution.Contents)
                    {
                        if (!reagents.TryAdd(reagent.Prototype, quantity))
                            reagents[reagent.Prototype] += quantity;
                    }
                }
            }
        }

        return reagents;
    }

    public bool CanCookRecipe(BaseCookingInstrumentComponent component,
        CookingRecipePrototype recipe,
        IEnumerable<EntityUid> entities,
        uint cookingTimer)
    {
        var satisfiedRecipe = SatisfyRecipe(component, recipe, GetSolids(entities), GetReagents(entities), cookingTimer);
        return satisfiedRecipe.Item2 > 0;
    }
}
