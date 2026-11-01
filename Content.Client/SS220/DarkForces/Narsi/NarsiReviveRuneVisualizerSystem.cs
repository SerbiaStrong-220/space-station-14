using Content.Client.SS220.DarkForces.Narsi;
using Robust.Client.GameObjects;
using Content.Shared.SS220.DarkForces.Runes;

namespace Content.Client.SS220.DarkForces.Narsi;

public sealed class NarsiReviveRuneVisualizerSystem : VisualizerSystem<NarsiRuneVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, NarsiRuneVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<RuneState>(uid, RuneStatus.Status, out var status, args.Component))
        {
            args.Sprite.LayerSetVisible(NarsiRuneVisualLayers.Idle, status == RuneState.Idle);
            args.Sprite.LayerSetVisible(NarsiRuneVisualLayers.Active, status == RuneState.Active);
        }
    }
}
