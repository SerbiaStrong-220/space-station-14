// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.UserInterface;

public sealed partial class CopyableRichTextLabel : ContainerButton
{
    [ViewVariables]
    public string? Text
    {
        get => Label.Text;
        set
        {
            if (value != null)
                Label.SetMessage(value);
        }
    }
    public RichTextLabel Label;

    public CopyableRichTextLabel()
    {
        var clipboard = IoCManager.Resolve<IClipboardManager>();
        Label = new RichTextLabel();

        if (Text != null)
            Label.SetMessage(Text);

        OnPressed += (args) =>
        {
            if (Label.Text != null)
                clipboard.SetText(Label.Text);
        };
        AddChild(Label);
    }

    public void SetMessage(FormattedMessage message, Type[]? tagsAllowed = null, Color? defaultColor = null)
    {
        Label.SetMessage(message, tagsAllowed, defaultColor);
    }

    public void SetMessage(string message, Type[]? tagsAllowed = null, Color? defaultColor = null)
    {
        Label.SetMessage(message, tagsAllowed, defaultColor);
    }
}
