// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Info;
using Content.Shared.SS220.Input;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;

namespace Content.Client.SS220.UserInterface.System.PinUI;

public sealed class PinUISystem : EntitySystem
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

    public Action<PinStateChangedArgs>? OnPinStateChanged;

    private readonly HashSet<Control> _pinnedControls = new();

    private static readonly Thickness BaseMargin = new(0, 0, 5, 0);

    public override void Initialize()
    {
        base.Initialize();

        _input.SetInputCommand(KeyFunctions220.PinUI,
            InputCmdHandler.FromDelegate(_ => HandlePinUI()));
    }

    private void HandlePinUI()
    {
        var controller = _uiManager.GetUIController<CloseRecentWindowUIController>();

        var window = controller.GetMostRecentlyInteractedWindow();
        if (window is not IPinnableWindow)
            return;

        SetPinned(window);
    }

    public static TextureButton AddPinButtonBeforeTarget(Control linkedControl,
        Control target,
        Thickness? margin = null)
    {
        var button = new PinButton(linkedControl);

        margin ??= BaseMargin;
        button.Margin = margin.Value;

        var parent = target.Parent;
        if (parent == null)
            return button;

        var index = target.GetPositionInParent();
        parent.AddChild(button);
        button.SetPositionInParent(index);

        return button;
    }

    public void SetPinned(Control control, bool pinned)
    {
        if (pinned && _pinnedControls.Add(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, true));
        else if (_pinnedControls.Remove(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, false));
    }

    public void SetPinned(Control? control)
    {
        if (control == null)
            return;

        if (_pinnedControls.Add(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, true));
        else if (_pinnedControls.Remove(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, false));
    }

    public bool IsPinned(Control control)
    {
        return _pinnedControls.Contains(control);
    }
}

public record struct PinStateChangedArgs(Control Control, bool Pinned);
