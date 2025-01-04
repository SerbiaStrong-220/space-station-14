// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.DialogWindowDescUI;
using Content.Shared.Administration;

namespace Content.Client.SS220.QuickDialog;

public sealed class QuickDialogSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeNetworkEvent<QuickDialogDescOpenEvent>(OpenDialog);
    }

    private void OpenDialog(QuickDialogDescOpenEvent ev)
    {
        var ok = (ev.Buttons & QuickDialogButtonFlag.OkButton) != 0;
        var window = new DialogWindowDesc(ev.Title, ev.Description, ev.Prompts, ok: ok);

        window.OnConfirmed += responses =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                responses,
                QuickDialogButtonFlag.OkButton));
        };

        window.OnCancelled += () =>
        {
            RaiseNetworkEvent(new QuickDialogResponseEvent(ev.DialogId,
                new(),
                QuickDialogButtonFlag.CancelButton));
        };
    }
}

