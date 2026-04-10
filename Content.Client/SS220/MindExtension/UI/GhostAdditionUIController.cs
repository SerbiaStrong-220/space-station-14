// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared.Ghost;
using Content.Shared.SS220.MindExtension.Events;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Timing;

namespace Content.Client.SS220.MindExtension.UI;

[UsedImplicitly]
public sealed partial class GhostAdditionUIController : UIController, IOnSystemChanged<MindExtensionSystem>
{
    [Dependency] private readonly IEntityNetworkManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    [UISystemDependency] private readonly MindExtensionSystem _extensionSystem = default!;

    private GhostAdditionGui? AdditionGui => UIManager.GetActiveUIWidgetOrNull<GhostAdditionGui>();

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
            AdditionGui?.SetRespawnRemainTimer(respawnRemainTime);
        }
        else
            AdditionGui?.LockRespawnTimer();
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
        if (AdditionGui == null)
            return;

        AdditionGui.OnRespawnPressed += RequestRespawn;
        AdditionGui.OnReturnToBodyPressed += RequestReturnToBody;
        AdditionGui.BodyMenuWindow.OnFollowBodyAction += OnFollowBodyAction;
        AdditionGui.BodyMenuWindow.OnToBodyAction += OnToBodyAction;
        AdditionGui.BodyMenuWindow.OnDeleteTrailPointAction += DeleteTrailPointAction;
    }

    public void UnloadGui()
    {
        if (AdditionGui == null)
            return;

        AdditionGui.OnRespawnPressed -= RequestRespawn;
        AdditionGui.OnReturnToBodyPressed -= RequestReturnToBody;
        AdditionGui.BodyMenuWindow.OnFollowBodyAction -= OnFollowBodyAction;
        AdditionGui.BodyMenuWindow.OnToBodyAction -= OnToBodyAction;
        AdditionGui.BodyMenuWindow.OnDeleteTrailPointAction -= DeleteTrailPointAction;
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
        AdditionGui?.BodyMenuWindow.OpenCentered();
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
        AdditionGui?.BodyMenuWindow.UpdateBodies(ev.TrailPoints);
    }

    private void OnDeleteTrailPointResponse(DeleteTrailPointResponse response)
    {
        AdditionGui?.BodyMenuWindow.DeleteBodyCard(response.Entity);
    }

    private void OnExtensionReturnResponse(ExtensionReturnResponse response)
    {
        AdditionGui?.BodyMenuWindow.Close();
    }

    private void OnRespawnedResponse(RespawnedResponse response)
    {
        AdditionGui?.BodyMenuWindow.Close();
    }

    #endregion
}
