// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SuperMatter.Observer;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Client.SS220.SuperMatter.Observer;

public sealed partial class SuperMatterObserverVisualReceiverSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SpriteSystem _sprite = default!;

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

        if (!_sprite.LayerMapTryGet(entity.Owner, SuperMatterVisualLayers.Shaded, out var layer, false))
            return;

        if (!_sprite.LayerMapTryGet(entity.Owner, SuperMatterVisualLayers.Unshaded, out var unshadedLayer, false))
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
                SetVisualLayers(entity.Owner, entity.Comp.DisabledState, layers);
                break;
            case SuperMatterVisualState.UnActiveState:
                if (entity.Comp.UnActiveState == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.UnActiveState, layers);
                break;
            case SuperMatterVisualState.Okay:
                if (entity.Comp.OnState == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.OnState, layers);
                break;
            case SuperMatterVisualState.Warning:
                if (entity.Comp.WarningState == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.WarningState, layers);
                break;
            case SuperMatterVisualState.Danger:
                if (entity.Comp.DangerState == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.DangerState, layers);
                break;
            case SuperMatterVisualState.Delaminate:
                if (entity.Comp.DelaminateState == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.DelaminateState, layers);
                break;
            case SuperMatterVisualState.RandomEvent:
                if (entity.Comp.RandomEvent == null)
                    break;
                SetVisualLayers(entity.Owner, entity.Comp.RandomEvent, layers);
                entity.Comp.RandomEventTime = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.RandomEventDuration);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void SetVisualLayers(EntityUid uid, Dictionary<SuperMatterVisualLayers, string> state, Dictionary<SuperMatterVisualLayers, int> layers)
    {
        foreach (SuperMatterVisualLayers visualLayerKey in Enum.GetValues(typeof(SuperMatterVisualLayers)))
        {
            if (!layers.TryGetValue(visualLayerKey, out var layer))
                continue;
            if (!state.TryGetValue(visualLayerKey, out var rsiState))
            {
                _sprite.LayerSetVisible(uid, layers[visualLayerKey], false);
                continue;
            }
            _sprite.LayerSetRsiState(uid, layer, rsiState);
            _sprite.LayerSetVisible(uid, layer, true);
        }
    }
}
