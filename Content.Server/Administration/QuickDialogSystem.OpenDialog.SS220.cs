using Content.Shared.Administration;
using JetBrains.Annotations;
using Robust.Shared.Player;

namespace Content.Server.Administration;

public sealed partial class QuickDialogSystem
{
    [PublicAPI]
    public void OpenDialog<T1>(ICommonSession session, string title, string description, string prompt, Action<T1> okAction,
        Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            description,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(T1)), prompt)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<T1>(TypeToEntryType(typeof(T1)), ev.Responses["1"], out var v1))
                    okAction.Invoke(v1);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    [PublicAPI]
    public void OpenDialogForTTSPrototypeId(ICommonSession session, string title, string description, string prompt, Action<string> okAction,
        EntityUid target, Action? cancelAction = null)
    {
        OpenDialogInternalForTTSPrototypeId(
            session,
            title,
            description,
            target,
            new List<QuickDialogEntry>
            {
                new("1", TypeToEntryType(typeof(string)), prompt)
            },
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if (TryParseQuickDialog<string>(TypeToEntryType(typeof(string)), ev.Responses["1"], out var v1))
                    okAction.Invoke(v1);
                else
                {
                    session.Channel.Disconnect("Replied with invalid quick dialog data.");
                    cancelAction?.Invoke();
                }
            }),
            cancelAction ?? (() => { })
        );
    }

    [PublicAPI]
    public void OpenDialog(ICommonSession session, string title, string description, string prompt, Action<bool> okAction,
        Action? cancelAction = null)
    {
        OpenDialogInternal(
            session,
            title,
            description,
            new List<QuickDialogEntry>(),
            QuickDialogButtonFlag.OkButton | QuickDialogButtonFlag.CancelButton,
            (ev =>
            {
                if ((ev.ButtonPressed & QuickDialogButtonFlag.OkButton) != 0)
                    okAction.Invoke(true);
                else
                    cancelAction?.Invoke();
            }),
            cancelAction ?? (() => { })
        );
    }
}
