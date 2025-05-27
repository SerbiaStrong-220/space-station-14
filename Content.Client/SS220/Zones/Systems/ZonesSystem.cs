
using Content.Client.Administration.Managers;
using Content.Client.SS220.Overlays;
using Content.Client.SS220.Zones.Overlays;
using Content.Client.SS220.Zones.UI;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Client.Console;
using Robust.Client.Graphics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly IClientConsoleHost _clientConsoleHost = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IClientAdminManager _clientAdmin = default!;

    public ZonesControlWindow ControlWindow = default!;

    public Entity<ZoneComponent>? SelectedZone => _selectedZone;

    private Entity<ZoneComponent>? _selectedZone;

    public Action<Entity<ZoneComponent>?>? ZoneSelected;

    private BoxesOverlay _overlay = default!;
    private ZonesBoxesDatasProvider _overlayProvider = default!;

    public override void Initialize()
    {
        base.Initialize();

        ControlWindow = new ZonesControlWindow();

        _overlay = BoxesOverlay.GetOverlay();

        _overlayProvider = new ZonesBoxesDatasProvider();

        _clientAdmin.AdminStatusUpdated += OnAdminStatusUpdated;

        SubscribeLocalEvent<ZoneComponent, ComponentInit>(OnZoneInit);
        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
        SubscribeLocalEvent<ZoneComponent, AfterAutoHandleStateEvent>(OnAfterZoneStateHandled);

        SubscribeLocalEvent<ZonesContainerComponent, ComponentInit>(OnContainerInit);
        SubscribeLocalEvent<ZonesContainerComponent, ComponentShutdown>(OnContainerShutdown);
        SubscribeLocalEvent<ZonesContainerComponent, AfterAutoHandleStateEvent>(OnAfterContainerStateHandled);
    }

    private void OnAdminStatusUpdated()
    {
        SetOverlay(_overlay.HasProvider(_overlayProvider));
    }

    private void OnZoneInit(Entity<ZoneComponent> entity, ref ComponentInit args)
    {
        ControlWindow.RefreshEntries();
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        if (entity == SelectedZone)
            SelectZone(null);

        ControlWindow.RefreshEntries();
    }

    private void OnAfterZoneStateHandled(Entity<ZoneComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }

    private void OnContainerInit(Entity<ZonesContainerComponent> entity, ref ComponentInit args)
    {
        ControlWindow.RefreshEntries();
    }

    private void OnContainerShutdown(Entity<ZonesContainerComponent> entity, ref ComponentShutdown args)
    {
        if (SelectedZone != null &&
            entity.Comp.Zones.Contains(GetNetEntity(SelectedZone.Value)))
            SelectZone(null);

        ControlWindow.RefreshEntries();
    }

    private void OnAfterContainerStateHandled(Entity<ZonesContainerComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }

    public void SelectZone(EntityUid? uid)
    {
        if (SelectedZone?.Owner == uid)
            return;

        Entity<ZoneComponent>? entity = null;
        if (TryComp<ZoneComponent>(uid, out var zoneComponent))
            entity = (uid.Value, zoneComponent);

        _selectedZone = entity;
        ZoneSelected?.Invoke(entity);
    }

    public void SetOverlay(bool value)
    {
        if (!_clientAdmin.HasFlag(AdminFlags.Mapping))
            value = false;

        switch (value)
        {
            case true:
                _overlay.AddProvider(_overlayProvider);
                break;

            case false:
                _overlay.RemoveProvider(_overlayProvider);
                break;
        }
    }

    public void ExecuteDeleteZonesContainer(EntityUid container)
    {
        _clientConsoleHost.ExecuteCommand($"zones:delete_container {GetNetEntity(container)}");
    }

    public void ExecuteDeleteZone(EntityUid zone)
    {
        _clientConsoleHost.ExecuteCommand($"zones:delete {GetNetEntity(zone)}");
    }

    public void ExecuteCreateZone(ZoneParamsState @params)
    {
        var boxes = string.Empty;
        foreach (var box in @params.Boxes)
        {
            if (!string.IsNullOrEmpty(boxes))
                boxes += "; ";

            boxes += $"({box.Left} {box.Bottom} {box.Right} {box.Top})";
        }

        _clientConsoleHost.ExecuteCommand($"zones:create {@params.Container} \"{boxes}\" name={@params.Name} protoid={@params.ProtoId} color={@params.Color.ToHex()} attachtogrid={@params.AttachToGrid}");
    }
}
