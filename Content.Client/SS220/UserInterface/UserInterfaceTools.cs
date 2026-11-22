using Robust.Client.Input;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.UserInterface;

public static class UserInterfaceTools
{
    public static Control? GetControlUnderMouse(IUserInterfaceManager ui, IInputManager input)
    {
        return ui.MouseGetControl(input.MouseScreenPosition);
    }

    public static bool IsChildOf(this Control control, Control other, bool recurcive = false)
    {
        if (control.Parent is not { } parent)
            return false;

        if (parent == other)
            return true;

        if (!recurcive)
            return false;

        return parent.IsChildOf(other, recurcive);
    }
}
