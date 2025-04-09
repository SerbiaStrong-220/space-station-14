// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SupaKitchen.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.SupaKitchen.Systems;

public abstract partial class SharedOvenSystem : CookingInstrumentSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvenComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<OvenComponent, StorageOpenAttemptEvent>(OnStorageOpenAttempt);
        SubscribeLocalEvent<OvenComponent, StorageCloseAttemptEvent>(OnStorageCloseAttempt);
        SubscribeLocalEvent<OvenComponent, StorageAfterOpenEvent>(OnStorageOpen);
    }

    private void OnInit(Entity<OvenComponent> entity, ref ComponentInit args)
    {
        if (entity.Comp.UseEntityStorage)
            entity.Comp.Container = _container.EnsureContainer<Container>(entity, SharedEntityStorageSystem.ContainerName);
        else
            entity.Comp.Container = _container.EnsureContainer<Container>(entity, entity.Comp.ContainerName);

        Dirty(entity);
    }

    #region Storage
    protected void OnStorageOpenAttempt(Entity<OvenComponent> entity, ref StorageOpenAttemptEvent args)
    {
        if (args.Cancelled || !entity.Comp.UseEntityStorage)
            return;

        if (entity.Comp.CurrentState is OvenState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageCloseAttempt(Entity<OvenComponent> entity, ref StorageCloseAttemptEvent args)
    {
        if (args.Cancelled || !entity.Comp.UseEntityStorage)
            return;

        if (entity.Comp.CurrentState is OvenState.Broken)
            args.Cancelled = true;
    }

    protected void OnStorageOpen(Entity<OvenComponent> entity, ref StorageAfterOpenEvent args)
    {
        Deactivate(entity, entity);
    }
    #endregion

    public void Deactivate(EntityUid uid, OvenComponent component, bool changeState = true)
    {
        if (component.CurrentState != OvenState.Active)
            return;

        if (changeState)
            SetState(uid, component, OvenState.Idle);

        _audio.Stop(GetEntity(component.PlayingStream));
    }

    public void SetState(EntityUid uid, OvenComponent component, OvenState state)
    {
        component.CurrentState = state;
        Dirty(uid, component);

        UpdateAppearance(uid, component);
    }

    private void UpdateAppearance(EntityUid uid, OvenComponent component)
    {
        var isActive = component.CurrentState is OvenState.Active;

        _appearance.SetData(uid, OvenVisuals.VisualState, component.CurrentState);
        _appearance.SetData(uid, OvenVisuals.Active, isActive);
    }
}
