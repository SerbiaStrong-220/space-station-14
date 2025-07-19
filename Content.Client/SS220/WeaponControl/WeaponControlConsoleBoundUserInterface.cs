using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Content.Client.Shuttles.UI;
using Content.Shared.Shuttles.BUIStates;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using RadarConsoleWindow = Content.Client.Shuttles.UI.RadarConsoleWindow;
using Content.Shared.SS220.WeaponControl;

namespace Content.Client.SS220.WeaponControl;

[UsedImplicitly]
public sealed class WeaponControlConsoleBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private WeaponControlConsoleWindow? _window;

    public WeaponControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<WeaponControlConsoleWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is not NavBoundUserInterfaceState cState)
            return;

        _window?.UpdateState(cState.State);
    }
}
