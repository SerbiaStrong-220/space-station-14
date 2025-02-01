using Content.Shared.DoAfter;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SupaKitchen.Systems;

public sealed partial class RecipeAssemblerSystem : SharedRecipeAssemblerSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly CookingInstrumentSystem _instrumentSystem = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RecipeAssemblerComponent, DoAfterAttemptEvent<RecipeAssemblerDoAfterEvent>>(OnDoAfterAttempt);
        SubscribeLocalEvent<RecipeAssemblerComponent, RecipeAssemblerDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfterAttempt(Entity<RecipeAssemblerComponent> entity, ref DoAfterAttemptEvent<RecipeAssemblerDoAfterEvent> args)
    {
        if (args.Cancelled || !TryComp<CookingInstrumentComponent>(entity, out var cookingInstrument))
            return;

        var recipe = _prototype.Index<CookingRecipePrototype>(args.Event.Recipe);
        var ents = _entityLookup.GetEntitiesInRange(entity, entity.Comp.Range);

        if (!_instrumentSystem.CanCookRecipe(cookingInstrument, recipe, ents, 0))
            args.Cancel();
    }

    private void OnDoAfter(Entity<RecipeAssemblerComponent> entity, ref RecipeAssemblerDoAfterEvent args)
    {
        if (args.Cancelled || !TryComp<CookingInstrumentComponent>(entity, out var cookingInstrument))
            return;

        var recipe = _prototype.Index<CookingRecipePrototype>(args.Recipe);
        var ents = _entityLookup.GetEntitiesInRange(entity, entity.Comp.Range);
        if (!_instrumentSystem.CanCookRecipe(cookingInstrument, recipe, ents, 0))
            return;

        var spawnCords = Transform(entity).Coordinates;
        Spawn(recipe.Result, spawnCords);
        _instrumentSystem.SubtractContents(ents, recipe);
    }
}
