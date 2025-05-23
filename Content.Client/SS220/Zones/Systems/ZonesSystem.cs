
using Content.Client.SS220.Zones.UI;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    public ZonesControlWindow ControlWindow = default!;

    public override void Initialize()
    {
        base.Initialize();

        ControlWindow = new ZonesControlWindow();

        SubscribeLocalEvent<ZoneComponent, AfterAutoHandleStateEvent>(OnAfterZoneStateHandled);
        SubscribeLocalEvent<ZonesContainerComponent, AfterAutoHandleStateEvent>(OnAfterZoneContainerStateHandled);
    }

    private void OnAfterZoneStateHandled(Entity<ZoneComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }


    private void OnAfterZoneContainerStateHandled(Entity<ZonesContainerComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        ControlWindow.RefreshEntries();
    }
}
