// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.SupaKitchen.Systems;

public sealed partial class CookingConstantlySystem : SharedCookingConstantlySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CookingConstantlyComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<CookingConstantlyComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<CookingConstantlyComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<CookingConstantlyComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<CookingConstantlyComponent, StorageAfterOpenEvent>(OnStorageOpen);

        SubscribeLocalEvent<CookingConstantlyComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnAppearanceChange(Entity<CookingConstantlyComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(entity, CookingConstantlyVisuals.Active, out var isActive))
            return;

        var state = isActive ? entity.Comp.ActiveState : entity.Comp.NonActiveState;
        args.Sprite.LayerSetState(CookingConstantlyVisuals.Active, state);
        args.Sprite.LayerSetVisible(CookingConstantlyVisuals.ActiveUnshaded, isActive);
    }
}
