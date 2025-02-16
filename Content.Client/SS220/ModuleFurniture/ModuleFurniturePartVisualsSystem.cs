// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ModuleFurniture;

public sealed class StorageContainerVisualsSystem : VisualizerSystem<ModuleFurniturePartComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, ModuleFurniturePartComponent component, ref AppearanceChangeEvent args)
    {
        // TODO: make it normal
        // prototypes accessible sprite and switching
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(ModuleFurnitureOpenVisuals.Layer, out var openLayer))
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, ModuleFurnitureOpenVisuals.InFurniture, out var inFurniture, args.Component)
            && !inFurniture)
        {
            args.Sprite.LayerSetVisible(openLayer, false);
            return;
        }

        if (!AppearanceSystem.TryGetData<bool>(uid, ModuleFurnitureOpenVisuals.Opened, out var opened, args.Component))
            return;

        args.Sprite.LayerSetVisible(openLayer, opened);
    }
}
