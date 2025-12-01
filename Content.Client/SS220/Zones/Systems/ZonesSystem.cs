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
using Robust.Shared.Prototypes;

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
    }

    private void OnAdminStatusUpdated()
    {
        SetOverlay(_overlayProvider.Active);
    }

    public void SetOverlay(bool value)
    {
        if (!_clientAdmin.HasFlag(AdminFlags.Mapping))
            value = false;

        _overlayProvider.Active = value;
    }

    public void CreateZoneRequest(
        NetEntity parent,
        EntProtoId<ZoneComponent> protoId,
        List<Box2> area,
        string? name = null,
        Color? color = null,
        bool attachToLattice = false)
    {
        var msg = new CreateZoneRequestMessage(parent, protoId, area, name, color, attachToLattice);
        RaiseNetworkEvent(msg);
    }

    public void ChangeZoneRequest(
        NetEntity zone,
        NetEntity? parent = null,
        List<Box2>? area = null,
        string? name = null,
        Color? color = null,
        bool? attachToLattice = null)
    {
        var msg = new ChangeZoneRequestMessage(zone, parent, area, name, color, attachToLattice);
        RaiseNetworkEvent(msg);
    }

    public void DeleteZoneRequest(NetEntity zone)
    {
        var msg = new DeleteZoneRequestMessage(zone);
        RaiseNetworkEvent(msg);
    }
}
