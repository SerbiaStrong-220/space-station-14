using Content.Client.Players;
using Content.Shared.Ghost;
using Content.Shared.Mind;
using Content.Shared.SS220.Mind;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client.SS220.Ghost;

//Написать нормальную клиентскую логику.
public sealed class GhostExtensionSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _temp = default!;
    [Dependency] private readonly SharedMindExtensionSystem _sharedMindExtensionSystem = default!;


    public event Action<GhostBodyListResponseEvent>? GhostBodyListResponse;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<GhostBodyListResponseEvent>(OnGhostBodyListResponseEvent);
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
