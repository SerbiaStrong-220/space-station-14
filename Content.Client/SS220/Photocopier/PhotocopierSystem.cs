// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Photocopier;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Photocopier;

public sealed partial class PhotocopierSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private readonly PhotocopierCombinedVisualState _fallbackVisualState =
        new(PhotocopierVisualState.Off, false, false, false);

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhotocopierComponent, AppearanceChangeEvent>(OnAppearanceChange);
        SubscribeLocalEvent<PhotocopierComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    private void OnAppearanceChange(EntityUid uid, PhotocopierComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(PhotocopierVisuals.VisualState, out var visualStateObject) ||
            visualStateObject is not PhotocopierCombinedVisualState visualState)
        {
            visualState = _fallbackVisualState;
        }

        UpdateAppearance(uid, visualState);
    }

    private void OnAnimationCompleted(EntityUid uid, PhotocopierComponent component, AnimationCompletedEvent args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
            !_appearanceSystem.TryGetData<PhotocopierCombinedVisualState>(uid, PhotocopierVisuals.VisualState, out var visualState, appearance))
        {
            visualState = _fallbackVisualState;
        }

        UpdateAppearance(uid, visualState);
    }

    private void UpdateAppearance(EntityUid uid, PhotocopierCombinedVisualState visualState)
    {
        SetLayerState(PhotocopierVisualLayers.Base, "off", uid);

        switch (visualState.State)
        {
            case PhotocopierVisualState.Off:
                HideLayer(PhotocopierVisualLayers.Led, uid);
                HideLayer(PhotocopierVisualLayers.Top, uid);
                HideLayer(PhotocopierVisualLayers.PrintAnim, uid);
                break;

            case PhotocopierVisualState.Powered:
                SetLayerState(PhotocopierVisualLayers.Led, "led_powered", uid);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", uid);
                HideLayer(PhotocopierVisualLayers.PrintAnim, uid);
                break;

            case PhotocopierVisualState.OutOfToner:
                SetLayerState(PhotocopierVisualLayers.Led, "led_out", uid);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", uid);
                HideLayer(PhotocopierVisualLayers.PrintAnim, uid);
                break;

            case PhotocopierVisualState.Printing:
                SetLayerState(PhotocopierVisualLayers.Led, "led_printing", uid);
                SetLayerState(PhotocopierVisualLayers.Top, "top_powered", uid);
                SetLayerState(PhotocopierVisualLayers.PrintAnim, "printing_paper", uid);
                break;

            case PhotocopierVisualState.Copying:
                SetLayerState(PhotocopierVisualLayers.Led, "led_printing", uid);
                SetLayerState(
                    PhotocopierVisualLayers.Top,
                    visualState.BurnsButts ? "top_scanning_emagged" : "top_scanning",
                    uid);
                SetLayerState(PhotocopierVisualLayers.PrintAnim, "printing_paper", uid);
                break;
        }

        if (visualState.State != PhotocopierVisualState.Off && visualState.BurnsButtManually)
        {
            SetLayerState(PhotocopierVisualLayers.Led, "led_printing", uid);
            SetLayerState(PhotocopierVisualLayers.Top, "top_scanning_emagged", uid);
        }

        if (visualState.GotItem)
            SetLayerState(PhotocopierVisualLayers.TopPaper, "top_paper", uid);
        else
            HideLayer(PhotocopierVisualLayers.TopPaper, uid);
    }

    private void SetLayerState(PhotocopierVisualLayers layer, string? state, EntityUid uid)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetVisible(uid, layer, true);
        _sprite.LayerSetAutoAnimated(uid, layer, true);
        _sprite.LayerSetRsiState(uid, layer, state);
    }

    private void HideLayer(PhotocopierVisualLayers layer, EntityUid uid)
    {
        if (!_sprite.LayerMapTryGet(uid, layer, out var actualLayer, false))
            return;

        _sprite.LayerSetVisible(uid, actualLayer, false);
    }
}
