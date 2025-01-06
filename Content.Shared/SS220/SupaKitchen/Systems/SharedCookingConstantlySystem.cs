// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SupaKitchen.Systems;

public abstract partial class SharedCookingConstantlySystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    protected void OnGetState(EntityUid uid, SharedCookingConstantlyComponent component, ref ComponentGetState args)
    {
        args.State = new CookingConstantlyComponentState(component.LastState,
            component.CurrentState,
            component.PlayingStream);
    }

    protected void OnHandleState(EntityUid uid, SharedCookingConstantlyComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not CookingConstantlyComponentState state)
            return;

        component.LastState = state.LastState;
        component.CurrentState = state.CurrentState;
        component.PlayingStream = state.PlayingStream;
    }

    #region Storage
    protected void OnStorageOpenAttempt(EntityUid uid, SharedCookingConstantlyComponent component, ref StorageOpenAttemptEvent args)
    {
        if (args.Cancelled || !component.UseEntityStorage)
            return;

        if (component.CurrentState is CookingConstantlyState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageCloseAttempt(EntityUid uid, SharedCookingConstantlyComponent component, ref StorageCloseAttemptEvent args)
    {
        if (args.Cancelled || !component.UseEntityStorage)
            return;

        if (component.CurrentState is CookingConstantlyState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageOpen(EntityUid uid, SharedCookingConstantlyComponent component, ref StorageAfterOpenEvent args)
    {
        Deactivate(uid, component);
    }
    #endregion

    public void Deactivate(EntityUid uid, SharedCookingConstantlyComponent component, bool changeState = true)
    {
        if (component.CurrentState != CookingConstantlyState.Active)
            return;

        if (changeState)
            SetState(uid, component, CookingConstantlyState.Idle);

        _audio.Stop(GetEntity(component.PlayingStream));
    }

    public void SetState(EntityUid uid, SharedCookingConstantlyComponent component, CookingConstantlyState state)
    {
        component.CurrentState = state;
        Dirty(uid, component);

        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, SharedCookingConstantlyComponent component)
    {
        var isActive = component.CurrentState is CookingConstantlyState.Active;

        _appearance.SetData(uid, CookingConstantlyVisuals.VisualState, component.CurrentState);
        _appearance.SetData(uid, CookingConstantlyVisuals.Active, isActive);
    }
}
