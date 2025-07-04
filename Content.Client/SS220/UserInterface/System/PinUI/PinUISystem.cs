// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface.System.PinUI;

public sealed class PinUISystem : EntitySystem
{

    public Action<PinStateChangedArgs>? OnPinStateChanged;

    public readonly HashSet<Control> PinnedControls = new();

    public static TextureButton AddPinButtonBeforeTarget(Control linkedControl, Control target)
    {
        var button = new PinButton(linkedControl);
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
        if (pinned && PinnedControls.Add(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, true));
        else if (PinnedControls.Remove(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, false));
    }

    public bool IsPinned(Control control)
    {
        return PinnedControls.Contains(control);
    }
}

public record struct PinStateChangedArgs(Control Control, bool Pinned);
