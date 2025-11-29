using Content.Shared.SS220.Mind;
using Robust.Client.Player;

namespace Content.Client.SS220.MindExtension;

public sealed class MindExtensionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _temp = default!;

    public event Action<GhostBodyListResponseEvent>? GhostBodyListResponse;
    public TimeSpan? RespawnTime { get; private set; }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostBodyListResponseEvent>(OnGhostBodyListResponseEvent);
        SubscribeNetworkEvent<UpdateRespawnTime>(OnUpdateRespawnTime);
    }

    private void OnUpdateRespawnTime(UpdateRespawnTime ev)
    {
        RespawnTime = ev.Time;
    }

    private void OnGhostBodyListResponseEvent(GhostBodyListResponseEvent ev)
    {
        GhostBodyListResponse?.Invoke(ev);
    }

    public void RespawnAction()
    {
        if (_temp.LocalEntity is null)
            return;

        var entity = (EntityUid)_temp.LocalEntity;

        if (!TryGetNetEntity(entity, out var netEntity))
            return;

        RaiseNetworkEvent(new ExtensionRespawnActionEvent((NetEntity)netEntity));
    }

    public void RequestBodies()
    {
        RaiseNetworkEvent(new GhostBodyListRequestEvent());
    }
    public void MoveToBody(NetEntity id)
    {
        RaiseNetworkEvent(new ExtensionReturnActionEvent(id));
    }
}
