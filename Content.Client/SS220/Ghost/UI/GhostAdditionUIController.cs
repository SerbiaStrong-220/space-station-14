using Content.Client.Ghost;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Ghost;
using Content.Shared.SS220.Mind;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.ContentPack;

namespace Content.Client.SS220.Ghost.UI;
public sealed partial class GhostAdditionUIController : UIController, IOnSystemChanged<GhostExtensionSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;

    [UISystemDependency] private readonly GhostSystem? _system = default;
    [UISystemDependency] private readonly GhostExtensionSystem _extensionSystem = default!;
    private GhostAdditionGui? Gui => UIManager.GetActiveUIWidgetOrNull<GhostAdditionGui>();
    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;
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

    public void OnSystemLoaded(GhostExtensionSystem system)
    {
        system.GhostBodyListResponse += OnGhostBodyListResponse;
    }

    public void OnSystemUnloaded(GhostExtensionSystem system)
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
    private void OnGhostBodyListResponse(GhostBodyListResponseEvent ev)
    {
        Gui?.BodyMenuWindow.UpdateBodies(ev.Bodies);
        Gui?.BodyMenuWindow.Populate();
    }
    #endregion
}
