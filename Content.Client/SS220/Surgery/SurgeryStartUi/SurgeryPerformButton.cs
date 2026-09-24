// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Graph;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Surgery.SurgeryStartUi;

public sealed class SurgeryPerformButton : ContainerButton
{
    [ViewVariables]
    public ProtoId<SurgeryGraphPrototype> GraphId;
    public RichTextLabel RichTextLabel { get; }

    public SurgeryPerformButton(ProtoId<SurgeryGraphPrototype> graphId)
    {
        GraphId = graphId;

        AddStyleClass(StyleClassButton);
        RichTextLabel = new RichTextLabel
        {
            StyleClasses = { StyleClassButton }
        };
        AddChild(RichTextLabel);

        VerticalAlignment = VAlignment.Top;
        HorizontalExpandAll = false;
        VerticalExpandAll = true;
        ToggleMode = true;
    }

    public void SetMessage(FormattedMessage msg)
    {
        RichTextLabel.SetMessage(msg);
    }

    [ViewVariables]
    public string? Text { get => RichTextLabel.Text; set => RichTextLabel.Text = value; }

    [ViewVariables]
    public bool HorizontalExpandAll
    {
        set
        {
            HorizontalExpand = value;
            RichTextLabel.HorizontalExpand = value;
        }
    }

    [ViewVariables]
    public bool VerticalExpandAll
    {
        set
        {
            VerticalExpand = value;
            RichTextLabel.VerticalExpand = value;
        }
    }
}
