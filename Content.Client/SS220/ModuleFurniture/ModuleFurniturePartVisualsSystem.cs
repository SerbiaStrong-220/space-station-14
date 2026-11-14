// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ModuleFurniture.Components;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.ModuleFurniture;

public sealed class StorageContainerVisualsSystem : VisualizerSystem<ModuleFurniturePartComponent>
{
    private const int DeltaDrawDepth = 1;

    protected override void OnAppearanceChange(EntityUid uid, ModuleFurniturePartComponent component, ref AppearanceChangeEvent args)
    {
        // TODO: make it normal
        // prototypes accessible sprite and switching
        if (args.Sprite == null)
            return;

        if (!args.Sprite.LayerMapTryGet(ModuleFurniturePartVisuals.LayerOpened, out var openLayer)
            || !args.Sprite.LayerMapTryGet(ModuleFurniturePartVisuals.LayerClosed, out var closedLayer)
            || !args.Sprite.LayerMapTryGet(ModuleFurniturePartVisuals.LayerItem, out var layerItem))
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, ModuleFurniturePartVisuals.InFurniture, out var inFurniture, args.Component)
            || !AppearanceSystem.TryGetData<bool>(uid, ModuleFurniturePartVisuals.Opened, out var opened, args.Component))
            return;

        if (!inFurniture)
        {
            args.Sprite.LayerSetVisible(openLayer, false);
            args.Sprite.LayerSetVisible(closedLayer, false);
            args.Sprite.LayerSetVisible(layerItem, true);
            return;
        }

        if (component.Opened != opened)
        {
            //bruh... Convert is prohibited by typecheck
            var boolToSignedOne = opened ? 1 : -1;
            args.Sprite.DrawDepth += DeltaDrawDepth * boolToSignedOne;
        }

        component.Opened = opened;

        args.Sprite.LayerSetVisible(layerItem, false);
        args.Sprite.LayerSetVisible(openLayer, opened);
        args.Sprite.LayerSetVisible(closedLayer, !opened);
    }
}
