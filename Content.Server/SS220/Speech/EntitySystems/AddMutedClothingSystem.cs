// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Speech.Components;
using Content.Shared.Clothing;
using Content.Shared.Speech.Muting;

namespace Content.Server.SS220.Speech.EntitySystems;

public sealed class AddMutedClothingSystem : EntitySystem
{
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<AddMutedClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<AddMutedClothingComponent, ClothingGotUnequippedEvent>(OnGotUnequipped);
    }

    private void OnGotEquipped(Entity<AddMutedClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        // does the user already muted
        if (HasComp<MutedComponent>(args.Wearer))
        {
            ent.Comp.IsActive = false;
            return;
        }

        // add mured to a wearer
        AddComp<MutedComponent>(args.Wearer);

        ent.Comp.IsActive = true;
    }

    private void OnGotUnequipped(Entity<AddMutedClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        if (!ent.Comp.IsActive)
            return;

        // try to remove Muted
        if (HasComp<MutedComponent>(args.Wearer))
        {
            RemComp<MutedComponent>(args.Wearer);
        }

        ent.Comp.IsActive = false;
    }
}
