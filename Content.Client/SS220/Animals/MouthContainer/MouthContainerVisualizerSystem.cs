using Content.Shared.SS220.Animals.MouthContainer;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Animals.MouthContainer;

public sealed class MouthContainerVisualizerSystem : VisualizerSystem<MouthContainerVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, MouthContainerVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<bool>(uid, MouthContainerVisuals.Visible, out var isStored, args.Component))
            SpriteSystem.LayerSetVisible((uid, args.Sprite), MouthContainerVisualLayers.Cheeks, isStored);
    }
}

public enum MouthContainerVisualLayers : byte
{
    Cheeks,
}
