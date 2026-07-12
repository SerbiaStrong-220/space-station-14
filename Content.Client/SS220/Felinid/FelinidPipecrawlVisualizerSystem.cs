using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.Client.SS220.Felinid;

public sealed class FelinidPipecrawlVisualizerSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private FelinidPipecrawlOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FelinidPipecrawlComponent, FelinidPipecrawlVisualsChangedEvent>(OnVisualsChanged);

        _overlay = new FelinidPipecrawlOverlay();
        _overlayManager.AddOverlay(_overlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlayManager.RemoveOverlay(_overlay);
    }

    private void OnVisualsChanged(
        Entity<FelinidPipecrawlComponent> ent,
        ref FelinidPipecrawlVisualsChangedEvent args)
    {
        UpdateVisibility(ent, args.Active);
    }

    private void UpdateVisibility(Entity<FelinidPipecrawlComponent> ent, bool active)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        if (!active)
        {
            RestoreVisibility(ent, sprite);
            return;
        }

        if (!TryComp<FelinidPipecrawlVisualStateComponent>(ent.Owner, out var state))
        {
            state = AddComp<FelinidPipecrawlVisualStateComponent>(ent.Owner);
            state.OriginalVisibility = sprite.Visible;
        }

        _sprite.SetVisible((ent.Owner, sprite), false);
    }

    private void RestoreVisibility(Entity<FelinidPipecrawlComponent> ent, SpriteComponent? sprite = null)
    {
        if (!TryComp<FelinidPipecrawlVisualStateComponent>(ent.Owner, out var state) ||
            !Resolve(ent.Owner, ref sprite, false))
        {
            return;
        }

        _sprite.SetVisible((ent.Owner, sprite), state.OriginalVisibility);
        RemCompDeferred<FelinidPipecrawlVisualStateComponent>(ent.Owner);
    }
}
