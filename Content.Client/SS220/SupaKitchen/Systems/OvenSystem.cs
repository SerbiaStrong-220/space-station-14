// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.SupaKitchen.Systems;

public sealed partial class OvenSystem : SharedOvenSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvenComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<OvenComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(entity, OvenVisuals.Active, out var isActive))
            return;

        if (args.Sprite.LayerExists(OvenVisuals.Active))
        {
            var state = isActive ? entity.Comp.ActiveState : entity.Comp.NonActiveState;
            args.Sprite.LayerSetState(OvenVisuals.Active, state);
        }

        if (args.Sprite.LayerExists(OvenVisuals.ActiveUnshaded))
            args.Sprite.LayerSetVisible(OvenVisuals.ActiveUnshaded, isActive);
    }
}
