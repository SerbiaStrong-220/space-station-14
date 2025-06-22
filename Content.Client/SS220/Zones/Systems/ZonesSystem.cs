// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Administration.Managers;
using Content.Client.SS220.Overlays;
using Content.Client.SS220.Zones.Overlays;
using Content.Client.SS220.Zones.UI;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Client.Console;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly IClientConsoleHost _clientConsoleHost = default!;
    [Dependency] private readonly IClientAdminManager _clientAdmin = default!;

    public ZonesControlWindow ControlWindow = default!;

    private ZonesBoxesOverlayProvider _overlayProvider = default!;

    public override void Initialize()
    {
        base.Initialize();

        ControlWindow = new ZonesControlWindow();

        var overlay = BoxesOverlay.GetOverlay();

        if (overlay.TryGetProvider<ZonesBoxesOverlayProvider>(out var provider))
            _overlayProvider = provider;
        else
        {
            _overlayProvider = new ZonesBoxesOverlayProvider();
            overlay.AddProvider(_overlayProvider);
        }

        _clientAdmin.AdminStatusUpdated += OnAdminStatusUpdated;

        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
        SubscribeLocalEvent<ZoneComponent, ComponentHandleState>(OnZoneHandleState);

        SubscribeLocalEvent<ZonesContainerComponent, ComponentShutdown>(OnContainerShutdown);
        SubscribeLocalEvent<ZonesContainerComponent, ComponentHandleState>(OnContainerHandleState);
    }

    private void OnAdminStatusUpdated()
    {
        SetOverlay(_overlayProvider.Active);
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        ControlWindow.RefreshEntries();
    }

    private void OnZoneHandleState(Entity<ZoneComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not ZoneComponentState state)
            return;

        entity.Comp.ZoneParams.HandleState(state.State);
        ControlWindow.RefreshEntries();
    }

    private void OnContainerShutdown(Entity<ZonesContainerComponent> entity, ref ComponentShutdown args)
    {
        ControlWindow.RefreshEntries();
    }


    private void OnContainerHandleState(Entity<ZonesContainerComponent> entity, ref ComponentHandleState args)
    {
        if (args.Current is not ZonesContainerComponentState state)
            return;

        entity.Comp.Zones = state.Zones;
        ControlWindow.RefreshEntries();
    }

    public void SetOverlay(bool value)
    {
        if (!_clientAdmin.HasFlag(AdminFlags.Mapping))
            value = false;

        _overlayProvider.Active = value;
    }

    public void ExecuteDeleteZonesContainer(EntityUid container)
    {
        _clientConsoleHost.ExecuteCommand($"zones:delete_container {GetNetEntity(container)}");
    }

    public void ExecuteDeleteZone(EntityUid zone)
    {
        _clientConsoleHost.ExecuteCommand($"zones:delete {GetNetEntity(zone)}");
    }

    public void ExecuteCreateZone(ZoneParams @params)
    {
        var tags = string.Join(' ', @params.GetTags());
        _clientConsoleHost.ExecuteCommand($"zones:create {tags}");
    }

    public void ExecuteChangeZone(Entity<ZoneComponent> zone, ZoneParams newParams)
    {
        var tags = string.Join(' ', newParams.GetTags());
        _clientConsoleHost.ExecuteCommand($"zones:change {GetNetEntity(zone)} {tags}");
    }
}
