// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Items.Systems;
using Content.Client.SS220.Weapons.Ranged.Visualizer.Components;
using Content.Client.Weapons.Ranged.Components;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Weapons.Ranged.Visualizer.Systems;

/// <summary>
/// This handles the display of inhand sprite on guns.
/// </summary>
public sealed class GunByHasAmmoVisualizerSystem : VisualizerSystem<GunByHasAmmoVisualsComponent>
{
    [Dependency] private readonly SharedItemSystem _itemSys = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunByHasAmmoVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals, after: new[] { typeof(ItemSystem) });
    }

    protected override void OnAppearanceChange(EntityUid uid, GunByHasAmmoVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite != null &&
            component.LayerNumber == null &&
            args.Sprite.LayerMapTryGet(GunVisualLayers.Base, out var layer))
        {
            component.LayerNumber = layer;
        }
        _itemSys.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, GunByHasAmmoVisualsComponent component, GetInhandVisualsEvent args)
    {
        if ( !TryComp(uid, out AppearanceComponent? appearance)
            || !AppearanceSystem.TryGetData<int>(uid, AmmoVisuals.AmmoCount, out var count, appearance)
            || component.LayerNumber == null)
            return;

        if (count != 0)
        {
            if(component.PreviousState == null)
                return;
            args.Layers[component.LayerNumber.Value].Item2.State = component.PreviousState;
            component.PreviousState = null;
            return;
        }

        if (!component.InhandVisuals.TryGetValue(args.Location, out var layers))
            return;

        foreach (var layer in layers)
        {
            component.PreviousState = args.Layers[component.LayerNumber.Value].Item2.State;
            args.Layers[component.LayerNumber.Value].Item2.State = layer.State;
        }
    }
}
