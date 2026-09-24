using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

public sealed partial class SimpleRadialMenu : RadialMenu
{
    private Tooltip CreateRichTooltip(Control hovered)
    {
        var tooltip = new Tooltip()
        {
            Tracking = hovered.TrackingTooltip,
        };

        if (FormattedMessage.TryFromMarkup(hovered.ToolTip ?? "", out var message))
        {
            tooltip.SetMessage(message);
        }
        else
        {
            tooltip.Text = hovered.ToolTip;
        }

        return tooltip;
    }
}

