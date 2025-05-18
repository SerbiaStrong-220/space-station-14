// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;

namespace Content.Client.SS220.Zones.Systems;

public sealed partial class ShowZonesSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private ZonesOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ZonesOverlay();

        SubscribeLocalEvent<ShowZonesComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ShowZonesComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnInit(Entity<ShowZonesComponent> entity, ref ComponentInit args)
    {
        if (entity != _player.LocalEntity)
            return;

        _overlayManager.AddOverlay(_overlay);
    }

    private void OnShutdown(Entity<ShowZonesComponent> entity, ref ComponentShutdown args)
    {
        if (entity != _player.LocalEntity)
            return;

        _overlayManager.RemoveOverlay(_overlay);
    }
}
