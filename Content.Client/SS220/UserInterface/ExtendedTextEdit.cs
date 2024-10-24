using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.UserInterface;

/// <summary>
/// Wrapper for <see cref="TextEdit"/> with extended features.
/// </summary>
public sealed class ExtendedTextEdit : ExtendedStringEditBase
{
    public TextEdit Edit => _edit;
    public Rope.Node TextRope
    {
        get => Edit.TextRope;
        set
        {
            Edit.TextRope = value;
            Refresh();
        }
    }

    protected override int CurrentLength => Edit.TextLength;

    private readonly TextEdit _edit;

    public ExtendedTextEdit() : base()
    {
        _edit = new TextEdit()
        {
            Margin = new Thickness(0f, 0f, 0f, 16f),
        };
        _edit.GetChild(0).Margin = new Thickness(0f, 0f, 20f, 0f); // I dont like how text goes over scrollbar, right margin should help
        _edit.OnTextChanged += OnEditTextChanged;
        AddChild(Edit);
        LimitLabel.Margin = new Thickness(0f, 0f, 4f, 0f);

        ConstructBase();
    }

    private void OnEditTextChanged(TextEdit.TextEditEventArgs args)
    {
        Refresh();
    }
}
