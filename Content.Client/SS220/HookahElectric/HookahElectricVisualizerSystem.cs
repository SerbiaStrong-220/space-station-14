using Content.Shared.SS220.HookahElectric;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.HookahElectric;

public sealed class HookahElectricVisualizerSystem : VisualizerSystem<HookahElectricVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HookahElectricVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (args.AppearanceData.TryGetValue(HookahElectricVisuals.Enabled, out var rawOn) && rawOn is bool on)
        {
            SpriteSystem.LayerSetRsiState((uid, args.Sprite),
                HookahElectricVisualLayers.Base,
                on ? component.OnState : component.OffState);
        }

        if (args.AppearanceData.TryGetValue(HookahElectricVisuals.LeftHose, out var rawLeft) && rawLeft is bool left)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), HookahElectricVisualLayers.LeftHose, left);
        }

        if (args.AppearanceData.TryGetValue(HookahElectricVisuals.RightHose, out var rawRight) && rawRight is bool right)
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), HookahElectricVisualLayers.RightHose, right);
        }
    }
}
