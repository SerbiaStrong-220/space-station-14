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

    private GhostAdditionGui? additionGui => UIManager.GetActiveUIWidgetOrNull<GhostAdditionGui>();

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
            additionGui?.SetRespawnRemainTimer(respawnRemainTime);
        }
        else
            additionGui?.LockRespawnTimer();
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
        if (additionGui == null)
            return;

        additionGui.OnRespawnPressed += RequestRespawn;
        additionGui.OnReturnToBodyPressed += RequestReturnToBody;
        additionGui.BodyMenuWindow.OnFollowBodyAction += OnFollowBodyAction;
        additionGui.BodyMenuWindow.OnToBodyAction += OnToBodyAction;
        additionGui.BodyMenuWindow.OnDeleteTrailPointAction += DeleteTrailPointAction;
    }

    public void UnloadGui()
    {
        if (additionGui == null)
            return;

        additionGui.OnRespawnPressed -= RequestRespawn;
        additionGui.OnReturnToBodyPressed -= RequestReturnToBody;
    }

    public void OnSystemLoaded(MindExtensionSystem system)
    {
        system.GhostBodyListResponse += OnGhostBodyListResponse;
        system.DeleteTrailPointResponse += OnDeleteTrailPointResponse;
        system.ExtensionReturnResponse += OnExtensionReturnResponse;
        system.RespawnedResponse += OnRespawnedResponse;

        system.RequestRespawnTimer();
    }

    public void OnSystemUnloaded(MindExtensionSystem system)
    {
        system.GhostBodyListResponse -= OnGhostBodyListResponse;
        system.DeleteTrailPointResponse -= OnDeleteTrailPointResponse;
        system.ExtensionReturnResponse -= OnExtensionReturnResponse;
        system.RespawnedResponse -= OnRespawnedResponse;

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
        additionGui?.BodyMenuWindow.OpenCentered();
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
        additionGui?.BodyMenuWindow.UpdateBodies(ev.TrailPoints);
    }

    private void OnDeleteTrailPointResponse(DeleteTrailPointResponse response)
    {
        additionGui?.BodyMenuWindow.DeleteBodyCard(response.Entity);
    }

    private void OnExtensionReturnResponse(ExtensionReturnResponse response)
    {
        additionGui?.BodyMenuWindow.Close();
    }
    private void OnRespawnedResponse(RespawnedResponse response)
    {
        additionGui?.BodyMenuWindow.Close();
    }

    #endregion
}
