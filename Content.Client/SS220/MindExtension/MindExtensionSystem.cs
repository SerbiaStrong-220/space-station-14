using Content.Shared.SS220.MindExtension.Events;
using Robust.Client.Player;

namespace Content.Client.SS220.MindExtension;

public sealed class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<GhostBodyListResponse>? GhostBodyListResponse;
    public event Action<DeleteTrailPointResponse>? DeleteTrailPointResponse;

    public TimeSpan? RespawnTime { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostBodyListResponse>(OnGhostBodyListResponseEvent);
        SubscribeNetworkEvent<RespawnTimeResponse>(OnRespawnTimeResponse);
        SubscribeNetworkEvent<DeleteTrailPointResponse>(OnDeleteTrailPointResponse);
    }

    private void OnRespawnTimeResponse(RespawnTimeResponse ev)
    {
        RespawnTime = ev.Time;
    }

    private void OnGhostBodyListResponseEvent(GhostBodyListResponse ev)
    {
        GhostBodyListResponse?.Invoke(ev);
    }

    private void OnDeleteTrailPointResponse(DeleteTrailPointResponse ev)
    {
        DeleteTrailPointResponse?.Invoke(ev);
    }

    public void RespawnAction()
    {
        if (_playerManager.LocalEntity is null)
            return;

        if (!TryGetNetEntity(_playerManager.LocalEntity.Value, out var netEntity))
            return;

        RaiseNetworkEvent(new ExtensionRespawnActionEvent(netEntity.Value));
    }

    public void RequestRespawnTimer()
    {
        RaiseNetworkEvent(new RespawnTimeRequest());
    }

    public void RequestBodies()
    {
        RaiseNetworkEvent(new GhostBodyListRequest());
    }

    public void MoveToBody(NetEntity id)
    {
        RaiseNetworkEvent(new ExtensionReturnActionEvent(id));
    }
}
