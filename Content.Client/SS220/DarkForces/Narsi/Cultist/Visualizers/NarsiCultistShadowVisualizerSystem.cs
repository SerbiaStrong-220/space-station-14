using Content.Shared.SS220.DarkForces.Narsi.Cultist.Shadow;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.DarkForces.Narsi.Cultist.Visualizers;

public sealed class NarsiCultistShadowVisualizerSystem : VisualizerSystem<NarsiCultistShadowVisualizeComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NarsiCultistShadowVisualizeComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, NarsiCultistShadowVisualizeComponent component, ComponentInit args)
    {
        var localEntity = GetEntity(component.EntityToCopy);
        if (!TryComp<SpriteComponent>(uid, out var sprite) || !TryComp<SpriteComponent>(localEntity, out var spriteToCopy))
            return;

        sprite.CopyFrom(spriteToCopy);
        Dirty(uid, sprite);
    }
}
