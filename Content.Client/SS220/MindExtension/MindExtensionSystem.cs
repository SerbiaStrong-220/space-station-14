// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MindExtension.Events;
using Robust.Client.Player;

namespace Content.Client.SS220.MindExtension;

public sealed class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public event Action<GhostBodyListResponse>? GhostBodyListResponse;
    public event Action<DeleteTrailPointResponse>? DeleteTrailPointResponse;
    public event Action<ExtensionReturnResponse>? ExtensionReturnResponse;
    public event Action<RespawnedResponse>? RespawnedResponse;

    public TimeSpan? RespawnTime { get; private set; }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostBodyListResponse>(OnGhostBodyListResponse);
        SubscribeNetworkEvent<RespawnTimeResponse>(OnRespawnTimeResponse);
        SubscribeNetworkEvent<DeleteTrailPointResponse>(OnDeleteTrailPointResponse);
        SubscribeNetworkEvent<ExtensionReturnResponse>(OnExtensionReturnResponse);
        SubscribeNetworkEvent<RespawnedResponse>(OnRespawnedResponse);
    }

    private void OnRespawnedResponse(RespawnedResponse ev)
    {
        RespawnedResponse?.Invoke(ev);
    }

    private void OnExtensionReturnResponse(ExtensionReturnResponse ev)
    {
        ExtensionReturnResponse?.Invoke(ev);
    }

    private void OnRespawnTimeResponse(RespawnTimeResponse ev)
    {
        RespawnTime = ev.Time;
    }

    private void OnGhostBodyListResponse(GhostBodyListResponse ev)
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

        RaiseNetworkEvent(new RespawnRequest(netEntity.Value));
    }

    public void RequestRespawnTimer()
    {
        RaiseNetworkEvent(new RespawnTimeRequest());
    }

    public void RequestBodies()
    {
        RaiseNetworkEvent(new GhostBodyListRequest());
    }

    public void DeleteTrailPointRequest(NetEntity entity)
    {
        RaiseNetworkEvent(new DeleteTrailPointRequest(entity));
    }

    public void MoveToBody(NetEntity id)
    {
        RaiseNetworkEvent(new ExtensionReturnRequest(id));
    }
}
