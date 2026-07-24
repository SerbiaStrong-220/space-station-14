using System.Diagnostics.CodeAnalysis;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Administration.UI.CustomControls;

public static class AdminListControlHelper
{
    public static readonly Color ListBackgroundColor = new(32, 32, 40);

    public static bool TryGetButtonLabel(BaseButton button, [NotNullWhen(true)] out Label? label)
    {
        label = null;

        foreach (var child in button.Children)
        {
            if (child is not BoxContainer boxContainer)
                continue;

            foreach (var boxChild in boxContainer.Children)
            {
                if (boxChild is not Label boxLabel)
                    continue;

                label = boxLabel;
                return true;
            }
        }

        return false;
    }

    public static void UpdateButtonLabel(BaseButton button, string text)
    {
        if (TryGetButtonLabel(button, out var label))
            label.Text = text;
    }
}
