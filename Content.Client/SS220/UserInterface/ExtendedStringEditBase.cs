using Content.Shared.SS220.Utility;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface;

public abstract class ExtendedStringEditBase : Control
{
    public int MaxLength
    {
        get => _maxLength;
        set
        {
            _maxLength = value;
            RefreshLimitLabel();
        }
    }

    protected abstract int CurrentLength { get; }

    protected readonly Label LimitLabel;
    private int _maxLength = 0;
    private StringBuffer _limitTextBuffer;

    protected ExtendedStringEditBase()
    {
        LimitLabel = new Label()
        {
            Name = "LimitLabel",
            Align = Label.AlignMode.Right,
            VAlign = Label.VAlignMode.Bottom,
            VerticalAlignment = VAlignment.Stretch,
            HorizontalAlignment = HAlignment.Stretch,
            FontColorOverride = new Color(1f, 1f, 1f, 0.4f), // This should come from stylesheet, but i dont care for now
        };
        LimitLabel.AddStyleClass("WindowFooterText");
    }

    public void Refresh()
    {
        RefreshLimitLabel();
    }

    protected void ConstructBase()
    {
        AddChild(LimitLabel);
        RefreshLimitLabel();
    }

    private void RefreshLimitLabel()
    {
        if (MaxLength <= 0)
        {
            LimitLabel.Visible = false;
            return;
        }
        LimitLabel.Visible = true;

        // Just string hacks here, nothing interesting
        var builder = _limitTextBuffer.BeginFormat();
        builder.Append(CurrentLength);
        builder.Append('/');
        builder.Append(MaxLength);
        _limitTextBuffer.EndFormat();
        LimitLabel.TextMemory = _limitTextBuffer;
    }
}
