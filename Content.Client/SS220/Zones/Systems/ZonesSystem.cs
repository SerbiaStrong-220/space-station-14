// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Zones.UI;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private ZonesControlUIController _controller = default!;

    public override void Initialize()
    {
        base.Initialize();

        _controller = _ui.GetUIController<ZonesControlUIController>();

        SubscribeLocalEvent<ZoneComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ZoneComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnInit(Entity<ZoneComponent> ent, ref ComponentInit args)
    {
        _controller.RefreshWindow();
    }

    private void OnShutdown(Entity<ZoneComponent> ent, ref ComponentShutdown args)
    {
        _controller.RefreshWindow();
    }

    private void OnAfterAutoHandleState(Entity<ZoneComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _controller.RefreshWindow();
    }

    public void CreateZoneRequest(
        NetEntity parent,
        EntProtoId<ZoneComponent> protoId,
        List<Box2> area,
        string? name = null,
        Color? color = null)
    {
        var msg = new CreateZoneRequestMessage(parent, protoId, area, name, color);
        RaiseNetworkEvent(msg);
    }

    public void ChangeZoneRequest(
        NetEntity zone,
        NetEntity? parent = null,
        EntProtoId<ZoneComponent>? protoId = null,
        List<Box2>? area = null,
        string? name = null,
        Color? color = null)
    {
        var msg = new ChangeZoneRequestMessage(zone, parent, protoId, area, name, color);
        RaiseNetworkEvent(msg);
    }

    public void DeleteZoneRequest(NetEntity zone)
    {
        var msg = new DeleteZoneRequestMessage(zone);
        RaiseNetworkEvent(msg);
    }
}
