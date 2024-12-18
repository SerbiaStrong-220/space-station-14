using Content.Shared.Sticky.Components;
using Robust.Client.GameObjects;

namespace Content.Client.Sticky.Visualizers;

public sealed class StickyVisualizerSystem : VisualizerSystem<StickyVisualizerComponent>
{
    private EntityQuery<SpriteComponent> _spriteQuery;

    public override void Initialize()
    {
        base.Initialize();

        _spriteQuery = GetEntityQuery<SpriteComponent>();

        SubscribeLocalEvent<StickyVisualizerComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<StickyVisualizerComponent> ent, ref ComponentInit args)
    {
        if (!_spriteQuery.TryComp(ent, out var sprite))
            return;

        ent.Comp.OriginalDrawDepth = sprite.DrawDepth;
        ent.Comp.OriginalNoRotation = sprite.NoRotation; // SS220 rotate ent face to the user
    }

    protected override void OnAppearanceChange(EntityUid uid, StickyVisualizerComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<bool>(uid, StickyVisuals.IsStuck, out var isStuck, args.Component))
            return;

        var drawDepth = isStuck ? comp.StuckDrawDepth : comp.OriginalDrawDepth;
        args.Sprite.DrawDepth = drawDepth;

        // SS220 rotate ent face to the user begin
        var noRotation = isStuck ? comp.StuckNoRotation : comp.OriginalNoRotation;
        args.Sprite.NoRotation = noRotation;
        // SS220 rotate ent face to the user end
    }
}
