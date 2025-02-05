// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Content.Shared.SS220.ModuleFurniture.Systems;

namespace Content.Server.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, InsertedFurniturePart>(OnInsertedFurniturePart);
    }

    private void OnInsertedFurniturePart(Entity<ModuleFurnitureComponent> entity, ref InsertedFurniturePart args)
    {
        if (!args.Used.HasValue)
        {
            Log.Error($"Got event {nameof(InsertedFurniturePart)} with null used property. That is incorrect behavior!");
            return;
        }

        if (!TryComp<ModuleFurniturePartComponent>(args.Used.Value, out var partComponent))
        {
            Log.Error($"Got entity {ToPrettyString(args.Used.Value)} without {nameof(ModuleFurniturePartComponent)}");
            return;
        }

        AddToModuleFurniture(entity, (args.Used.Value, partComponent), args.Offset);
    }

}
