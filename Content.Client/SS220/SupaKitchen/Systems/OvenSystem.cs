// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.SS220.SupaKitchen.Systems;
using Content.Shared.Storage.Components;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.SupaKitchen.Systems;

public sealed partial class OvenSystem : SharedOvenSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvenComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<OvenComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<OvenComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<OvenComponent, StorageAfterOpenEvent>(OnStorageOpen);

        SubscribeLocalEvent<OvenComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnHandleState(Entity<OvenComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not OvenComponentState state)
            return;

        entity.Comp.LastState = state.LastState;
        entity.Comp.CurrentState = state.CurrentState;
        entity.Comp.PlayingStream = state.PlayingStream;
    }

    private void OnAppearanceChange(Entity<OvenComponent> entity, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!_appearance.TryGetData<bool>(entity, OvenVisuals.Active, out var isActive))
            return;

        var state = isActive ? entity.Comp.ActiveState : entity.Comp.NonActiveState;
        args.Sprite.LayerSetState(OvenVisuals.Active, state);
        args.Sprite.LayerSetVisible(OvenVisuals.ActiveUnshaded, isActive);
    }
}
