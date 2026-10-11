using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.SS220.Felinid;

public sealed partial class DisposalPipeCrawlerVisualizerSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private DisposalPipeCrawlerOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalPipeCrawlerComponent, DisposalPipeCrawlerVisualsChangedEvent>(OnVisualsChanged);

        _overlay = new DisposalPipeCrawlerOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnVisualsChanged(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref DisposalPipeCrawlerVisualsChangedEvent args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (args.InsidePipe)
        {
            if (!HasComp<DisposalPipeCrawlerVisualStateComponent>(ent.Owner))
                AddComp<DisposalPipeCrawlerVisualStateComponent>(ent.Owner).OriginalVisibility = sprite.Visible;

            _sprite.SetVisible((ent.Owner, sprite), false);
            return;
        }

        if (!TryComp<DisposalPipeCrawlerVisualStateComponent>(ent.Owner, out var state))
            return;

        _sprite.SetVisible((ent.Owner, sprite), state.OriginalVisibility);
        RemCompDeferred<DisposalPipeCrawlerVisualStateComponent>(ent.Owner);
    }
}
