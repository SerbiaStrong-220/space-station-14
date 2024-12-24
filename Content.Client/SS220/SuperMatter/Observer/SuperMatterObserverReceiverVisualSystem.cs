// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.SS220.SuperMatter.Observer;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.SS220.SuperMatter.Observer;

public sealed class SuperMatterObserverVisualReceiverSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SuperMatterObserverVisualReceiverComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }
    private void OnAppearanceChange(Entity<SuperMatterObserverVisualReceiverComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<SuperMatterVisualState>(entity.Owner, SuperMatterVisuals.VisualState, out var state, args.Component))
            return;
        if (!args.Sprite.LayerMapTryGet(SuperMatterVisualLayers.Shaded, out var layer))
            return;
        if (!args.Sprite.LayerMapTryGet(SuperMatterVisualLayers.Unshaded, out var unshadedLayer))
            return;
        Dictionary<SuperMatterVisualLayers, int> layers = new()
                {{ SuperMatterVisualLayers.Shaded, layer },
                    { SuperMatterVisualLayers.Unshaded, unshadedLayer }};
        if (_gameTiming.CurTime < entity.Comp.RandomEventTime)
            return;
        // For those who wanted to make it right. Make it, thanks
        switch (state)
        {
            case SuperMatterVisualState.Disable:
                if (entity.Comp.DisabledState == null)
                    break;
                SetVisualLayers(entity.Comp.DisabledState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.UnActiveState:
                if (entity.Comp.UnActiveState == null)
                    break;
                SetVisualLayers(entity.Comp.UnActiveState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.Okay:
                if (entity.Comp.OnState == null)
                    break;
                SetVisualLayers(entity.Comp.OnState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.Warning:
                if (entity.Comp.WarningState == null)
                    break;
                SetVisualLayers(entity.Comp.WarningState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.Danger:
                if (entity.Comp.DangerState == null)
                    break;
                SetVisualLayers(entity.Comp.DangerState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.Delaminate:
                if (entity.Comp.DelaminateState == null)
                    break;
                SetVisualLayers(entity.Comp.DelaminateState, layers, args.Sprite);
                break;
            case SuperMatterVisualState.RandomEvent:
                if (entity.Comp.RandomEvent == null)
                    break;
                SetVisualLayers(entity.Comp.RandomEvent, layers, args.Sprite);
                entity.Comp.RandomEventTime = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.RandomEventDuration);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    private void SetVisualLayers(Dictionary<SuperMatterVisualLayers, string> state, Dictionary<SuperMatterVisualLayers, int> layers, SpriteComponent sprite)
    {
        foreach (SuperMatterVisualLayers visualLayerKey in Enum.GetValues(typeof(SuperMatterVisualLayers)))
        {
            if (!layers.TryGetValue(visualLayerKey, out var layer))
                continue;
            if (!state.TryGetValue(visualLayerKey, out var rsiState))
            {
                sprite.LayerSetVisible(layers[visualLayerKey], false);
                continue;
            }
            sprite.LayerSetState(layer, rsiState);
            sprite.LayerSetVisible(layer, true);
        }
    }
}
