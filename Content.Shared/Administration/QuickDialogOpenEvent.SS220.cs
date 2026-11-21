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
public sealed class QuickDialogTtsVoicePreferencesOpenEvent(string title, string description, NetEntity target, List<QuickDialogEntry> prompts, int dialogId, QuickDialogButtonFlag buttons) : EntityEventArgs
{
    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Title = title;

    /// <summary>
    /// The title of the dialog.
    /// </summary>
    public string Description = description;

    /// <summary>
    /// The internal dialog ID.
    /// </summary>
    public int DialogId = dialogId;

    public NetEntity Target { init; get; } = target;

    /// <summary>
    /// The prompts to show the user.
    /// </summary>
    public List<QuickDialogEntry> Prompts = prompts;

    /// <summary>
    /// The buttons presented for the user.
    /// </summary>
    public QuickDialogButtonFlag Buttons = buttons;
}
