// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Ghost;
using Content.Shared.SS220.MindExtension.Events;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.SS220.MindExtension.UI;
public sealed partial class GhostAdditionUIController : UIController, IOnSystemChanged<MindExtensionSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [UISystemDependency] private readonly MindExtensionSystem _extensionSystem = default!;

    private GhostAdditionGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostAdditionGui>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        if (_extensionSystem?.RespawnTime is not null)
        {
            var respawnRemainTime = _extensionSystem.RespawnTime.Value - _gameTiming.CurTime;
            Gui?.SetRespawnRemainTimer(respawnRemainTime);
        }
        else
            Gui?.LockRespawnTimer();
    }
    private void OnScreenLoad()
    {
        LoadGui();
    }

    private void OnScreenUnload()
    {
        UnloadGui();
    }

    public void LoadGui()
    {
        if (Gui == null)
            return;

        Gui.RespawnPressed += RequestRespawn;
        Gui.ReturnToBodyPressed += RequestReturnToBody;
        Gui.BodyMenuWindow.FollowBodyAction += OnFollowBodyAction;
        Gui.BodyMenuWindow.ToBodyAction += OnToBodyAction;
        Gui.BodyMenuWindow.DeleteTrailPointAction += DeleteTrailPointAction;
    }

    public void UnloadGui()
    {
        if (Gui == null)
            return;

        Gui.RespawnPressed -= RequestRespawn;
        Gui.ReturnToBodyPressed -= RequestReturnToBody;
    }

    public void OnSystemLoaded(MindExtensionSystem system)
    {
        system.GhostBodyListResponse += OnGhostBodyListResponse;
        system.DeleteTrailPointResponse += OnDeleteTrailPointResponse;

        system.RequestRespawnTimer();
    }

    public void OnSystemUnloaded(MindExtensionSystem system)
    {
        system.GhostBodyListResponse -= OnGhostBodyListResponse;
        system.DeleteTrailPointResponse -= OnDeleteTrailPointResponse;

        system.RequestRespawnTimer();
    }

    #region UiEvents
    private void RequestReturnToBody()
    {
        RequestBodies();
    }

    private void RequestRespawn()
    {
        _extensionSystem?.RespawnAction();
    }

    private void RequestBodies()
    {
        _extensionSystem.RequestBodies();
        Gui?.BodyMenuWindow.OpenCentered();
    }
    private void OnFollowBodyAction(NetEntity entity)
    {
        var msg = new GhostWarpToTargetRequestEvent(entity);
        _net.SendSystemNetworkMessage(msg);
    }
    private void OnToBodyAction(NetEntity entity)
    {
        _extensionSystem?.MoveToBody(entity);
    }

    private void DeleteTrailPointAction(NetEntity entity)
    {
        _extensionSystem?.DeleteTrailPointRequest(entity);
    }

    #endregion

    #region EsEvents

    private void OnGhostBodyListResponse(GhostBodyListResponse ev)
    {
        Gui?.BodyMenuWindow.UpdateBodies(ev.Bodies);
    }

    private void OnDeleteTrailPointResponse(DeleteTrailPointResponse response)
    {
        Gui?.BodyMenuWindow.DeleteBodyCard(response.Entity);
    }

    #endregion
}
