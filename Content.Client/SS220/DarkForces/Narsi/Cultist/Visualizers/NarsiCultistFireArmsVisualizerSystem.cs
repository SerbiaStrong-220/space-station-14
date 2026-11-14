using Content.Shared.SS220.DarkForces.Narsi.Cultist.FireArms;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using NarsiCultistFireArmsVisualizerComponent = Content.Shared.SS220.DarkForces.Narsi.Cultist.FireArms.NarsiCultistFireArmsVisualizerComponent;

namespace Content.Client.SS220.DarkForces.Narsi.Cultist.Visualizers;

public sealed class NarsiCultistFireArmsVisualizerSystem : VisualizerSystem<NarsiCultistFireArmsVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NarsiCultistFireArmsVisualizerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NarsiCultistFireArmsVisualizerComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(EntityUid uid, NarsiCultistFireArmsVisualizerComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.RemoveLayer(NarsiCultistFireArmsLayers.Fire);
    }

    private void OnComponentInit(EntityUid uid, NarsiCultistFireArmsVisualizerComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("SS220/DarkForces/Cult/Effects/fire_arms.rsi/"), "fire_arms"));
        sprite.LayerMapSet(NarsiCultistFireArmsLayers.Fire, layer);
    }

    protected override void OnAppearanceChange(EntityUid uid, NarsiCultistFireArmsVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<NarsiCultistFireArmsState>(uid, NarsiCultistFireArmsStatus.Status, out var armsState, args.Component))
        {
            args.Sprite.LayerSetVisible(NarsiCultistFireArmsLayers.Fire, armsState == NarsiCultistFireArmsState.Fire);
        }
    }
}

public enum NarsiCultistFireArmsLayers : byte
{
    Fire
}
