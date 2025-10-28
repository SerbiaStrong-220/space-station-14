// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Experience.Ui;

public static class ExperienceUiStyleDefinitions
{
    public static readonly Thickness BaseTabLikeThickness = new(10f, 0f, 0f, 0f);
    public static readonly Thickness DividerThickness = new(10f, 0f, 15f, 0f);

    public static readonly float ToolTipStretchModifier = 1.5f;
    public static readonly float TooltipMaxWidth = 500f;
    public static readonly float TooltipMinWidth = 300f;

    public static Control RichExperienceTooltip(Control hovered)
    {
        var tooltip = new Tooltip()
        {
            Tracking = hovered.TrackingTooltip,
            MaxWidth = Math.Clamp(hovered.Width * ToolTipStretchModifier, TooltipMinWidth, TooltipMaxWidth),
        };

        if (FormattedMessage.TryFromMarkup(hovered.ToolTip ?? "", out var message))
            tooltip.SetMessage(message);
        else
            tooltip.Text = hovered.ToolTip;

        return tooltip;
    }
}
