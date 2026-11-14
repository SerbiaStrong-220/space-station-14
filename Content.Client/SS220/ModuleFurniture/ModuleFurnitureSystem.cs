// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Content.Shared.SS220.ModuleFurniture.Systems;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.ModuleFurniture;

public sealed partial class ModuleFurnitureSystem : SharedModuleFurnitureSystem<ModuleFurnitureComponent>
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ModuleFurnitureComponent, ComponentHandleState>(HandleCompState);
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
    }
}
