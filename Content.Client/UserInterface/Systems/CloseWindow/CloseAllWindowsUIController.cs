using Content.Client.Gameplay;
using Content.Client.Info;
using Content.Client.SS220.UserInterface;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client.UserInterface.Systems.Info;

public sealed class CloseAllWindowsUIController : UIController
{
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public override void Initialize()
    {
        _inputManager.SetInputCommand(EngineKeyFunctions.WindowCloseAll,
            InputCmdHandler.FromDelegate(session => CloseAllWindows()));
    }

    private void CloseAllWindows()
    {
        foreach (var childControl in new List<Control>(_uiManager.WindowRoot.Children)) // Copy children list as it will be modified on Close()
        {
            //ss220 add pin for ui start
            if (PinUISystem.GetPinned(childControl))
            {
                continue;
            }
            //ss220 add pin for ui end

            if (childControl is BaseWindow)
            {
                ((BaseWindow) childControl).Close();
            }
        }
    }
}

