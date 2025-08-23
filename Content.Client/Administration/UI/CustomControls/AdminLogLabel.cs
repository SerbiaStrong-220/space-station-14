using Content.Shared.Administration.Logs;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client.Administration.UI.CustomControls;

public sealed class AdminLogLabel : RichTextLabel
{
    private readonly Color _selectedTextColor = Color.FromHex("#b3f1abff");
    // null used for base RichTextLabel color
    private readonly Color? _textColor = null;

    public AdminLogLabel(ref SharedAdminLog log, HSeparator separator)
    {
        Log = log;
        Separator = separator;

        //SS220-make-copy-for-logs-begin
        MouseFilter = MouseFilterMode.Stop;
        OnKeyBindDown += (args) =>
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            MarkedForCopying = !_markedForCopying;
        };
        //SS220-make-copy-for-logs-end

        SetMessage($"{log.Date:HH:mm:ss}: {log.Message}", defaultColor: _textColor); //SS220-make-copy-for-logs
        OnVisibilityChanged += VisibilityChanged;
    }

    public SharedAdminLog Log { get; }

    public HSeparator Separator { get; }

    // SS220-make-copy-for-logs-begin
    private bool _markedForCopying = false;
    public bool MarkedForCopying
    {
        // two purposes
        // 1. save marked log in case of mistake
        // 2. make copying intuitive
        get { return Visible && _markedForCopying; }
        set
        {
            if (value)
                SetMessage($"{Log.Date:HH:mm:ss}: {Log.Message}", defaultColor: _selectedTextColor);
            else
                SetMessage($"{Log.Date:HH:mm:ss}: {Log.Message}", defaultColor: _textColor);

            _markedForCopying = value;
        }
    }
    // SS220-make-copy-for-logs-end

    private void VisibilityChanged(Control control)
    {
        Separator.Visible = Visible;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        OnVisibilityChanged -= VisibilityChanged;
    }
}
