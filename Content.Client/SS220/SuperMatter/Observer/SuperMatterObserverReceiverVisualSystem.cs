// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using Content.Shared.SS220.SuperMatter.Observer;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.SuperMatter.Observer;

public sealed class SuperMatterObserverVisualReceiverSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

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

        if (!args.Sprite.LayerMapTryGet(SuperMatterVisualLayers.Lights, out var layer))
            return;
        if (!args.Sprite.LayerMapTryGet(SuperMatterVisualLayers.UnShaded, out var unshadedLayer))
            return;
        // For those who wanted to make it right. Make it, thanks
        var isUnshadedVisible = false;
        switch (state)
        {
            case SuperMatterVisualState.Disable:
                isUnshadedVisible = true;
                if (entity.Comp.DisabledState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.DisabledState);
                break;
            case SuperMatterVisualState.UnActiveState:
                isUnshadedVisible = true;
                if (entity.Comp.UnActiveState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.UnActiveState);
                break;
            case SuperMatterVisualState.Okay:
                if (entity.Comp.OnState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.OnState);
                break;
            case SuperMatterVisualState.Warning:
                if (entity.Comp.WarningState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.WarningState);
                break;
            case SuperMatterVisualState.Danger:
                if (entity.Comp.DangerState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.DangerState);
                break;
            case SuperMatterVisualState.Delaminate:
                if (entity.Comp.DelaminateState == null)
                    break;
                args.Sprite.LayerSetState(layer, entity.Comp.DelaminateState);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        args.Sprite.LayerSetVisible(unshadedLayer, isUnshadedVisible);
    }
}
