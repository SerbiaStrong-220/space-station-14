// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Interactable.Components;
using Content.Shared.Hands;
using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Robust.Client.Player;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem<ModuleFurnitureComponent>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentHandleState>(HandleCompState);

        SubscribeLocalEvent<ModuleFurniturePartComponent, GotEquippedHandEvent>(OnPartEquip);
        SubscribeLocalEvent<ModuleFurniturePartComponent, GotUnequippedHandEvent>(OnPartUneqiup);
    }

    public void UpdateVisual(Entity<ModuleFurnitureComponent> entity)
    {
        if (entity.Comp.CachedLayout.Count == 0)
            EnsureComp<InteractionOutlineComponent>(entity.Owner);
        else
            RemComp<InteractionOutlineComponent>(entity.Owner);
    }

    private void HandleCompState(Entity<ModuleFurnitureComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not ModuleFurnitureComponentState state)
            return;

        entity.Comp.TileLayoutSize = state.TileLayoutSize;
        entity.Comp.CachedLayout.Clear();
        foreach (var (key, value) in state.Layout)
        {
            entity.Comp.CachedLayout.Add(key, value);
        }

        for (int height = 0; height < state.TileLayoutSize.Y; height++)
        {
            for (int width = 0; width < state.TileLayoutSize.X; width++)
            {
                entity.Comp.CachedOccupation[(width, height)] = state.Occupation[(width + state.TileLayoutSize.X * height)];
            }
        }

        UpdateVisual(entity);
    }

    private void OnPartEquip(Entity<ModuleFurniturePartComponent> entity, ref GotEquippedHandEvent args)
    {
        if (args.User != _playerManager.LocalEntity)
            return;

        EnsureComp<InteractionOutlineComponent>(entity);
    }

    private void OnPartUneqiup(Entity<ModuleFurniturePartComponent> entity, ref GotUnequippedHandEvent args)
    {
        if (args.User != _playerManager.LocalEntity)
            return;

        var query = EntityQueryEnumerator<ModuleFurnitureComponent>();
        while (query.MoveNext(out var uid, out var furnitureComponent))
        {
            if (furnitureComponent.CachedLayout.Count != 0)
                RemComp<InteractionOutlineComponent>(uid);
        }
    }
}
