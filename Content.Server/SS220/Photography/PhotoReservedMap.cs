// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking;
using Content.Server.Players.RateLimiting;
using Content.Shared.Players.RateLimiting;
using Content.Shared.GameTicking;
using Content.Shared.SS220.Photography;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.SS220.Photography;

public sealed partial class PhotoReservedMap : EntitySystem
{
    [Dependency] private readonly IMapManager _mapMan = default!;

    [Dependency] private PlayerRateLimitManager _rateLimit = default!;

    public MapId? ReservedMap { get; private set; }
    private ISawmill _sawmill = Logger.GetSawmill("photo-manager");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeNetworkEvent<PhotoRequestMapMessage>(OnMapRequest);
    }

    public MapId EnsureMap()
    {
        if (!ReservedMap.HasValue || !_mapMan.MapExists(ReservedMap.Value))
        {
            ReservedMap = _mapMan.CreateMap();
            _mapMan.SetMapPaused(ReservedMap.Value, true);
            // New maps are uninitialized by default; do not run gameplay MapInit on photo replicas.
            _sawmill.Debug($"Reserved map created! MapId: {ReservedMap}");
        }

        return ReservedMap.Value;
    }

    private void OnMapRequest(PhotoRequestMapMessage args, EntitySessionEventArgs eventArgs)
    {
        if (_rateLimit.CountAction(eventArgs.SenderSession, PhotoManager.RequestRateLimitKey) != RateLimitStatus.Allowed)
            return;

        var ev = new PhotoReservedMapMessage(ReservedMap);
        RaiseNetworkEvent(ev, eventArgs.SenderSession);
    }

    private void OnPostGameMapLoad(PostGameMapLoad args)
    {
        _sawmill.Debug("Ensuring reserved map...");
        var map = EnsureMap();
        var ev = new PhotoReservedMapMessage(map);
        RaiseNetworkEvent(ev, Filter.Broadcast());
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        if (ReservedMap.HasValue && _mapMan.MapExists(ReservedMap.Value))
        {
            _mapMan.DeleteMap(ReservedMap.Value);
            _sawmill.Debug("Reserved map has been removed");
        }

        ReservedMap = null;
    }
}
