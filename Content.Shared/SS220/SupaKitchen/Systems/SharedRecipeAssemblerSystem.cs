using Content.Shared.DoAfter;
using Content.Shared.Placeable;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen.Systems;

public abstract partial class SharedRecipeAssemblerSystem : CookingInstrumentSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecipeAssemblerComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);

        /// ItemPlacer
        SubscribeLocalEvent<RecipeAssemblerComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<RecipeAssemblerComponent, ItemRemovedEvent>(OnItemRemoved);
    }

    private void OnAlternativeVerb(Entity<RecipeAssemblerComponent> entity, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var recipes = GetSatisfiedRecipes(entity.Comp, entity.Comp.Entities, 0);

        foreach (var (recipe, count) in recipes)
        {
            for (var i = 0; i < count; i++)
            {
                var proto = _prototypeManager.Index<EntityPrototype>(recipe.Result.Id);
                var verb = new AlternativeVerb()
                {
                    Text = Loc.GetString("recipe-assembler-verb", ("result", proto.Name), ("cookingTime", recipe.CookTime)),
                    Category = VerbCategory.AvalibleRecipes,
                    Act = () => StartAssembling(entity, recipe, user)
                };

                args.Verbs.Add(verb);
            }
        }
    }

    protected void StartAssembling(Entity<RecipeAssemblerComponent> entity,
        CookingRecipePrototype recipe,
        EntityUid user)
    {
        var doAfter = new DoAfterArgs(EntityManager, user, TimeSpan.FromSeconds(recipe.CookTime), new RecipeAssemblerDoAfterEvent(recipe.ID), entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            DistanceThreshold = 2,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            AttemptFrequency = AttemptFrequency.EveryTick,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    #region ItemPlacer
    private void OnItemPlaced(Entity<RecipeAssemblerComponent> entity, ref ItemPlacedEvent args)
    {
        entity.Comp.Entities.Add(args.OtherEntity);
        Dirty(entity);
    }

    private void OnItemRemoved(Entity<RecipeAssemblerComponent> entity, ref ItemRemovedEvent args)
    {
        entity.Comp.Entities.Remove(args.OtherEntity);
        Dirty(entity);
    }
    #endregion
}

[Serializable, NetSerializable]
public sealed partial class RecipeAssemblerDoAfterEvent : SimpleDoAfterEvent
{
    public readonly string Recipe;

    public RecipeAssemblerDoAfterEvent(string recipe)
    {
        Recipe = recipe;
    }
}
