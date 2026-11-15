// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Gameplay;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client.SS220.Pathology.Ui;

public sealed partial class PathologyHudDollController :
    UIController,
    IOnStateEntered<GameplayState>,
    IOnSystemChanged<PathologySystem>
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IEntityManager _entityManager = default!;

    private AlertsUI? UI => UIManager.GetActiveUIWidgetOrNull<AlertsUI>();

    public override void Initialize()
    {
        base.Initialize();

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += Refresh;
    }

    public void OnStateEntered(GameplayState state)
    {
        Refresh();
    }

    public void OnSystemLoaded(PathologySystem system)
    {
        system.OnTargetUpdated += HandleTargetUpdated;
        system.OnLocalPlayerChanged += HandleLocalPlayerChanged;
        Refresh();
    }

    public void OnSystemUnloaded(PathologySystem system)
    {
        system.OnTargetUpdated -= HandleTargetUpdated;
        system.OnLocalPlayerChanged -= HandleLocalPlayerChanged;
    }

    private void HandleTargetUpdated(EntityUid uid)
    {
        if (uid == _player.LocalEntity)
            Refresh(_player.LocalEntity);
    }

    private void HandleLocalPlayerChanged(EntityUid? entity)
    {
        Refresh(entity);
    }

    private void Refresh()
    {
        Refresh(_player.LocalEntity);
    }

    private void Refresh(EntityUid? target)
    {
        UI?.SetPathologyTarget(target, _entityManager);
    }
}
