// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Rounding;
using Content.Shared.SS220.Virology;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Virology;

public sealed class VaccinatorVisualizerSystem : VisualizerSystem<VaccinatorBufferVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, VaccinatorBufferVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        var sprite = (uid, args.Sprite);

        AppearanceSystem.TryGetData<float>(uid, VaccinatorVisuals.BufferFill, out var fill, args.Component);
        var level = component.MaxFillLevels > 0
            ? ContentHelpers.RoundToLevels(fill, 1f, component.MaxFillLevels + 1)
            : 0;

        if (level <= 0)
        {
            SpriteSystem.LayerSetVisible(sprite, VaccinatorVisualLayers.Buffer, false);
            return;
        }

        SpriteSystem.LayerSetVisible(sprite, VaccinatorVisualLayers.Buffer, true);
        SpriteSystem.LayerSetRsiState(sprite, VaccinatorVisualLayers.Buffer, $"{component.FillBaseName}-{level}");
    }
}
