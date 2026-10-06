using Robust.Shared.Serialization;

namespace Content.Shared.Administration;

[Serializable, NetSerializable]
public sealed class QuickDialogDescOpenEvent : EntityEventArgs
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Title;

    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Description;

    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public int DialogId;

    /// <summary>
    /// The prompts to show the user.
    /// </summary>
    public List<QuickDialogEntry> Prompts;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public QuickDialogButtonFlag Buttons = QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton;

    public QuickDialogDescOpenEvent(string title, string description, List<QuickDialogEntry> prompts, int dialogId, QuickDialogButtonFlag buttons)
    {
        Title = title;
        Description = description;
        Prompts = prompts;
        Buttons = buttons;
        DialogId = dialogId;
    }
}

[Serializable, NetSerializable]
public sealed class QuickDialogTTSProtoIdOpenEvent : EntityEventArgs
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Title;

    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Description;

    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public int DialogId;

    public NetEntity Target { init; get; }

    /// <summary>
    /// The prompts to show the user.
    /// </summary>
    public List<QuickDialogEntry> Prompts;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public QuickDialogButtonFlag Buttons = QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton;

    public QuickDialogTTSProtoIdOpenEvent(string title, string description, NetEntity target, List<QuickDialogEntry> prompts, int dialogId, QuickDialogButtonFlag buttons)
    {
        Title = title;
        Description = description;
        Prompts = prompts;
        Buttons = buttons;
        DialogId = dialogId;
        Target = target;
    }
}
