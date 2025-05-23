
using Content.Client.Administration.Managers;
using Content.Client.SS220.Zones.UI;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IClientAdminManager _clientAdmin = default!;

    public ZonesControlWindow ControlWindow = default!;

    public Entity<ZoneComponent>? SelectedZone;

    public Action<Entity<ZoneComponent>?>? ZoneSelected;

    private ZonesOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        ControlWindow = new ZonesControlWindow();
        _overlay = new ZonesOverlay();

        _clientAdmin.AdminStatusUpdated += OnAdminStatusUpdated;
        SubscribeLocalEvent<ZoneComponent, AfterAutoHandleStateEvent>(OnAfterZoneStateHandled);
        SubscribeLocalEvent<ZonesContainerComponent, AfterAutoHandleStateEvent>(OnAfterZoneContainerStateHandled);
    }

    private void OnAdminStatusUpdated()
    {
        SetOverlay(_overlayManager.HasOverlay<ZonesOverlay>());
    }

    private void OnAfterZoneStateHandled(Entity<ZoneComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }


    private void OnAfterZoneContainerStateHandled(Entity<ZonesContainerComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }

    public void SelectZone(EntityUid? uid)
    {
        Entity<ZoneComponent>? entity = null;
        if (TryComp<ZoneComponent>(uid, out var zoneComponent))
            entity = (uid.Value, zoneComponent);

        SelectedZone = entity;
        ZoneSelected?.Invoke(entity);
    }

    public void SetOverlay(bool value)
    {
        if (!_clientAdmin.HasFlag(AdminFlags.Mapping))
            value = false;

        switch (value)
        {
            case true:
                _overlayManager.AddOverlay(_overlay);
                break;

            case false:
                _overlayManager.RemoveOverlay(_overlay);
                break;
        }
    }
}
