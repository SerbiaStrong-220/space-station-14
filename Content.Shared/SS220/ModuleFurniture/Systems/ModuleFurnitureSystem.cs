// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Events;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;


namespace Content.Shared.SS220.ModuleFurniture.Systems;

public abstract partial class SharedModuleFurnitureSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    private const float DoAfterMovementThreshold = 0.15f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, InteractUsingEvent>(OnInsertableInteract);
    }

    public bool CanInsert(Entity<ModuleFurnitureComponent?> entity, Entity<ModuleFurniturePartComponent?> target, [NotNullWhen(true)] out Vector2i? offset)
    {
        offset = null;
        if (!Resolve(entity.Owner, ref entity.Comp) || !Resolve(target.Owner, ref target.Comp))
            return false;

        var moduleComp = entity.Comp;
        var partComp = target.Comp;
        DebugTools.Assert(moduleComp.CachedLayout.Keys.Count == moduleComp.DrawerContainer.Count);

        var lastCacheEntry = moduleComp.DrawerContainer.Count == 0 ? new Vector2i(0, 0) : moduleComp.CachedLayout.Keys.Last();
        DebugTools.Assert(lastCacheEntry.X < moduleComp.TileLayoutSize.X && lastCacheEntry.Y < moduleComp.TileLayoutSize.Y);

        // TODO: GRAAAAAAAAAAAAAAA
        // So we need to find offset where to place new part (also it need for server part, so !!!internal shared)
        // Dont forget to think of (3,1) or even (3,3)
        if (moduleComp.TileLayoutSize.X - lastCacheEntry.X >= partComp.ContainerSize.X)
        {
            offset = lastCacheEntry;
            return true;
        }

        return false;
    }

    public bool TryInsert(Entity<ModuleFurnitureComponent?> entity, Entity<ModuleFurniturePartComponent?> target, EntityUid user)
    {
        if (!CanInsert(entity, target, out var offset))
            return false;

        var doafterArgs = new DoAfterArgs(
            EntityManager, user,
            TimeSpan.FromSeconds(2), // qol picked number
            new InsertedFurniturePart(offset.Value), entity.Owner, entity.Owner, target.Owner)
        {
            NeedHand = true,
            BreakOnMove = true,
            MovementThreshold = DoAfterMovementThreshold,
        };

        return _doAfter.TryStartDoAfter(doafterArgs);
    }

    private void OnInsertableInteract(Entity<ModuleFurnitureComponent> entity, ref InteractUsingEvent args)
    {
        if (args.Handled || !HasComp<ModuleFurniturePartComponent>(args.Used))
            return;

        args.Handled = TryInsert((entity.Owner, entity.Comp), args.Used, args.User);
    }
}
