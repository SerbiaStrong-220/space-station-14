using Content.Shared.SS220.DarkForces.Narsi.Cultist.Blindness;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.SS220.DarkForces.Narsi.Cultist.Visualizers;

public sealed class NarsiCultistBlindnessVisualizeSystem : VisualizerSystem<NarsiCultistBlindnessVisualizeComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NarsiCultistBlindnessVisualizeComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NarsiCultistBlindnessVisualizeComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(EntityUid uid, NarsiCultistBlindnessVisualizeComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.RemoveLayer(NarsiCultistBlindnessLayers.Blindness);
    }

    private void OnComponentInit(EntityUid uid, NarsiCultistBlindnessVisualizeComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("SS220/DarkForces/Cult/Effects/blindness.rsi"), "blindness"));
        sprite.LayerMapSet(NarsiCultistBlindnessLayers.Blindness, layer);
    }

    protected override void OnAppearanceChange(EntityUid uid, NarsiCultistBlindnessVisualizeComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<NarsiCultistBlindnessState>(uid, NarsiCultistBlindnessStatus.Status, out var muzzleState, args.Component))
        {
            args.Sprite.LayerSetVisible(NarsiCultistBlindnessLayers.Blindness, muzzleState == NarsiCultistBlindnessState.Blindness);
        }
    }
}

public enum NarsiCultistBlindnessLayers : byte
{
    Blindness
}
