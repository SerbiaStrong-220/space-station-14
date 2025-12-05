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
        if (_extensionSystem?.RespawnTime is null)
            return;

        var temp = (TimeSpan)_extensionSystem.RespawnTime - _gameTiming.CurTime;
        Gui?.SetRespawnTimer(temp, _gameTiming.CurTime);
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
    }

    public void OnSystemUnloaded(MindExtensionSystem system)
    {
        system.GhostBodyListResponse -= OnGhostBodyListResponse;
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
        Gui?.BodyMenuWindow.Populate();
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

    #endregion

    #region EsEvents
    private void OnGhostBodyListResponse(GhostBodyListResponse ev)
    {
        Gui?.BodyMenuWindow.UpdateBodies(ev.Bodies);
        Gui?.BodyMenuWindow.Populate();
    }
    #endregion
}
