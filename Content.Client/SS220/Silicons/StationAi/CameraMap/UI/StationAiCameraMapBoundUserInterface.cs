// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Silicons.StationAi.CameraMap.UI;

public sealed class StationAiCameraMapBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private StationAiCameraMapWindow? _window;

    public StationAiCameraMapBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        var system = EntMan.System<StationAiCameraMapSystem>();
        _window = this.CreateWindow<StationAiCameraMapWindow>();
        _window.SetFavorites(system.Favorites);
        _window.OnJump += target => SendMessage(new StationAiCameraMapJumpMessage(target));
        _window.OnFavoriteToggled += camera =>
        {
            system.ToggleFavorite(camera);
            _window.SetFavorites(system.Favorites);
        };

        UpdateCooldown();
    }

    public void UpdateCooldown()
    {
        if (_window != null && EntMan.TryGetComponent<StationAiCameraMapComponent>(Owner, out var comp))
            _window.SetCooldown(comp.NextJump, comp.Cooldown);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is StationAiCameraMapState cast && EntMan.TryGetEntity(cast.Grid, out var grid))
            _window?.UpdateState(grid.Value, cast.Targets);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);

        if (message is StationAiCameraMapFailureMessage failure)
            _window?.ShowFailure(failure.Failure);
    }
}
