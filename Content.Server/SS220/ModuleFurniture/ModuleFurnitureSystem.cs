// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Content.Shared.Storage;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem<ModuleFurnitureComponent>
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentShutdown>(OnFurnitureShutdown);

        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentGetState>(GetCompState);

        SubscribeLocalEvent<ModuleFurnitureComponent, DeconstructFurnitureEvent>(OnDeconstructFurniturePart);

        SubscribeLocalEvent<ModuleFurniturePartComponent, EntGotRemovedFromContainerMessage>(OnPartRemovedFromContainer);
    }

    private void OnMapInit(Entity<ModuleFurnitureComponent> entity, ref MapInitEvent _)
    {
        MakeClearOccupation(entity.Comp);

        if (!TryComp<ModuleFurnitureFillComponent>(entity, out var fillComp))
            return;

        foreach (var protoId in fillComp.FillingEntity)
        {
            var spawnedUid = SpawnInContainerOrDrop(protoId, entity.Owner, ModuleFurnitureComponent.ContainerId);
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
            _appearance.SetData(spawnedUid, ModuleFurniturePartVisuals.InFurniture, true);
            AddToLayout(entity.Comp, (spawnedUid, partComponent), offset.Value);
            _appearance.SetData(spawnedUid, ModuleFurniturePartVisuals.Opened, false);
        }
        Dirty(entity);
    }

    private void OnComponentInit(Entity<ModuleFurnitureComponent> entity, ref ComponentInit _)
    {
        entity.Comp.DrawerContainer = _container.EnsureContainer<Container>(entity.Owner, ModuleFurnitureComponent.ContainerId);
        entity.Comp.DrawerContainer.ShowContents = true;
    }

    private void OnFurnitureShutdown(Entity<ModuleFurnitureComponent> entity, ref ComponentShutdown _)
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
        if (args.Cancelled)
            return;

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
        Dirty(entity);
    }

    private void OnRemoveFurniturePart(Entity<ModuleFurnitureComponent> entity, ref RemoveFurniturePartEvent args)
    {
        if (args.Cancelled)
            return;

        DebugTools.Assert(entity.Comp.DrawerContainer.Count == entity.Comp.CachedLayout.Values.Count);
        if (entity.Comp.DrawerContainer.Count == 0)
        {
            Log.Warning("Got a remove furniture part event, but there arent any entities in container");
            return;
        }

        var (lastKey, netEntityToRemove) = entity.Comp.CachedLayout.Last();
        var entityToRemove = GetEntity(netEntityToRemove);
        if (!entity.Comp.DrawerContainer.Contains(entityToRemove))
        {
            Log.Error($"Got a remove furniture part event, but container of {ToPrettyString(entity)} do not contain a {ToPrettyString(entityToRemove)} aka last entity in CachedOccupation");
            return;
        }

        FreeOccupation(entity.Comp, lastKey, entityToRemove);
        entity.Comp.CachedLayout.Remove(lastKey);
        if (!_container.RemoveEntity(entity, entityToRemove))
        {
            Log.Error($"Cant remove {ToPrettyString(entityToRemove)} from container of the {ToPrettyString(entity)}");
            return;
        }

        _appearance.SetData(entityToRemove, ModuleFurniturePartVisuals.InFurniture, false);

        DebugTools.Assert(!entity.Comp.CachedLayout.Values.Contains(netEntityToRemove));
        DebugTools.Assert(!entity.Comp.DrawerContainer.Contains(entityToRemove));

        Dirty(entity);
    }

    private void OnDeconstructFurniturePart(Entity<ModuleFurnitureComponent> entity, ref DeconstructFurnitureEvent args)
    {
        // If needed
    }

    private void OnPartRemovedFromContainer(Entity<ModuleFurniturePartComponent> entity, ref EntGotRemovedFromContainerMessage args)
    {
        if (MetaData(entity).EntityLifeStage == EntityLifeStage.Terminating)
            return;

        if (_container.TryGetContainer(entity.Owner, StorageComponent.ContainerId, out var container))
            _container.EmptyContainer(container, true);

        EntityManager.QueueDeleteEntity(entity);
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
