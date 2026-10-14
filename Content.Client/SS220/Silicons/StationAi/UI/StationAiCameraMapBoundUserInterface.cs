// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Shared.SS220.Silicons.StationAi;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Silicons.StationAi.UI;

public sealed class StationAiCameraMapBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private StationAiCameraMapWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<StationAiCameraMapWindow>();
        if (!EntMan.TryGetComponent<StationAiCameraMapPreferencesComponent>(Owner, out var preferences))
            preferences = EntMan.AddComponent<StationAiCameraMapPreferencesComponent>(Owner);
        RestoreBounds(_window, preferences);
        _window.SetFavorites(preferences.Favorites);
        _window.OnTargetSelected += target => SendMessage(new StationAiCameraMapTeleportMessage(target));
        _window.OnCameraSwitched += () => EntMan.System<StationAiCameraSwitchEffectSystem>().Play();
        // The effect plays from OnCameraSwitched once the server confirms the jump and starts the cooldown.
        _window.OnJumpToCore += () => SendMessage(new StationAiCameraMapJumpToCoreMessage());
        _window.OnCrewSelected += sensor => SendMessage(new StationAiCameraMapCrewMessage(sensor));
        _window.OnClose += () =>
        {
            preferences.WindowSize = _window.Size;
            preferences.WindowPosition = _window.Position;
        };
    }

    public static void RestoreBounds(StationAiCameraMapWindow window, StationAiCameraMapPreferencesComponent preferences)
    {
        if (preferences.WindowSize is not { } size || preferences.WindowPosition is not { } position)
            return;

        window.SetSize = Vector2.Max(size, window.MinSize);
        // The screen may have shrunk since; keep at least the title bar reachable.
        if (window.Parent is { } parent)
            position = Vector2.Clamp(position, Vector2.Zero, Vector2.Max(Vector2.Zero, parent.Size - new Vector2(120, 40)));
        LayoutContainer.SetPosition(window, position);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is StationAiCameraMapFailureMessage failure)
            _window?.ShowFailure(failure.Failure);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is StationAiCameraMapState map && EntMan.TryGetEntity(map.Grid, out var grid))
            _window?.UpdateMap(grid.Value, map);
    }
}
