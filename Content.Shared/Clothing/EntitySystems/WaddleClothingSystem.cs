using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.SS220.Movement.Components;
using Content.Shared.SS220.Movement.Systems;

namespace Content.Shared.Clothing.EntitySystems;

public sealed class WaddleClothingSystem : EntitySystem
{
    [Dependency] private readonly TemporaryWaddleSystem _temporaryWaddleSystem = default!; //SS220 Temporary waddle status effect

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<WaddleWhenWornComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(EntityUid entity, WaddleWhenWornComponent comp, ClothingGotEquippedEvent args)
    {
        var waddleAnimComp = EnsureComp<WaddleAnimationComponent>(args.Wearer);

        waddleAnimComp.AnimationLength = comp.AnimationLength;
        waddleAnimComp.HopIntensity = comp.HopIntensity;
        waddleAnimComp.RunAnimationLengthMultiplier = comp.RunAnimationLengthMultiplier;
        waddleAnimComp.TumbleIntensity = comp.TumbleIntensity;
    }

    private void OnGotUnequipped(EntityUid entity, WaddleWhenWornComponent comp, ClothingGotUnequippedEvent args)
    {
        //RemComp<WaddleAnimationComponent>(args.Wearer) //SS220 convert to comment

        //SS220 Temporary waddle status effect begin
        if (HasComp<TemporaryWaddleComponent>(args.Wearer))
        {
            _temporaryWaddleSystem.SetAnimationValues(args.Wearer);
        }
        else
            RemComp<WaddleAnimationComponent>(args.Wearer);
        //SS220 Temporary waddle status effect end
    }
}
