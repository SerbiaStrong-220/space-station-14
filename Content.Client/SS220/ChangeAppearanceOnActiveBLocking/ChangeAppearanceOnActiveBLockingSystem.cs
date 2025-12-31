using Content.Shared.Blocking;
using Content.Shared.Inventory.Events;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.SS220.ItemToggle;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client.SS220.ChangeAppearanceOnActiveBLocking;
public sealed partial class ChangeAppearanceOnActiveBLockingSystem : VisualizerSystem<ChangeAppearanceOnActiveBLockingComponent>
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ChangeAppearanceOnActiveBLockingComponent, ActiveBlockingEvent>(OnActiveBlock);
    }

    public void OnAppearanceChanged(EntityUid uid, ChangeAppearanceOnActiveBLockingComponent component, ref AppearanceChangeEvent args)
    {
        if (!TryComp<AppearanceComponent>(uid, out var AppearanceComp))
        {
            return;
        }

        // Update the item's sprite
        if (args.Sprite != null && component.SpriteLayer != null &&
            SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.SpriteLayer, out var layer, false))
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), layer, component.Toggled);
        }
    }
    public void OnActiveBlock(EntityUid uid, ChangeAppearanceOnActiveBLockingComponent component, ActiveBlockingEvent args)
    {
        component.Toggled = args.Active;
    }
}
