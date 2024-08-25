using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface;

/// <summary>
/// Wrapper for <see cref="LineEdit"/> with extended features.
/// </summary>
public sealed class ExtendedLineEdit : ExtendedStringEditBase
{
    public LineEdit Edit => _edit;
    public string Text
    {
        get => Edit.Text;
        set
        {
            Edit.Text = value;
            Refresh();
        }
    }

    protected override int CurrentLength => Edit.Text.Length;

    private readonly LineEdit _edit;

    public ExtendedLineEdit()
    {
        _edit = new LineEdit();
        _edit.OnTextChanged += OnEditTextChanged;
        AddChild(Edit);
        LimitLabel.Margin = new Thickness(0f, 0f, 4f, 0f);

        ConstructBase();
    }

    private void OnEditTextChanged(LineEdit.LineEditEventArgs args)
    {
        Refresh();
    }
}
