using Content.Shared.SS220.Hookah;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Hookah;

public sealed class HookahVisualizerSystem : VisualizerSystem<HookahVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, HookahVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(HookahVisuals.State, out var raw) ||
            raw is not HookahVisualState state)
            return;

        var stateName = state switch
        {
            HookahVisualState.UnlitNoHose => component.UnlitNoHoseState,
            HookahVisualState.CoalUnlit => component.CoalUnlitState,
            HookahVisualState.CoalUnlitNoHose => component.CoalUnlitNoHoseState,
            HookahVisualState.CoalLit => component.CoalLitState,
            HookahVisualState.CoalLitNoHose => component.CoalLitNoHoseState,
            _ => component.UnlitState,
        };

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), 0, stateName);
    }
}
