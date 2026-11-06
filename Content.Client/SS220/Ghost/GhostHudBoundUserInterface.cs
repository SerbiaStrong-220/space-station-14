// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Ghost;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Ghost;

[UsedImplicitly]
public sealed class GhostHudBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private GhostHudWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<GhostHudWindow>();
        _window.OnHudToggled += (hud, enabled) => SendMessage(new GhostHudToggledMessage(hud, enabled));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_window == null)
            return;

        if (state is not GhostHudBoundUserInterfaceState hudState)
            return;

        _window.SetState(hudState);
    }
}
