// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem<ModuleFurnitureComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentGetState>(GetCompState);
        SubscribeLocalEvent<ModuleFurnitureComponent, InsertedFurniturePart>(OnInsertedFurniturePart);
    }

    private void OnMapInit(Entity<ModuleFurnitureComponent> entity, ref MapInitEvent _)
    {
        MakeClearOccupation(entity.Comp);

        foreach (var protoId in entity.Comp.FillingEntity)
        {
            var spawnedUid = SpawnInContainerOrDrop(protoId, entity.Owner, entity.Comp.ContainerId);
            if (!TryComp<ModuleFurniturePartComponent>(spawnedUid, out var partComponent))
            {
                Log.Error($"Spawned entity which filled {ToPrettyString(entity)} but it doesnt have {nameof(ModuleFurniturePartComponent)}, protoId was {protoId}");
                continue;
            }
            if (!TryGetOffsetForPlacement(entity.Comp, partComponent, out var offset))
            {
                Log.Warning($"Filled an modular furniture with entities which cant be placet in it. Furniture protoId {MetaData(entity.Owner).EntityPrototype} and stopped at {protoId}");
                _container.RemoveEntity(entity.Owner, spawnedUid);
                continue;
            }
            AddToOccupation(entity.Comp, (spawnedUid, partComponent), offset.Value);
            AddToLayout(entity.Comp, (spawnedUid, partComponent), offset.Value);
        }
        Dirty(entity);
    }

    private void OnComponentInit(Entity<ModuleFurnitureComponent> entity, ref ComponentInit _)
    {
        entity.Comp.DrawerContainer = _container.EnsureContainer<Container>(entity.Owner, entity.Comp.ContainerId);
    }

    private void OnRemove(Entity<ModuleFurnitureComponent> entity, ref ComponentRemove _)
    {
        _container.EmptyContainer(entity.Comp.DrawerContainer);
    }

    private void GetCompState(Entity<ModuleFurnitureComponent> entity, ref ComponentGetState args)
    {
        DebugTools.Assert(entity.Comp.CachedOccupation.Values.Count == entity.Comp.TileLayoutSize.X * entity.Comp.TileLayoutSize.Y);
        args.State = new ModuleFurnitureComponentState(entity.Comp.CachedOccupation.Values.ToList(), entity.Comp.CachedLayout.ToDictionary(), entity.Comp.TileLayoutSize);
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

#if DEBUG
        Log.Debug($"Adding to {ToPrettyString(entity)} part {ToPrettyString(args.Used)} in place {args.Offset}");
        PrintDebugOccupation(entity.Comp);
#endif
        AddToModuleFurniture(entity, (args.Used.Value, partComponent), args.Offset);
        Dirty(entity);
    }

    private void ForceRebuildLayout(ModuleFurnitureComponent furnitureComp)
    {
        foreach (var uid in furnitureComp.DrawerContainer.ContainedEntities)
        {
            if (!TryComp<ModuleFurniturePartComponent>(uid, out var partComp))
            {
                Log.Error($"Cant get part component for placement of the {ToPrettyString(uid)} during force rebuild layout");
                continue;
            }

            if (!TryGetOffsetForPlacement(furnitureComp, partComp, out var offset))
            {
                Log.Error($"Cant get offset for placement of the {ToPrettyString(uid)} in furniture during force rebuild layout");
                continue;
            }

            AddToModuleFurniture(furnitureComp, (uid, partComp), offset.Value);
        }
    }
}
