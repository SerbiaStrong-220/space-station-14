// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SupaKitchen.Systems;

public abstract partial class SharedOvenSystem : CookingInstrumentSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    #region Storage
    protected void OnStorageOpenAttempt<T>(Entity<T> entity, ref StorageOpenAttemptEvent args) where T : SharedOvenComponent
    {
        if (args.Cancelled || !entity.Comp.UseEntityStorage)
            return;

        if (entity.Comp.CurrentState is OvenState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageCloseAttempt<T>(Entity<T> entity, ref StorageCloseAttemptEvent args) where T : SharedOvenComponent
    {
        if (args.Cancelled || !entity.Comp.UseEntityStorage)
            return;

        if (entity.Comp.CurrentState is OvenState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageOpen<T>(Entity<T> entity, ref StorageAfterOpenEvent args) where T : SharedOvenComponent
    {
        Deactivate(entity, entity);
    }
    #endregion

    public void Deactivate(EntityUid uid, SharedOvenComponent component, bool changeState = true)
    {
        if (component.CurrentState != OvenState.Active)
            return;

        if (changeState)
            SetState(uid, component, OvenState.Idle);

        _audio.Stop(GetEntity(component.PlayingStream));
    }

    public void SetState(EntityUid uid, SharedOvenComponent component, OvenState state)
    {
        component.CurrentState = state;
        Dirty(uid, component);

        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, SharedOvenComponent component)
    {
        var isActive = component.CurrentState is OvenState.Active;

        _appearance.SetData(uid, OvenVisuals.VisualState, component.CurrentState);
        _appearance.SetData(uid, OvenVisuals.Active, isActive);
    }
}
