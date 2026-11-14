using Content.Shared.SS220.DarkForces.Narsi.Cultist.Muzzle;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.SS220.DarkForces.Narsi.Cultist.Visualizers;

public sealed class NarsiCultistMuzzleVisualizerSystem : VisualizerSystem<NarsiCultistMuzzleVisualizerComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NarsiCultistMuzzleVisualizerComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<NarsiCultistMuzzleVisualizerComponent, ComponentRemove>(OnComponentRemove);
    }

    private void OnComponentRemove(EntityUid uid, NarsiCultistMuzzleVisualizerComponent component, ComponentRemove args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        sprite.RemoveLayer(NarsiCultistMuzzleLayers.Muzzle);
    }

    private void OnComponentInit(EntityUid uid, NarsiCultistMuzzleVisualizerComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var layer = sprite.AddLayer(new SpriteSpecifier.Rsi(new ResPath("SS220/DarkForces/Cult/Effects/muzzle.rsi"), "muzzle"));
        sprite.LayerMapSet(NarsiCultistMuzzleLayers.Muzzle, layer);
    }

    protected override void OnAppearanceChange(EntityUid uid, NarsiCultistMuzzleVisualizerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (AppearanceSystem.TryGetData<NarsiCultistMuzzleState>(uid, NarsiCultistMuzzleStatus.Status, out var muzzleState, args.Component))
        {
            args.Sprite.LayerSetVisible(NarsiCultistMuzzleLayers.Muzzle, muzzleState == NarsiCultistMuzzleState.Muzzle);
        }
    }
}

public enum NarsiCultistMuzzleLayers : byte
{
    Muzzle
}
