// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.UserInterface.System.PinUI;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.SS220.UserInterface.Controls;

[Virtual]
public sealed partial class PinnableDefaultWindow : DefaultWindow, IPinnableWindow
{
    public PinnableDefaultWindow()
    {
        PinUISystem.AddPinButtonBeforeTarget(this, CloseButton);
    }
}

/// <summary>
/// Interface that marks this window can be pinned by hotkey
/// </summary>
public interface IPinnableWindow;
